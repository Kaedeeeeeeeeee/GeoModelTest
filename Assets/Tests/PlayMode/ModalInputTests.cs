using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public class ModalInputTests
{
    private Type State => Type.GetType("Core.GameInputState, Assembly-CSharp", true);
    private Type Settings => Type.GetType("SettingsManager, Assembly-CSharp", true);
    private static object Call(Type type, string method, object target = null, params object[] args) =>
        type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Invoke(target, args);
    private InputSettings.UpdateMode _previousUpdateMode;
    private InputSettings.BackgroundBehavior _previousBackgroundBehavior;
    private Keyboard _keyboard;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _previousEditorInputBehavior;
#endif

    [SetUp]
    public void SetUp()
    {
#if UNITY_EDITOR
        _previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
#endif
        _previousUpdateMode = InputSystem.settings.updateMode;
        _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
    }

    private bool IsOpen => (bool)State.GetProperty("IsModalOpen").GetValue(null);

    [UnityTest]
    public IEnumerator NestedModals_ShouldRestoreOriginalTimeOnlyAfterLastClose()
    {
        Time.timeScale = 0.75f;
        var parent = (IDisposable)Call(State, "Acquire", null, new object[] { null });
        var child = (IDisposable)Call(State, "Acquire", null, new object[] { null });
        yield return null;
        Assert.AreEqual(0f, Time.timeScale);
        parent.Dispose();
        yield return null;
        Assert.IsTrue(IsOpen);
        Assert.AreEqual(0f, Time.timeScale);
        child.Dispose();
        yield return null;
        Assert.IsFalse(IsOpen);
        Assert.AreEqual(0.75f, Time.timeScale);
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator Escape_ShouldBeConsumedOnceBySettingsAndModalRouter()
    {
        var settings = Settings.GetProperty("Instance").GetValue(null);
        Call(Settings, "CloseSettings", settings);
        Call(State, "ReleaseAll");
        Time.timeScale = 1f;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        _keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.EnableDevice(_keyboard);
        _keyboard.MakeCurrent();
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        Assert.IsTrue(_keyboard.escapeKey.wasPressedThisFrame, "Injected Escape must reach the game input state.");
        Assert.AreSame(_keyboard, Keyboard.current);
        // Batch-mode has no focused Game view: drive the production Update handlers
        // in their declared execution order in the same injected-input frame.
        Call(Settings, "Update", settings);
        Assert.IsTrue((bool)Settings.GetProperty("IsSettingsOpen").GetValue(settings));
        Assert.AreEqual(0f, Time.timeScale);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        InputSystem.Update();
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        var router = UnityEngine.Object.FindFirstObjectByType(State);
        Call(State, "Update", router);
        Call(Settings, "Update", settings);
        Assert.IsFalse((bool)Settings.GetProperty("IsSettingsOpen").GetValue(settings));
        Assert.IsFalse(IsOpen);
        Assert.AreEqual(1f, Time.timeScale);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        InputSystem.Update();
        yield return null;
        Assert.IsFalse(IsOpen);
    }

    [UnityTest]
    public IEnumerator HistoryOverSettings_ShouldPreservePauseUntilSettingsCloses()
    {
        var settings = Settings.GetProperty("Instance").GetValue(null);
        Time.timeScale = 1f;
        Call(Settings, "OpenSettings", settings);
        Type history = Type.GetType("StorySystem.StoryHistoryUI, Assembly-CSharp", true);
        Call(history, "Show");
        yield return null;
        Call(history, "CloseCurrent");
        yield return null;
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(IsOpen);
        Call(Settings, "CloseSettings", settings);
        yield return null;
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(IsOpen);
    }

    [TearDown]
    public void TearDown()
    {
        Call(State, "ReleaseAll");
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorInputBehavior;
#endif
        InputSystem.settings.updateMode = _previousUpdateMode;
        InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
        Time.timeScale = 1f;
    }
}
