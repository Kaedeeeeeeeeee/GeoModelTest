using System;
using UnityEngine;

namespace Backend
{
    /// <summary>Only acknowledged completion snapshots are eligible for questionnaire links.</summary>
    public static class SurveyCompletionStore
    {
        public const string StorageKey = "Backend.SurveyCompletion.v1";
        [Serializable]
        public sealed class Completion
        {
            public string participantId, sessionId, runId, server;
        }

        public static void Remember(ProgressSnapshot snapshot, string server)
        {
            if (snapshot?.payload == null ||
                !snapshot.payload.TryGetValue("investigationComplete", out var complete) || !string.Equals(complete.ToString(), "true", StringComparison.OrdinalIgnoreCase) ||
                !snapshot.payload.TryGetValue("runId", out var run) || !Guid.TryParse(run.ToString(), out _)) return;
            var state = new Completion { participantId = snapshot.participantId, sessionId = snapshot.sessionId, runId = run.ToString(), server = server.TrimEnd('/') };
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public static Completion Current(string server, string runId)
        {
            try
            {
                var state = JsonUtility.FromJson<Completion>(PlayerPrefs.GetString(StorageKey, ""));
                return state != null && state.runId == runId && state.server == server.TrimEnd('/') &&
                    state.participantId == PlayerPrefs.GetString(BackendSessionStore.ResearchParticipantIdKey, "") &&
                    Guid.TryParse(state.sessionId, out _) ? state : null;
            }
            catch { return null; }
        }
    }
}
