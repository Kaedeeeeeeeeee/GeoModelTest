-- The teacher asked for the respondent ID to be entered again in the questionnaire as a
-- double check against the 保護者同意 form. Only participants with a guardian form are asked.
-- A different ID is accepted after the student confirms it; the mismatch is kept for review.
begin;
alter table public.survey_responses
  add column respondent_id text check (respondent_id is null or respondent_id ~ '^p[0-9]{7}$'),
  add column respondent_id_matches boolean;
comment on column public.survey_responses.respondent_id is
  'Respondent ID the student re-entered in the questionnaire (double check). Null when no guardian form applies.';
comment on column public.survey_responses.respondent_id_matches is
  'Whether respondent_id equalled guardian_consents.respondent_id at submission. False only after the student confirmed a different ID.';

drop function public.use_survey_ticket(text, jsonb);
create function public.use_survey_ticket(p_token_hash text, p_answers jsonb default null,
  p_respondent_id text default null, p_confirm_respondent_mismatch boolean default false)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare v_ticket public.survey_tickets%rowtype; v_response public.survey_responses%rowtype;
  v_key text; v_value text; v_number integer; v_expected text; v_matches boolean;
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
  select g.respondent_id into v_expected from public.guardian_consents g where g.participant_id = v_ticket.participant_id;
  if p_answers is null then
    return jsonb_build_object('ok',true,'submitted',false,'surveyVersion',v_ticket.survey_version,
      'respondentIdRequired',v_expected is not null);
  end if;
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
  if v_expected is null then
    if p_respondent_id is not null then
      raise exception using errcode = '22023', message = 'Unexpected respondent ID';
    end if;
  else
    if p_respondent_id is null or p_respondent_id !~ '^p[0-9]{7}$' then
      raise exception using errcode = '22023', message = 'Enter the respondent ID';
    end if;
    v_matches := p_respondent_id = v_expected;
    if not v_matches and not coalesce(p_confirm_respondent_mismatch, false) then
      raise exception using errcode = 'GQ409', message = 'Respondent ID differs from the guardian form';
    end if;
  end if;
  insert into public.survey_responses(participant_id,study_id,session_id,run_id,survey_version,answers,
      respondent_id,respondent_id_matches)
    values (v_ticket.participant_id,v_ticket.study_id,v_ticket.session_id,v_ticket.run_id,v_ticket.survey_version,p_answers,
      case when v_expected is not null then p_respondent_id end, v_matches)
    on conflict (participant_id,run_id,survey_version) do nothing returning * into v_response;
  if v_response.id is null then
    select * into v_response from public.survey_responses where participant_id = v_ticket.participant_id
      and run_id = v_ticket.run_id and survey_version = v_ticket.survey_version;
  end if;
  return jsonb_build_object('ok',true,'submitted',true,'receiptId',v_response.id);
end $$;
revoke all on function public.use_survey_ticket(text,jsonb,text,boolean) from public,anon,authenticated;
grant execute on function public.use_survey_ticket(text,jsonb,text,boolean) to service_role;
notify pgrst, 'reload schema';
commit;
