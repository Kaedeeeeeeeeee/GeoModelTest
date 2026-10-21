-- 2026-10-21: we cannot tell whether the guardian or the child typed the right respondent ID, so the status
-- page no longer picks the questionnaire ID when the two differ. Any participant whose questionnaire ID differs
-- from the guardian-form ID goes to review with both IDs shown, and is not counted until a person decides.
-- Completers are now only participants whose IDs agree (or whose responses predate the double check).
begin;
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
        bool_or(respondent_id <> guardian_id) is true as differs
      from responses group by participant_id
  ), completers as (
    -- The same ID from two participants (one child in two browsers) counts once.
    select guardian_id as respondent_id, min(first_at) as completed_at
      from participants where not differs group by guardian_id
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
    'completers', coalesce((select jsonb_agg(jsonb_build_object('respondentId', respondent_id, 'completedAt', completed_at)
        order by completed_at, respondent_id) from completers), '[]'::jsonb),
    -- guardianMismatch: one questionnaire ID, different from the guardian form.
    -- multipleAnswers: answered again with another ID (siblings sharing a browser, or a typo the second time).
    'review', coalesce((select jsonb_agg(jsonb_build_object('guardianFormId', p.guardian_id,
        'kind', case when (select count(*) from entered e where e.participant_id = p.participant_id) > 1
          then 'multipleAnswers' else 'guardianMismatch' end,
        'entries', (select jsonb_agg(jsonb_build_object('respondentId', e.respondent_id, 'submittedAt', e.submitted_at)
          order by e.submitted_at) from entered e where e.participant_id = p.participant_id)) order by p.first_at)
      from participants p where p.differs), '[]'::jsonb)
  ) into v_result;
  return v_result;
end $$;
revoke all on function public.research_status(text) from public, anon, authenticated;
grant execute on function public.research_status(text) to service_role;
notify pgrst, 'reload schema';
commit;
