import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const allowedEvents = new Set([
  "session_started",
  "session_ended",
  "scene_loaded",
  "tool_equipped",
  "tool_used",
  "quest_started",
  "objective_completed",
  "quest_completed",
  "progress_dirty",
  "manual_flush",
]);

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const maxEvents = 100;
const maxEventPropsBytes = 8192;
const maxSnapshotBytes = 65536;
const maxBodyBytes = 256 * 1024;

type JsonRecord = Record<string, unknown>;

type TelemetryEvent = {
  id: string;
  name: string;
  occurredAt: string;
  sceneName?: string;
  props?: JsonRecord;
};

type ProgressSnapshot = {
  currentScene?: string;
  completedQuests?: string[];
  completedObjectives?: string[];
  storyFlags?: string[];
  unlockedToolIds?: string[];
  inventoryCount?: number;
  warehouseCount?: number;
  encyclopediaDiscovered?: number;
  encyclopediaTotal?: number;
  updatedAt?: string;
  payload?: JsonRecord;
};

type IngestRequest = {
  installId: string;
  sessionId: string;
  gameVersion?: string;
  platform?: string;
  buildTarget?: string;
  language?: string;
  currentScene?: string;
  events?: TelemetryEvent[];
  progressSnapshot?: ProgressSnapshot;
  sessionEnd?: {
    endedAt?: string;
  };
};

function jsonResponse(status: number, body: JsonRecord): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json",
    },
  });
}

function fail(status: number, message: string): Response {
  return jsonResponse(status, { ok: false, error: message });
}

function isPlainObject(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function byteLength(value: unknown): number {
  return new TextEncoder().encode(JSON.stringify(value ?? {})).length;
}

function parseDate(value: string | undefined, fallback: string): string {
  if (!value) return fallback;
  const timestamp = Date.parse(value);
  if (Number.isNaN(timestamp)) return fallback;
  return new Date(timestamp).toISOString();
}

function sanitizeStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return [...new Set(
    value
      .filter((item): item is string => typeof item === "string")
      .map((item) => item.trim())
      .filter((item) => item.length > 0 && item.length <= 128),
  )].slice(0, 500);
}

