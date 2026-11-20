using NUnit.Framework;

/// <summary>Study 1 players stalled without knowing the next step; Dr.Kaede's idle hint names it.</summary>
public class IdleHintWatcherTests
{
    private static object Hint(string destination, string scene, bool talk, bool hammer, bool tower, bool line) =>
        BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("UISystem.IdleHintWatcher"), "HintKey",
            destination, scene, talk, hammer, tower, line);

    [Test]
    public void Hint_ShouldNameTheNextStep()
    {
        Assert.AreEqual("ui.idle.talk", Hint(null, "Laboratory Scene", true, false, false, true));
        Assert.AreEqual("ui.idle.shift", Hint("MainScene", "Laboratory Scene", false, true, false, false));
        Assert.AreEqual("ui.idle.hammer", Hint(null, "MainScene", false, true, false, true));
        Assert.AreEqual("ui.idle.tower", Hint(null, "MainScene", false, false, true, true));
        Assert.AreEqual("ui.idle.follow", Hint(null, "Laboratory Scene", false, false, false, true));
        Assert.AreEqual("ui.idle.task", Hint(null, "Laboratory Scene", false, false, false, false));
    }

    [Test]
    public void Hints_ShouldWaitBeforeRepeating()
    {
        var type = BackendTestReflection.GetType("UISystem.IdleHintWatcher");
        Assert.AreEqual(20f, (float)BackendTestReflection.InvokeStatic(type, "DelayBeforeHint", 0));
        Assert.AreEqual(40f, (float)BackendTestReflection.InvokeStatic(type, "DelayBeforeHint", 1));
    }
}
