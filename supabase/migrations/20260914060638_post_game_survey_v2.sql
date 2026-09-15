-- 13-question revision. Stable answer IDs retain their original concepts; the
-- browser numbers the reordered questions 1-13. Version 2 removes q12 and changes
-- q10 from agreement to text length. Existing version-1 tickets/responses retain
-- the old validation and are never rewritten.
begin;
alter table public.survey_tickets drop constraint survey_tickets_survey_version_check;
alter table public.survey_tickets add constraint survey_tickets_survey_version_check
  check (survey_version in ('post-game-ja-v1','post-game-ja-v2'));

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
    values (p_token_hash,v_participant.id,v_participant.study_id,p_session_id,p_run_id,'post-game-ja-v2')
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
    if v_key !~ '^q([1-9]|1[0-4])$' or jsonb_typeof(p_answers->v_key) <> 'string'
      or (v_ticket.survey_version = 'post-game-ja-v2' and v_key = 'q12') then
      raise exception using errcode = '22023', message = 'Unexpected answer field';
    end if;
  end loop;
  for v_number in 1..14 loop
    v_key := 'q' || v_number; v_value := p_answers->>v_key;
    if v_number <= 11 and (v_value is null or v_value not in ('1','2','3','4','5','skip')) then
      raise exception using errcode = '22023', message = 'Select an answer for every required question';
    elsif v_number = 12 and v_ticket.survey_version = 'post-game-ja-v1' and (v_value is null or v_value not in ('none','little','some','strong','not_seen','skip')) then
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

commit;
