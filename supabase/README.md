# Supabase backend v1

This folder contains the lightweight backend for anonymous Unity WebGL telemetry and progress snapshots.

## Setup

1. Enable Supabase Authentication anonymous sign-ins for the project.
2. Run the migration in `supabase/migrations/20260618150000_backend_v1.sql`.
3. Deploy the Edge Function:

   ```bash
   supabase functions deploy game-ingest
   ```

4. Ensure the function has access to server-side secrets:

   ```bash
   supabase secrets set SUPABASE_SERVICE_ROLE_KEY=...
   ```

5. In Unity, create `Assets/Resources/BackendSettings.asset` from `Tools > Backend > Create Supabase Backend Settings`.
6. Fill in:
   - `Supabase Url`
   - `Publishable Key`
   - `Enable Backend`

Do not put `service_role` or secret keys in Unity assets, WebGL builds, or itch.io configuration. The Unity client only stores the publishable key and the anonymous user's access token.

## Data flow

- Unity signs in anonymously through Supabase Auth.
- Unity batches telemetry in memory and mirrors the latest unsent events to `PlayerPrefs`.
- Unity posts batches to `functions/v1/game-ingest`.
- The Edge Function validates the anonymous JWT and writes with the service role key.
- Direct table access for `anon` and `authenticated` is revoked; RLS remains enabled.
