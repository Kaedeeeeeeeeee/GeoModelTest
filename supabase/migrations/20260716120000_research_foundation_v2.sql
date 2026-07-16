-- Research foundation v2.
-- Raw participation codes and directly identifying information must never be stored here.

create table if not exists public.studies (
  id uuid primary key default gen_random_uuid(),
  study_key text not null unique,
  status text not null default 'locked'
    check (status in ('development', 'locked', 'active', 'closed')),
  research_entry_enabled boolean not null default false,
  protocol_version text not null,
  retention_until timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint studies_key_length check (char_length(study_key) between 1 and 64),
  constraint studies_protocol_version_length check (char_length(protocol_version) between 1 and 64)
);

create table if not exists public.study_participants (
  id uuid primary key default gen_random_uuid(),
  study_id uuid not null references public.studies(id) on delete cascade,
  auth_user_id uuid unique references auth.users(id) on delete set null,
  participant_code_hash text not null unique,
  cohort text,
  condition text not null,
  protocol_version text not null,
  consent_version text,
  guardian_consent_at timestamptz,
  student_assent_at timestamptz,
  activated_at timestamptz,
  withdrawn_at timestamptz,
  status text not null default 'ready'
    check (status in ('ready', 'active', 'withdrawn', 'completed')),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint study_participants_code_hash_format
    check (participant_code_hash ~ '^[0-9a-f]{64}$'),
  constraint study_participants_condition_length check (char_length(condition) between 1 and 32),
  constraint study_participants_protocol_version_length check (char_length(protocol_version) between 1 and 64),
  constraint study_participants_consent_version_length
    check (consent_version is null or char_length(consent_version) between 1 and 64)
);

alter table public.game_sessions
  add column if not exists participant_id uuid references public.study_participants(id) on delete cascade,
  add column if not exists last_heartbeat_at timestamptz,
  add column if not exists end_reason text,
  add column if not exists content_version text,
  add column if not exists story_route text,
  add column if not exists protocol_version text,
  add column if not exists condition text;

alter table public.telemetry_events
  add column if not exists participant_id uuid references public.study_participants(id) on delete cascade;

alter table public.telemetry_events
  drop constraint if exists telemetry_events_name_allowed;

alter table public.telemetry_events
  add constraint telemetry_events_name_allowed check (
    event_name in (
      'session_started',
      'session_ended',
      'session_heartbeat',
      'research_mode_started',
      'scene_loaded',
      'tool_equipped',
      'tool_used',
      'quest_started',
      'objective_completed',
      'quest_completed',
      'progress_dirty',
      'story_content_notice_decision',
      'quiz_question_shown',
      'quiz_hint_viewed',
      'quiz_answered',
      'manual_flush'
    )
  );

create table if not exists public.quiz_attempts (
  event_id uuid primary key,
  participant_id uuid not null references public.study_participants(id) on delete cascade,
  session_id uuid not null references public.game_sessions(id) on delete cascade,
  run_id uuid not null,
  question_id text not null,
  question_version text not null,
  choice_id text not null,
  attempt_index smallint not null check (attempt_index between 1 and 100),
  is_correct boolean not null,
  used_hint boolean not null,
  response_time_ms integer not null check (response_time_ms between 0 and 3600000),
  occurred_at timestamptz not null,
  received_at timestamptz not null default now(),
  game_version text not null,
  content_version text not null,
  story_route text not null,
  condition text not null,
  constraint quiz_attempts_question_id_length check (char_length(question_id) between 1 and 128),
  constraint quiz_attempts_question_version_length check (char_length(question_version) between 1 and 64),
  constraint quiz_attempts_choice_id_length check (char_length(choice_id) between 1 and 128),
  constraint quiz_attempts_game_version_length check (char_length(game_version) between 1 and 64),
  constraint quiz_attempts_content_version_length check (char_length(content_version) between 1 and 128),
  constraint quiz_attempts_story_route_length check (char_length(story_route) between 1 and 64),
  constraint quiz_attempts_condition_length check (char_length(condition) between 1 and 32),
  unique (session_id, question_id, attempt_index)
);

create table if not exists public.current_progress (
  participant_id uuid primary key references public.study_participants(id) on delete cascade,
  session_id uuid references public.game_sessions(id) on delete set null,
  event_id uuid not null,
  current_scene text,
  completed_quests text[] not null default '{}',
  completed_objectives text[] not null default '{}',
  story_flags text[] not null default '{}',
  unlocked_tool_ids text[] not null default '{}',
  inventory_count integer not null default 0 check (inventory_count >= 0),
  warehouse_count integer not null default 0 check (warehouse_count >= 0),
  encyclopedia_discovered integer not null default 0 check (encyclopedia_discovered >= 0),
  encyclopedia_total integer not null default 0 check (encyclopedia_total >= 0),
  progress_payload jsonb not null default '{}'::jsonb,
  occurred_at timestamptz not null,
  received_at timestamptz not null default now()
);

