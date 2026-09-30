-- Respondent ID double check in the questionnaire. All synthetic records are rolled back.
begin;
do $$
declare
  v_study uuid := gen_random_uuid();
  v_answers jsonb := '{}'::jsonb;
  v_result jsonb;
  v_guardian uuid; v_plain uuid; v_token text; v_plain_token text;
  v_row public.survey_responses%rowtype;
begin
  for i in 1..11 loop v_answers := v_answers || jsonb_build_object('q'||i,'3'); end loop;
  insert into public.studies(id,study_key,status,research_entry_enabled,protocol_version)
    values(v_study,'survey-respondent-'||v_study,'development',true,'survey-respondent-qa');

  -- Participant A has a 保護者同意 form with p1234567; participant B has none.
  for i in 1..2 loop
    declare
      v_user uuid := gen_random_uuid(); v_participant uuid := gen_random_uuid(); v_session uuid := gen_random_uuid();
      v_hash text := replace(gen_random_uuid()::text,'-','') || replace(gen_random_uuid()::text,'-','');
    begin
      insert into auth.users(id,created_at,updated_at,is_anonymous) values(v_user,now(),now(),true);
      insert into public.study_participants(id,study_id,auth_user_id,participant_code_hash,condition,protocol_version)
        values(v_participant,v_study,v_user,v_hash,'QA','survey-respondent-qa');
      insert into public.game_sessions(id,user_id,install_id,participant_id) values(v_session,v_user,gen_random_uuid()::text,v_participant);
      insert into public.survey_tickets(token_hash,participant_id,study_id,session_id,run_id,survey_version)
        values(v_hash,v_participant,v_study,v_session,gen_random_uuid(),'post-game-ja-v2');
      if i = 1 then
        insert into public.guardian_consents(participant_id,study_id,consent_version,guardian_name,respondent_id,
            guardian_confirmed,guardian_consented_at,student_assented_at)
          values(v_participant,v_study,'qa','QA','p1234567',true,now(),now());
        v_guardian := v_participant; v_token := v_hash;
      else
        v_plain := v_participant; v_plain_token := v_hash;
      end if;
    end;
  end loop;

  v_result := public.use_survey_ticket(v_token);
  assert (v_result->>'respondentIdRequired')::boolean, 'Guardian-form participants must be asked for the ID';
  v_result := public.use_survey_ticket(v_plain_token);
  assert not (v_result->>'respondentIdRequired')::boolean, 'Participants without a guardian form must not be asked';

  begin
    perform public.use_survey_ticket(v_token,v_answers);
    raise exception 'Missing respondent ID incorrectly accepted';
  exception when sqlstate '22023' then
    if sqlerrm <> 'Enter the respondent ID' then raise; end if;
  end;
  begin
    perform public.use_survey_ticket(v_token,v_answers,'P1234567');
    raise exception 'Unnormalized respondent ID incorrectly accepted';
  exception when sqlstate '22023' then
    if sqlerrm <> 'Enter the respondent ID' then raise; end if;
  end;
  begin
    perform public.use_survey_ticket(v_token,v_answers,'p7654321');
    raise exception 'Unconfirmed different ID incorrectly accepted';
  exception when sqlstate 'GQ409' then null;
  end;
  assert not exists(select 1 from public.survey_responses where participant_id=v_guardian),
    'A rejected submission must not store anything';

  v_result := public.use_survey_ticket(v_token,v_answers,'p7654321',true);
  assert (v_result->>'submitted')::boolean, 'A confirmed different ID must be accepted';
  select * into v_row from public.survey_responses where participant_id=v_guardian;
  assert v_row.respondent_id = 'p7654321' and v_row.respondent_id_matches = false, 'The mismatch must be kept for review';
  assert not (v_row.answers ? 'respondentId'), 'The ID is stored beside the answers, not inside them';

  -- A second run for the same participant with the matching ID.
  update public.survey_tickets set run_id=gen_random_uuid() where token_hash=v_token;
  v_result := public.use_survey_ticket(v_token,v_answers,'p1234567');
  assert (select respondent_id_matches from public.survey_responses where id=(v_result->>'receiptId')::uuid),
    'A matching ID must be recorded as matching';

  begin
    perform public.use_survey_ticket(v_plain_token,v_answers,'p1234567');
    raise exception 'ID incorrectly accepted without a guardian form';
  exception when sqlstate '22023' then
    if sqlerrm <> 'Unexpected respondent ID' then raise; end if;
  end;
  v_result := public.use_survey_ticket(v_plain_token,v_answers);
  select * into v_row from public.survey_responses where id=(v_result->>'receiptId')::uuid;
  assert v_row.respondent_id is null and v_row.respondent_id_matches is null, 'No ID columns without a guardian form';

  assert not has_function_privilege('anon','public.use_survey_ticket(text,jsonb,text,boolean)','execute');
  assert not has_function_privilege('authenticated','public.use_survey_ticket(text,jsonb,text,boolean)','execute');
  assert has_function_privilege('service_role','public.use_survey_ticket(text,jsonb,text,boolean)','execute');
  assert not exists(select 1 from pg_proc where proname='use_survey_ticket' and pronargs=2), 'The old two-argument function must be gone';
end $$;
select 'PASS: ID asked only with a guardian form; missing/invalid rejected; different ID needs confirmation and is kept; matching recorded; permissions preserved' as verification;
rollback;
