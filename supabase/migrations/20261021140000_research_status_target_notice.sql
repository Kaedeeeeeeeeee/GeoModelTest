-- 2026-10-21: the researcher wants one email when a status link reaches its target, so they can ask Testee
-- to stop distribution. The geo-q.jp Worker checks every 5 minutes, claims the notice here before sending,
-- and releases the claim if the email fails, so the next check retries. A claimed notice is never sent again.
begin;
alter table public.research_status_links add column target_notified_at timestamptz;

create or replace function public.claim_research_status_target(p_token_hash text)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_status jsonb; v_claimed boolean;
begin
  v_status := public.research_status(p_token_hash);
  update public.research_status_links set target_notified_at = now()
    where token_hash = p_token_hash and revoked_at is null and target_notified_at is null
      and (v_status->>'completed')::int >= target_count;
  v_claimed := found;
  return v_status || jsonb_build_object('notify', v_claimed);
end $$;

create or replace function public.release_research_status_target(p_token_hash text)
returns void language sql security invoker set search_path = public, pg_temp as $$
  update public.research_status_links set target_notified_at = null where token_hash = p_token_hash;
$$;

revoke all on function public.claim_research_status_target(text) from public, anon, authenticated;
revoke all on function public.release_research_status_target(text) from public, anon, authenticated;
grant execute on function public.claim_research_status_target(text) to service_role;
grant execute on function public.release_research_status_target(text) to service_role;
notify pgrst, 'reload schema';
commit;