create table if not exists public.progress_history (
  event_id uuid primary key,
  participant_id uuid not null references public.study_participants(id) on delete cascade,
  session_id uuid not null references public.game_sessions(id) on delete cascade,
  current_scene text,
  completed_quests text[] not null default '{}',
  completed_objectives text[] not null default '{}',
  story_flags text[] not null default '{}',
  unlocked_tool_ids text[] not null default '{}',
  inventory_count integer not null default 0 check (inventory_count >= 0),
  warehouse_count integer not null default 0 check (warehouse_count >= 0),
  encyclopedia_discovered integer not null default 0 check (encyclopedia_discovered >= 0),
  encyclopedia_total integer not null default 0 check (encyclopedia_total >= 0),
  progress_payload jsonb not null default '{}'::jsonb,
  occurred_at timestamptz not null,
  received_at timestamptz not null default now()
);

create table if not exists public.research_rate_limits (
  bucket_key text primary key,
  window_started_at timestamptz not null,
  request_count integer not null check (request_count > 0),
  updated_at timestamptz not null default now(),
  constraint research_rate_limits_bucket_key_format check (bucket_key ~ '^[0-9a-f]{64}$')
);

create index if not exists idx_study_participants_study_id
  on public.study_participants(study_id);
create index if not exists idx_study_participants_study_status
  on public.study_participants(study_id, status);
create index if not exists idx_game_sessions_participant_started
  on public.game_sessions(participant_id, started_at desc);
create index if not exists idx_game_sessions_open_heartbeat
  on public.game_sessions(last_heartbeat_at)
  where ended_at is null and participant_id is not null;
create index if not exists idx_telemetry_events_participant_occurred
  on public.telemetry_events(participant_id, occurred_at desc);
create index if not exists idx_quiz_attempts_participant_question
  on public.quiz_attempts(participant_id, question_id, occurred_at);
create index if not exists idx_quiz_attempts_session_id
  on public.quiz_attempts(session_id);
create index if not exists idx_current_progress_session_id
  on public.current_progress(session_id);
create index if not exists idx_progress_history_participant_occurred
  on public.progress_history(participant_id, occurred_at desc);
create index if not exists idx_progress_history_session_id
  on public.progress_history(session_id);

drop trigger if exists trg_studies_updated_at on public.studies;
create trigger trg_studies_updated_at
before update on public.studies
for each row execute function public.backend_set_updated_at();

drop trigger if exists trg_study_participants_updated_at on public.study_participants;
create trigger trg_study_participants_updated_at
before update on public.study_participants
for each row execute function public.backend_set_updated_at();

alter table public.studies enable row level security;
alter table public.study_participants enable row level security;
alter table public.quiz_attempts enable row level security;
alter table public.current_progress enable row level security;
alter table public.progress_history enable row level security;
alter table public.research_rate_limits enable row level security;

revoke all on table public.studies from anon, authenticated;
revoke all on table public.study_participants from anon, authenticated;
revoke all on table public.quiz_attempts from anon, authenticated;
revoke all on table public.current_progress from anon, authenticated;
revoke all on table public.progress_history from anon, authenticated;
revoke all on table public.research_rate_limits from anon, authenticated;

grant select, insert, update, delete on table public.studies to service_role;
grant select, insert, update, delete on table public.study_participants to service_role;
grant select, insert, update, delete on table public.quiz_attempts to service_role;
grant select, insert, update, delete on table public.current_progress to service_role;
grant select, insert, update, delete on table public.progress_history to service_role;
grant select, insert, update, delete on table public.research_rate_limits to service_role;

create or replace function public.ingest_research_batch(
  p_user_id uuid,
  p_participant_id uuid,
  p_study_id uuid,
  p_session_id uuid,
  p_install_id text,
  p_game_version text,
  p_platform text,
  p_build_target text,
  p_language text,
  p_current_scene text,
  p_content_version text,
  p_story_route text,
  p_protocol_version text,
  p_condition text,
  p_events jsonb default '[]'::jsonb,
  p_quiz_attempts jsonb default '[]'::jsonb,
  p_progress jsonb default null,
  p_session_end jsonb default null
)
returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_now timestamptz := now();
  v_participant public.study_participants%rowtype;
  v_study public.studies%rowtype;
  v_existing_session record;
  v_end_at timestamptz;
  v_end_reason text;
  v_progress_event_id uuid;
  v_progress_occurred_at timestamptz;
