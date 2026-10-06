using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class MobileWheelTouchRegressionTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private Component _wheel;
    private Component _controls;
    private Component _tools;
    private object _mobile;
    private Component _mobileBefore;
    private object _mobileInputModeBefore;
    private bool _mobileDesktopTestModeBefore;
    private Touchscreen _touchscreen;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private EventSystem _eventSystem;
    private bool _eventSystemEnabled;
    private readonly Dictionary<BaseInputModule, bool> _inputModules = new Dictionary<BaseInputModule, bool>();
    private readonly HashSet<GameObject> _persistentRootsBefore = new HashSet<GameObject>();
    private InputSettings.UpdateMode _updateMode;
    private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
    private UnityEditor.EditorWindow _gameView;
    private int _gameViewSizeIndex;
#endif
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private float _timeScale;
    private CursorLockMode _cursorLock;
    private bool _cursorVisible;
    private Vector2Int _screenSize;

    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Prop(object target, string name) => (target as Type ?? target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args) =>
        (target as Type ?? target.GetType()).GetMethod(name, Flags).Invoke(target is Type ? null : target, args);
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name), FindObjectsInactive.Include);
    private bool IsOpen => (bool)Prop(_wheel, "IsWheelOpen");
    private string CurrentToolId => Call(_tools, "GetCurrentTool") is object tool ? (string)Get(tool, "toolID") : "none";
    private Vector2 Center => RectTransformUtility.WorldToScreenPoint(null, ((GameObject)Get(_wheel, "wheelUI")).transform.position);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _inputModules.Clear();
        _persistentRootsBefore.Clear();
        _mobileBefore = Find("MobileInputManager");
        if (_mobileBefore != null)
        {
            _mobileInputModeBefore = Get(_mobileBefore, "currentInputMode");
            _mobileDesktopTestModeBefore = (bool)Get(_mobileBefore, "desktopTestMode");
        }
        _timeScale = Time.timeScale;
        _cursorLock = Cursor.lockState;
        _cursorVisible = Cursor.visible;
        _backend = Resources.Load("BackendSettings");
        _backendEnabled = (bool)Get(_backend, "enableBackend");
        Set(_backend, "enableBackend", false);
        var probe = new GameObject("MobileWheelPersistentSceneProbe");
        UnityEngine.Object.DontDestroyOnLoad(probe);
        foreach (var root in probe.scene.GetRootGameObjects())
            if (root != probe) _persistentRootsBefore.Add(root);
        UnityEngine.Object.DestroyImmediate(probe);
        _updateMode = InputSystem.settings.updateMode;
        _background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        _editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        _touchscreen = InputSystem.AddDevice<Touchscreen>();
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _mouse = InputSystem.AddDevice<Mouse>();
        _touchscreen.MakeCurrent();
        _keyboard.MakeCurrent();
        _mouse.MakeCurrent();
        _screenSize = new Vector2Int(Screen.width, Screen.height);
#if UNITY_EDITOR
        var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
        _gameView = UnityEditor.EditorWindow.GetWindow(gameViewType);
        _gameViewSizeIndex = (int)gameViewType.GetProperty("selectedSizeIndex", Flags).GetValue(_gameView);
