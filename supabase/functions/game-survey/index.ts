import { serve } from "https://deno.land/std@0.224.0/http/server.ts";

const headers = { "Content-Type": "application/json", "Cache-Control": "no-store", "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, apikey, content-type", "Access-Control-Allow-Methods": "POST, OPTIONS" };
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const reply = (status: number, data: unknown) => new Response(JSON.stringify(data), { status, headers });
async function digest(value: string) {
  return Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value))), b => b.toString(16).padStart(2,"0")).join("");
}
serve(async req => {
  if (req.method === "OPTIONS") return new Response("ok", { headers });
  if (req.method !== "POST") return reply(405,{ok:false,error:"method"});
  const server = Deno.env.get("SUPABASE_URL");
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (!server || !serviceKey) return reply(503,{ok:false,error:"unavailable"});
  try {
    const reader = req.body?.getReader();
    if (!reader) return reply(400,{ok:false,error:"invalid"});
    const chunks: Uint8Array[] = []; let length = 0;
    while (true) {
      const {done,value} = await reader.read(); if (done) break;
      length += value.length;
      if (length > 32768) { await reader.cancel(); return reply(413,{ok:false,error:"too_large"}); }
      chunks.push(value);
    }
    const bytes = new Uint8Array(length); let offset = 0;
    for (const chunk of chunks) { bytes.set(chunk,offset); offset += chunk.length; }
    const body = JSON.parse(new TextDecoder().decode(bytes));
    if (!body || typeof body !== "object" || Array.isArray(body)) return reply(400,{ok:false,error:"invalid"});
    const token = req.headers.get("authorization")?.match(/^Bearer (.+)$/)?.[1] ?? "";
    if (!token || token.length > 4096) return reply(401,{ok:false,error:"access"});
    const rpc = async (name: string, params: unknown) => {
      const response = await fetch(`${server}/rest/v1/rpc/${name}`, { method:"POST",headers:{apikey:serviceKey,Authorization:`Bearer ${serviceKey}`,"Content-Type":"application/json"},body:JSON.stringify(params)});
      return {response,data:await response.json()};
    };
    if (body.action === "issue") {
      if (Object.keys(body).some(k=>!["action","sessionId","runId"].includes(k)) || !uuid.test(body.sessionId) || !uuid.test(body.runId)) return reply(400,{ok:false,error:"invalid"});
      const auth = await fetch(`${server}/auth/v1/user`,{headers:{apikey:serviceKey,Authorization:`Bearer ${token}`}});
      if (!auth.ok) return reply(401,{ok:false,error:"access"});
      const user = await auth.json();
      const rate = await rpc("consume_research_rate_limit",{p_bucket_key:await digest(`survey:${user.id}`),p_limit:10,p_window_seconds:60});
      if (!rate.response.ok) return reply(503,{ok:false,error:"unavailable"});
      if (rate.data !== true) return reply(429,{ok:false,error:"rate"});
      const ticket = Array.from(crypto.getRandomValues(new Uint8Array(32)),b=>b.toString(16).padStart(2,"0")).join("");
      const result = await rpc("issue_survey_ticket",{p_user_id:user.id,p_session_id:body.sessionId,p_run_id:body.runId,p_token_hash:await digest(ticket)});
      if (!result.response.ok) return reply(result.data.code === "22023" ? 409 : 403,{ok:false,error:result.data.code === "22023" ? "not_ready" : "access"});
      return reply(200,{ok:true,ticket,...result.data});
    }
    if (!["open","submit"].includes(body.action) || !/^[0-9a-f]{64}$/.test(token)) return reply(400,{ok:false,error:"invalid"});
    const allowed = body.action === "open" ? ["action"] : ["action","answers"];
    if (Object.keys(body).some(k=>!allowed.includes(k)) || (body.action === "submit" && (!body.answers || typeof body.answers !== "object" || Array.isArray(body.answers)))) return reply(400,{ok:false,error:"invalid"});
    const result = await rpc("use_survey_ticket",{p_token_hash:await digest(token),p_answers:body.action === "submit" ? body.answers : null});
    if (!result.response.ok) return reply(result.data.code === "22023" ? 400 : 403,{ok:false,error:result.data.code === "22023" ? "answers" : "access"});
    return reply(200,result.data);
  } catch (_) { return reply(400,{ok:false,error:"invalid"}); }
});