begin
  select * into v_participant
  from public.study_participants
  where id = p_participant_id and study_id = p_study_id;

  if not found or v_participant.auth_user_id is distinct from p_user_id then
    raise exception using errcode = '42501', message = 'Participant is not bound to the authenticated user';
  end if;

  select * into v_study from public.studies where id = p_study_id;
  if not found or not v_study.research_entry_enabled or v_study.status not in ('development', 'active') then
    raise exception using errcode = '42501', message = 'Study is not accepting research data';
  end if;

  if v_participant.withdrawn_at is not null or v_participant.status not in ('ready', 'active') then
    raise exception using errcode = '42501', message = 'Participant is not active';
  end if;

  if v_participant.condition <> p_condition or
     v_participant.protocol_version <> p_protocol_version or
     v_study.protocol_version <> p_protocol_version then
    raise exception using errcode = '22023', message = 'Research condition or protocol version mismatch';
  end if;

  select user_id, participant_id into v_existing_session
  from public.game_sessions where id = p_session_id;
  if found and (v_existing_session.user_id <> p_user_id or
                v_existing_session.participant_id is distinct from p_participant_id) then
    raise exception using errcode = '42501', message = 'Session belongs to another participant';
  end if;

  insert into public.player_profiles (
    user_id, install_id, first_seen_at, last_seen_at, first_game_version,
    latest_game_version, platform, language, build_target
  ) values (
    p_user_id, p_install_id, v_now, v_now, p_game_version,
    p_game_version, p_platform, p_language, p_build_target
  )
  on conflict (user_id) do update set
    last_seen_at = excluded.last_seen_at,
    latest_game_version = excluded.latest_game_version,
    platform = excluded.platform,
    language = excluded.language,
    build_target = excluded.build_target;

  insert into public.game_sessions (
    id, user_id, install_id, participant_id, started_at, last_heartbeat_at,
    game_version, platform, build_target, language, last_scene,
    content_version, story_route, protocol_version, condition
  ) values (
    p_session_id, p_user_id, p_install_id, p_participant_id, v_now, v_now,
    p_game_version, p_platform, p_build_target, p_language, p_current_scene,
    p_content_version, p_story_route, p_protocol_version, p_condition
  )
  on conflict (id) do update set
    last_heartbeat_at = v_now,
    game_version = excluded.game_version,
    platform = excluded.platform,
    build_target = excluded.build_target,
    language = excluded.language,
    last_scene = excluded.last_scene,
    content_version = excluded.content_version,
    story_route = excluded.story_route,
    protocol_version = excluded.protocol_version,
    condition = excluded.condition;

  insert into public.telemetry_events (
    id, user_id, participant_id, session_id, install_id, event_name,
    event_props, occurred_at, received_at, game_version, scene_name
  )
  select
    (event_item->>'id')::uuid,
    p_user_id,
    p_participant_id,
    p_session_id,
    p_install_id,
    event_item->>'name',
    coalesce(event_item->'props', '{}'::jsonb),
    (event_item->>'occurredAt')::timestamptz,
    v_now,
    p_game_version,
    coalesce(event_item->>'sceneName', p_current_scene)
  from jsonb_array_elements(coalesce(p_events, '[]'::jsonb)) as event_item
  on conflict (id) do nothing;

  insert into public.quiz_attempts (
    event_id, participant_id, session_id, run_id, question_id, question_version,
    choice_id, attempt_index, is_correct, used_hint, response_time_ms,
    occurred_at, received_at, game_version, content_version, story_route, condition
  )
  select
    (attempt_item->>'eventId')::uuid,
    p_participant_id,
    p_session_id,
    (attempt_item->>'runId')::uuid,
    attempt_item->>'questionId',
    attempt_item->>'questionVersion',
    attempt_item->>'choiceId',
    (attempt_item->>'attemptIndex')::smallint,
    (attempt_item->>'isCorrect')::boolean,
    (attempt_item->>'usedHint')::boolean,
    (attempt_item->>'responseTimeMs')::integer,
    (attempt_item->>'occurredAt')::timestamptz,
    v_now,
    attempt_item->>'gameVersion',
    attempt_item->>'contentVersion',
    attempt_item->>'storyRoute',
    p_condition
  from jsonb_array_elements(coalesce(p_quiz_attempts, '[]'::jsonb)) as attempt_item
  on conflict do nothing;

  if p_progress is not null and jsonb_typeof(p_progress) = 'object' then
    v_progress_event_id := (p_progress->>'eventId')::uuid;
    v_progress_occurred_at := (p_progress->>'updatedAt')::timestamptz;

    insert into public.current_progress (
      participant_id, session_id, event_id, current_scene, completed_quests,
      completed_objectives, story_flags, unlocked_tool_ids, inventory_count,
      warehouse_count, encyclopedia_discovered, encyclopedia_total,
      progress_payload, occurred_at, received_at
    ) values (
      p_participant_id,
      p_session_id,
      v_progress_event_id,
      coalesce(p_progress->>'currentScene', p_current_scene),
      array(select jsonb_array_elements_text(coalesce(p_progress->'completedQuests', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'completedObjectives', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'storyFlags', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'unlockedToolIds', '[]'::jsonb))),
      greatest(0, coalesce((p_progress->>'inventoryCount')::integer, 0)),
      greatest(0, coalesce((p_progress->>'warehouseCount')::integer, 0)),
      greatest(0, coalesce((p_progress->>'encyclopediaDiscovered')::integer, 0)),
      greatest(0, coalesce((p_progress->>'encyclopediaTotal')::integer, 0)),
      coalesce(p_progress->'payload', '{}'::jsonb),
      v_progress_occurred_at,
      v_now
    )
    on conflict (participant_id) do update set
      session_id = excluded.session_id,
      event_id = excluded.event_id,
      current_scene = excluded.current_scene,
      completed_quests = excluded.completed_quests,
      completed_objectives = excluded.completed_objectives,
      story_flags = excluded.story_flags,
      unlocked_tool_ids = excluded.unlocked_tool_ids,
      inventory_count = excluded.inventory_count,
      warehouse_count = excluded.warehouse_count,
      encyclopedia_discovered = excluded.encyclopedia_discovered,
      encyclopedia_total = excluded.encyclopedia_total,
      progress_payload = excluded.progress_payload,
      occurred_at = excluded.occurred_at,
      received_at = excluded.received_at
    where excluded.occurred_at >= public.current_progress.occurred_at;

    insert into public.progress_history (
      event_id, participant_id, session_id, current_scene, completed_quests,
      completed_objectives, story_flags, unlocked_tool_ids, inventory_count,
      warehouse_count, encyclopedia_discovered, encyclopedia_total,
      progress_payload, occurred_at, received_at
    ) values (
      v_progress_event_id,
      p_participant_id,
      p_session_id,
      coalesce(p_progress->>'currentScene', p_current_scene),
      array(select jsonb_array_elements_text(coalesce(p_progress->'completedQuests', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'completedObjectives', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'storyFlags', '[]'::jsonb))),
      array(select jsonb_array_elements_text(coalesce(p_progress->'unlockedToolIds', '[]'::jsonb))),
      greatest(0, coalesce((p_progress->>'inventoryCount')::integer, 0)),
      greatest(0, coalesce((p_progress->>'warehouseCount')::integer, 0)),
      greatest(0, coalesce((p_progress->>'encyclopediaDiscovered')::integer, 0)),
      greatest(0, coalesce((p_progress->>'encyclopediaTotal')::integer, 0)),
      coalesce(p_progress->'payload', '{}'::jsonb),
      v_progress_occurred_at,
      v_now
    )
    on conflict (event_id) do nothing;
  end if;

  if p_session_end is not null and jsonb_typeof(p_session_end) = 'object' then
    v_end_at := coalesce((p_session_end->>'endedAt')::timestamptz, v_now);
    v_end_reason := coalesce(nullif(p_session_end->>'reason', ''), 'explicit_exit');
  else
    select
      max((event_item->>'occurredAt')::timestamptz),
      max(event_item->'props'->>'reason')
    into v_end_at, v_end_reason
    from jsonb_array_elements(coalesce(p_events, '[]'::jsonb)) as event_item
    where event_item->>'name' = 'session_ended';
  end if;

  if v_end_at is not null then
    update public.game_sessions
    set ended_at = greatest(started_at, v_end_at),
        end_reason = coalesce(nullif(v_end_reason, ''), 'client_event'),
        last_heartbeat_at = greatest(coalesce(last_heartbeat_at, started_at), v_end_at)
    where id = p_session_id and participant_id = p_participant_id;
  end if;

  return jsonb_build_object(
    'ok', true,
    'acceptedEvents', jsonb_array_length(coalesce(p_events, '[]'::jsonb)),
    'acceptedQuizAttempts', jsonb_array_length(coalesce(p_quiz_attempts, '[]'::jsonb)),
    'sessionId', p_session_id
  );
