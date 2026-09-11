-- Close abandoned research sessions using their last heartbeat. Legacy sessions
-- without a participant_id are deliberately excluded by the called function.
create extension if not exists pg_cron;
select cron.schedule(
  'geomodel-research-session-timeout',
  '* * * * *',
  $$select public.infer_stale_research_sessions(now() - interval '120 seconds');$$
);
