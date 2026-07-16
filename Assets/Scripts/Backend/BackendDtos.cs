using System.Collections.Generic;
using Newtonsoft.Json;

namespace Backend
{
    public sealed class BackendAuthResponse
    {
        [JsonProperty("access_token")] public string accessToken;
        [JsonProperty("refresh_token")] public string refreshToken;
        [JsonProperty("expires_in")] public int expiresIn;
        [JsonProperty("user")] public BackendAuthUser user;
    }

    public sealed class BackendAuthUser
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("is_anonymous")] public bool isAnonymous;
    }

    public sealed class TelemetryEvent
    {
        public string id;
        public string participantId;
        public string studyId;
        public string condition;
        public string sessionId;
        public string name;
        public string occurredAt;
        public string sceneName;
        public Dictionary<string, object> props = new Dictionary<string, object>();
    }

    public sealed class ProgressSnapshot
    {
        public string eventId;
        public string participantId;
        public string studyId;
        public string condition;
        public string sessionId;
        public string currentScene;
        public List<string> completedQuests = new List<string>();
        public List<string> completedObjectives = new List<string>();
        public List<string> storyFlags = new List<string>();
        public List<string> unlockedToolIds = new List<string>();
        public int inventoryCount;
        public int warehouseCount;
        public int encyclopediaDiscovered;
        public int encyclopediaTotal;
        public string updatedAt;
        public Dictionary<string, object> payload = new Dictionary<string, object>();
    }

    public sealed class SessionEndPayload
    {
        public string endedAt;
        public string reason;
    }

    public sealed class QuizAttemptUpload
    {
        public string eventId;
        public string participantId;
        public string studyId;
        public string condition;
        public string sessionId;
        public string runId;
        public string questionId;
        public string questionVersion;
        public string choiceId;
        public int attemptIndex;
        public bool isCorrect;
        public bool usedHint;
        public long responseTimeMs;
        public string occurredAt;
        public string gameVersion;
        public string contentVersion;
        public string storyRoute;
    }

    public sealed class ResearchParticipationResponse
    {
        public bool ok;
        public string participantId;
        public string studyId;
        public string condition;
        public string protocolVersion;
        public string error;
    }

    public sealed class ResearchContext
    {
        public string participantId;
        public string studyId;
        public string condition;
        public string protocolVersion;
    }

    public sealed class TelemetryBatch
    {
        public string participantId;
        public string studyId;
        public string condition;
        public string sessionId;
        public List<TelemetryEvent> events = new List<TelemetryEvent>();
        public List<QuizAttemptUpload> quizAttempts = new List<QuizAttemptUpload>();

        public int Count => events.Count + quizAttempts.Count;
    }

    public sealed class IngestRequest
    {
        public string installId;
        public string participantId;
        public string studyId;
        public string condition;
        public string protocolVersion;
        public string sessionId;
        public string gameVersion;
        public string platform;
        public string buildTarget;
        public string language;
        public string currentScene;
        public string contentVersion;
        public string storyRoute;
        public List<TelemetryEvent> events = new List<TelemetryEvent>();
        public List<QuizAttemptUpload> quizAttempts = new List<QuizAttemptUpload>();
        public ProgressSnapshot progressSnapshot;
        public SessionEndPayload sessionEnd;
    }

    public static class BackendJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None
        };
    }
}
