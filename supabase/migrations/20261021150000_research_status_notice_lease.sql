-- Review of 2026-10-21: claiming the target notice set target_notified_at before the email was sent, so a lost
-- claim response, a crash before sending, or a failed release left the notice marked as sent forever.
-- A claim is now a 10-minute lease with its own ID. The Worker confirms it after the email is sent, which is
-- the only step that sets target_notified_at; a lease that is never confirmed or released simply expires and
-- the next check retries. If the confirmation itself is lost, the email may be sent twice, never zero times.
begin;
alter table public.research_status_links
  add column target_notice_claim uuid,
  add column target_notice_claimed_at timestamptz;

create or replace function public.claim_research_status_target(p_token_hash text)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_status jsonb; v_claim uuid := gen_random_uuid(); v_claimed boolean;
begin
  v_status := public.research_status(p_token_hash);
  update public.research_status_links set target_notice_claim = v_claim, target_notice_claimed_at = now()
    where token_hash = p_token_hash and revoked_at is null and target_notified_at is null
      and (target_notice_claimed_at is null or target_notice_claimed_at < now() - interval '10 minutes')
      and (v_status->>'completed')::int >= target_count;
  v_claimed := found;
  return v_status || jsonb_build_object('notify', v_claimed, 'claimId', case when v_claimed then v_claim end);
end $$;

create or replace function public.confirm_research_status_target(p_token_hash text, p_claim uuid)
returns boolean language plpgsql security invoker set search_path = public, pg_temp as $$
begin
  update public.research_status_links set target_notified_at = now()
    where token_hash = p_token_hash and target_notice_claim = p_claim and target_notified_at is null;
  return found;
end $$;

drop function public.release_research_status_target(text);
create function public.release_research_status_target(p_token_hash text, p_claim uuid)
returns boolean language plpgsql security invoker set search_path = public, pg_temp as $$
begin
  update public.research_status_links set target_notice_claim = null, target_notice_claimed_at = null
    where token_hash = p_token_hash and target_notice_claim = p_claim and target_notified_at is null and revoked_at is null;
  return found;
end $$;

revoke all on function public.confirm_research_status_target(text, uuid) from public, anon, authenticated;
revoke all on function public.release_research_status_target(text, uuid) from public, anon, authenticated;
grant execute on function public.confirm_research_status_target(text, uuid) to service_role;
grant execute on function public.release_research_status_target(text, uuid) to service_role;
notify pgrst, 'reload schema';
commit;
