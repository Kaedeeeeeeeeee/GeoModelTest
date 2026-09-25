-- Isolated boundary checks. All synthetic records are rolled back.
begin;
do $$
declare
  v_study uuid := gen_random_uuid();
  v_user uuid := gen_random_uuid();
  v_participant uuid := gen_random_uuid();
  v_session uuid := gen_random_uuid();
  v_run uuid := gen_random_uuid();
  v_token text := replace(gen_random_uuid()::text,'-','') || replace(gen_random_uuid()::text,'-','');
  v_answers jsonb := '{}'::jsonb;
  v_result jsonb;
  v_receipt text;
  v_key text;
  v_before bigint;
begin
  select count(*) into v_before from public.survey_responses;
  insert into auth.users(id,created_at,updated_at,is_anonymous) values(v_user,now(),now(),true);
  insert into public.studies(id,study_key,status,research_entry_enabled,protocol_version)
    values(v_study,'survey-limits-'||v_study,'development',true,'survey-limits-qa');
  insert into public.study_participants(id,study_id,auth_user_id,participant_code_hash,condition,protocol_version)
    values(v_participant,v_study,v_user,v_token,'QA','survey-limits-qa');
  insert into public.game_sessions(id,user_id,install_id,participant_id)
    values(v_session,v_user,gen_random_uuid()::text,v_participant);
  insert into public.survey_tickets(token_hash,participant_id,study_id,session_id,run_id,survey_version)
    values(v_token,v_participant,v_study,v_session,v_run,'post-game-ja-v2');
  for i in 1..11 loop v_answers := v_answers || jsonb_build_object('q'||i,'3'); end loop;
  foreach v_key in array array['q13','q14'] loop
    begin
      perform public.use_survey_ticket(v_token,v_answers||jsonb_build_object(v_key,repeat('あ',301)));
      raise exception '301-character answer incorrectly accepted for %',v_key;
    exception when sqlstate '22023' then
      if sqlerrm <> 'Free text is too long' then raise; end if;
    end;
  end loop;
  begin
    perform public.use_survey_ticket(v_token,v_answers||jsonb_build_object('q7','skip'));
    raise exception 'Removed skip choice incorrectly accepted';
  exception when sqlstate '22023' then
    if sqlerrm <> 'Select an answer for every required question' then raise; end if;
  end;
  v_result := public.use_survey_ticket(v_token,v_answers||jsonb_build_object('q13',repeat('あ',300),'q14',repeat('い',300)));
  assert v_result->>'submitted' = 'true','300-character answers must be accepted';
  v_receipt := v_result->>'receiptId';
  v_result := public.use_survey_ticket(v_token,v_answers);
  assert v_result->>'receiptId' = v_receipt,'Retry must retain the first response';
  assert (select char_length(answers->>'q13') from public.survey_responses where id=v_receipt::uuid)=300,'First answer must remain unchanged';
  update public.survey_tickets set run_id=gen_random_uuid() where token_hash=v_token;
  v_result := public.use_survey_ticket(v_token,v_answers);
  assert v_result->>'submitted' = 'true','Both optional text fields may be omitted';
  assert (select count(*) from public.survey_responses)=v_before+2,'Only the two synthetic responses should be added';
  assert not has_function_privilege('anon','public.use_survey_ticket(text,jsonb)','execute');
  assert not has_function_privilege('authenticated','public.use_survey_ticket(text,jsonb)','execute');
  assert has_function_privilege('service_role','public.use_survey_ticket(text,jsonb)','execute');
end $$;
select 'PASS: 300 accepted, 301 rejected for both fields, skip rejected, blank text allowed, retry unchanged, permissions preserved' as verification;
rollback;
