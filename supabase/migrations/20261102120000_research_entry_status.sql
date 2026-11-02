-- 2026-11-02: Testee asked whether the game stops taking monitors once the target is reached. The user chose
-- to close only the front door: the guardian consent page asks research_entry_status() first and shows a
-- "募集終了" sheet instead of the form when entry is closed. Activation is not blocked, so anyone who opened
-- the page before the target was reached (or is playing) still finishes and answers normally.
--
-- Entry is closed when the open-play study is not accepting entry, or when a status link marked closes_entry
-- has reached its target with the same count Testee sees on the status page.
begin;
alter table public.research_status_links add column closes_entry boolean not null default false;
comment on column public.research_status_links.closes_entry is
  'When the completed count on this link reaches target_count, the guardian consent page shows 募集終了.';

-- Testee's production link (2026-10-21, target 20). Not present in local stacks.
update public.research_status_links set closes_entry = true
  where id = 'd006abf0-ec3e-4060-ad2c-da9a70c867af' and revoked_at is null;

-- Called by the public page with the publishable key, so it runs as the owner and returns only the flag.
create or replace function public.research_entry_status()
returns jsonb language plpgsql stable security definer set search_path = public, pg_temp as $$
declare v_study public.studies%rowtype; v_link public.research_status_links%rowtype;
begin
  select * into v_study from public.studies
    where entry_mode = 'open_play' and status = 'active' and research_entry_enabled
      and (retention_until is null or retention_until > now());
  if not found then return jsonb_build_object('open', false); end if;
  for v_link in select * from public.research_status_links
      where study_id = v_study.id and closes_entry and revoked_at is null loop
    if (public.research_status(v_link.token_hash)->>'completed')::int >= v_link.target_count then
      return jsonb_build_object('open', false);
    end if;
  end loop;
  return jsonb_build_object('open', true);
end $$;
revoke all on function public.research_entry_status() from public;
grant execute on function public.research_entry_status() to anon, authenticated, service_role;
notify pgrst, 'reload schema';
commit;
