-- Code-free play uses anonymous records; the research sample is the submitted responses.
-- Invitation studies keep their existing consent and code rules.
alter table public.studies add column entry_mode text not null default 'invitation'
  check (entry_mode in ('invitation', 'open_play'));
alter table public.study_participants add column entry_mode text not null default 'invitation'
  check (entry_mode in ('invitation', 'open_play'));
alter table public.study_participants alter column participant_code_hash drop not null;
alter table public.study_participants add constraint participant_entry_code
  check ((entry_mode = 'invitation' and participant_code_hash is not null)
      or (entry_mode = 'open_play' and participant_code_hash is null));
create unique index studies_one_open_play_entry on public.studies(entry_mode)
  where entry_mode = 'open_play' and status = 'active' and research_entry_enabled;

create or replace function public.research_participant_is_eligible(p_participant_id uuid)
returns boolean language sql stable security invoker set search_path = public, pg_temp as $$
  select exists (
    select 1 from public.study_participants p join public.studies s on s.id = p.study_id
    where p.id = p_participant_id and p.withdrawn_at is null
      and p.status in ('ready','active','completed')
      and s.research_entry_enabled and s.status in ('development','active')
      and (s.retention_until is null or s.retention_until > now())
      and p.protocol_version = s.protocol_version and p.entry_mode = s.entry_mode
      and (s.entry_mode = 'open_play' or s.status = 'development' or (
        nullif(trim(p.consent_version), '') is not null
        and p.guardian_consent_at <= now() and p.student_assent_at <= now()
      ))
  );
$$;
revoke all on function public.research_participant_is_eligible(uuid) from public, anon, authenticated;
grant execute on function public.research_participant_is_eligible(uuid) to service_role;

create function public.activate_open_play_participant(p_user_id uuid)
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
    if not public.research_participant_is_eligible(v_participant.id) then
      raise exception using errcode = '42501', message = 'Participant is not eligible';
    end if;
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
  return jsonb_build_object('ok', true, 'participantId', v_participant.id,
    'studyId', v_participant.study_id, 'condition', v_participant.condition,
    'protocolVersion', v_participant.protocol_version);
end;
$$;
revoke all on function public.activate_open_play_participant(uuid) from public, anon, authenticated;
grant execute on function public.activate_open_play_participant(uuid) to service_role;

insert into public.studies(study_key, status, research_entry_enabled, protocol_version, entry_mode)
  values ('geoquest-open-play-2026-09', 'active', true, 'open-play-v1', 'open_play');
comment on column public.study_participants.entry_mode is
  'Enrollment route only. An anonymous play record does not itself count as a survey respondent or evidence of consent.';
