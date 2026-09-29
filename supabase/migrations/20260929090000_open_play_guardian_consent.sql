-- Open play now records the online 保護者同意 form (entered on the game page before
-- Unity starts) together with the student's title-screen assent. The guardian name
-- lives only in guardian_consents and never joins research exports.
alter table public.studies add column requires_guardian_consent boolean not null default false;
comment on column public.studies.requires_guardian_consent is
  'Open play only: activation must carry the online guardian consent form and student assent.';

create table public.guardian_consents (
  participant_id uuid primary key references public.study_participants(id) on delete cascade,
  study_id uuid not null references public.studies(id),
  consent_version text not null check (char_length(consent_version) between 1 and 64),
  guardian_name text not null check (
    char_length(guardian_name) between 1 and 100
    and guardian_name = btrim(guardian_name)
    and guardian_name !~ '[[:cntrl:]]'),
  respondent_id text not null check (respondent_id ~ '^[A-Za-z0-9._-]{1,64}$'),
  guardian_confirmed boolean not null check (guardian_confirmed),
  guardian_consented_at timestamptz not null,
  student_assented_at timestamptz not null,
  recorded_at timestamptz not null default now()
);
create index guardian_consents_study on public.guardian_consents(study_id);
alter table public.guardian_consents enable row level security;
revoke all on table public.guardian_consents from public, anon, authenticated;
grant select, insert, update, delete on table public.guardian_consents to service_role;
comment on table public.guardian_consents is
  'Online 保護者同意 form (one per participant, first submission kept). Holds the guardian name: keep out of research exports.';
comment on column public.guardian_consents.guardian_consented_at is
  'Device clock when the guardian answered. study_participants.guardian_consent_at is capped at the server time.';

-- Everything except consent: withdrawal, study state, retention and protocol binding.
create function public.research_participant_entry_open(p_participant_id uuid)
returns boolean language sql stable security invoker set search_path = public, pg_temp as $$
  select exists (
    select 1 from public.study_participants p join public.studies s on s.id = p.study_id
    where p.id = p_participant_id and p.withdrawn_at is null
      and p.status in ('ready','active','completed')
      and s.research_entry_enabled and s.status in ('development','active')
      and (s.retention_until is null or s.retention_until > now())
      and p.protocol_version = s.protocol_version and p.entry_mode = s.entry_mode
  );
$$;
revoke all on function public.research_participant_entry_open(uuid) from public, anon, authenticated;
grant execute on function public.research_participant_entry_open(uuid) to service_role;

create or replace function public.research_participant_is_eligible(p_participant_id uuid)
returns boolean language sql stable security invoker set search_path = public, pg_temp as $$
  select public.research_participant_entry_open(p_participant_id) and exists (
    select 1 from public.study_participants p join public.studies s on s.id = p.study_id
    where p.id = p_participant_id and (
      (s.entry_mode = 'open_play' and not s.requires_guardian_consent)
      or (s.entry_mode = 'invitation' and s.status = 'development')
      or (nullif(trim(p.consent_version), '') is not null
        and p.guardian_consent_at <= now() and p.student_assent_at <= now()))
  );
$$;
revoke all on function public.research_participant_is_eligible(uuid) from public, anon, authenticated;
grant execute on function public.research_participant_is_eligible(uuid) to service_role;

create function public.record_guardian_consent(p_participant_id uuid, p_study_id uuid, p_consent jsonb)
returns void language plpgsql security invoker set search_path = public, pg_temp as $$
declare
  v_version text;
  v_name text;
  v_respondent text;
  v_guardian_at timestamptz;
  v_student_at timestamptz;
begin
  if p_consent is null or jsonb_typeof(p_consent) <> 'object' then
    raise exception using errcode = 'GC001', message = 'Guardian consent required';
  end if;
  v_version := p_consent->>'consentVersion';
  v_name := p_consent->>'guardianName';
  v_respondent := p_consent->>'respondentId';
  begin
    v_guardian_at := (p_consent->>'guardianConsentedAt')::timestamptz;
    v_student_at := (p_consent->>'studentAssentedAt')::timestamptz;
  exception when others then
    raise exception using errcode = 'GC002', message = 'Invalid guardian consent';
  end;
  -- Device clocks drift, so only reject clearly impossible answer times.
  if p_consent->'guardianAgreed' is distinct from 'true'::jsonb
     or p_consent->'guardianConfirmed' is distinct from 'true'::jsonb
     or p_consent->'studentAssented' is distinct from 'true'::jsonb
     or v_version is null or char_length(v_version) not between 1 and 64
     or v_name is null or char_length(v_name) not between 1 and 100
     or v_name <> btrim(v_name) or v_name ~ '[[:cntrl:]]'
     or v_respondent is null or v_respondent !~ '^[A-Za-z0-9._-]{1,64}$'
     or v_guardian_at is null or v_student_at is null
     or v_guardian_at not between now() - interval '30 days' and now() + interval '1 day'
     or v_student_at not between now() - interval '30 days' and now() + interval '1 day' then
    raise exception using errcode = 'GC002', message = 'Invalid guardian consent';
  end if;
  insert into public.guardian_consents(participant_id, study_id, consent_version, guardian_name,
    respondent_id, guardian_confirmed, guardian_consented_at, student_assented_at)
  values (p_participant_id, p_study_id, v_version, v_name, v_respondent, true, v_guardian_at, v_student_at);
  update public.study_participants
    set consent_version = v_version,
        guardian_consent_at = least(v_guardian_at, now()),
        student_assent_at = least(v_student_at, now())
    where id = p_participant_id;
end;
$$;
revoke all on function public.record_guardian_consent(uuid, uuid, jsonb) from public, anon, authenticated;
grant execute on function public.record_guardian_consent(uuid, uuid, jsonb) to service_role;

drop function public.activate_open_play_participant(uuid);
create function public.activate_open_play_participant(p_user_id uuid, p_consent jsonb default null)
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
  -- The first activation after the web consent step stores the form; later ones reuse it.
  -- A failure rolls back the whole call, so no participant exists without consent.
  if v_participant.entry_mode = 'open_play' and v_study.requires_guardian_consent
     and not exists (select 1 from public.guardian_consents c where c.participant_id = v_participant.id) then
    perform public.record_guardian_consent(v_participant.id, v_study.id, p_consent);
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

update public.studies set requires_guardian_consent = true where study_key = 'geoquest-open-play-2026-09';
comment on column public.study_participants.entry_mode is
  'Enrollment route only. An anonymous play record alone is not a survey response; open-play consent is in guardian_consents.';

notify pgrst, 'reload schema';
