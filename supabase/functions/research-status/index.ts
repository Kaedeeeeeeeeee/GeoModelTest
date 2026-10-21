import { serve } from "https://deno.land/std@0.224.0/http/server.ts";

// Collection status for a research status link. The geo-q.jp Worker calls this with the link token;
// the service credential never leaves Supabase. The target-reached email uses three more actions:
// {"action":"claim"} takes a 10-minute lease and returns its claimId, {"action":"confirm","claimId":…}
// marks the email as sent, and {"action":"release","claimId":…} gives the lease back when sending failed.
// The link token is shared with Testee, so these also need STATUS_NOTIFY_SECRET, which only the Worker
// knows; otherwise anyone with the page URL could suppress or repeat the email.
const headers = { "Content-Type": "application/json", "Cache-Control": "no-store" };
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const procedures = new Map([
  ["read","research_status"],["claim","claim_research_status_target"],
  ["confirm","confirm_research_status_target"],["release","release_research_status_target"],
]);
const reply = (status: number, data: unknown) => new Response(JSON.stringify(data), { status, headers });
async function digest(value: string) {
  return Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value))), b => b.toString(16).padStart(2,"0")).join("");
}
serve(async req => {
  if (req.method !== "POST") return reply(405,{ok:false,error:"method"});
  const server = Deno.env.get("SUPABASE_URL");
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  const notifySecret = Deno.env.get("STATUS_NOTIFY_SECRET");
  if (!server || !serviceKey) return reply(503,{ok:false,error:"unavailable"});
  const token = req.headers.get("authorization")?.match(/^Bearer ([0-9a-f]{64})$/)?.[1];
  if (!token) return reply(403,{ok:false,error:"access"});
  try {
    const text = await req.text();
    if (text.length > 256) return reply(413,{ok:false,error:"too_large"});
    let body: {action?: unknown, claimId?: unknown} = {action:"read"};
    if (text) { try { body = JSON.parse(text) ?? {}; } catch (_) { return reply(400,{ok:false,error:"invalid"}); } }
    const action = String(body.action);
    const procedure = procedures.get(action);
    const withClaim = action === "confirm" || action === "release";
    if (!procedure || (withClaim && (typeof body.claimId !== "string" || !uuid.test(body.claimId)))) return reply(400,{ok:false,error:"invalid"});
    // Comparing digests keeps the comparison time independent of the secret.
    if (action !== "read" && (!notifySecret || await digest(req.headers.get("x-notify-secret") ?? "") !== await digest(notifySecret))) {
      return reply(403,{ok:false,error:"access"});
    }
    const hash = await digest(token);
    const service = {apikey:serviceKey,Authorization:`Bearer ${serviceKey}`,"Content-Type":"application/json"};
    const rpc = async (name: string, params: unknown) => {
      const response = await fetch(`${server}/rest/v1/rpc/${name}`, { method:"POST",headers:service,body:JSON.stringify(params)});
      const raw = await response.text();
      return {response,data:raw ? JSON.parse(raw) : null};
    };
    // Look the link up before rate limiting, so random tokens never create rate-limit rows.
    const link = await fetch(`${server}/rest/v1/research_status_links?select=id&revoked_at=is.null&token_hash=eq.${hash}`, {headers:service});
    if (!link.ok) return reply(503,{ok:false,error:"unavailable"});
    if (!(await link.json()).length) return reply(403,{ok:false,error:"access"});
    const rate = await rpc("consume_research_rate_limit",{p_bucket_key:await digest(`status:${hash}`),p_limit:30,p_window_seconds:60});
    if (!rate.response.ok) return reply(503,{ok:false,error:"unavailable"});
    if (rate.data !== true) return reply(429,{ok:false,error:"rate"});
    const result = await rpc(procedure, withClaim ? {p_token_hash:hash,p_claim:body.claimId} : {p_token_hash:hash});
    if (!result.response.ok) return result.data?.code === "42501" ? reply(403,{ok:false,error:"access"}) : reply(503,{ok:false,error:"unavailable"});
    return reply(200, withClaim ? {ok:true,done:result.data === true} : {ok:true,...result.data});
  } catch (_) { return reply(503,{ok:false,error:"unavailable"}); }
});
