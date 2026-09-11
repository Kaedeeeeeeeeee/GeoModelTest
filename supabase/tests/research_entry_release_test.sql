begin;
create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;
select plan(14);
insert into auth.users(id,created_at,updated_at,is_anonymous) values
('a1111111-1111-4111-8111-111111111111',now(),now(),true),
('a1111111-1111-4111-8111-111111111112',now(),now(),true);
insert into studies(id,study_key,status,research_entry_enabled,protocol_version) values
('a2222222-2222-4222-8222-222222222222','release-contract','active',true,'release-test');
insert into study_participants(id,study_id,participant_code_hash,condition,protocol_version) values
('a3333333-3333-4333-8333-333333333333','a2222222-2222-4222-8222-222222222222',repeat('e',64),'game','release-test');
select ok(not has_function_privilege('anon','public.activate_research_participant(text,uuid)','EXECUTE'),'anonymous Data API cannot activate participants');
select ok(not has_function_privilege('authenticated','public.activate_research_participant(text,uuid)','EXECUTE'),'players cannot bypass the verified Edge Function');
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'active study requires recorded consent');
select throws_ok($$select activate_research_participant(repeat('e',64),'a1111111-1111-4111-8111-111111111111')$$,'42501','Participant is not eligible','missing consent cannot activate');
update study_participants set consent_version='synthetic-test-only',guardian_consent_at=now(),student_assent_at=now()+interval '1 day' where id='a3333333-3333-4333-8333-333333333333';
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'future consent rejected');
update study_participants set student_assent_at=now() where id='a3333333-3333-4333-8333-333333333333';
select ok(research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'eligible active participant accepted');
select is(activate_research_participant(repeat('e',64),'a1111111-1111-4111-8111-111111111111')->>'participantId','a3333333-3333-4333-8333-333333333333','release activation binds the verified identity');
select throws_ok($$select activate_research_participant(repeat('e',64),'a1111111-1111-4111-8111-111111111112')$$,'23505','Participation code is already bound','another identity cannot claim the code');
update studies set retention_until=now()-interval '1 second' where study_key='release-contract';
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'expired study rejected');
update studies set retention_until=null,research_entry_enabled=false where study_key='release-contract';
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'closed entry rejected');
update studies set research_entry_enabled=true where study_key='release-contract';
update study_participants set withdrawn_at=now() where id='a3333333-3333-4333-8333-333333333333';
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'withdrawn participant rejected');
update studies set status='development' where study_key='release-contract';
update study_participants set withdrawn_at=null,guardian_consent_at=null,student_assent_at=null,consent_version=null where id='a3333333-3333-4333-8333-333333333333';
select ok(research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'internal test entry stays available without fabricated consent');
update study_participants set protocol_version='different' where id='a3333333-3333-4333-8333-333333333333';
select ok(not research_participant_is_eligible('a3333333-3333-4333-8333-333333333333'),'protocol mismatch rejected');
select ok(has_function_privilege('service_role','public.activate_research_participant(text,uuid)','EXECUTE'),'server role can activate');
select * from finish();
rollback;
