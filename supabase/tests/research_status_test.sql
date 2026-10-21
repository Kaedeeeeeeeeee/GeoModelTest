-- Read-only status page for Testee. All synthetic records are rolled back.
begin;
do $$
declare
  v_study uuid := gen_random_uuid();
  v_other uuid := gen_random_uuid();
  v_hash text := encode(sha256('status-qa'::bytea), 'hex');
  v_revoked text := encode(sha256('status-qa-revoked'::bytea), 'hex');
  v_result jsonb;
  v_ids text[];
  v_participants uuid[] := '{}';
  v_case record;
begin
  insert into public.studies(id,study_key,status,research_entry_enabled,protocol_version)
    values(v_study,'status-'||v_study,'development',true,'status-qa'),
          (v_other,'status-other-'||v_other,'development',true,'status-qa');
  insert into public.research_status_links(token_hash,study_id,label,counted_from,target_count)
    values(v_hash,v_study,'QA 回収状況',now() - interval '1 hour',20),
          (v_revoked,v_study,'QA 作廃',now() - interval '1 hour',20);
  update public.research_status_links set revoked_at = now() where token_hash = v_revoked;

  -- guardian ID, recorded before the cutoff, issued a ticket, questionnaire IDs re-entered (null = before the double check), study
  for v_case in
    select * from (values
      ('1111', false, true,  array['1111'],         v_study),  -- plain completer
      ('2222', false, true,  array['3333'],         v_study),  -- questionnaire ID differs from the guardian form: review
      ('4444', false, true,  array[null]::text[],   v_study),  -- response from before the double check
      ('5555', false, true,  array['5555','6666'],  v_study),  -- answered twice, the second time with another ID: review
      ('1111', false, true,  array['1111'],         v_study),  -- same child in a second browser: counted once
      ('7777', false, true,  array[]::text[],       v_study),  -- cleared the game, no questionnaire yet
      ('8888', false, false, array[]::text[],       v_study),  -- consented only
      ('9999', true,  true,  array['9999'],         v_study),  -- test play before the cutoff
      ('9998', false, true,  array['9998'],         v_other)   -- another study
    ) as t(guardian_id, before_cutoff, ticket, entered, study_id)
  loop
    declare
      v_user uuid := gen_random_uuid(); v_participant uuid := gen_random_uuid(); v_session uuid := gen_random_uuid();
      v_code text := replace(gen_random_uuid()::text,'-','') || replace(gen_random_uuid()::text,'-','');
      v_run uuid; v_entered text;
    begin
      insert into auth.users(id,created_at,updated_at,is_anonymous) values(v_user,now(),now(),true);
      insert into public.study_participants(id,study_id,auth_user_id,participant_code_hash,condition,protocol_version)
        values(v_participant,v_case.study_id,v_user,v_code,'QA','status-qa');
      insert into public.guardian_consents(participant_id,study_id,consent_version,guardian_name,respondent_id,
          guardian_confirmed,guardian_consented_at,student_assented_at,recorded_at)
        values(v_participant,v_case.study_id,'qa','状況QA保護者',v_case.guardian_id,true,now(),now(),
          case when v_case.before_cutoff then now() - interval '2 hours' else now() end);
      insert into public.game_sessions(id,user_id,install_id,participant_id) values(v_session,v_user,gen_random_uuid()::text,v_participant);
      if v_case.ticket then
        insert into public.survey_tickets(token_hash,participant_id,study_id,session_id,run_id,survey_version)
          values(v_code,v_participant,v_case.study_id,v_session,gen_random_uuid(),'post-game-ja-v2');
      end if;
      foreach v_entered in array v_case.entered loop
        v_run := gen_random_uuid();
        insert into public.survey_responses(participant_id,study_id,session_id,run_id,survey_version,answers,respondent_id,respondent_id_matches)
          values(v_participant,v_case.study_id,v_session,v_run,'post-game-ja-v2','{}'::jsonb,v_entered,
            case when v_entered is not null then v_entered = v_case.guardian_id end);
      end loop;
      v_participants := v_participants || v_participant;
    end;
  end loop;

  v_result := public.research_status(v_hash);
  assert v_result->>'label' = 'QA 回収状況' and (v_result->>'target')::int = 20, 'Link settings must be returned';
  assert (v_result->>'consented')::int = 7, 'Only guardian forms of this study from the cutoff on count: '||(v_result->>'consented');
  assert (v_result->>'gameCleared')::int = 6, 'Cleared means a questionnaire ticket was issued: '||(v_result->>'gameCleared');
  assert (v_result->>'surveyed')::int = 5, 'Participants with a submitted questionnaire: '||(v_result->>'surveyed');
  select array_agg(x->>'respondentId' order by x->>'respondentId') into v_ids from jsonb_array_elements(v_result->'completers') x;
  assert v_ids = array['1111','4444'], 'Only participants whose IDs agree are completers: '||array_to_string(v_ids,',');
  assert (v_result->>'completed')::int = 2, 'The same ID from two browsers counts once';
  assert not exists (select 1 from jsonb_array_elements(v_result->'completers') x where x ? 'guardianFormIds'),
    'Completers carry no second ID any more';
  select array_agg(x->>'guardianFormId' order by x->>'guardianFormId') into v_ids from jsonb_array_elements(v_result->'review') x;
  assert v_ids = array['2222','5555'], 'Any ID differing from the guardian form goes to review: '||array_to_string(v_ids,',');
  assert (select x->'entries' from jsonb_array_elements(v_result->'review') x where x->>'guardianFormId' = '2222')->0->>'respondentId' = '3333',
    'Review shows the questionnaire ID beside the guardian-form ID';
  assert (select jsonb_array_length(x->'entries') from jsonb_array_elements(v_result->'review') x where x->>'guardianFormId' = '5555') = 2,
    'Every questionnaire ID is listed';
  assert (select x->>'kind' from jsonb_array_elements(v_result->'review') x where x->>'guardianFormId' = '2222') = 'guardianMismatch'
    and (select x->>'kind' from jsonb_array_elements(v_result->'review') x where x->>'guardianFormId' = '5555') = 'multipleAnswers',
    'Review tells a single differing ID apart from answering again with another ID';
  assert v_result::text not like '%状況QA保護者%', 'Guardian names must never leave the database';
  for i in 1..array_length(v_participants,1) loop
    assert v_result::text not like '%'||v_participants[i]||'%', 'Participant UUIDs must not be returned';
  end loop;

  begin
    perform public.research_status(v_revoked);
    raise exception 'Revoked link incorrectly accepted';
  exception when sqlstate '42501' then null;
  end;
  begin
    perform public.research_status(encode(sha256('unknown'::bytea), 'hex'));
    raise exception 'Unknown link incorrectly accepted';
  exception when sqlstate '42501' then null;
  end;
  assert not has_function_privilege('anon','public.research_status(text)','execute')
    and not has_function_privilege('authenticated','public.research_status(text)','execute'), 'Only service_role may read the status';
  assert not has_table_privilege('anon','public.research_status_links','select'), 'Link hashes are service-only';

  -- Target notice: claimed once when the target is reached, released when the email fails.
  assert not (public.claim_research_status_target(v_hash)->>'notify')::boolean, 'Below the target nothing is claimed';
  assert (select target_notified_at from public.research_status_links where token_hash = v_hash) is null, 'No claim below the target';
  update public.research_status_links set target_count = 2 where token_hash = v_hash;
  v_result := public.claim_research_status_target(v_hash);
  assert (v_result->>'notify')::boolean and (v_result->>'completed')::int = 2, 'Reaching the target claims the notice with the status';
  assert not (public.claim_research_status_target(v_hash)->>'notify')::boolean, 'A claimed notice is not claimed again';
  perform public.release_research_status_target(v_hash);
  assert (public.claim_research_status_target(v_hash)->>'notify')::boolean, 'A released notice is retried';
  begin
    perform public.claim_research_status_target(v_revoked);
    raise exception 'Revoked link incorrectly claimed';
  exception when sqlstate '42501' then null;
  end;
  assert not has_function_privilege('anon','public.claim_research_status_target(text)','execute')
    and not has_function_privilege('anon','public.release_research_status_target(text)','execute'), 'Only service_role may claim notices';
end $$;
rollback;
