-- Read-only administrator export; run with a trusted database connection.
-- One row per questionnaire. Quiz measures use the same participant AND run.
-- The invitation issuance register maps participant_id back to the distributed code.
select r.id as response_id, r.study_id, r.participant_id, r.run_id,
       r.session_id as completion_session_id, p.cohort, p.condition,
       r.survey_version, r.submitted_at,
       r.answers->>'q1' as q1, r.answers->>'q2' as q2,
       r.answers->>'q3' as q3, r.answers->>'q4' as q4,
       r.answers->>'q5' as q5, r.answers->>'q6' as q6,
       r.answers->>'q7' as q7, r.answers->>'q8' as q8,
       r.answers->>'q9' as q9, r.answers->>'q10' as q10,
       r.answers->>'q11' as q11, r.answers->>'q12' as q12,
       r.answers->>'q13' as q13, r.answers->>'q14' as q14,
       q.first_correct, q.wrong_attempts, q.mastered,
       s.ended_at as completion_session_ended_at
from public.survey_responses r
join public.study_participants p on p.id = r.participant_id
join public.game_sessions s on s.id = r.session_id
cross join lateral (
  select count(distinct question_id) filter (where attempt_index = 1 and is_correct) as first_correct,
         count(*) filter (where not is_correct) as wrong_attempts,
         count(distinct question_id) filter (where is_correct) as mastered
  from public.quiz_attempts a
  where a.participant_id = r.participant_id and a.run_id = r.run_id
) q
order by r.submitted_at, r.id;
