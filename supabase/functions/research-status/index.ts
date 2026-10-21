import { serve } from "https://deno.land/std@0.224.0/http/server.ts";

// Read-only collection status for a research status link. The geo-q.jp Worker calls this with the link token;
// the service credential never leaves Supabase.
const headers = { "Content-Type": "application/json", "Cache-Control": "no-store" };
const reply = (status: number, data: unknown) => new Response(JSON.stringify(data), { status, headers });
async function digest(value: string) {
  return Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value))), b => b.toString(16).padStart(2,"0")).join("");
}
serve(async req => {
  if (req.method !== "POST") return reply(405,{ok:false,error:"method"});
  const server = Deno.env.get("SUPABASE_URL");
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (!server || !serviceKey) return reply(503,{ok:false,error:"unavailable"});
  const token = req.headers.get("authorization")?.match(/^Bearer ([0-9a-f]{64})$/)?.[1];
  if (!token) return reply(403,{ok:false,error:"access"});
  try {
    const hash = await digest(token);
    const rpc = async (name: string, params: unknown) => {
      const response = await fetch(`${server}/rest/v1/rpc/${name}`, { method:"POST",headers:{apikey:serviceKey,Authorization:`Bearer ${serviceKey}`,"Content-Type":"application/json"},body:JSON.stringify(params)});
      return {response,data:await response.json()};
    };
    const rate = await rpc("consume_research_rate_limit",{p_bucket_key:await digest(`status:${hash}`),p_limit:30,p_window_seconds:60});
    if (!rate.response.ok) return reply(503,{ok:false,error:"unavailable"});
    if (rate.data !== true) return reply(429,{ok:false,error:"rate"});
    const result = await rpc("research_status",{p_token_hash:hash});
    if (!result.response.ok) return result.data.code === "42501" ? reply(403,{ok:false,error:"access"}) : reply(503,{ok:false,error:"unavailable"});
    return reply(200,{ok:true,...result.data});
  } catch (_) { return reply(503,{ok:false,error:"unavailable"}); }
});
