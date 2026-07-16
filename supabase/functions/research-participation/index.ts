import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const maxBodyBytes = 4096;
const maxAttemptsPerMinute = 10;

type JsonRecord = Record<string, unknown>;

function jsonResponse(status: number, body: JsonRecord): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

function fail(status: number, error: string): Response {
  return jsonResponse(status, { ok: false, error });
}

function isPlainObject(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

async function hmacSha256Hex(secret: string, value: string): Promise<string> {
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey(
    "raw",
    encoder.encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("HMAC", key, encoder.encode(value));
  return Array.from(new Uint8Array(signature))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");
}

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (req.method !== "POST") return fail(405, "Method not allowed");

  const forwardedFor = req.headers.get("x-forwarded-for")?.split(",")[0]?.trim() ?? "unknown";
  const rawBody = await req.text();
  if (new TextEncoder().encode(rawBody).length > maxBodyBytes) {
    return fail(413, "Request body too large");
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(rawBody);
  } catch {
    return fail(400, "Invalid JSON body");
  }

  if (!isPlainObject(parsed) || typeof parsed.participantCode !== "string") {
    return fail(400, "参加コードを確認してください。");
  }

  const participantCode = parsed.participantCode.trim().toUpperCase();
  if (!/^[A-Z0-9-]{8,64}$/.test(participantCode)) {
    return fail(400, "参加コードを確認してください。");
  }

  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  const codePepper = Deno.env.get("PARTICIPANT_CODE_PEPPER");
  if (!supabaseUrl || !serviceRoleKey || !codePepper || codePepper.length < 32) {
    return fail(500, "Research participation is not configured");
  }

  const authHeader = req.headers.get("authorization") ?? "";
  const token = authHeader.startsWith("Bearer ") ? authHeader.slice(7).trim() : "";
  if (!token) return fail(401, "Missing bearer token");

  const supabase = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });
  const { data: userData, error: userError } = await supabase.auth.getUser(token);
  if (userError || !userData.user) return fail(401, "Invalid bearer token");

  const rateBucketKey = await hmacSha256Hex(codePepper, `rate:${forwardedFor}`);
  const { data: rateAllowed, error: rateError } = await supabase.rpc("consume_research_rate_limit", {
    p_bucket_key: rateBucketKey,
    p_limit: maxAttemptsPerMinute,
    p_window_seconds: 60,
  });
  if (rateError) {
    console.error("Rate limit check failed", rateError.code, rateError.message);
    return fail(500, "参加コードを確認できませんでした。");
  }
  if (rateAllowed !== true) {
    return fail(429, "確認回数が多すぎます。少し待ってから再試行してください。");
  }

  const codeHash = await hmacSha256Hex(codePepper, participantCode);
  const { data, error } = await supabase.rpc("activate_development_participant", {
    p_code_hash: codeHash,
    p_user_id: userData.user.id,
  });

  if (error) {
    console.error("Participant activation failed", error.code, error.message);
    if (error.code === "P0002") return fail(404, "参加コードを確認できませんでした。");
    if (error.code === "23505") return fail(409, "この参加コードは別の端末で有効化されています。");
    if (error.code === "42501") return fail(403, "研究参加の受付は現在停止しています。");
    if (error.code === "22023") return fail(409, "研究手順のバージョンが一致しません。");
    return fail(500, "参加コードを有効化できませんでした。");
  }

  return jsonResponse(200, isPlainObject(data) ? data : { ok: true });
});
