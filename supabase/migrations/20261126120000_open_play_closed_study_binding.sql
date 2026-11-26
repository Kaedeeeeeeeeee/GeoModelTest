-- 2026-11-26: The user replayed on a browser that had played during the Testee study. That browser's anonymous
-- identity is still bound to the Testee participant, and the Testee study stopped taking players on 2026-11-11,
-- so every activation was refused with 42501 and the survey button kept saying 「接続を確認して…」.
-- Report this one case with its own code so the game can start a fresh anonymous identity for the study that is
-- open now. The Testee participant and its data are not touched. Withdrawal and protocol mismatch keep 42501.
begin;
create or replace function public.activate_open_play_participant(p_user_id uuid, p_consent jsonb default null)
returns jsonb language plpgsql security invoker set search_path = public, pg_temp as $$
declare
  v_participant public.study_participants%rowtype;
  v_study public.studies%rowtype;
begin
  -- The service-only caller obtains this ID from Auth.getUser; never from the request body.
  if p_user_id is null then
    raise exception using errcode = '42501', message = 'Anonymous game identity required';
  end if;
  -- Serialize activation for this authenticated identity, including the first request.
  perform pg_advisory_xact_lock(hashtextextended(p_user_id::text, 0));
  select * into v_participant from public.study_participants where auth_user_id = p_user_id for update;
  if found then
    -- Preserve existing bindings, withdrawals and invitation eligibility rules.
    if not public.research_participant_entry_open(v_participant.id) then
      select * into v_study from public.studies where id = v_participant.study_id;
      if v_participant.withdrawn_at is null and v_participant.status in ('ready','active','completed')
         and not (v_study.research_entry_enabled and v_study.status in ('development','active')
           and (v_study.retention_until is null or v_study.retention_until > now()))
         and exists (select 1 from public.studies s where s.id <> v_study.id and s.entry_mode = 'open_play'
           and s.status = 'active' and s.research_entry_enabled
           and (s.retention_until is null or s.retention_until > now())) then
        raise exception using errcode = 'GS410', message = 'Identity belongs to a closed study';
      end if;
      raise exception using errcode = '42501', message = 'Participant is not eligible';
    end if;
    select * into v_study from public.studies where id = v_participant.study_id;
  else
    select * into v_study from public.studies
      where entry_mode = 'open_play' and status = 'active' and research_entry_enabled
        and (retention_until is null or retention_until > now()) for share;
    if not found then
      raise exception using errcode = '42501', message = 'Study entry is closed';
    end if;
    insert into public.study_participants(study_id, auth_user_id, entry_mode, condition,
      protocol_version, cohort, status, activated_at)
    values(v_study.id, p_user_id, 'open_play', 'game', v_study.protocol_version,
      'open-play', 'active', now()) returning * into v_participant;
  end if;
  -- The first activation after the web consent step stores it; later ones reuse it.
  -- A failure rolls back the whole call, so no participant exists without consent.
  if v_participant.entry_mode = 'open_play' and v_study.requires_guardian_consent
     and not exists (select 1 from public.guardian_consents c where c.participant_id = v_participant.id) then
    perform public.record_guardian_consent(v_participant.id, v_study.id, p_consent);
  elsif v_participant.entry_mode = 'open_play' and not v_study.requires_guardian_consent
     and v_study.requires_student_assent and v_participant.student_assent_at is null then
    perform public.record_student_assent(v_participant.id, p_consent);
  end if;
  if not public.research_participant_is_eligible(v_participant.id) then
    raise exception using errcode = '42501', message = 'Participant is not eligible';
  end if;
  return jsonb_build_object('ok', true, 'participantId', v_participant.id,
    'studyId', v_participant.study_id, 'condition', v_participant.condition,
    'protocolVersion', v_participant.protocol_version);
end;
$$;
revoke all on function public.activate_open_play_participant(uuid, jsonb) from public, anon, authenticated;
grant execute on function public.activate_open_play_participant(uuid, jsonb) to service_role;
notify pgrst, 'reload schema';
commit;
