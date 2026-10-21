-- Testee (2026-10-21) cannot see anything after a monitor leaves for geo-q.jp and asked for the completer IDs
-- and collection status each time. A status link gives them a read-only page on geo-q.jp instead.
-- Only the SHA-256 of the link token is stored, like survey tickets. The page shows stage counts and
-- respondent IDs, never guardian names or participant UUIDs.
begin;
create table public.research_status_links (
  id uuid primary key default gen_random_uuid(),
  token_hash text not null unique check (token_hash ~ '^[0-9a-f]{64}$'),
  study_id uuid not null references public.studies(id) on delete cascade,
  label text not null check (char_length(label) between 1 and 100 and label !~ '[[:cntrl:]]'),
  -- Earlier guardian forms (our own and the teacher's test plays) are left out of the counts.
  counted_from timestamptz not null,
  target_count integer not null check (target_count between 1 and 10000),
  created_at timestamptz not null default now(),
  revoked_at timestamptz
);
alter table public.research_status_links enable row level security;
revoke all on public.research_status_links from public, anon, authenticated;
grant all on public.research_status_links to service_role;

-- Completers follow `research-admin.py completers`: one ID per participant, the ID re-entered in the
-- questionnaire (Testee's specification of 2026-10-02), or the guardian-form ID for responses from
-- before that double check. A participant who re-entered two different IDs is listed for review and
-- not counted until a person decides. The same ID from two participants counts once.
create or replace function public.research_status(p_token_hash text)
returns jsonb language plpgsql stable security invoker set search_path = public, pg_temp as $$
declare v_link public.research_status_links%rowtype; v_result jsonb;
begin
  select * into v_link from public.research_status_links
    where token_hash = p_token_hash and revoked_at is null;
  if not found then raise exception using errcode = '42501', message = 'Status link is invalid'; end if;
  with consented as (
    select participant_id, respondent_id as guardian_id from public.guardian_consents
      where study_id = v_link.study_id and recorded_at >= v_link.counted_from
  ), responses as (
    select r.participant_id, r.respondent_id, r.submitted_at, c.guardian_id
      from public.survey_responses r join consented c using (participant_id)
  ), entered as (
    select participant_id, respondent_id, min(submitted_at) as submitted_at
      from responses where respondent_id is not null group by participant_id, respondent_id
  ), participants as (
    select participant_id, min(guardian_id) as guardian_id, min(submitted_at) as first_at,
        (select count(*) from entered e where e.participant_id = r.participant_id) as entered_ids
      from responses r group by participant_id
  ), reported as (
    select coalesce(e.respondent_id, p.guardian_id) as respondent_id,
        coalesce(e.submitted_at, p.first_at) as completed_at, p.guardian_id
      from participants p left join entered e using (participant_id)
      where p.entered_ids <= 1
  ), completers as (
    select respondent_id, min(completed_at) as completed_at,
        array_agg(distinct guardian_id) filter (where guardian_id <> respondent_id) as guardian_ids
      from reported group by respondent_id
  )
  select jsonb_build_object(
    'label', v_link.label,
    'countedFrom', v_link.counted_from,
    'target', v_link.target_count,
    'generatedAt', now(),
    'consented', (select count(*) from consented),
    -- A questionnaire ticket is issued only after the game is cleared.
    'gameCleared', (select count(distinct t.participant_id) from public.survey_tickets t join consented c using (participant_id)),
    'surveyed', (select count(*) from participants),
    'completed', (select count(*) from completers),
    'completers', coalesce((select jsonb_agg(jsonb_build_object('respondentId', respondent_id, 'completedAt', completed_at,
        'guardianFormIds', coalesce(to_jsonb(guardian_ids), '[]'::jsonb)) order by completed_at, respondent_id) from completers), '[]'::jsonb),
    'review', coalesce((select jsonb_agg(jsonb_build_object('guardianFormId', p.guardian_id,
        'entries', (select jsonb_agg(jsonb_build_object('respondentId', e.respondent_id, 'submittedAt', e.submitted_at)
          order by e.submitted_at) from entered e where e.participant_id = p.participant_id)) order by p.first_at)
      from participants p where p.entered_ids > 1), '[]'::jsonb)
  ) into v_result;
  return v_result;
end $$;
revoke all on function public.research_status(text) from public, anon, authenticated;
grant execute on function public.research_status(text) to service_role;
notify pgrst, 'reload schema';
commit;
