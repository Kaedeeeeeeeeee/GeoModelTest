begin;
create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;
-- 2026-11-26: an identity bound to the closed Testee study must get its own error so the game can start afresh.
select plan(6);
insert into auth.users(id,created_at,updated_at,is_anonymous) values
('e4444444-4444-4444-8444-444444444441',now(),now(),true),
('e4444444-4444-4444-8444-444444444442',now(),now(),true);
insert into study_participants(study_id,auth_user_id,entry_mode,condition,protocol_version,cohort,status,activated_at)
  select id,'e4444444-4444-4444-8444-444444444441','open_play','game',protocol_version,'open-play','completed',now()
  from studies where study_key='geoquest-open-play-2026-09';
select throws_ok($$select activate_open_play_participant('e4444444-4444-4444-8444-444444444441')$$,'GS410',
  'Identity belongs to a closed study','a closed-study identity is told to start afresh while another study is open');
select is((select count(*) from study_participants where auth_user_id='e4444444-4444-4444-8444-444444444441'),1::bigint,
  'the closed-study participant is kept as it was');
select ok((select study_id=(select id from studies where study_key='geoquest-open-play-2026-09')
  from study_participants where auth_user_id='e4444444-4444-4444-8444-444444444441'),'its binding is not moved');

update study_participants set withdrawn_at=now() where auth_user_id='e4444444-4444-4444-8444-444444444441';
select throws_ok($$select activate_open_play_participant('e4444444-4444-4444-8444-444444444441')$$,'42501',
  'Participant is not eligible','a withdrawal is never answered with a fresh start');
update study_participants set withdrawn_at=null where auth_user_id='e4444444-4444-4444-8444-444444444441';

update studies set research_entry_enabled=false where study_key='geoquest-tamagawa-2026-12';
select throws_ok($$select activate_open_play_participant('e4444444-4444-4444-8444-444444444441')$$,'42501',
  'Participant is not eligible','with no open study the old answer stays');
update studies set research_entry_enabled=true where study_key='geoquest-tamagawa-2026-12';

update study_participants set protocol_version='wrong' where auth_user_id='e4444444-4444-4444-8444-444444444441';
update studies set research_entry_enabled=false where study_key='geoquest-tamagawa-2026-12';
update studies set research_entry_enabled=true where study_key='geoquest-open-play-2026-09';
select throws_ok($$select activate_open_play_participant('e4444444-4444-4444-8444-444444444441')$$,'42501',
  'Participant is not eligible','a protocol mismatch in an open study is not a closed study');
select * from finish();
rollback;
