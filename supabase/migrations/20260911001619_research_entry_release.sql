-- Release entry remains server-controlled per study and participant.
-- Active studies require researcher-recorded consent metadata; development studies do not.
create or replace function public.research_participant_is_eligible(p_participant_id uuid)
returns boolean language sql stable security invoker set search_path = public, pg_temp as $$
  select exists (
    select 1 from public.study_participants p join public.studies s on s.id = p.study_id
    where p.id = p_participant_id and p.withdrawn_at is null
      and p.status in ('ready','active','completed')
      and s.research_entry_enabled and s.status in ('development','active')
      and (s.retention_until is null or s.retention_until > now())
      and p.protocol_version = s.protocol_version
      and (s.status = 'development' or (
        nullif(trim(p.consent_version), '') is not null
        and p.guardian_consent_at <= now() and p.student_assent_at <= now()
      ))
  );
$$;
revoke all on function public.research_participant_is_eligible(uuid) from public, anon, authenticated;
grant execute on function public.research_participant_is_eligible(uuid) to service_role;

create or replace function public.activate_research_participant(
  p_code_hash text,
  p_user_id uuid
)
returns jsonb
language plpgsql
security invoker
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
  if not found or v_study.status not in ('development','active') or not v_study.research_entry_enabled then
    raise exception using errcode = '42501', message = 'Study entry is closed';
  end if;

  if v_participant.withdrawn_at is not null or v_participant.status not in ('ready', 'active') then
    raise exception using errcode = '42501', message = 'Participant is not eligible';
  end if;

  if v_participant.protocol_version <> v_study.protocol_version then
    raise exception using errcode = '22023', message = 'Protocol version mismatch';
  end if;

  if not public.research_participant_is_eligible(v_participant.id) then
    raise exception using errcode = '42501', message = 'Participant is not eligible';
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

revoke all on function public.activate_research_participant(text, uuid) from public, anon, authenticated;
grant execute on function public.activate_research_participant(text, uuid) to service_role;

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
security invoker
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

  if not public.research_participant_is_eligible(p_participant_id) then
    raise exception using errcode = '42501', message = 'Participant is not eligible';
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

create or replace function public.issue_survey_ticket(p_user_id uuid, p_session_id uuid, p_run_id uuid, p_token_hash text)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_participant public.study_participants%rowtype; v_ticket public.survey_tickets%rowtype;
begin
  select p.* into v_participant from public.study_participants p
    join public.game_sessions g on g.participant_id = p.id
    join public.studies s on s.id = p.study_id
    where g.id = p_session_id and p.auth_user_id = p_user_id
      and p.status in ('ready','active','completed') and p.withdrawn_at is null
      and s.status in ('development','active') and s.research_entry_enabled
      and (s.retention_until is null or s.retention_until > now())
      and public.research_participant_is_eligible(p.id);
  if not found then raise exception using errcode = '42501', message = 'Survey access denied'; end if;
  if not exists (select 1 from public.progress_history h
    where h.session_id = p_session_id and h.participant_id = v_participant.id
      and h.progress_payload->>'runId' = p_run_id::text
      and h.progress_payload->>'investigationComplete' = 'true') then
    raise exception using errcode = '22023', message = 'Completed game record is not synchronized';
  end if;
  insert into public.survey_tickets(token_hash,participant_id,study_id,session_id,run_id,survey_version)
    values (p_token_hash,v_participant.id,v_participant.study_id,p_session_id,p_run_id,'post-game-ja-v1')
    returning * into v_ticket;
  return jsonb_build_object('expiresAt',v_ticket.expires_at,'surveyVersion',v_ticket.survey_version);
end $$;

create or replace function public.use_survey_ticket(p_token_hash text, p_answers jsonb default null)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_ticket public.survey_tickets%rowtype; v_response public.survey_responses%rowtype;
  v_key text; v_value text; v_number integer;
begin
  select t.* into v_ticket from public.survey_tickets t
    join public.study_participants p on p.id = t.participant_id
    join public.studies s on s.id = t.study_id
    where t.token_hash = p_token_hash and t.expires_at > now()
      and p.withdrawn_at is null and p.status in ('ready','active','completed')
      and s.status in ('development','active') and s.research_entry_enabled
      and (s.retention_until is null or s.retention_until > now())
      and public.research_participant_is_eligible(p.id) for update of t;
  if not found then raise exception using errcode = '42501', message = 'Survey link is invalid or expired'; end if;
  select * into v_response from public.survey_responses where participant_id = v_ticket.participant_id
    and run_id = v_ticket.run_id and survey_version = v_ticket.survey_version;
  if found then return jsonb_build_object('ok',true,'submitted',true,'receiptId',v_response.id); end if;
  if p_answers is null then return jsonb_build_object('ok',true,'submitted',false,'surveyVersion',v_ticket.survey_version); end if;
  if jsonb_typeof(p_answers) <> 'object' or octet_length(p_answers::text) > 24000 then
    raise exception using errcode = '22023', message = 'Invalid survey answers';
  end if;
  for v_key in select jsonb_object_keys(p_answers) loop
    if v_key !~ '^q([1-9]|1[0-4])$' or jsonb_typeof(p_answers->v_key) <> 'string' then
      raise exception using errcode = '22023', message = 'Unexpected answer field';
    end if;
  end loop;
  for v_number in 1..14 loop
    v_key := 'q' || v_number; v_value := p_answers->>v_key;
    if v_number <= 11 and (v_value is null or v_value not in ('1','2','3','4','5','skip')) then
      raise exception using errcode = '22023', message = 'Select an answer for every required question';
    elsif v_number = 12 and (v_value is null or v_value not in ('none','little','some','strong','not_seen','skip')) then
      raise exception using errcode = '22023', message = 'Invalid anxiety answer';
    elsif v_number >= 13 and char_length(coalesce(v_value,'')) > 2000 then
      raise exception using errcode = '22023', message = 'Free text is too long';
    end if;
  end loop;
  insert into public.survey_responses(participant_id,study_id,session_id,run_id,survey_version,answers)
    values (v_ticket.participant_id,v_ticket.study_id,v_ticket.session_id,v_ticket.run_id,v_ticket.survey_version,p_answers)
    on conflict (participant_id,run_id,survey_version) do nothing returning * into v_response;
  if v_response.id is null then
    select * into v_response from public.survey_responses where participant_id = v_ticket.participant_id
      and run_id = v_ticket.run_id and survey_version = v_ticket.survey_version;
  end if;
  return jsonb_build_object('ok',true,'submitted',true,'receiptId',v_response.id);
end $$;
revoke all on function public.issue_survey_ticket(uuid,uuid,uuid,text) from public,anon,authenticated;
revoke all on function public.use_survey_ticket(text,jsonb) from public,anon,authenticated;
grant execute on function public.issue_survey_ticket(uuid,uuid,uuid,text) to service_role;
grant execute on function public.use_survey_ticket(text,jsonb) to service_role;
comment on table public.survey_responses is 'Server-bound post-game questionnaire responses. Identity comes from a scoped ticket, never from editable form fields.';
