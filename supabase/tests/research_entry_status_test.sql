-- 募集終了 check for the guardian consent page. All synthetic records are rolled back.
begin;
do $$
declare
  v_study uuid := gen_random_uuid();
  v_hash text := encode(sha256('entry-qa'::bytea), 'hex');
  v_case record;
begin
  -- Only one open-play study can take entry; put this one in place of whatever the stack has.
  update public.studies set research_entry_enabled = false where entry_mode = 'open_play';
  insert into public.studies(id,study_key,status,entry_mode,research_entry_enabled,protocol_version)
    values(v_study,'entry-'||v_study,'active','open_play',true,'entry-qa');
  assert public.research_entry_status() = '{"open": true}'::jsonb, 'Open without any closing link';

  insert into public.research_status_links(token_hash,study_id,label,counted_from,target_count)
    values(v_hash,v_study,'QA 募集',now() - interval '1 hour',2);

  -- guardian ID, questionnaire ID: one completer and one in review (not counted)
  for v_case in select * from (values ('1111','1111'), ('2222','3333')) as t(guardian_id, entered) loop
    declare
      v_user uuid := gen_random_uuid(); v_participant uuid := gen_random_uuid(); v_session uuid := gen_random_uuid();
    begin
      insert into auth.users(id,created_at,updated_at,is_anonymous) values(v_user,now(),now(),true);
      insert into public.study_participants(id,study_id,auth_user_id,participant_code_hash,condition,protocol_version)
        values(v_participant,v_study,v_user,replace(gen_random_uuid()::text,'-','')||replace(gen_random_uuid()::text,'-',''),'QA','entry-qa');
      insert into public.guardian_consents(participant_id,study_id,consent_version,guardian_name,respondent_id,
          guardian_confirmed,guardian_consented_at,student_assented_at,recorded_at)
        values(v_participant,v_study,'qa','募集QA保護者',v_case.guardian_id,true,now(),now(),now());
      insert into public.game_sessions(id,user_id,install_id,participant_id) values(v_session,v_user,gen_random_uuid()::text,v_participant);
      insert into public.survey_responses(participant_id,study_id,session_id,run_id,survey_version,answers,respondent_id,respondent_id_matches)
        values(v_participant,v_study,v_session,gen_random_uuid(),'post-game-ja-v2','{}'::jsonb,v_case.entered,v_case.entered = v_case.guardian_id);
    end;
  end loop;
  assert (public.research_status(v_hash)->>'completed')::int = 1, 'Fixture: one completer, one in review';

  update public.research_status_links set target_count = 1 where token_hash = v_hash;
  assert (public.research_entry_status()->>'open')::boolean, 'A link that is not marked closes_entry never closes entry';

  update public.research_status_links set closes_entry = true, target_count = 2 where token_hash = v_hash;
  assert (public.research_entry_status()->>'open')::boolean, 'Below the target entry stays open (review is not counted)';

  update public.research_status_links set target_count = 1 where token_hash = v_hash;
  assert public.research_entry_status() = '{"open": false}'::jsonb, 'Reaching the target closes entry and returns only the flag';

  update public.research_status_links set revoked_at = now() where token_hash = v_hash;
  assert (public.research_entry_status()->>'open')::boolean, 'A revoked link no longer closes entry';

  update public.studies set research_entry_enabled = false where id = v_study;
  assert not (public.research_entry_status()->>'open')::boolean, 'A study that takes no entry shows 募集終了';

  assert has_function_privilege('anon','public.research_entry_status()','execute'), 'The public page may ask';
  assert not has_function_privilege('anon','public.research_status(text)','execute'), 'Counts and IDs stay service-only';
end $$;
rollback;
