-- Survey credentials have one scope: submit this participant's completed run questionnaire.
-- Only their SHA-256 digest is stored. No invitation codes or game auth tokens are exposed.
create table public.survey_tickets (
  id uuid primary key default gen_random_uuid(),
  token_hash text not null unique check (token_hash ~ '^[0-9a-f]{64}$'),
  participant_id uuid not null references public.study_participants(id) on delete cascade,
  study_id uuid not null references public.studies(id) on delete cascade,
  session_id uuid not null references public.game_sessions(id) on delete cascade,
  run_id uuid not null,
  survey_version text not null check (survey_version = 'post-game-ja-v1'),
  created_at timestamptz not null default now(),
  expires_at timestamptz not null default now() + interval '24 hours'
);
create table public.survey_responses (
  id uuid primary key default gen_random_uuid(),
  participant_id uuid not null references public.study_participants(id) on delete cascade,
  study_id uuid not null references public.studies(id) on delete cascade,
  session_id uuid not null references public.game_sessions(id) on delete cascade,
  run_id uuid not null,
  survey_version text not null,
  answers jsonb not null check (jsonb_typeof(answers) = 'object'),
  submitted_at timestamptz not null default now(),
  unique (participant_id, run_id, survey_version)
);
alter table public.survey_tickets enable row level security;
alter table public.survey_responses enable row level security;
revoke all on public.survey_tickets, public.survey_responses from public, anon, authenticated;
grant all on public.survey_tickets, public.survey_responses to service_role;
create index survey_tickets_participant on public.survey_tickets(participant_id);
create index survey_tickets_study on public.survey_tickets(study_id);
create index survey_tickets_session on public.survey_tickets(session_id);
create index survey_responses_study on public.survey_responses(study_id);
create index survey_responses_session on public.survey_responses(session_id);
create index progress_history_run on public.progress_history ((progress_payload->>'runId'));

create function public.issue_survey_ticket(p_user_id uuid, p_session_id uuid, p_run_id uuid, p_token_hash text)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_participant public.study_participants%rowtype; v_ticket public.survey_tickets%rowtype;
begin
  select p.* into v_participant from public.study_participants p
    join public.game_sessions g on g.participant_id = p.id
    join public.studies s on s.id = p.study_id
    where g.id = p_session_id and p.auth_user_id = p_user_id
      and p.status in ('ready','active','completed') and p.withdrawn_at is null
      and s.status in ('development','active') and s.research_entry_enabled
      and (s.retention_until is null or s.retention_until > now());
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

create function public.use_survey_ticket(p_token_hash text, p_answers jsonb default null)
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
      and (s.retention_until is null or s.retention_until > now()) for update of t;
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