#endif
        typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
            .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { 844, 390 });

        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        Call(Prop(T("StorySystem.StoryDirector"), "Instance"), "CancelPlayback");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        Set(quests, "autoStartIntroQuestIfNone", false);
        Call(quests, "CancelPendingPlayback");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;

        _wheel = Find("InventoryUISystem");
        _controls = Find("MobileControlsUI");
        _tools = Find("ToolManager");
        _mobile = Prop(T("MobileInputManager"), "Instance");
        Assert.NotNull(_wheel);
        Assert.NotNull(_controls);
        Assert.NotNull(_tools);
        Call(_mobile, "EnableDesktopTestMode", true);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        Set(_controls, "forceShowOnDesktop", true);
        _controls.gameObject.SetActive(true);
        Set(_controls, "inputManager", _mobile);
        Call(_controls, "SetupCanvas");
        Call(_controls, "SetupVirtualControls");
        // Drive the real Update input handlers explicitly, so execution order is controlled.
        ((Behaviour)_controls).enabled = false;
        ((Behaviour)_wheel).enabled = false;
        _eventSystem = EventSystem.current;
        if (_eventSystem != null)
        {
            _eventSystemEnabled = _eventSystem.enabled;
            _eventSystem.enabled = false;
        }
        foreach (var module in UnityEngine.Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            _inputModules[module] = module.enabled;
            module.enabled = false;
        }
        var persistent = Find("PlayerPersistentData");
        foreach (string id in new[] { "999", "1002", "1001" }) Call(persistent, "MarkToolUnlocked", id);
        Call(persistent, "ApplyUnlockedToolsToScene");
        Call(_wheel, "InitializeTools");
        Call(_tools, "UnequipCurrentTool");
        Canvas.ForceUpdateCanvases();
        // CancelPlayback deliberately keeps gameplay blocked through the next frame.
        // Let that real dialogue-close protection expire before injecting test touches.
        yield return null;
        yield return null;
        Assert.AreEqual(844, Screen.width);
        Assert.AreEqual(390, Screen.height);
        Assert.IsFalse((bool)Prop(T("Core.GameInputState"), "GameplayBlocked"));
        Assert.IsFalse((bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive"));
    }

    [UnityTest]
    public IEnumerator TopToolsTouchRelease_ShouldKeepWheelOpenThenAllowNewHammerTouch()
    {
        var button = (Button)Get(_controls, "toolWheelButton");
        Vector2 buttonPoint = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        foreach (bool wheelRunsFirst in new[] { false, true })
        {
            string order = wheelRunsFirst ? "wheel-before-controls" : "controls-before-wheel";
            Call(_tools, "UnequipCurrentTool");
            Touch(1, TouchPhase.Began, buttonPoint);
            Call(_controls, "ProcessRawTouchFallbackInput");
            Assert.IsFalse(IsOpen, "The real top button must open on release, not on touch-down.");
            Touch(1, TouchPhase.Ended, buttonPoint);
            Assert.IsTrue(_touchscreen.primaryTouch.press.wasReleasedThisFrame);
            if (wheelRunsFirst) Call(_wheel, "Update");
            Call(_controls, "ProcessRawTouchFallbackInput");
            Assert.IsTrue(IsOpen, "The real raw-touch top button must open the wheel.");
            Assert.AreEqual("1001", ToolIdAt(buttonPoint), "The opening button direction must reproduce the drill-tower sector.");
            // No yield or input update here: the wheel observes the very same opening release.
            Call(_wheel, "Update");
            Debug.LogWarning($"[MobileWheelRegression] order={order}; released={_touchscreen.primaryTouch.press.wasReleasedThisFrame}; wheelOpen={IsOpen}; currentTool={CurrentToolId}; expectedTool=none");
            Assert.IsTrue(IsOpen, "Opening-touch release must leave the wheel open regardless of Update order.");
            Assert.AreEqual("none", CurrentToolId, "Opening-touch release must not equip the drill tower.");
            InputSystem.Update();
            yield return null;

            Vector2 hammerPoint = SectorPoint("1002", false);
            Touch(2, TouchPhase.Began, hammerPoint);
            Call(_wheel, "Update");
            Assert.IsTrue(IsOpen, "The new hammer touch selects only on release.");
            Touch(2, TouchPhase.Ended, hammerPoint);
            Call(_wheel, "Update");
            Assert.IsFalse(IsOpen);
            Assert.AreEqual("1002", CurrentToolId, "A fresh hammer-sector touch must equip the real hammer.");
            Debug.LogWarning($"[MobileWheelRegression] order={order}; newTouchSelected={CurrentToolId}; wheelOpen={IsOpen}");
            InputSystem.Update();
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator WheelOpenedDuringHeldTouch_ShouldIgnoreItAndPreserveDirectionalSelectionAndDeadZone()
    {
        Vector2 point = new Vector2(40f, Screen.height * 0.5f);
        Touch(10, TouchPhase.Began, point);
        // Models any mobile entry opening while a previously begun finger is still down.
        Call(_mobile, "TriggerToolWheelInput");
        Call(_wheel, "Update");
        Touch(10, TouchPhase.Ended, point);
        Call(_wheel, "Update");
        Assert.IsTrue(IsOpen, "A finger already held when the wheel opens must never become its selection gesture.");
        Assert.AreEqual("none", CurrentToolId);
        InputSystem.Update();
        yield return null;

        Touch(11, TouchPhase.Began, Center);
        Call(_wheel, "Update");
        Touch(11, TouchPhase.Ended, Center);
        Call(_wheel, "Update");
        Assert.IsTrue(IsOpen, "A fresh touch ending in the center dead zone must keep the wheel open.");
        Assert.AreEqual("none", CurrentToolId);
        InputSystem.Update();
        yield return null;

        Touch(12, TouchPhase.Began, SectorPoint("999", false));
        Call(_wheel, "Update");
        Vector2 outsideHammer = SectorPoint("1002", true);
        Touch(12, TouchPhase.Moved, outsideHammer);
        Call(_wheel, "Update");
        Touch(12, TouchPhase.Ended, outsideHammer);
        Call(_wheel, "Update");
        Assert.IsFalse(IsOpen);
        Assert.AreEqual("1002", CurrentToolId, "The final direction outside the rim must retain the October 5 hammer selection behavior.");
        Debug.LogWarning("[MobileWheelRegression] preexistingHeldTouch=ignored; centerDeadZone=preserved; outsideReleaseDirection=1002");
        InputSystem.Update();
        yield return null;

        Call(_tools, "UnequipCurrentTool");
        Call(_mobile, "TriggerToolWheelInput");
        yield return null;
        Touch(13, TouchPhase.Began, SectorPoint("1002", false));
        Call(_wheel, "Update");
        Touch(13, TouchPhase.Canceled, SectorPoint("1002", false));
        Call(_wheel, "Update");
        Assert.IsTrue(IsOpen, "Canceled touches must not equip a tool.");
        Assert.AreEqual("none", CurrentToolId);
        InputSystem.Update();
        yield return null;

        Vector2 shortTap = SectorPoint("1002", false);
        InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = 14, phase = TouchPhase.Began, position = shortTap });
        InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = 14, phase = TouchPhase.Ended, position = shortTap });
        InputSystem.Update();
        Assert.IsFalse(_touchscreen.primaryTouch.press.isPressed, "The short tap must end before the wheel samples input.");
        Call(_wheel, "Update");
        Assert.IsFalse(IsOpen);
        Assert.AreEqual("1002", CurrentToolId, "A fresh touch begun and ended in one input update must still select the hammer.");
        Debug.LogWarning("[MobileWheelRegression] canceledTouch=ignored; sameUpdateShortTap=1002");
    }

    [UnityTest]
    public IEnumerator DesktopTab_ShouldIgnoreTouchAlreadyBegunInSameInputFrameAndAllowMouseSelection()
    {
        Call(_mobile, "EnableDesktopTestMode", false);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        Call(_wheel, "RefreshMobileModeState");
        InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = 20, phase = TouchPhase.Began, position = new Vector2(40f, Screen.height * 0.5f) });
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Tab));
        InputSystem.Update();
        Call(_wheel, "Update");
        Assert.IsTrue(IsOpen, "A touch begun before Tab opens the wheel in the same input frame must not bypass touch ownership via the desktop path.");
        Assert.AreEqual("none", CurrentToolId);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        Touch(20, TouchPhase.Ended, new Vector2(40f, Screen.height * 0.5f));
        InputSystem.Update();
        yield return null;

        Vector2 hammer = SectorPoint("1002", true);
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = hammer, buttons = 1 });
        InputSystem.Update();
        Call(_wheel, "Update");
        Assert.IsFalse(IsOpen);
        Assert.AreEqual("1002", CurrentToolId, "Desktop Tab and mouse direction selection must remain available.");
        Debug.LogWarning("[MobileWheelRegression] desktopTabPreexistingTouch=ignored; desktopMouseOutsideSector=1002");
        Call(_tools, "UnequipCurrentTool");
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = Center });
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Tab));
        InputSystem.Update();
        Call(_wheel, "Update");
        Assert.IsTrue(IsOpen);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        InputSystem.Update();
        yield return null;
        Touch(21, TouchPhase.Began, SectorPoint("1002", false));
        Call(_wheel, "Update");
        Assert.IsFalse(IsOpen);
        Assert.AreEqual("1002", CurrentToolId, "A fresh touch after Tab must retain desktop touch compatibility.");
        Touch(21, TouchPhase.Ended, SectorPoint("1002", false));
        Debug.LogWarning("[MobileWheelRegression] desktopFreshTouch=1002");
    }

    private void Touch(int id, TouchPhase phase, Vector2 position)
    {
        InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = id, phase = phase, position = position });
        InputSystem.Update();
    }

    private string ToolIdAt(Vector2 point)
    {
        int slot = (int)Call(_wheel, "GetWheelSlotAtScreenPoint", point);
        var tools = (IList)Get(_wheel, "availableTools");
        return slot >= 0 && slot < tools.Count ? (string)Get(tools[slot], "toolID") : "none";
    }

    private Vector2 SectorPoint(string toolId, bool outside)
    {
        var tools = (IList)Get(_wheel, "availableTools");
        int slot = tools.Cast<object>().ToList().FindIndex(t => (string)Get(t, "toolID") == toolId);
        Assert.GreaterOrEqual(slot, 0);
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform[])Get(_wheel, "wheelSlots"))[slot].position);
        if (!outside) return point;
        var rect = (RectTransform)((GameObject)Get(_wheel, "wheelUI")).transform;
        float radius = rect.rect.width * rect.GetComponentInParent<Canvas>().scaleFactor * 0.5f;
        return Center + (point - Center).normalized * (radius + 20f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_wheel != null && IsOpen) Call(_wheel, "CloseWheel", false);
        if (_tools != null) Call(_tools, "UnequipCurrentTool");
        if (_mobileBefore != null)
        {
            Call(_mobileBefore, "EnableDesktopTestMode", _mobileDesktopTestModeBefore);
            Call(_mobileBefore, "SwitchInputMode", _mobileInputModeBefore);
        }
        Call(Prop(T("StorySystem.StoryDirector"), "Instance"), "CancelPlayback");
        Call(Prop(T("QuestSystem.QuestManager"), "Instance"), "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        // Remove this fixture's real scene and newly created persistent roots while
        // its synthetic devices are still valid. A UI module must not retain pointers
        // to a removed touchscreen between tests.
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) UnityEngine.Object.Destroy(root);
        var probe = new GameObject("MobileWheelPersistentSceneProbe");
        UnityEngine.Object.DontDestroyOnLoad(probe);
        foreach (var root in probe.scene.GetRootGameObjects())
            if (root != probe && !_persistentRootsBefore.Contains(root)) UnityEngine.Object.Destroy(root);
        UnityEngine.Object.Destroy(probe);
        yield return null;
        if (_touchscreen != null) InputSystem.RemoveDevice(_touchscreen);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        foreach (var pair in _inputModules)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        if (_eventSystem != null) _eventSystem.enabled = _eventSystemEnabled;
        InputSystem.settings.updateMode = _updateMode;
        InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
