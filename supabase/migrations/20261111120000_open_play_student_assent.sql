-- 2026-11-11: 稲垣先生 asked to drop the 保護者同意 page before 玉川中 checks the game in advance; the
-- 生徒の同意 page stays. The user chose a separate study for 玉川中 so its data stays apart from Testee's:
-- the Testee open-play study stops taking new players (its data and status page are untouched) and a new
-- open-play study requires only the student's assent from the web page.
begin;
alter table public.studies add column requires_student_assent boolean not null default false;
comment on column public.studies.requires_student_assent is
  'Open play only: activation must carry the student assent from the web page (no guardian form).';

-- Same rules as before, plus student-assent-only open play.
create or replace function public.research_participant_is_eligible(p_participant_id uuid)
returns boolean language sql stable security invoker set search_path = public, pg_temp as $$
  select public.research_participant_entry_open(p_participant_id) and exists (
    select 1 from public.study_participants p join public.studies s on s.id = p.study_id
    where p.id = p_participant_id and (
      (s.entry_mode = 'open_play' and not s.requires_guardian_consent and not s.requires_student_assent)
      or (s.entry_mode = 'open_play' and not s.requires_guardian_consent and s.requires_student_assent
        and nullif(trim(p.consent_version), '') is not null and p.student_assent_at <= now())
      or (s.entry_mode = 'invitation' and s.status = 'development')
      or (nullif(trim(p.consent_version), '') is not null
        and p.guardian_consent_at <= now() and p.student_assent_at <= now()))
  );
$$;
revoke all on function public.research_participant_is_eligible(uuid) from public, anon, authenticated;
grant execute on function public.research_participant_is_eligible(uuid) to service_role;

create function public.record_student_assent(p_participant_id uuid, p_consent jsonb)
returns void language plpgsql security invoker set search_path = public, pg_temp as $$
declare
  v_version text;
  v_student_at timestamptz;
begin
  if p_consent is null or jsonb_typeof(p_consent) <> 'object' then
    raise exception using errcode = 'GC001', message = 'Student assent required';
  end if;
  v_version := p_consent->>'consentVersion';
  begin
    v_student_at := (p_consent->>'studentAssentedAt')::timestamptz;
  exception when others then
    raise exception using errcode = 'GC002', message = 'Invalid student assent';
  end;
  -- Device clocks drift, so only reject clearly impossible answer times.
  if p_consent->'studentAssented' is distinct from 'true'::jsonb
     or v_version is null or char_length(v_version) not between 1 and 64
     or v_student_at is null
     or v_student_at not between now() - interval '30 days' and now() + interval '1 day' then
    raise exception using errcode = 'GC002', message = 'Invalid student assent';
  end if;
  update public.study_participants
    set consent_version = v_version, student_assent_at = least(v_student_at, now())
    where id = p_participant_id;
end;
$$;
revoke all on function public.record_student_assent(uuid, jsonb) from public, anon, authenticated;
grant execute on function public.record_student_assent(uuid, jsonb) to service_role;

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

-- Only one open-play study takes new players (studies_one_open_play_entry): close Testee's first.
update public.studies set research_entry_enabled = false where study_key = 'geoquest-open-play-2026-09';
insert into public.studies(study_key, status, research_entry_enabled, protocol_version, entry_mode,
    requires_guardian_consent, requires_student_assent)
  values ('geoquest-tamagawa-2026-12', 'active', true, 'open-play-v1', 'open_play', false, true);

notify pgrst, 'reload schema';
commit;
