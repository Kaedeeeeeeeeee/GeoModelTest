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
  "session_heartbeat",
  "research_mode_started",
  "scene_loaded",
  "tool_equipped",
  "tool_used",
  "quest_started",
  "objective_completed",
  "quest_completed",
  "progress_dirty",
  "story_content_notice_decision",
  "quiz_question_shown",
  "quiz_hint_viewed",
  "quiz_answered",
  "manual_flush",
]);

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const maxItems = 100;
const maxEventPropsBytes = 8 * 1024;
const maxSnapshotBytes = 64 * 1024;
const maxBodyBytes = 256 * 1024;
const maxPastAgeMs = 30 * 24 * 60 * 60 * 1000;
const maxFutureSkewMs = 5 * 60 * 1000;

type JsonRecord = Record<string, unknown>;

type TelemetryEvent = {
  id: string;
  participantId: string;
  studyId: string;
  condition: string;
  sessionId: string;
  name: string;
  occurredAt: string;
  sceneName?: string;
  props?: JsonRecord;
};

type QuizAttempt = {
  eventId: string;
  participantId: string;
  studyId: string;
  condition: string;
  sessionId: string;
  runId: string;
  questionId: string;
  questionVersion: string;
  choiceId: string;
  attemptIndex: number;
  isCorrect: boolean;
  usedHint: boolean;
  responseTimeMs: number;
  occurredAt: string;
  gameVersion: string;
  contentVersion: string;
  storyRoute: string;
};

type ProgressSnapshot = {
  eventId: string;
  participantId: string;
  studyId: string;
  condition: string;
  sessionId: string;
  currentScene?: string;
  completedQuests?: string[];
  completedObjectives?: string[];
  storyFlags?: string[];
  unlockedToolIds?: string[];
  inventoryCount?: number;
  warehouseCount?: number;
  encyclopediaDiscovered?: number;
  encyclopediaTotal?: number;
  updatedAt: string;
  payload?: JsonRecord;
};

type IngestRequest = {
  installId: string;
  participantId: string;
  studyId: string;
  condition: string;
  protocolVersion: string;
  sessionId: string;
  gameVersion: string;
  platform: string;
  buildTarget: string;
  language: string;
  currentScene: string;
  contentVersion: string;
  storyRoute: string;
  events?: TelemetryEvent[];
  quizAttempts?: QuizAttempt[];
  progressSnapshot?: ProgressSnapshot;
  sessionEnd?: { endedAt: string; reason: string };
};

function jsonResponse(status: number, body: JsonRecord): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

function fail(status: number, message: string): Response {
  return jsonResponse(status, { ok: false, error: message });
}

