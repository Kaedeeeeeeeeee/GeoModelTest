begin;
create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;
select plan(19);
insert into auth.users(id,created_at,updated_at,is_anonymous) values
('d3333333-3333-4333-8333-333333333331',now(),now(),true),
('d3333333-3333-4333-8333-333333333332',now(),now(),true),
('d3333333-3333-4333-8333-333333333333',now(),now(),true);
create temp table assent as select jsonb_build_object(
  'consentVersion','student-ja-2026-11-11','studentAssented',true,'studentAssentedAt',now()-interval '1 minute') as body;
create function pg_temp.rejects(p_user uuid, p_patch jsonb) returns text language plpgsql as $$
begin
  perform public.activate_open_play_participant(p_user, (select body from assent) || p_patch);
  return 'accepted';
exception when others then
  return sqlstate;
end;
$$;

-- 2026-11-11: 玉川中 gets its own study; Testee's stops taking new players but keeps its data.
select ok((select not research_entry_enabled and requires_guardian_consent from studies where study_key='geoquest-open-play-2026-09'),
  'the Testee study takes no new players and keeps the guardian rule');
select ok((select status='active' and research_entry_enabled and entry_mode='open_play' and requires_student_assent and not requires_guardian_consent
  from studies where study_key='geoquest-tamagawa-2026-12'),'the 玉川中 study takes new players with student assent only');
select is((select count(*) from studies where entry_mode='open_play' and status='active' and research_entry_enabled),1::bigint,
  'exactly one open-play study takes new players');
select ok(not has_function_privilege('authenticated','public.record_student_assent(uuid,jsonb)','EXECUTE'),'players cannot record assent directly');
select ok(has_function_privilege('service_role','public.activate_open_play_participant(uuid,jsonb)','EXECUTE'),'verified server can activate');

select throws_ok($$select activate_open_play_participant('d3333333-3333-4333-8333-333333333331')$$,'GC001','Student assent required','new player needs the student assent');
select is((select count(*) from study_participants where auth_user_id='d3333333-3333-4333-8333-333333333331'),0::bigint,'rejected activation leaves no play record');
select is(pg_temp.rejects('d3333333-3333-4333-8333-333333333331','{"studentAssented":false}'),'GC002','unconfirmed assent rejected');
select is(pg_temp.rejects('d3333333-3333-4333-8333-333333333331','{"consentVersion":""}'),'GC002','consent version required');
select is(pg_temp.rejects('d3333333-3333-4333-8333-333333333331','{"studentAssentedAt":"not a date"}'),'GC002','malformed timestamp rejected');
select is(pg_temp.rejects('d3333333-3333-4333-8333-333333333331',jsonb_build_object('studentAssentedAt',now()+interval '2 days')),'GC002','far-future device time rejected');

create temp table activated as select activate_open_play_participant('d3333333-3333-4333-8333-333333333331',(select body from assent)) as result;
select is((select result->>'ok' from activated),'true','student assent activates the player');
select is((select s.study_key from study_participants p join studies s on s.id=p.study_id where p.auth_user_id='d3333333-3333-4333-8333-333333333331'),
  'geoquest-tamagawa-2026-12','new players join the 玉川中 study');
select ok((select consent_version='student-ja-2026-11-11' and student_assent_at<=now() and guardian_consent_at is null
  from study_participants where auth_user_id='d3333333-3333-4333-8333-333333333331'),'assent stored without a guardian time');
select is((select count(*) from guardian_consents c join study_participants p on p.id=c.participant_id where p.auth_user_id='d3333333-3333-4333-8333-333333333331'),
  0::bigint,'no guardian form is stored');
select ok(research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'assenting player can upload and answer');
select is(activate_open_play_participant('d3333333-3333-4333-8333-333333333331')->>'participantId',(select result->>'participantId' from activated),
  'later activation reuses the stored assent');

-- Check effects in later statements: a statement cannot see its own function's writes.
create temp table skewed as select activate_open_play_participant('d3333333-3333-4333-8333-333333333332',
  (select body from assent)||jsonb_build_object('studentAssentedAt',now()+interval '3 minutes')) as result;
select ok((select result->>'ok'='true' from skewed)
  and (select student_assent_at<=now() from study_participants where auth_user_id='d3333333-3333-4333-8333-333333333332'),'small clock skew accepted and capped');

-- A play record without assent (inserted directly) cannot upload until it assents.
insert into study_participants(study_id,auth_user_id,entry_mode,condition,protocol_version,cohort,status,activated_at)
  select id,'d3333333-3333-4333-8333-333333333333','open_play','game',protocol_version,'open-play','active',now() from studies where study_key='geoquest-tamagawa-2026-12';
select ok(not research_participant_is_eligible((select id from study_participants where auth_user_id='d3333333-3333-4333-8333-333333333333')),
  'a record without assent cannot upload');
select * from finish();
rollback;
