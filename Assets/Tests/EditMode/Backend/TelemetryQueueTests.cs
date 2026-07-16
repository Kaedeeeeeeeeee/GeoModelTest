using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TelemetryQueueTests
{
    [SetUp]
    public void SetUp()
    {
        var queueType = BackendTestReflection.GetType("Backend.TelemetryQueue");
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(queueType, "PendingEventsPrefsKey"));
        PlayerPrefs.Save();
    }

    [Test]
    public void PeekBatch_ShouldRespectBatchSize()
    {
        var queueType = BackendTestReflection.GetType("Backend.TelemetryQueue");
        var queue = System.Activator.CreateInstance(queueType, 10);
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { CreateBoundEvent(queueType, "scene_loaded") });
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { CreateBoundEvent(queueType, "tool_used") });
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { CreateBoundEvent(queueType, "quest_started") });

        var batch = queueType.GetMethod("PeekBatch").Invoke(queue, new object[] { 2 });

        Assert.AreEqual(2, BackendTestReflection.GetProperty(batch, "Count"));
    }

    [Test]
    public void Enqueue_ShouldClampOversizedProps()
    {
        var queueType = BackendTestReflection.GetType("Backend.TelemetryQueue");
        var queue = System.Activator.CreateInstance(queueType, 10);
        int maxEventPropsBytes = (int)BackendTestReflection.GetField(queueType, "MaxEventPropsBytes");
        var evt = BackendTestReflection.InvokeStatic(
            queueType,
            "Create",
            "tool_used",
            "MainScene",
            new Dictionary<string, object>
            {
                ["blob"] = new string('x', maxEventPropsBytes + 1024)
            });
        Bind(evt);

        queueType.GetMethod("Enqueue").Invoke(queue, new[] { evt });

        var props = (Dictionary<string, object>)BackendTestReflection.GetField(evt, "props");
        int size = (int)BackendTestReflection.InvokeStatic(queueType, "EstimateJsonBytes", props);
        Assert.IsTrue((bool)props["truncated"]);
        Assert.LessOrEqual(size, maxEventPropsBytes);
    }

    [Test]
    public void PersistedQuizAttempt_ShouldKeepStableEventId_WhenQueueReloads()
    {
        var queueType = BackendTestReflection.GetType("Backend.TelemetryQueue");
        var attemptType = BackendTestReflection.GetType("Backend.QuizAttemptUpload");
        var attempt = System.Activator.CreateInstance(attemptType);
        string eventId = System.Guid.NewGuid().ToString("D");
        BackendTestReflection.SetField(attempt, "eventId", eventId);
        BackendTestReflection.SetField(attempt, "runId", System.Guid.NewGuid().ToString("D"));
        BackendTestReflection.SetField(attempt, "questionId", "q.weathering_order");
        BackendTestReflection.SetField(attempt, "choiceId", "q.weathering_order.correct_sequence");
        BackendTestReflection.SetField(attempt, "attemptIndex", 1);
        BackendTestReflection.SetField(attempt, "occurredAt", System.DateTimeOffset.UtcNow.ToString("o"));
        Bind(attempt);

        var firstQueue = System.Activator.CreateInstance(queueType, 10);
        Assert.IsTrue((bool)BackendTestReflection.InvokeInstance(firstQueue, "EnqueueQuizAttempt", attempt));

        var reloadedQueue = System.Activator.CreateInstance(queueType, 10);
        var batch = BackendTestReflection.InvokeInstance(reloadedQueue, "PeekBatch", 10);
        var attempts = (System.Collections.IList)BackendTestReflection.GetField(batch, "quizAttempts");

        Assert.AreEqual(1, attempts.Count);
        Assert.AreEqual(eventId, BackendTestReflection.GetField(attempts[0], "eventId"));
    }

    [Test]
    public void Enqueue_ShouldRejectExpiredOfflineData()
    {
        var queueType = BackendTestReflection.GetType("Backend.TelemetryQueue");
        var queue = System.Activator.CreateInstance(queueType, 10);
        var evt = CreateBoundEvent(queueType, "scene_loaded");
        BackendTestReflection.SetField(
            evt,
            "occurredAt",
            System.DateTimeOffset.UtcNow.AddDays(-31).ToString("o"));

        Assert.IsFalse((bool)BackendTestReflection.InvokeInstance(queue, "Enqueue", evt));
        Assert.AreEqual(0, BackendTestReflection.GetProperty(queue, "Count"));
    }

    private static object CreateBoundEvent(System.Type queueType, string name)
    {
        var evt = BackendTestReflection.InvokeStatic(queueType, "Create", name, "MainScene", null);
        Bind(evt);
        return evt;
    }

    private static void Bind(object value)
    {
        BackendTestReflection.SetField(value, "participantId", "11111111-1111-4111-8111-111111111111");
        BackendTestReflection.SetField(value, "studyId", "22222222-2222-4222-8222-222222222222");
        BackendTestReflection.SetField(value, "condition", "A");
        BackendTestReflection.SetField(value, "sessionId", "33333333-3333-4333-8333-333333333333");
    }
}
