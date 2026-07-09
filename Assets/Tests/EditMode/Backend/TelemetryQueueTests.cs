using System.Collections;
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
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { BackendTestReflection.InvokeStatic(queueType, "Create", "scene_loaded", "MainScene", null) });
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { BackendTestReflection.InvokeStatic(queueType, "Create", "tool_used", "MainScene", null) });
        queueType.GetMethod("Enqueue").Invoke(queue, new[] { BackendTestReflection.InvokeStatic(queueType, "Create", "quest_started", "MainScene", null) });

        var batch = (ICollection)queueType.GetMethod("PeekBatch").Invoke(queue, new object[] { 2 });

        Assert.AreEqual(2, batch.Count);
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

        queueType.GetMethod("Enqueue").Invoke(queue, new[] { evt });

        var props = (Dictionary<string, object>)BackendTestReflection.GetField(evt, "props");
        int size = (int)BackendTestReflection.InvokeStatic(queueType, "EstimateJsonBytes", props);
        Assert.IsTrue((bool)props["truncated"]);
        Assert.LessOrEqual(size, maxEventPropsBytes);
    }
}