end;
$$;

revoke all on function public.ingest_research_batch(
  uuid, uuid, uuid, uuid, text, text, text, text, text, text,
  text, text, text, text, jsonb, jsonb, jsonb, jsonb
) from public, anon, authenticated;
grant execute on function public.ingest_research_batch(
  uuid, uuid, uuid, uuid, text, text, text, text, text, text,
  text, text, text, text, jsonb, jsonb, jsonb, jsonb
) to service_role;

create or replace function public.infer_stale_research_sessions(p_before timestamptz default now() - interval '120 seconds')
returns integer
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_count integer;
begin
  update public.game_sessions
  set ended_at = greatest(started_at, last_heartbeat_at),
      end_reason = 'heartbeat_timeout'
  where participant_id is not null
    and ended_at is null
    and last_heartbeat_at < p_before;
  get diagnostics v_count = row_count;
  return v_count;
end;
$$;

revoke all on function public.infer_stale_research_sessions(timestamptz) from public, anon, authenticated;
grant execute on function public.infer_stale_research_sessions(timestamptz) to service_role;

create or replace function public.activate_development_participant(
  p_code_hash text,
  p_user_id uuid
)
returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_participant public.study_participants%rowtype;
  v_study public.studies%rowtype;
begin
  select * into v_participant
  from public.study_participants
  where participant_code_hash = p_code_hash
  for update;

  if not found then
    raise exception using errcode = 'P0002', message = 'Participation code was not found';
  end if;

  select * into v_study from public.studies where id = v_participant.study_id;
  if not found or v_study.status <> 'development' or not v_study.research_entry_enabled then
    raise exception using errcode = '42501', message = 'Study entry is closed';
  end if;

  if v_participant.withdrawn_at is not null or v_participant.status not in ('ready', 'active') then
    raise exception using errcode = '42501', message = 'Participant is not eligible';
  end if;

  if v_participant.protocol_version <> v_study.protocol_version then
    raise exception using errcode = '22023', message = 'Protocol version mismatch';
  end if;

  if v_participant.auth_user_id is not null and v_participant.auth_user_id <> p_user_id then
    raise exception using errcode = '23505', message = 'Participation code is already bound';
  end if;

  update public.study_participants
  set auth_user_id = p_user_id,
      status = 'active',
      activated_at = coalesce(activated_at, now())
  where id = v_participant.id;

  return jsonb_build_object(
    'ok', true,
    'participantId', v_participant.id,
    'studyId', v_participant.study_id,
    'condition', v_participant.condition,
    'protocolVersion', v_participant.protocol_version
  );
