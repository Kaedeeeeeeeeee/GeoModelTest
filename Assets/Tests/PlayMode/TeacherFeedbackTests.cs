using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TeacherFeedbackTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> objects = new List<GameObject>();
    private InputSettings.UpdateMode previousMode;
    private Mouse mouse;
    private readonly Dictionary<string, string> savedPreferences = new Dictionary<string, string>();
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
#endif
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Type type, string name, object target, params object[] args) =>
        type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
            m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(target, args);
    private static object Call(object target, string name, params object[] args) => Call(target.GetType(), name, target, args);
    private static object Static(string type, string name, params object[] args) => Call(T(type), name, null, args);
    private static object Read(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private GameObject New(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    private Component Add(string name, GameObject host = null)
    {
        var component = (host ?? New(name)).AddComponent(T(name));
        if (component is Behaviour behaviour) behaviour.enabled = false;
        return component;
    }

    [SetUp]
    public void SetUp()
    {
        savedPreferences.Clear();
        foreach (string key in new[] { "StorySystem.Investigation.v1" })
            savedPreferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key, "") : null;
        // The investigation is run-scoped; a completed user save must not change this fixture.
        Static("StorySystem.InvestigationProgress", "Reset");
        previousMode = InputSystem.settings.updateMode;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        Static("Core.GameInputState", "ReleaseAll");
        Static("UISystem.FirstControlGuide", "CloseCurrent");
        Static("StorySystem.StoryDirector+SubtitleUI", "CancelAll");
    }

    [UnityTest]
    public IEnumerator WheelSelection_ShouldWaitForReleaseAndANewClickBeforeUsingSwitcher()
    {
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.EnableDevice(mouse);
        mouse.MakeCurrent();
        var host = New("SelectionPlayer");
        var manager = Add("ToolManager", host);
        var tool = Add("SceneSwitcherTool", host);
        var ui = Add("InventoryUISystem");
        var toolArray = Array.CreateInstance(T("CollectionTool"), 1);
        toolArray.SetValue(tool, 0);
        Set(manager, "availableTools", toolArray);
        Set(tool, "toolID", "999");
        Set(tool, "sceneManager", T("GameSceneManager").GetProperty("Instance").GetValue(null));
        Call(ui, "InitializeTools");
        var tools = (IList)ui.GetType().GetField("availableTools", Flags).GetValue(ui);
        Assert.IsTrue(tools.Cast<object>().Any(t => t.GetType() == T("EmptyHandTool")));
        int index = tools.IndexOf(tool);
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
        InputSystem.Update();
        Call(ui, "SelectToolAndStartPreview", index);
        Call(tool, "HandleInput");
        Assert.AreSame(tool, Call(manager, "GetCurrentTool"));
        Assert.IsFalse((bool)T("Core.GameInputState").GetProperty("IsModalOpen").GetValue(null));
        yield return null;
        yield return null;
        Assert.IsTrue((bool)T("CollectionTool").GetProperty("IsSelectionInputSuppressed").GetValue(null), "Holding the selection gesture must stay blocked.");
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        Assert.IsFalse((bool)T("CollectionTool").GetProperty("IsSelectionInputSuppressed").GetValue(null));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
        InputSystem.Update();
        Call(tool, "HandleInput");
        var scenes = T("GameSceneManager").GetProperty("Instance").GetValue(null);
        var selection = (GameObject)scenes.GetType().GetField("sceneSelectionUI").GetValue(scenes);
        Assert.IsTrue(selection.activeSelf, "A new use click must open destination selection.");
        Call(scenes, "CancelSceneSelection");
        Assert.IsNull(Call(manager, "GetCurrentTool"));
        Assert.IsFalse((bool)Read(tool, "IsEquipped"));
    }

    [UnityTest]
    public IEnumerator EmptyHands_ShouldUnequipInsteadOfStartingAnAction()
    {
        var host = New("EmptyHandsPlayer");
        var manager = Add("ToolManager", host);
        var tool = Add("SceneSwitcherTool", host);
        var toolArray = Array.CreateInstance(T("CollectionTool"), 1); toolArray.SetValue(tool, 0);
        Set(manager, "availableTools", toolArray);
        var ui = Add("InventoryUISystem");
        Call(ui, "InitializeTools");
        Call(manager, "EquipTool", tool);
        var list = (IList)ui.GetType().GetField("availableTools", Flags).GetValue(ui);
        var empty = list.Cast<object>().Single(t => t.GetType() == T("EmptyHandTool"));
        Call(ui, "SelectToolAndStartPreview", list.IndexOf(empty));
        Assert.IsNull(Call(manager, "GetCurrentTool"));
        Assert.IsFalse((bool)Read(tool, "IsEquipped"));
        Assert.IsFalse((bool)Read(empty, "IsEquipped"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator HelpOverDestinationSelection_ShouldRestorePauseThenCancelPutsToolAway()
    {
        yield return null;
        yield return null;
        var host = New("NestedHelpPlayer");
        var manager = Add("ToolManager", host);
        Set(manager, "availableTools", Array.CreateInstance(T("CollectionTool"), 0));
        var tool = Add("SceneSwitcherTool", host);
        Call(manager, "EquipTool", tool);
        var scenes = T("GameSceneManager").GetProperty("Instance").GetValue(null);
        Time.timeScale = 0.75f;
        Call(scenes, "ShowSceneSelectionUI", tool);
        Static("UISystem.FirstControlGuide", "Show", false);
        yield return null;
        Static("UISystem.FirstControlGuide", "CloseCurrent");
        Assert.AreEqual(0f, Time.timeScale);
        Call(scenes, "CancelSceneSelection");
        Assert.AreEqual(0.75f, Time.timeScale);
        Assert.IsNull(Call(manager, "GetCurrentTool"));
    }

    [UnityTest]
    public IEnumerator Reminder_ShouldRemainReplayableWithoutChangingQuestOrCheckpoints()
    {
        var director = T("StorySystem.StoryDirector").GetProperty("Instance").GetValue(null);
        string quests = PlayerPrefs.GetString("QuestSystem.CompletedQuests", "");
        string checkpoints = PlayerPrefs.GetString("StorySystem.Checkpoints.v1", "");
        for (int repeat = 0; repeat < 2; repeat++)
        {
            bool finished = false;
            Call(director, "PlayReminder", "Dr.Kaede", "同じ場所で試料を採ってきてね。", (Action)(() => finished = true));
            yield return null;
            var canvas = GameObject.Find("SubtitleCanvas");
            Assert.NotNull(canvas);
            var hint = canvas.transform.Find("BG/Hint").GetComponent(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
            // Read TMP through the component's inherited text property.
            StringAssert.Contains((string)Static("UISystem.GameUI", "L", "ui.dialog.finish"), (string)Read(hint, "text"));
            canvas.transform.Find("BG").GetComponent<Button>().onClick.Invoke();
            for (int frame = 0; frame < 5; frame++) yield return null;
            Assert.IsTrue(finished);
            Assert.AreEqual(quests, PlayerPrefs.GetString("QuestSystem.CompletedQuests", ""));
            Assert.AreEqual(checkpoints, PlayerPrefs.GetString("StorySystem.Checkpoints.v1", ""));
        }
    }

    [UnityTest]
    public IEnumerator GuidePages_ShouldFitEveryLanguageOnDesktopAndTouch()
    {
        var localization = T("LocalizationManager").GetProperty("Instance").GetValue(null);
        var languageType = T("LanguageSettings+Language");
        var original = Read(localization, "CurrentLanguage");
        foreach (string language in new[] { "Japanese", "ChineseSimplified", "English" })
        {
            Call(localization, "SwitchLanguage", Enum.Parse(languageType, language));
            foreach (bool touch in new[] { false, true })
            {
                Static("UISystem.FirstControlGuide", "Show", touch);
                var canvas = GameObject.Find("FirstControlGuide");
                var guide = canvas.GetComponent(T("UISystem.FirstControlGuide"));
                for (int page = 0; page < 4; page++)
                {
                    if (page > 0) Call(guide, "ChangePage", 1);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in canvas.GetComponentsInChildren<Text>())
                    {
                        Assert.IsFalse(label.text.StartsWith("ui.guide."), label.name);
                        if (label.name != "Instruction") continue;
                        Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 2f, language + " / " + page + " / " + label.text);
                    }
                }
                Static("UISystem.FirstControlGuide", "CloseCurrent");
                yield return null;
            }
        }
        Call(localization, "SwitchLanguage", original);
    }

    [UnityTest]
    public IEnumerator Npc_ShouldShowNewQuestMarkerThenRepeatReminderAfterCompletion()
    {
        yield return null;
        yield return null;
        var quests = T("QuestSystem.QuestManager").GetProperty("Instance").GetValue(null);
        var registry = (IDictionary)quests.GetType().GetField("_quests", Flags).GetValue(quests);
        var quest = Activator.CreateInstance(T("QuestSystem.Quest"));
        Set(quest, "id", "teacher.feedback.test");
        Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), "InProgress"));
        registry.Add("teacher.feedback.test", quest);
        var npc = Add("QuestSystem.QuestNpcInteraction");
        var stageType = npc.GetType().GetNestedType("QuestInteractionStage", BindingFlags.NonPublic);
        var stage = Activator.CreateInstance(stageType);
        Set(stage, "questId", "teacher.feedback.test");
        var stages = Array.CreateInstance(stageType, 1); stages.SetValue(stage, 0);
        Set(npc, "stages", stages);
        var camera = New("MarkerCamera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 2, -6);
        Set(npc, "viewCamera", camera);
        Call(npc, "Start");
        Call(npc, "Update");
        Assert.IsTrue((bool)Read(npc, "HasNewConversation"));
        Assert.IsTrue(GameObject.Find("QuestNpcMarker").activeSelf);
        Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), "Completed"));
        Call(npc, "Update");
        Assert.IsFalse((bool)Read(npc, "HasNewConversation"));
        var marker = (GameObject)npc.GetType().GetField("markerCanvasGO", Flags).GetValue(npc);
        Assert.IsFalse(marker.activeSelf);
        Call(npc, "BeginInteraction");
        Assert.IsTrue((bool)Read(npc, "IsRepeatingReminder"));
        yield return null;
        GameObject.Find("SubtitleCanvas").transform.Find("BG").GetComponent<Button>().onClick.Invoke();
        for (int frame = 0; frame < 5; frame++) yield return null;
        Call(npc, "BeginInteraction");
        Assert.IsTrue((bool)Read(npc, "IsRepeatingReminder"));
        registry.Remove("teacher.feedback.test");
    }

    [UnityTest]
    public IEnumerator FieldGuide_ShouldWaitForStoryThenShowOnceEvenWithoutASamplingTask()
    {
        yield return null;
        yield return null;
        var originalScene = SceneManager.GetActiveScene();
        var field = SceneManager.CreateScene("MainScene");
        var director = T("StorySystem.StoryDirector").GetProperty("Instance").GetValue(null);
        var flags = (HashSet<string>)director.GetType().GetField("_flags", Flags).GetValue(director);
        bool hadIntro = flags.Contains("story.lab.intro");
        int completed = PlayerPrefs.GetInt("FirstControlGuide.FieldCompleted.v1", 0);
        flags.Add("story.lab.intro");
        PlayerPrefs.DeleteKey("FirstControlGuide.FieldCompleted.v1");
        SceneManager.SetActiveScene(field);
        Call(director, "PlayReminder", "Dr.Kaede", "先に説明を聞いてね。", (Action)null);
        Assert.IsFalse((bool)Static("UISystem.FirstControlGuide", "TryShowForFirstField"));
        Call(director, "CancelPlayback");
        // Prevent the persistent access component from racing this explicit test.
        var access = UnityEngine.Object.FindFirstObjectByType(T("UISystem.ControlGuideAccess")) as Behaviour;
        if (access != null) access.enabled = false;
        yield return null;
        yield return null;
        Assert.IsTrue((bool)Static("UISystem.FirstControlGuide", "TryShowForFirstField"));
        var guide = GameObject.Find("FirstControlGuide").GetComponent(T("UISystem.FirstControlGuide"));
        Assert.AreEqual(3, Read(guide, "Page"));
        Call(guide, "Complete");
        Assert.IsFalse((bool)Static("UISystem.FirstControlGuide", "TryShowForFirstField"));
        SceneManager.SetActiveScene(originalScene);
        yield return SceneManager.UnloadSceneAsync(field);
        if (!hadIntro) flags.Remove("story.lab.intro");
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", completed);
        if (access != null) access.enabled = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        InputSystem.settings.updateMode = previousMode;
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
        Static("UISystem.FirstControlGuide", "CloseCurrent");
        var director = T("StorySystem.StoryDirector").GetProperty("Instance").GetValue(null);
        Call(director, "CancelPlayback");
        var scenes = T("GameSceneManager").GetProperty("Instance").GetValue(null);
        Call(scenes, "HideSceneSelectionUI");
        Static("Core.GameInputState", "ReleaseAll");
        foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
        foreach (var saved in savedPreferences)
        {
            if (saved.Value == null) PlayerPrefs.DeleteKey(saved.Key);
            else PlayerPrefs.SetString(saved.Key, saved.Value);
        }
        T("StorySystem.InvestigationProgress").GetField("_state", Flags).SetValue(null, null);
        Time.timeScale = 1f;
    }
}
