-- Supabase backend v1 for anonymous Unity WebGL telemetry and progress snapshots.
-- Tables are not exposed to game clients. Writes go through the game-ingest Edge Function.

create extension if not exists pgcrypto;

create or replace function public.backend_set_updated_at()
returns trigger
language plpgsql
set search_path = public
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

revoke all on function public.backend_set_updated_at() from public;

create table if not exists public.player_profiles (
  user_id uuid primary key references auth.users(id) on delete cascade,
  install_id text not null,
  first_seen_at timestamptz not null default now(),
  last_seen_at timestamptz not null default now(),
  first_game_version text,
  latest_game_version text,
  platform text,
  language text,
  build_target text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint player_profiles_install_id_format
    check (install_id ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')
);

create table if not exists public.game_sessions (
  id uuid primary key,
  user_id uuid not null references auth.users(id) on delete cascade,
  install_id text not null,
  started_at timestamptz not null default now(),
  ended_at timestamptz,
  game_version text,
  platform text,
  build_target text,
  language text,
  last_scene text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint game_sessions_install_id_format
    check (install_id ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')
);

create table if not exists public.telemetry_events (
  id uuid primary key,
  user_id uuid not null references auth.users(id) on delete cascade,
  session_id uuid not null references public.game_sessions(id) on delete cascade,
  install_id text not null,
  event_name text not null,
  event_props jsonb not null default '{}'::jsonb,
  occurred_at timestamptz not null,
  received_at timestamptz not null default now(),
  game_version text,
  scene_name text,
  constraint telemetry_events_name_allowed check (
    event_name in (
      'session_started',
      'session_ended',
      'scene_loaded',
      'tool_equipped',
      'tool_used',
      'quest_started',
      'objective_completed',
      'quest_completed',
      'progress_dirty',
      'manual_flush'
    )
  ),
  constraint telemetry_events_install_id_format
    check (install_id ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')
);

create table if not exists public.progress_snapshots (
  user_id uuid primary key references auth.users(id) on delete cascade,
  install_id text not null,
  session_id uuid references public.game_sessions(id) on delete set null,
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
  updated_at timestamptz not null,
  received_at timestamptz not null default now(),
  constraint progress_snapshots_install_id_format
    check (install_id ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')
);

create index if not exists idx_player_profiles_install_id on public.player_profiles(install_id);
create index if not exists idx_player_profiles_last_seen_at on public.player_profiles(last_seen_at desc);

create index if not exists idx_game_sessions_user_id on public.game_sessions(user_id);
create index if not exists idx_game_sessions_install_id on public.game_sessions(install_id);
create index if not exists idx_game_sessions_started_at on public.game_sessions(started_at desc);

create index if not exists idx_telemetry_events_user_id on public.telemetry_events(user_id);
create index if not exists idx_telemetry_events_session_id on public.telemetry_events(session_id);
create index if not exists idx_telemetry_events_event_name on public.telemetry_events(event_name);
create index if not exists idx_telemetry_events_occurred_at on public.telemetry_events(occurred_at desc);
create index if not exists idx_telemetry_events_received_at on public.telemetry_events(received_at desc);

create index if not exists idx_progress_snapshots_updated_at on public.progress_snapshots(updated_at desc);

drop trigger if exists trg_player_profiles_updated_at on public.player_profiles;
create trigger trg_player_profiles_updated_at
before update on public.player_profiles
for each row execute function public.backend_set_updated_at();

drop trigger if exists trg_game_sessions_updated_at on public.game_sessions;
create trigger trg_game_sessions_updated_at
before update on public.game_sessions
for each row execute function public.backend_set_updated_at();

alter table public.player_profiles enable row level security;
alter table public.game_sessions enable row level security;
alter table public.telemetry_events enable row level security;
alter table public.progress_snapshots enable row level security;

-- Supabase Data API visibility is now grant-based for new public tables.
-- Keep direct table access closed to browser/mobile clients; service_role is used only by Edge Functions.
revoke all on table public.player_profiles from anon, authenticated;
revoke all on table public.game_sessions from anon, authenticated;
revoke all on table public.telemetry_events from anon, authenticated;
revoke all on table public.progress_snapshots from anon, authenticated;

grant select, insert, update, delete on table public.player_profiles to service_role;
grant select, insert, update, delete on table public.game_sessions to service_role;
grant select, insert, update, delete on table public.telemetry_events to service_role;
grant select, insert, update, delete on table public.progress_snapshots to service_role;
