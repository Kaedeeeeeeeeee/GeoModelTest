begin;
create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;
select plan(31);
insert into auth.users(id,created_at,updated_at,is_anonymous) values
('c2222222-2222-4222-8222-222222222221',now(),now(),true),
('c2222222-2222-4222-8222-222222222222',now(),now(),true),
('c2222222-2222-4222-8222-222222222223',now(),now(),true),
('c2222222-2222-4222-8222-222222222224',now(),now(),true),
('c2222222-2222-4222-8222-222222222225',now(),now(),true);
create temp table form as select jsonb_build_object(
  'consentVersion','guardian-ja-2026-09-29','guardianAgreed',true,'guardianConfirmed',true,
  'guardianName','山田　花子','respondentId','p1234567','guardianConsentedAt',now()-interval '5 minutes',
  'studentAssented',true,'studentAssentedAt',now()-interval '1 minute') as body;
create function pg_temp.rejects(p_user uuid, p_patch jsonb) returns text language plpgsql as $$
begin
  perform public.activate_open_play_participant(p_user, (select body from form) || p_patch);
  return 'accepted';
exception when others then
  return sqlstate;
end;
$$;

select ok((select requires_guardian_consent from studies where study_key='geoquest-open-play-2026-09'),'open play requires the online guardian form');
select ok((select relrowsecurity from pg_class where oid='public.guardian_consents'::regclass),'guardian form table has RLS');
select ok(not has_table_privilege('anon','public.guardian_consents','SELECT'),'anonymous API cannot read guardian names');
select ok(not has_table_privilege('authenticated','public.guardian_consents','SELECT,INSERT,UPDATE'),'players cannot read or write guardian forms');
select ok(not has_function_privilege('authenticated','public.record_guardian_consent(uuid,uuid,jsonb)','EXECUTE'),'players cannot record consent directly');
select ok(not has_function_privilege('authenticated','public.activate_open_play_participant(uuid,jsonb)','EXECUTE'),'players cannot bypass the Edge function');
select ok(has_function_privilege('service_role','public.activate_open_play_participant(uuid,jsonb)','EXECUTE'),'verified server can activate');

select throws_ok($$select activate_open_play_participant('c2222222-2222-4222-8222-222222222221')$$,'GC001','Guardian consent required','new player needs the guardian form');
select is((select count(*) from study_participants where auth_user_id='c2222222-2222-4222-8222-222222222221'),0::bigint,'rejected activation leaves no play record');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"guardianAgreed":false}'),'GC002','declined consent rejected');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"guardianConfirmed":null}'),'GC002','guardian confirmation required');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"studentAssented":false}'),'GC002','student assent required');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"guardianName":""}'),'GC002','empty guardian name rejected');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221',jsonb_build_object('guardianName','山田'||chr(10)||'花子')),'GC002','control characters rejected');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"respondentId":"P1234567"}'),'GC002','respondent ID must arrive normalized');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"respondentId":"p123456"}'),'GC002','respondent ID is p and exactly 7 digits');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221','{"guardianConsentedAt":"not a date"}'),'GC002','malformed timestamp rejected');
select is(pg_temp.rejects('c2222222-2222-4222-8222-222222222221',jsonb_build_object('studentAssentedAt',now()+interval '2 days')),'GC002','far-future device time rejected');
select is((select count(*) from guardian_consents c join study_participants p on p.id=c.participant_id where p.auth_user_id='c2222222-2222-4222-8222-222222222221'),0::bigint,'no partial forms stored');

create temp table activated as select activate_open_play_participant('c2222222-2222-4222-8222-222222222221',(select body from form)) as result;
select is((select result->>'ok' from activated),'true','valid form activates the player');
select is((select guardian_name||'/'||respondent_id from guardian_consents where participant_id=(select (result->>'participantId')::uuid from activated)),'山田　花子/p1234567','form stored with name and respondent ID');
select ok((select consent_version='guardian-ja-2026-09-29' and guardian_consent_at<=now() and student_assent_at<=now() from study_participants where auth_user_id='c2222222-2222-4222-8222-222222222221'),'participant consent metadata set');
select ok(research_participant_is_eligible((select (result->>'participantId')::uuid from activated)),'consented player can upload');
select is(activate_open_play_participant('c2222222-2222-4222-8222-222222222221')->>'participantId',(select result->>'participantId' from activated),'later activation reuses the stored form');
select is((activate_open_play_participant('c2222222-2222-4222-8222-222222222221',(select body from form)||'{"guardianName":"別人","respondentId":"p7654321"}'))->>'ok','true','repeat form is accepted');
select is((select guardian_name||'/'||respondent_id from guardian_consents where participant_id=(select (result->>'participantId')::uuid from activated)),'山田　花子/p1234567','first form is kept');

-- Check effects in later statements: a statement cannot see its own function's writes.
create temp table skewed as select activate_open_play_participant('c2222222-2222-4222-8222-222222222222',(select body from form)||jsonb_build_object('guardianConsentedAt',now()+interval '2 minutes','studentAssentedAt',now()+interval '3 minutes')) as result;
select ok((select result->>'ok'='true' from skewed)
  and (select guardian_consent_at<=now() and student_assent_at<=now() from study_participants where auth_user_id='c2222222-2222-4222-8222-222222222222'),'small clock skew accepted and capped');

-- A play record created before the form existed (earlier test builds) must consent before uploading.
insert into study_participants(study_id,auth_user_id,entry_mode,condition,protocol_version,cohort,status,activated_at)
  select id,'c2222222-2222-4222-8222-222222222223','open_play','game',protocol_version,'open-play','active',now() from studies where study_key='geoquest-open-play-2026-09';
select ok(not research_participant_is_eligible((select id from study_participants where auth_user_id='c2222222-2222-4222-8222-222222222223')),'earlier record without consent cannot upload');
create temp table earlier as select activate_open_play_participant('c2222222-2222-4222-8222-222222222223',(select body from form)) as result;
select ok((select result->>'ok'='true' from earlier)
  and research_participant_is_eligible((select id from study_participants where auth_user_id='c2222222-2222-4222-8222-222222222223')),'earlier record becomes eligible after the form');

select activate_open_play_participant('c2222222-2222-4222-8222-222222222224',(select body from form));
update study_participants set withdrawn_at=now() where auth_user_id='c2222222-2222-4222-8222-222222222224';
select throws_ok($$select activate_open_play_participant('c2222222-2222-4222-8222-222222222224')$$,'42501','Participant is not eligible','withdrawal still blocks re-entry');

update studies set requires_guardian_consent=false where study_key='geoquest-open-play-2026-09';
select is((activate_open_play_participant('c2222222-2222-4222-8222-222222222225'))->>'ok','true','studies without the requirement keep code-free entry');
select * from finish();
rollback;
