-- Current questionnaire: five choices and optional free text up to 300 characters.
-- Existing responses are never rewritten; legacy v1 validation remains compatible.
begin;
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
    if v_number <= 11 and (v_value is null or v_value not in ('1','2','3','4','5','skip')
      or (v_ticket.survey_version = 'post-game-ja-v2' and v_value = 'skip')) then
      raise exception using errcode = '22023', message = 'Select an answer for every required question';
    elsif v_number = 12 and v_ticket.survey_version = 'post-game-ja-v1' and (v_value is null or v_value not in ('none','little','some','strong','not_seen','skip')) then
      raise exception using errcode = '22023', message = 'Invalid anxiety answer';
    elsif v_number >= 13 and char_length(coalesce(v_value,'')) > (case when v_ticket.survey_version = 'post-game-ja-v2' then 300 else 2000 end) then
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
revoke all on function public.use_survey_ticket(text,jsonb) from public,anon,authenticated;
grant execute on function public.use_survey_ticket(text,jsonb) to service_role;
commit;