function isPlainObject(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function byteLength(value: string | unknown): number {
  const serialized = typeof value === "string" ? value : JSON.stringify(value ?? {});
  return new TextEncoder().encode(serialized).length;
}

function validString(value: unknown, min: number, max: number): value is string {
  return typeof value === "string" && value.trim().length >= min && value.trim().length <= max;
}

function validClientTime(value: unknown): value is string {
  if (typeof value !== "string") return false;
  const timestamp = Date.parse(value);
  if (Number.isNaN(timestamp)) return false;
  const delta = timestamp - Date.now();
  return delta <= maxFutureSkewMs && delta >= -maxPastAgeMs;
}

function validNonNegativeInt(value: unknown, max: number): value is number {
  return typeof value === "number" && Number.isInteger(value) && value >= 0 && value <= max;
}

function validStringArray(value: unknown): boolean {
  return value === undefined || (
    Array.isArray(value) &&
    value.length <= 500 &&
    value.every((item) => validString(item, 1, 128))
  );
}

function validateBinding(
  value: { participantId?: string; studyId?: string; condition?: string; sessionId?: string },
  body: IngestRequest,
): boolean {
  return value.participantId === body.participantId &&
    value.studyId === body.studyId &&
    value.condition === body.condition &&
    value.sessionId === body.sessionId;
}

function validateRequest(value: unknown): { ok: true; body: IngestRequest } | { ok: false; response: Response } {
  if (!isPlainObject(value)) {
    return { ok: false, response: fail(400, "Body must be a JSON object") };
  }

  const body = value as unknown as IngestRequest;
  for (const [name, id] of [
    ["installId", body.installId],
    ["participantId", body.participantId],
    ["studyId", body.studyId],
    ["sessionId", body.sessionId],
  ] as const) {
    if (!uuidPattern.test(id ?? "")) {
      return { ok: false, response: fail(400, `Invalid ${name}`) };
    }
  }

  const boundedStrings: Array<[string, unknown, number]> = [
    ["condition", body.condition, 32],
    ["protocolVersion", body.protocolVersion, 64],
    ["gameVersion", body.gameVersion, 64],
    ["platform", body.platform, 64],
    ["buildTarget", body.buildTarget, 64],
    ["language", body.language, 64],
    ["currentScene", body.currentScene, 128],
    ["contentVersion", body.contentVersion, 128],
    ["storyRoute", body.storyRoute, 64],
  ];
  for (const [name, text, max] of boundedStrings) {
    if (!validString(text, 1, max)) {
      return { ok: false, response: fail(400, `Invalid ${name}`) };
    }
  }

  if (body.events !== undefined && !Array.isArray(body.events)) {
    return { ok: false, response: fail(400, "events must be an array") };
  }
  if (body.quizAttempts !== undefined && !Array.isArray(body.quizAttempts)) {
    return { ok: false, response: fail(400, "quizAttempts must be an array") };
  }
  if ((body.events?.length ?? 0) + (body.quizAttempts?.length ?? 0) > maxItems) {
    return { ok: false, response: fail(413, "Too many items") };
  }

  for (const event of body.events ?? []) {
    if (!isPlainObject(event) || !uuidPattern.test(event.id ?? "") || !validateBinding(event, body)) {
      return { ok: false, response: fail(400, "Invalid event identity or research binding") };
    }
    if (!allowedEvents.has(event.name)) {
      return { ok: false, response: fail(400, `Unknown event name: ${event.name}`) };
    }
    if (!validClientTime(event.occurredAt) ||
        (event.sceneName !== undefined && !validString(event.sceneName, 1, 128))) {
      return { ok: false, response: fail(400, "Invalid event time or scene") };
    }
    if (event.props !== undefined && (!isPlainObject(event.props) || byteLength(event.props) > maxEventPropsBytes)) {
      return { ok: false, response: fail(413, "Invalid or oversized event props") };
    }
  }

  for (const attempt of body.quizAttempts ?? []) {
    if (!isPlainObject(attempt) ||
        !uuidPattern.test(attempt.eventId ?? "") ||
        !uuidPattern.test(attempt.runId ?? "") ||
        !validateBinding(attempt, body)) {
      return { ok: false, response: fail(400, "Invalid quiz attempt identity or research binding") };
    }
    if (!validString(attempt.questionId, 1, 128) ||
        !validString(attempt.questionVersion, 1, 64) ||
        !validString(attempt.choiceId, 1, 128) ||
        !validString(attempt.gameVersion, 1, 64) ||
        !validString(attempt.contentVersion, 1, 128) ||
        !validString(attempt.storyRoute, 1, 64) ||
        !validNonNegativeInt(attempt.responseTimeMs, 3_600_000) ||
        !validNonNegativeInt(attempt.attemptIndex, 100) || attempt.attemptIndex < 1 ||
        typeof attempt.isCorrect !== "boolean" || typeof attempt.usedHint !== "boolean" ||
        !validClientTime(attempt.occurredAt)) {
      return { ok: false, response: fail(400, "Invalid quiz attempt payload") };
    }
  }

  if (body.progressSnapshot !== undefined) {
    const snapshot = body.progressSnapshot;
    if (!isPlainObject(snapshot) || byteLength(snapshot) > maxSnapshotBytes ||
        !uuidPattern.test(snapshot.eventId ?? "") || !validateBinding(snapshot, body) ||
        !validClientTime(snapshot.updatedAt) ||
        !validStringArray(snapshot.completedQuests) ||
        !validStringArray(snapshot.completedObjectives) ||
        !validStringArray(snapshot.storyFlags) ||
        !validStringArray(snapshot.unlockedToolIds)) {
      return { ok: false, response: fail(400, "Invalid progress snapshot") };
    }
    for (const count of [
      snapshot.inventoryCount,
      snapshot.warehouseCount,
      snapshot.encyclopediaDiscovered,
      snapshot.encyclopediaTotal,
    ]) {
      if (count !== undefined && !validNonNegativeInt(count, 1_000_000)) {
        return { ok: false, response: fail(400, "Invalid progress count") };
      }
    }
    if (snapshot.payload !== undefined && !isPlainObject(snapshot.payload)) {
      return { ok: false, response: fail(400, "Progress payload must be an object") };
    }
  }

  if (body.sessionEnd !== undefined &&
      (!isPlainObject(body.sessionEnd) || !validClientTime(body.sessionEnd.endedAt) ||
       !validString(body.sessionEnd.reason, 1, 64))) {
    return { ok: false, response: fail(400, "Invalid sessionEnd") };
  }

  return { ok: true, body };
}

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (req.method !== "POST") return fail(405, "Method not allowed");

  const rawBody = await req.text();
  if (byteLength(rawBody) > maxBodyBytes) return fail(413, "Request body too large");

  let parsedBody: unknown;
  try {
    parsedBody = JSON.parse(rawBody);
  } catch {
    return fail(400, "Invalid JSON body");
  }

  const validation = validateRequest(parsedBody);
  if (!validation.ok) return validation.response;

  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (!supabaseUrl || !serviceRoleKey) return fail(500, "Missing server configuration");

  const authHeader = req.headers.get("authorization") ?? "";
  const token = authHeader.startsWith("Bearer ") ? authHeader.slice(7).trim() : "";
  if (!token) return fail(401, "Missing bearer token");

  const supabase = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });
  const { data: userData, error: userError } = await supabase.auth.getUser(token);
  if (userError || !userData.user) return fail(401, "Invalid bearer token");

  const body = validation.body;
  const metadataInstallId = typeof userData.user.user_metadata?.install_id === "string"
    ? userData.user.user_metadata.install_id
    : "";
  if (metadataInstallId && metadataInstallId !== body.installId) {
    return fail(403, "installId does not match authenticated user metadata");
  }

  const { data, error } = await supabase.rpc("ingest_research_batch", {
    p_user_id: userData.user.id,
    p_participant_id: body.participantId,
    p_study_id: body.studyId,
    p_session_id: body.sessionId,
    p_install_id: body.installId,
    p_game_version: body.gameVersion,
    p_platform: body.platform,
    p_build_target: body.buildTarget,
    p_language: body.language,
    p_current_scene: body.currentScene,
    p_content_version: body.contentVersion,
    p_story_route: body.storyRoute,
    p_protocol_version: body.protocolVersion,
    p_condition: body.condition,
    p_events: body.events ?? [],
    p_quiz_attempts: body.quizAttempts ?? [],
    p_progress: body.progressSnapshot ?? null,
    p_session_end: body.sessionEnd ?? null,
  });

  if (error) {
    const forbidden = error.code === "42501";
    console.error("ingest_research_batch failed", error.code, error.message);
    return fail(forbidden ? 403 : 500, forbidden ? error.message : "Research batch was not stored");
  }

  return jsonResponse(200, isPlainObject(data) ? data : { ok: true });
});