end;
$$;

revoke all on function public.activate_development_participant(text, uuid) from public, anon, authenticated;
grant execute on function public.activate_development_participant(text, uuid) to service_role;

create or replace function public.consume_research_rate_limit(
  p_bucket_key text,
  p_limit integer default 10,
  p_window_seconds integer default 60
)
returns boolean
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_count integer;
  v_now timestamptz := now();
begin
  if p_bucket_key !~ '^[0-9a-f]{64}$' or p_limit not between 1 and 1000 or p_window_seconds not between 1 and 3600 then
    raise exception using errcode = '22023', message = 'Invalid rate limit parameters';
  end if;

  insert into public.research_rate_limits(bucket_key, window_started_at, request_count, updated_at)
  values (p_bucket_key, v_now, 1, v_now)
  on conflict (bucket_key) do update set
    window_started_at = case
      when public.research_rate_limits.window_started_at <= v_now - make_interval(secs => p_window_seconds)
        then v_now
      else public.research_rate_limits.window_started_at
    end,
    request_count = case
      when public.research_rate_limits.window_started_at <= v_now - make_interval(secs => p_window_seconds)
        then 1
      else public.research_rate_limits.request_count + 1
    end,
    updated_at = v_now
  returning request_count into v_count;

  return v_count <= p_limit;
end;
$$;

revoke all on function public.consume_research_rate_limit(text, integer, integer) from public, anon, authenticated;
grant execute on function public.consume_research_rate_limit(text, integer, integer) to service_role;

comment on table public.study_participants is
  'Pseudonymous research participants only. Never store names, school identifiers, contact details, or raw participation codes.';
comment on table public.progress_snapshots is
  'Legacy v1 latest-save table. Research v2 uses current_progress and progress_history.';
