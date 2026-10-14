using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PhaseShifterGuidanceTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => (target as Type ?? target.GetType()).GetField(field, Flags).GetValue(target is Type ? null : target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Prop(object target, string name) => (target as Type ?? target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args) =>
        (target as Type ?? target.GetType()).GetMethod(name, Flags).Invoke(target is Type ? null : target, args);
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));
    private static readonly Type Hud = T("UISystem.CollectionGuidanceHUD");

    private object _wheel, _mobile, _director, _quests, _manager;
    private Component _tools;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private InputSettings.UpdateMode _updateMode;
    private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
#endif
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private int _viewSizeIndex = -1;
    private string _output;
    private readonly List<string> _checks = new List<string>();
    private readonly RealSceneFixtureCleanup _cleanup = new RealSceneFixtureCleanup();

    private void Check(bool condition, string message)
    {
        Assert.IsTrue(condition, message);
        _checks.Add("PASS " + message);
        File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
    }

    [UnityTest]
    public IEnumerator ActualScenes_ShouldGuideEachStepOfThePhaseShifterTrip()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_PHASE_SHIFTER_GUIDANCE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_PHASE_SHIFTER_GUIDANCE to verify the trip guidance in the actual scenes.");
        Directory.CreateDirectory(_output);
        _cleanup.Capture();
        _viewSizeIndex = GameViewSelection.Index;
        SetSize(960, 540);
        _backend = Resources.Load("BackendSettings");
        if (_backend != null) { _backendEnabled = (bool)Get(_backend, "enableBackend"); Set(_backend, "enableBackend", false); }
        _updateMode = InputSystem.settings.updateMode;
        _background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        _editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _mouse = InputSystem.AddDevice<Mouse>();
        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.lab.return");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        _mobile = Prop(T("MobileInputManager"), "Instance");
        _manager = Prop(T("GameSceneManager"), "Instance");
        Set(_quests, "autoStartIntroQuestIfNone", false);
        StopDialogue();
        Call(_director, "ReloadFlags");

        // The shipped manager route keeps the current-scene name that disables the "current location" button.
        Call(_manager, "SwitchToScene", "Laboratory Scene");
        yield return WaitForScene("Laboratory Scene");
        PrepareScene();
        Call(_mobile, "EnableDesktopTestMode", false);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        PrepareQuest(null);
        yield return Frames(8);
        Check(Destination() == null && Recommended() == null && !Highlight() && Card() == null,
            "A laboratory without a field objective shows no trip cue.");

        // Step 1: the quest sends the player outdoors, so the cue starts at the tool list.
        PrepareQuest("q.field.phase");
        yield return Frames(8);
        Check(Destination() == "MainScene", "A pending field objective recommends the field from the laboratory: " + Destination());
        Check(Recommended() == "999" && !Highlight(), "The cue first recommends the phase shifter: " + Recommended());
        Check(Hint() == "Tabで道具を開き、光っている「フェーズシフター」を選ぼう。", "Desktop cue names the tool to pick: " + Hint());
        Check(TitleText() == "フェーズシフター", "The card is titled with the phase shifter: " + TitleText());
        yield return Capture("01-lab-desktop-select-shifter");

        // Step 2: the wheel highlights the shifter while the card yields to it.
        int slot = Slot("999");
        yield return Tap(Key.Tab);
        MoveMouse(RectTransformUtility.WorldToScreenPoint(null, ((GameObject)Get(_wheel, "wheelUI")).transform.position));
        yield return Frames(4);
        Check((bool)Prop(_wheel, "IsWheelOpen") && Recommended() == "999" && Card() == null,
            "The open wheel keeps the recommendation and hides the card.");
        var sectors = (Graphic[])Get(_wheel, "sectorGraphics");
        Check(sectors[slot].color != (Color)Get(_wheel, "slotBackgroundColor") &&
            sectors.Where((g, i) => i != slot).All(g => g.color == (Color)Get(_wheel, "slotBackgroundColor")),
            "Only the phase-shifter sector is highlighted.");
        yield return Capture("02-lab-desktop-wheel");

        // Step 3: holding the shifter is not using it, so the cue moves to the primary action.
        Call(_wheel, "SelectToolAndStartPreview", slot);
        Call(_wheel, "CloseWheel", false);
        yield return Frames(8);
        var shifter = Call(_tools, "GetCurrentTool");
        Check(shifter != null && (string)Get(shifter, "toolID") == "999", "The real wheel equips the phase shifter.");
        Check(Recommended() == null && Highlight(), "With the shifter in hand the cue asks for the primary action.");
        Check(Hint() == "左クリックして、移動先の「野外調査エリア」を選ぼう。", "Desktop cue names the action and destination: " + Hint());
        yield return Capture("03-lab-desktop-use-shifter");

        // Step 4: the destination list marks where the quest is going.
        yield return WaitUntilUsable(shifter);
        Check((bool)Call(shifter, "RequestPrimaryUse"), "The shipped primary action opens the destination list.");
        yield return Frames(4);
        var selection = (GameObject)Get(_manager, "sceneSelectionUI");
        Check(selection != null && selection.activeInHierarchy && Card() == null, "The destination list replaces the card.");
        var fieldOutline = SceneOutline(selection, "MainScene");
        Check(fieldOutline.enabled && !SceneOutline(selection, "Laboratory Scene").enabled,
            "Only the field destination is outlined in the laboratory.");
        Color first = fieldOutline.effectColor;
        yield return new WaitForSecondsRealtime(0.3f);
        Check(fieldOutline.effectColor != first, "The destination outline breathes like the other cues.");
        yield return Capture("04-lab-destination-list");

        // Closing the list puts the shifter away, so the cue restarts from the tool list.
        Call(_manager, "CancelSceneSelection");
        yield return Frames(8);
        Check(Call(_tools, "GetCurrentTool") == null && Recommended() == "999" && !Highlight(),
            "Cancelling the trip restarts the cue from tool selection.");

        // The same trip on the virtual controls.
        var controls = EnableTouch();
        SetSize(844, 390);
        yield return Frames(10);
        Check(Hint() == "道具ボタンをタップし、光っている「フェーズシフター」を選ぼう。", "Touch cue points at the tool button: " + Hint());
        Check(ControlOutline(controls, "toolWheelGuidanceOutline").enabled && !ControlOutline(controls, "interactGuidanceOutline").enabled,
            "Touch step 1 outlines only the real tool button.");
        var openTools = GameObject.Find("CollectionGuidanceCanvas/CollectionHint/OpenTools");
        Check(openTools != null && openTools.activeInHierarchy, "Touch step 1 offers the tool-menu entrance on the card.");
        CheckCardLayout(controls, "touch step 1");
        yield return Capture("05-lab-touch-select-shifter");

        openTools.GetComponent<Button>().onClick.Invoke();
        yield return Frames(6);
        Check((bool)Prop(_wheel, "IsWheelOpen") && Recommended() == "999", "The card entrance opens the wheel with the shifter recommended.");
        yield return Capture("06-lab-touch-wheel");
        var slots = (RectTransform[])Get(_wheel, "wheelSlots");
        Check((bool)Call(_wheel, "TrySelectToolAtScreenPoint", ScreenRect(slots[Slot("999")]).center, "触屏"),
            "Touch selection reaches the phase-shifter slot.");
        Call(_wheel, "CloseWheel", false);
        yield return Frames(8);
        shifter = Call(_tools, "GetCurrentTool");
        Check(shifter != null && (string)Get(shifter, "toolID") == "999", "Touch selection equips the phase shifter.");
        Check(Hint() == "「調べる」をタップして、移動先の「野外調査エリア」を選ぼう。", "Touch cue names the button and destination: " + Hint());
        Check(ControlOutline(controls, "interactGuidanceOutline").enabled && !ControlOutline(controls, "toolWheelGuidanceOutline").enabled,
            "Touch step 2 outlines only the real interact button.");
        Check(!openTools.activeInHierarchy, "The tool-menu entrance leaves once the shifter is in hand.");
        CheckCardLayout(controls, "touch step 2");
        yield return Capture("07-lab-touch-use-shifter");

        // The real interact button reaches the shifter through the shipped mobile routing.
        yield return WaitUntilUsable(shifter);
        Call(controls, "OnInteractButtonDown");
        yield return Frames(3);
        Call(controls, "OnInteractButtonUp");
        yield return Frames(6);
        selection = (GameObject)Get(_manager, "sceneSelectionUI");
        Check(selection != null && selection.activeInHierarchy, "The interact button opens the destination list.");
        Check(SceneOutline(selection, "MainScene").enabled, "The field destination is outlined for touch as well.");
        Check(!ControlOutline(controls, "interactGuidanceOutline").enabled, "The interact outline stops once the list is open.");
        yield return Capture("08-lab-touch-destination-list");

        // Travelling ends the cue: the field objective is now in the current scene.
        selection.GetComponentsInChildren<Button>(true).First(b => b.name == "SceneButton_MainScene").onClick.Invoke();
        yield return WaitForScene("MainScene");
        PrepareScene();
        yield return Frames(10);
        Check(Destination() == null && Recommended() != "999" && !Highlight(),
            "Arriving in the field ends the trip cue: " + Destination() + "/" + Recommended());
        yield return Capture("09-field-arrived");

        // The way back uses the same steps with the laboratory as the destination.
        PrepareQuest("q.lab.return");
        controls = EnableTouch();
        Call(_tools, "UnequipCurrentTool");
        yield return Frames(10);
        Check(Destination() == "Laboratory Scene" && Recommended() == "999",
            "A pending return objective recommends the laboratory from the field: " + Destination() + "/" + Recommended());
        Check(Hint() == "道具ボタンをタップし、光っている「フェーズシフター」を選ぼう。", "Return cue starts at the tool button: " + Hint());
        yield return Capture("10-field-touch-select-shifter");
        Call(_wheel, "SelectToolAndStartPreview", Slot("999"));
        Call(_wheel, "CloseWheel", false);
        yield return Frames(8);
        shifter = Call(_tools, "GetCurrentTool");
        Check(Hint() == "「調べる」をタップして、移動先の「G-Lab研究室」を選ぼう。", "Return cue names the laboratory: " + Hint());
        Check(ControlOutline(controls, "interactGuidanceOutline").enabled, "Return step 2 outlines the interact button.");
        yield return Capture("11-field-touch-use-shifter");
        yield return WaitUntilUsable(shifter);
        Call(controls, "OnInteractButtonDown");
        yield return Frames(3);
        Call(controls, "OnInteractButtonUp");
        yield return Frames(6);
        selection = (GameObject)Get(_manager, "sceneSelectionUI");
        Check(selection != null && selection.activeInHierarchy, "The interact button opens the destination list in the field.");
        Check(SceneOutline(selection, "Laboratory Scene").enabled && !SceneOutline(selection, "MainScene").enabled,
            "Only the laboratory destination is outlined in the field.");
        yield return Capture("12-field-touch-destination-list");

        // Without a trip objective the list stays neutral.
        Call(_manager, "CancelSceneSelection");
        PrepareQuest(null);
        yield return Frames(6);
        Call(_manager, "ShowSceneSelectionUI", shifter);
        yield return Frames(4);
        selection = (GameObject)Get(_manager, "sceneSelectionUI");
        Check(!SceneOutline(selection, "Laboratory Scene").enabled && !SceneOutline(selection, "MainScene").enabled,
            "Free travel outlines no destination.");
        Call(_manager, "CancelSceneSelection");

        // The other languages resolve the new step instead of showing its key.
        PrepareQuest("q.lab.return");
        Call(_wheel, "SelectToolAndStartPreview", Slot("999"));
        Call(_wheel, "CloseWheel", false);
        foreach (string language in new[] { "English", "ChineseSimplified" })
        {
            SwitchLanguage(language);
            yield return Frames(8);
            Check(!Hint().Contains("ui.collection") && !Hint().Contains("{") && Hint().Length > 0,
                language + " touch cue is localized: " + Hint());
            _checks.Add("TEXT " + language + " touch: " + Hint());
        }
        Call(_mobile, "EnableDesktopTestMode", false);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        foreach (string language in new[] { "English", "ChineseSimplified" })
        {
            SwitchLanguage(language);
            yield return Frames(8);
            Check(!Hint().Contains("ui.collection") && !Hint().Contains("{") && Hint().Length > 0,
                language + " desktop cue is localized: " + Hint());
            _checks.Add("TEXT " + language + " desktop: " + Hint());
        }
        File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
    }

    private void StopDialogue()
    {
        Call(_director, "CancelPlayback");
        Call(_quests, "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Time.timeScale = 1f;
    }

    private void PrepareScene()
    {
        StopDialogue();
        _wheel = Find("InventoryUISystem");
        _tools = Find("ToolManager");
        var persistent = Find("PlayerPersistentData");
        foreach (string id in new[] { "999", "1002" }) Call(persistent, "MarkToolUnlocked", id);
        Call(persistent, "ApplyUnlockedToolsToScene");
        Call(_wheel, "InitializeTools");
        SwitchLanguage("Japanese");
    }

    private void PrepareQuest(string id)
    {
        StopDialogue();
        foreach (var quest in ((IDictionary)Get(_quests, "_quests")).Values)
            Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), (string)Get(quest, "id") == id ? "InProgress" : "NotStarted"));
        ((HashSet<string>)Get(_quests, "_completedObjectives")).Clear();
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
    }

    private Component EnableTouch()
    {
        Call(_mobile, "EnableDesktopTestMode", true);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        // The laboratory builds its controls at scene start only on a touch device; this is that shipped route.
        if (UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include) == null)
            Call(T("LaboratoryMobileUIHelper"), "InitializeLaboratoryMobileUI");
        var controls = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        Check(controls != null, "The scene contains its real virtual controls.");
        Set(controls, "forceShowOnDesktop", true);
        controls.gameObject.SetActive(true);
        if (Get(controls, "interactButton") == null) Call(controls, "StartOriginalLogic");
        return controls;
    }

    private IEnumerator WaitForScene(string scene)
    {
        float deadline = Time.realtimeSinceStartup + 40f;
        while ((SceneManager.GetActiveScene().name != scene || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) &&
            Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "The real scene route reaches " + scene + ".");
        yield return new WaitForSecondsRealtime(1.5f);
    }

    private IEnumerator WaitUntilUsable(object tool)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!(bool)Get(tool, "canUse") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue((bool)Get(tool, "canUse"), "The phase shifter finishes its cooldown.");
    }

    private void CheckCardLayout(Component controls, string context)
    {
        var card = ScreenRect(Card().GetComponent<RectTransform>());
        foreach (string field in new[] { "inventoryButton", "toolWheelButton", "interactButton", "secondaryInteractButton" })
            Check(!card.Overlaps(ScreenRect(((Button)Get(controls, field)).GetComponent<RectTransform>())),
                context + ": the card does not cover " + field + ".");
        Check(!card.Overlaps(ScreenRect(GameObject.Find("QuestUICanvas/QuestPanel").GetComponent<RectTransform>())),
            context + ": the card and the quest card do not overlap.");
        var instruction = GameObject.Find("CollectionGuidanceCanvas/CollectionHint/NextAction").GetComponent<Text>();
        Check(instruction.cachedTextGenerator.characterCountVisible >= instruction.text.Length,
            context + ": the whole instruction is visible.");
    }

    private int Slot(string id)
    {
        var tools = (IList)Get(_wheel, "availableTools");
        return Enumerable.Range(0, tools.Count).First(i => (string)Get(tools[i], "toolID") == id);
    }

    private static string Destination() => (string)Call(Hud, "RecommendedDestination");
    private static string Recommended() => (string)Prop(Hud, "RecommendedToolId");
    private static bool Highlight() => (bool)Prop(Hud, "HighlightPrimaryAction");
    private static GameObject Card() => GameObject.Find("CollectionGuidanceCanvas/CollectionHint");
    private static string Hint() => GameObject.Find("CollectionGuidanceCanvas/CollectionHint/NextAction")?.GetComponent<Text>().text ?? "";
    private static string TitleText() => GameObject.Find("CollectionGuidanceCanvas/CollectionHint/Title")?.GetComponent<Text>().text ?? "";
    private static Outline SceneOutline(GameObject selection, string scene) =>
        selection.GetComponentsInChildren<Button>(true).First(b => b.name == "SceneButton_" + scene).GetComponent<Outline>();
    private static Outline ControlOutline(Component controls, string field) => (Outline)Get(controls, field);
    private static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private void MoveMouse(Vector2 point) => InputSystem.QueueStateEvent(_mouse, new MouseState { position = point });
    private IEnumerator Tap(Key key)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        yield return Frames(2);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return Frames(2);
    }
    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    private static void SetSize(int w, int h) => typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
        .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { w, h });
    private static void SwitchLanguage(string language) => Call(Prop(T("LocalizationManager"), "Instance"), "SwitchLanguage",
        Enum.Parse(T("LanguageSettings+Language"), language));
    private IEnumerator Capture(string name) => (IEnumerator)typeof(TeacherReviewFlowCaptureTests).GetMethod("CaptureTo", Flags)
        .Invoke(new TeacherReviewFlowCaptureTests(), new object[] { Path.Combine(_output, name + ".png") });

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (string.IsNullOrEmpty(_output)) yield break;
        if (_manager != null) Call(_manager, "HideSceneSelectionUI");
        if (_wheel != null && (UnityEngine.Object)_wheel != null) Call(_wheel, "CloseWheel", false);
        if (_mobile != null)
        {
            Call(_mobile, "EnableDesktopTestMode", false);
            Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        }
        if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
        SwitchLanguage("Japanese");
        if (_viewSizeIndex >= 0) GameViewSelection.Index = _viewSizeIndex;
        // Remove the real scenes while the test devices are still valid for their UI modules.
        yield return _cleanup.Restore();
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        InputSystem.settings.updateMode = _updateMode;
        InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
#endif
    }
}