#endif
        if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
#if UNITY_EDITOR
        if (_gameView != null)
        {
            _gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(_gameView, _gameViewSizeIndex);
            _gameView.Repaint();
            _gameView.Focus();
        }
        else
#endif
        {
            typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
                .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { _screenSize.x, _screenSize.y });
        }
        Time.timeScale = _timeScale;
        Cursor.lockState = _cursorLock;
        Cursor.visible = _cursorVisible;
        yield return null;
        yield return null;
        // A free-aspect GameView can resolve to a different viewport after batch-mode
        // focus changes. Restore the actual original dimensions before the next fixture.
        if (Screen.width != _screenSize.x || Screen.height != _screenSize.y)
        {
            typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
                .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { _screenSize.x, _screenSize.y });
            yield return null;
            yield return null;
        }
        Canvas.ForceUpdateCanvases();
        Debug.LogWarning($"[MobileWheelRegression] restoredScreen={Screen.width}x{Screen.height}; originalScreen={_screenSize.x}x{_screenSize.y}; safeArea={Screen.safeArea}; mobileUIAdapterAlive={Find("MobileUIAdapter") != null}");
        Assert.AreEqual(_screenSize.x, Screen.width, "Fixture cleanup must restore the actual original viewport width.");
        Assert.AreEqual(_screenSize.y, Screen.height, "Fixture cleanup must restore the actual original viewport height.");
    }
}