function validateRequest(body: unknown): { ok: true; value: IngestRequest } | { ok: false; response: Response } {
  if (!isPlainObject(body)) {
    return { ok: false, response: fail(400, "Body must be a JSON object") };
  }

  const request = body as IngestRequest;
  if (!uuidPattern.test(request.installId ?? "")) {
    return { ok: false, response: fail(400, "Invalid installId") };
  }

  if (!uuidPattern.test(request.sessionId ?? "")) {
    return { ok: false, response: fail(400, "Invalid sessionId") };
  }

  if (request.events !== undefined && !Array.isArray(request.events)) {
    return { ok: false, response: fail(400, "events must be an array") };
  }

  if ((request.events?.length ?? 0) > maxEvents) {
    return { ok: false, response: fail(413, "Too many events") };
  }

  for (const event of request.events ?? []) {
    if (!uuidPattern.test(event.id ?? "")) {
      return { ok: false, response: fail(400, "Invalid event id") };
    }

    if (!allowedEvents.has(event.name)) {
      return { ok: false, response: fail(400, `Unknown event name: ${event.name}`) };
    }

    if (Number.isNaN(Date.parse(event.occurredAt ?? ""))) {
      return { ok: false, response: fail(400, "Invalid event occurredAt") };
    }

    if (event.props !== undefined && !isPlainObject(event.props)) {
      return { ok: false, response: fail(400, "Event props must be an object") };
    }

    if (byteLength(event.props ?? {}) > maxEventPropsBytes) {
      return { ok: false, response: fail(413, "Event props too large") };
    }
  }

  if (request.progressSnapshot !== undefined && !isPlainObject(request.progressSnapshot)) {
    return { ok: false, response: fail(400, "progressSnapshot must be an object") };
  }

  if (request.progressSnapshot && byteLength(request.progressSnapshot) > maxSnapshotBytes) {
    return { ok: false, response: fail(413, "progressSnapshot too large") };
  }

  return { ok: true, value: request };
}

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return fail(405, "Method not allowed");
  }

  const contentLength = Number(req.headers.get("content-length") ?? "0");
  if (contentLength > maxBodyBytes) {
    return fail(413, "Request body too large");
  }

  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (!supabaseUrl || !serviceRoleKey) {
    return fail(500, "Function is missing Supabase server configuration");
  }

  const authHeader = req.headers.get("authorization") ?? "";
  const token = authHeader.startsWith("Bearer ") ? authHeader.slice("Bearer ".length).trim() : "";
  if (!token) {
    return fail(401, "Missing bearer token");
  }

  const supabase = createClient(supabaseUrl, serviceRoleKey, {
    auth: {
      persistSession: false,
      autoRefreshToken: false,
    },
  });

  const { data: userData, error: userError } = await supabase.auth.getUser(token);
  if (userError || !userData.user) {
    return fail(401, "Invalid bearer token");
  }

  let parsedBody: unknown;
  try {
    parsedBody = await req.json();
  } catch {
    return fail(400, "Invalid JSON body");
  }

  const validation = validateRequest(parsedBody);
  if (!validation.ok) {
    return validation.response;
  }

  const body = validation.value;
  const user = userData.user;
  const metadataInstallId = typeof user.user_metadata?.install_id === "string"
    ? user.user_metadata.install_id
    : "";

  if (metadataInstallId && metadataInstallId !== body.installId) {
    return fail(403, "installId does not match authenticated user metadata");
  }

  const now = new Date().toISOString();
  const { data: existingProfile, error: profileReadError } = await supabase
    .from("player_profiles")
    .select("install_id")
    .eq("user_id", user.id)
    .maybeSingle();

  if (profileReadError) {
    return fail(500, "Failed to read player profile");
  }

  if (existingProfile && existingProfile.install_id !== body.installId) {
    return fail(409, "installId is already bound differently for this user");
  }

  if (!existingProfile) {
    const { error } = await supabase.from("player_profiles").insert({
      user_id: user.id,
      install_id: body.installId,
      first_seen_at: now,
      last_seen_at: now,
      first_game_version: body.gameVersion ?? null,
      latest_game_version: body.gameVersion ?? null,
      platform: body.platform ?? null,
      language: body.language ?? null,
      build_target: body.buildTarget ?? null,
    });

    if (error) {
      return fail(500, "Failed to create player profile");
    }
  } else {
    const { error } = await supabase.from("player_profiles")
      .update({
        last_seen_at: now,
        latest_game_version: body.gameVersion ?? null,
        platform: body.platform ?? null,
        language: body.language ?? null,
        build_target: body.buildTarget ?? null,
      })
      .eq("user_id", user.id);

    if (error) {
      return fail(500, "Failed to update player profile");
    }
  }

  const { data: existingSession, error: sessionReadError } = await supabase
    .from("game_sessions")
    .select("id")
    .eq("id", body.sessionId)
    .maybeSingle();

  if (sessionReadError) {
    return fail(500, "Failed to read session");
  }

  const sessionEndAt = parseDate(body.sessionEnd?.endedAt, now);
  if (!existingSession) {
    const { error } = await supabase.from("game_sessions").insert({
      id: body.sessionId,
      user_id: user.id,
      install_id: body.installId,
      started_at: now,
      ended_at: body.sessionEnd ? sessionEndAt : null,
      game_version: body.gameVersion ?? null,
      platform: body.platform ?? null,
      build_target: body.buildTarget ?? null,
      language: body.language ?? null,
      last_scene: body.currentScene ?? null,
    });

    if (error) {
      return fail(500, "Failed to create session");
    }
  } else {
    const { error } = await supabase.from("game_sessions")
      .update({
        ended_at: body.sessionEnd ? sessionEndAt : undefined,
        game_version: body.gameVersion ?? null,
        platform: body.platform ?? null,
        build_target: body.buildTarget ?? null,
        language: body.language ?? null,
        last_scene: body.currentScene ?? null,
      })
      .eq("id", body.sessionId)
      .eq("user_id", user.id);

    if (error) {
      return fail(500, "Failed to update session");
    }
  }

  const events = body.events ?? [];
  if (events.length > 0) {
    const rows = events.map((event) => ({
      id: event.id,
      user_id: user.id,
      session_id: body.sessionId,
      install_id: body.installId,
      event_name: event.name,
      event_props: event.props ?? {},
      occurred_at: parseDate(event.occurredAt, now),
      game_version: body.gameVersion ?? null,
      scene_name: event.sceneName ?? body.currentScene ?? null,
    }));

    const { error } = await supabase
      .from("telemetry_events")
      .upsert(rows, { onConflict: "id", ignoreDuplicates: true });

    if (error) {
      return fail(500, "Failed to insert telemetry events");
    }
  }

  if (body.progressSnapshot) {
    const snapshot = body.progressSnapshot;
    const { error } = await supabase
      .from("progress_snapshots")
      .upsert({
        user_id: user.id,
        install_id: body.installId,
        session_id: body.sessionId,
        current_scene: snapshot.currentScene ?? body.currentScene ?? null,
        completed_quests: sanitizeStringArray(snapshot.completedQuests),
        completed_objectives: sanitizeStringArray(snapshot.completedObjectives),
        story_flags: sanitizeStringArray(snapshot.storyFlags),
        unlocked_tool_ids: sanitizeStringArray(snapshot.unlockedToolIds),
        inventory_count: Math.max(0, Number(snapshot.inventoryCount ?? 0)),
        warehouse_count: Math.max(0, Number(snapshot.warehouseCount ?? 0)),
        encyclopedia_discovered: Math.max(0, Number(snapshot.encyclopediaDiscovered ?? 0)),
        encyclopedia_total: Math.max(0, Number(snapshot.encyclopediaTotal ?? 0)),
        progress_payload: snapshot.payload ?? {},
        updated_at: parseDate(snapshot.updatedAt, now),
        received_at: now,
      }, { onConflict: "user_id" });

    if (error) {
      return fail(500, "Failed to upsert progress snapshot");
    }
  }

  return jsonResponse(200, {
    ok: true,
    acceptedEvents: events.length,
    userId: user.id,
    sessionId: body.sessionId,
  });
});
