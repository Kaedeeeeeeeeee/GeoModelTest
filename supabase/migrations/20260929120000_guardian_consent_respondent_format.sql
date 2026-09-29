-- Testee respondent IDs are a lowercase "p" followed by 7 digits (e.g. p1234567).
-- The web form and Edge function normalize full-width/uppercase input first.
-- NOT VALID keeps the single verification row recorded before the format was known.
alter table public.guardian_consents drop constraint guardian_consents_respondent_id_check;
alter table public.guardian_consents add constraint guardian_consents_respondent_id_check
  check (respondent_id ~ '^p[0-9]{7}$') not valid;

create or replace function public.record_guardian_consent(p_participant_id uuid, p_study_id uuid, p_consent jsonb)
returns void language plpgsql security invoker set search_path = public, pg_temp as $$
declare
  v_version text;
  v_name text;
  v_respondent text;
  v_guardian_at timestamptz;
  v_student_at timestamptz;
begin
  if p_consent is null or jsonb_typeof(p_consent) <> 'object' then
    raise exception using errcode = 'GC001', message = 'Guardian consent required';
  end if;
  v_version := p_consent->>'consentVersion';
  v_name := p_consent->>'guardianName';
  v_respondent := p_consent->>'respondentId';
  begin
    v_guardian_at := (p_consent->>'guardianConsentedAt')::timestamptz;
    v_student_at := (p_consent->>'studentAssentedAt')::timestamptz;
  exception when others then
    raise exception using errcode = 'GC002', message = 'Invalid guardian consent';
  end;
  -- Device clocks drift, so only reject clearly impossible answer times.
  if p_consent->'guardianAgreed' is distinct from 'true'::jsonb
     or p_consent->'guardianConfirmed' is distinct from 'true'::jsonb
     or p_consent->'studentAssented' is distinct from 'true'::jsonb
     or v_version is null or char_length(v_version) not between 1 and 64
     or v_name is null or char_length(v_name) not between 1 and 100
     or v_name <> btrim(v_name) or v_name ~ '[[:cntrl:]]'
     or v_respondent is null or v_respondent !~ '^p[0-9]{7}$'
     or v_guardian_at is null or v_student_at is null
     or v_guardian_at not between now() - interval '30 days' and now() + interval '1 day'
     or v_student_at not between now() - interval '30 days' and now() + interval '1 day' then
    raise exception using errcode = 'GC002', message = 'Invalid guardian consent';
  end if;
  insert into public.guardian_consents(participant_id, study_id, consent_version, guardian_name,
    respondent_id, guardian_confirmed, guardian_consented_at, student_assented_at)
  values (p_participant_id, p_study_id, v_version, v_name, v_respondent, true, v_guardian_at, v_student_at);
  update public.study_participants
    set consent_version = v_version,
        guardian_consent_at = least(v_guardian_at, now()),
        student_assent_at = least(v_student_at, now())
    where id = p_participant_id;
end;
$$;
revoke all on function public.record_guardian_consent(uuid, uuid, jsonb) from public, anon, authenticated;
grant execute on function public.record_guardian_consent(uuid, uuid, jsonb) to service_role;
