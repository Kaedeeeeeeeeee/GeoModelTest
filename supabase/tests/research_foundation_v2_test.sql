begin;

create extension if not exists pgtap with schema extensions;
set local search_path = public, extensions;

select plan(18);

select has_table('public', 'studies', 'studies table exists');
select has_table('public', 'study_participants', 'study_participants table exists');
select has_table('public', 'quiz_attempts', 'quiz_attempts table exists');
select has_table('public', 'current_progress', 'current_progress table exists');
select has_table('public', 'progress_history', 'progress_history table exists');
select ok(not has_table_privilege('anon', 'public.studies', 'SELECT'), 'anon cannot read studies directly');
select ok(
  not has_table_privilege('authenticated', 'public.quiz_attempts', 'INSERT'),
  'authenticated clients cannot insert quiz attempts directly'
);

insert into auth.users (id, created_at, updated_at, is_anonymous)
values ('99999999-9999-4999-8999-999999999999', now(), now(), true);

insert into public.studies (id, study_key, status, research_entry_enabled, protocol_version)
values
  ('22222222-2222-4222-8222-222222222222', 'foundation-test', 'development', true, 'protocol-v1'),
  ('22222222-2222-4222-8222-222222222223', 'locked-test', 'locked', false, 'protocol-v1');

insert into public.study_participants (
  id, study_id, participant_code_hash, condition, protocol_version, status, withdrawn_at
) values
  (
    '11111111-1111-4111-8111-111111111111',
    '22222222-2222-4222-8222-222222222222',
    repeat('a', 64), 'A', 'protocol-v1', 'ready', null
  ),
  (
    '11111111-1111-4111-8111-111111111112',
    '22222222-2222-4222-8222-222222222223',
    repeat('b', 64), 'A', 'protocol-v1', 'ready', null
  ),
  (
    '11111111-1111-4111-8111-111111111113',
    '22222222-2222-4222-8222-222222222222',
    repeat('c', 64), 'A', 'protocol-v1', 'withdrawn', now()
  );

select is(
  public.activate_development_participant(
    repeat('a', 64),
    '99999999-9999-4999-8999-999999999999'
  )->>'participantId',
  '11111111-1111-4111-8111-111111111111',
  'development participant is atomically activated'
);
select throws_ok(
  $$select public.activate_development_participant(repeat('d', 64), '99999999-9999-4999-8999-999999999999')$$,
  'P0002', 'Participation code was not found', 'unknown participation code is rejected'
);
select throws_ok(
  $$select public.activate_development_participant(repeat('b', 64), '99999999-9999-4999-8999-999999999999')$$,
  '42501', 'Study entry is closed', 'locked study is rejected'
);
select throws_ok(
  $$select public.activate_development_participant(repeat('c', 64), '99999999-9999-4999-8999-999999999999')$$,
  '42501', 'Participant is not eligible', 'withdrawn participant is rejected'
);

do $$
begin
  for i in 1..2 loop
    perform public.ingest_research_batch(
      '99999999-9999-4999-8999-999999999999',
      '11111111-1111-4111-8111-111111111111',
      '22222222-2222-4222-8222-222222222222',
      '33333333-3333-4333-8333-333333333333',
      '88888888-8888-4888-8888-888888888888',
      'test-game', 'Editor', 'StandaloneOSX', 'Japanese', 'MainScene',
      'content-v1', 'research-route', 'protocol-v1', 'A',
      jsonb_build_array(jsonb_build_object(
        'id', '44444444-4444-4444-8444-444444444444',
        'name', 'session_started',
        'occurredAt', now(),
        'sceneName', 'MainScene',
        'props', '{}'::jsonb
      )),
      jsonb_build_array(jsonb_build_object(
        'eventId', '55555555-5555-4555-8555-555555555555',
        'runId', '66666666-6666-4666-8666-666666666666',
        'questionId', 'q.weathering_order',
        'questionVersion', 'story-formative-v1',
        'choiceId', 'q.weathering_order.correct_sequence',
        'attemptIndex', 1,
        'isCorrect', true,
        'usedHint', false,
        'responseTimeMs', 1200,
        'occurredAt', now(),
        'gameVersion', 'test-game',
        'contentVersion', 'content-v1',
        'storyRoute', 'research-route'
      )),
      jsonb_build_object(
        'eventId', '77777777-7777-4777-8777-777777777777',
        'updatedAt', now(),
        'currentScene', 'MainScene',
        'completedQuests', jsonb_build_array('quest-1'),
        'completedObjectives', '[]'::jsonb,
        'storyFlags', '[]'::jsonb,
        'unlockedToolIds', '[]'::jsonb,
        'inventoryCount', 1,
        'warehouseCount', 0,
        'encyclopediaDiscovered', 1,
        'encyclopediaTotal', 10,
        'payload', '{}'::jsonb
      ),
      null
    );
  end loop;
