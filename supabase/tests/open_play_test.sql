begin;
create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;
select plan(14);
insert into auth.users(id,created_at,updated_at,is_anonymous) values
('b1111111-1111-4111-8111-111111111111',now(),now(),true),
('b1111111-1111-4111-8111-111111111112',now(),now(),true),
('b1111111-1111-4111-8111-111111111113',now(),now(),false);
select ok(not has_function_privilege('anon','public.activate_open_play_participant(uuid)','EXECUTE'),'anonymous API cannot select another identity');
select ok(not has_function_privilege('authenticated','public.activate_open_play_participant(uuid)','EXECUTE'),'authenticated API cannot bypass the Edge function');
select ok(has_function_privilege('service_role','public.activate_open_play_participant(uuid)','EXECUTE'),'verified server can activate');
select throws_ok($$select activate_open_play_participant(null)$$,'42501','Anonymous game identity required','null identity rejected');
create temp table activated as select activate_open_play_participant('b1111111-1111-4111-8111-111111111111') as result;
select is((select result->>'ok' from activated),'true','ordinary player activates without code');
select is(activate_open_play_participant('b1111111-1111-4111-8111-111111111111')->>'participantId',(select result->>'participantId' from activated),'repeat activation is idempotent');
select ok((select participant_code_hash is null and consent_version is null and guardian_consent_at is null and student_assent_at is null from study_participants where auth_user_id='b1111111-1111-4111-8111-111111111111'),'no invented code or consent');
select is((select count(*) from survey_responses where participant_id=(select (result->>'participantId')::uuid from activated)),0::bigint,'activation is not a questionnaire response');
select ok(research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'open play can upload completed game');
update studies set research_entry_enabled=false where entry_mode='open_play';
select throws_ok($$select activate_open_play_participant('b1111111-1111-4111-8111-111111111112')$$,'42501','Study entry is closed','closed study rejects new players');
select ok(not research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'closed study also stops existing access');
update studies set research_entry_enabled=true where study_key='geoquest-open-play-2026-09';
update study_participants set withdrawn_at=now() where auth_user_id='b1111111-1111-4111-8111-111111111111';
select throws_ok($$select activate_open_play_participant('b1111111-1111-4111-8111-111111111111')$$,'42501','Participant is not eligible','withdrawal cannot be bypassed by automatic enrollment');
update study_participants set withdrawn_at=null,protocol_version='wrong' where auth_user_id='b1111111-1111-4111-8111-111111111111';
select ok(not research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'protocol mismatch rejected');
update study_participants set protocol_version='open-play-v1' where auth_user_id='b1111111-1111-4111-8111-111111111111';
update studies set retention_until=now()-interval '1 second' where entry_mode='open_play';
select ok(not research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'expired study rejected');
select * from finish();
rollback;
