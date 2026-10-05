-- Testee corrected the respondent ID format on 2026-10-02: digits only, 1 to 10 of them,
-- the length differing per person (it was thought to be "p" + 7 digits). The web form and the
-- Edge function normalize full-width digits, spaces and dashes first; the ID stays text, so
-- leading zeros are kept. NOT VALID keeps the earlier "p" + 7 digit rows from before the change.
begin;
alter table public.guardian_consents drop constraint guardian_consents_respondent_id_check;
alter table public.guardian_consents add constraint guardian_consents_respondent_id_check
  check (respondent_id ~ '^[0-9]{1,10}$') not valid;
alter table public.survey_responses drop constraint survey_responses_respondent_id_check;
alter table public.survey_responses add constraint survey_responses_respondent_id_check
  check (respondent_id is null or respondent_id ~ '^[0-9]{1,10}$') not valid;

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
     or v_respondent is null or v_respondent !~ '^[0-9]{1,10}$'
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

create or replace function public.use_survey_ticket(p_token_hash text, p_answers jsonb default null,
  p_respondent_id text default null, p_confirm_respondent_mismatch boolean default false)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_ticket public.survey_tickets%rowtype; v_response public.survey_responses%rowtype;
  v_key text; v_value text; v_number integer; v_expected text; v_matches boolean;
begin
  select t.* into v_ticket from public.survey_tickets t
    join public.study_participants p on p.id = t.participant_id
    join public.studies s on s.id = t.study_id
    where t.token_hash = p_token_hash and t.expires_at > now()
      and p.withdrawn_at is null and p.status in ('ready','active','completed')
      and s.status in ('development','active') and s.research_entry_enabled
      and (s.retention_until is null or s.retention_until > now())
      and public.research_participant_is_eligible(p.id) for update of t;
  if not found then raise exception using errcode = '42501', message = 'Survey link is invalid or expired'; end if;
  select * into v_response from public.survey_responses where participant_id = v_ticket.participant_id
    and run_id = v_ticket.run_id and survey_version = v_ticket.survey_version;
  if found then return jsonb_build_object('ok',true,'submitted',true,'receiptId',v_response.id); end if;
  select g.respondent_id into v_expected from public.guardian_consents g where g.participant_id = v_ticket.participant_id;
  if p_answers is null then
    return jsonb_build_object('ok',true,'submitted',false,'surveyVersion',v_ticket.survey_version,
      'respondentIdRequired',v_expected is not null);
  end if;
  if jsonb_typeof(p_answers) <> 'object' or octet_length(p_answers::text) > 24000 then
    raise exception using errcode = '22023', message = 'Invalid survey answers';
  end if;
  for v_key in select jsonb_object_keys(p_answers) loop
    if v_key !~ '^q([1-9]|1[0-4])$' or jsonb_typeof(p_answers->v_key) <> 'string'
      or (v_ticket.survey_version = 'post-game-ja-v2' and v_key = 'q12') then
      raise exception using errcode = '22023', message = 'Unexpected answer field';
    end if;
  end loop;
  for v_number in 1..14 loop
    v_key := 'q' || v_number; v_value := p_answers->>v_key;
    if v_number <= 11 and (v_value is null or v_value not in ('1','2','3','4','5','skip')
      or (v_ticket.survey_version = 'post-game-ja-v2' and v_value = 'skip')) then
      raise exception using errcode = '22023', message = 'Select an answer for every required question';
    elsif v_number = 12 and v_ticket.survey_version = 'post-game-ja-v1' and (v_value is null or v_value not in ('none','little','some','strong','not_seen','skip')) then
      raise exception using errcode = '22023', message = 'Invalid anxiety answer';
    elsif v_number >= 13 and char_length(coalesce(v_value,'')) > (case when v_ticket.survey_version = 'post-game-ja-v2' then 300 else 2000 end) then
      raise exception using errcode = '22023', message = 'Free text is too long';
    end if;
  end loop;
  if v_expected is null then
    if p_respondent_id is not null then
      raise exception using errcode = '22023', message = 'Unexpected respondent ID';
    end if;
  else
    if p_respondent_id is null or p_respondent_id !~ '^[0-9]{1,10}$' then
      raise exception using errcode = '22023', message = 'Enter the respondent ID';
    end if;
    v_matches := p_respondent_id = v_expected;
    if not v_matches and not coalesce(p_confirm_respondent_mismatch, false) then
      raise exception using errcode = 'GQ409', message = 'Respondent ID differs from the guardian form';
    end if;
  end if;
  insert into public.survey_responses(participant_id,study_id,session_id,run_id,survey_version,answers,
      respondent_id,respondent_id_matches)
    values (v_ticket.participant_id,v_ticket.study_id,v_ticket.session_id,v_ticket.run_id,v_ticket.survey_version,p_answers,
      case when v_expected is not null then p_respondent_id end, v_matches)
    on conflict (participant_id,run_id,survey_version) do nothing returning * into v_response;
  if v_response.id is null then
    select * into v_response from public.survey_responses where participant_id = v_ticket.participant_id
      and run_id = v_ticket.run_id and survey_version = v_ticket.survey_version;
  end if;
  return jsonb_build_object('ok',true,'submitted',true,'receiptId',v_response.id);
end $$;
revoke all on function public.use_survey_ticket(text,jsonb,text,boolean) from public,anon,authenticated;
grant execute on function public.use_survey_ticket(text,jsonb,text,boolean) to service_role;
notify pgrst, 'reload schema';
commit;