end;
$$;

select is(
  (select count(*) from public.telemetry_events where id = '44444444-4444-4444-8444-444444444444'),
  1::bigint,
  'retry does not duplicate telemetry events'
);
select is(
  (select count(*) from public.quiz_attempts where event_id = '55555555-5555-4555-8555-555555555555'),
  1::bigint,
  'retry does not duplicate quiz attempts'
);
select is(
  (select count(*) from public.progress_history where event_id = '77777777-7777-4777-8777-777777777777'),
  1::bigint,
  'retry does not duplicate progress history'
);
select is(
  (select count(*) from public.current_progress where participant_id = '11111111-1111-4111-8111-111111111111'),
  1::bigint,
  'current progress remains one atomic upsert row'
);

create function pg_temp.invalid_batch_rolls_back()
returns boolean
language plpgsql
as $$
begin
  begin
    perform public.ingest_research_batch(
      '99999999-9999-4999-8999-999999999999',
      '11111111-1111-4111-8111-111111111111',
      '22222222-2222-4222-8222-222222222222',
      '33333333-3333-4333-8333-333333333333',
      '88888888-8888-4888-8888-888888888888',
      'test-game', 'Editor', 'StandaloneOSX', 'Japanese', 'MainScene',
      'content-v1', 'research-route', 'protocol-v1', 'A',
      jsonb_build_array(jsonb_build_object(
        'id', '44444444-4444-4444-8444-444444444445',
        'name', 'manual_flush',
        'occurredAt', now(),
        'props', '{}'::jsonb
      )),
      jsonb_build_array(jsonb_build_object(
        'eventId', '55555555-5555-4555-8555-555555555556',
        'runId', '66666666-6666-4666-8666-666666666666',
        'questionId', 'q.invalid',
        'questionVersion', 'story-formative-v1',
        'choiceId', 'q.invalid.choice',
        'attemptIndex', 2,
        'isCorrect', false,
        'usedHint', false,
        'responseTimeMs', -1,
        'occurredAt', now(),
        'gameVersion', 'test-game',
        'contentVersion', 'content-v1',
        'storyRoute', 'research-route'
      )),
      null,
      null
    );
    return false;
  exception when check_violation then
    return not exists (
      select 1 from public.telemetry_events
      where id = '44444444-4444-4444-8444-444444444445'
    );
  end;
end;
$$;

select ok(
  pg_temp.invalid_batch_rolls_back(),
  'a failed quiz insert rolls back the event from the same batch'
);

update public.game_sessions
set last_heartbeat_at = now() - interval '121 seconds', ended_at = null, end_reason = null
where id = '33333333-3333-4333-8333-333333333333';

select is(
  public.infer_stale_research_sessions(now() - interval '120 seconds'),
  1,
  'stale heartbeat closes one research session'
);
select is(
  (select end_reason from public.game_sessions where id = '33333333-3333-4333-8333-333333333333'),
  'heartbeat_timeout',
  'inferred session stores the heartbeat timeout reason'
);

select * from finish();
rollback;
