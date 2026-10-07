using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Opt-in playthrough of the teaching route from field hammer sampling to the lab report, using the
/// shipped scenes, real scene switches, real tools and the real dialogue UI. Every dialogue line and the
/// key HUD moments are captured, so the same run documents the route before and after a content change.
/// Set GEOMODEL_TEACHER_REVIEW_CAPTURE to an output folder; set GEOMODEL_TEACHER_REVIEW_EXPECT=after to
/// also enforce the 2026-10-05 teacher-review behaviour.
/// </summary>
public class TeacherReviewFlowCaptureTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => (target as Type ?? target.GetType()).GetField(field, Flags).GetValue(target is Type ? null : target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Prop(object target, string name) => (target is Type type ? type : target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        var method = type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
            m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
        return method.Invoke(target is Type ? null : target, args);
    }
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));

    // Correct choice positions follow Docs/quiz-fixed-choice-order.md; prompts are matched without readings.
    private static readonly (string prompt, int correct, string answer)[] Answers =
    {
        ("0.06", 2, "泥岩"), ("塩酸", 1, "石灰岩"), ("くぎ", 0, "チャート"),
        ("サンゴみたいに", 1, "示相化石"), ("どんな環境", 1, "あたたかくて浅い海"),
        ("できたのは", 2, "中生代"), ("アンモナイトみたいなの", 0, "示準化石"),
        ("凝灰岩", 1, "火山が噴火"), ("傾", 2, "東"), ("野外で見たがけ", 0, "しゅう曲"),
    };

    private string _output;
    private bool _expectAfter;
    private object _quests, _director;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private InputSettings.UpdateMode _inputMode;
    private InputSettings.BackgroundBehavior _background;
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private readonly List<string> _checks = new List<string>();
    private readonly List<string> _dialogue = new List<string>();
    private readonly SortedDictionary<string, string> _observations = new SortedDictionary<string, string>();

    private void Check(bool condition, string message)
    {
        Assert.IsTrue(condition, message);
        _checks.Add("PASS " + message);
        File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
    }

    private void Expect(bool condition, string message)
    {
        if (!_expectAfter) { _checks.Add((condition ? "(after-behaviour present) " : "(after-behaviour absent) ") + message); return; }
        Check(condition, message);
    }

    private void Observe(string key, object value)
    {
        _observations[key] = value?.ToString() ?? "";
        File.WriteAllLines(Path.Combine(_output, "observations.txt"), _observations.Select(kv => kv.Key + " = " + kv.Value));
    }

    [UnityTest]
    public IEnumerator TeachingRoute_FieldToReport_ShouldCaptureEveryStep()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_TEACHER_REVIEW_CAPTURE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_TEACHER_REVIEW_CAPTURE to opt into the teacher-review route capture.");
        _expectAfter = Environment.GetEnvironmentVariable("GEOMODEL_TEACHER_REVIEW_EXPECT") == "after";
        Directory.CreateDirectory(_output);
        foreach (var file in Directory.GetFiles(_output)) File.Delete(file);

        typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
            .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { 960, 540 });
        _backend = Resources.Load("BackendSettings");
        if (_backend != null) { _backendEnabled = (bool)Get(_backend, "enableBackend"); Set(_backend, "enableBackend", false); }
        _inputMode = InputSystem.settings.updateMode;
        _background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _mouse = InputSystem.AddDevice<Mouse>();

        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        var localization = Prop(T("LocalizationManager"), "Instance");
        Call(localization, "SwitchLanguage", Enum.Parse(T("LanguageSettings+Language"), "Japanese"));
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        StopDialogue();
        Call(_director, "ReloadFlags");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Call(Prop(T("StorySystem.QuizScoreManager"), "Instance"), "StartNewRun");
        Call(T("StorySystem.InvestigationProgress"), "Reset");
        yield return Frames(3);

        // ---- A: field hammer sampling (feedback items 1, 2 and 3) ----
        foreach (string id in new[] { "q.lab.intro", "q.lab.drkaede" }) CompleteQuest(id);
        var wheel = Find("InventoryUISystem");
        Call(wheel, "InitializeTools");
        var wheelIds = ((IList)Get(wheel, "availableTools")).Cast<object>().Select(t => (string)Get(t, "toolID")).ToArray();
        Observe("A.wheel.ids", string.Join(",", wheelIds));
        Check(wheelIds.SequenceEqual(new[] { "0", "999", "1002", "1001" }),
            "The opening wheel follows empty hands, phase shifter, hammer, tower and hides 1000/1100/1101.");
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
        yield return Press(Key.Tab);
        yield return Frames(4);
        Check((bool)Prop(wheel, "IsWheelOpen") && (int)Get(wheel, "selectedSlot") == -1,
            "Real Tab opens the equal-sector wheel and the center dead zone selects nothing.");
        Check(((Graphic[])Get(wheel, "sectorGraphics")).Length == 4 &&
            ((RectTransform[])Get(wheel, "wheelSlots")).All(r => r.GetComponent<Image>() == null),
            "The wheel has four sectors with no square or empty slots.");
        yield return Capture("A00-teaching-tool-wheel");
        var anchors = (RectTransform[])Get(wheel, "wheelSlots");
        for (int i = 0; i < anchors.Length; i++)
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, anchors[i].position);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point });
            yield return Frames(3);
            Check((int)Get(wheel, "selectedSlot") == i &&
                ((Graphic[])Get(wheel, "sectorGraphics"))[i].color == (Color)Get(wheel, "selectedSlotBackgroundColor"),
                "Real mouse hover highlights sector " + i + " in clockwise teaching order.");
        }
        var hammerPoint = RectTransformUtility.WorldToScreenPoint(null, anchors[2].position);
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = hammerPoint, buttons = 1 });
        yield return Frames(2);
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = hammerPoint });
        yield return Frames(2);
        Check(!(bool)Prop(wheel, "IsWheelOpen") && (string)Get(Call(Find("ToolManager"), "GetCurrentTool"), "toolID") == "1002",
            "A real click on the hammer sector equips the geological hammer.");
        // Reproduce an older save without deleting its simple-drill unlock.
        var persistent = Find("PlayerPersistentData");
        foreach (string id in new[] { "1000", "1100", "1101" }) Call(persistent, "MarkToolUnlocked", id);
        Call(persistent, "ApplyUnlockedToolsToScene");
        Call(wheel, "InitializeTools");
        Check(((IList)Get(wheel, "availableTools")).Cast<object>().Select(t => (string)Get(t, "toolID"))
            .SequenceEqual(new[] { "0", "999", "1002", "1001" }), "Legacy 1000/1100/1101 unlocks never enter the wheel.");
        Call(_quests, "StartQuest", "q.field.phase");
        Call(_quests, "CompleteObjective", "q.field.phase.enter_field");
        Set(_quests, "_fieldPhaseTargetIndex", 0);
        Call(_quests, "ActivateCurrentFieldTarget");
        Call(_quests, "TryBindSampleEvents");
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
        var guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        Component target = null;
        var inventory = Find("SampleInventory");
        var hammer = Tool("1002");
        var sites = new[] { "chapter3.field.sample_site_a", "chapter3.field.sample_site_b", "chapter3.field.sample_site_c" };
        int before;
        var outsideTarget = UnityEngine.Object.FindObjectsByType(T("GuidanceSystem.GuidanceTarget"), FindObjectsSortMode.None)
            .Cast<Component>().First(t => (string)Prop(t, "TargetId") == sites[1]);
        PlaceAtHammerTarget(outsideTarget);
        Call(guidance, "RegisterPlayer", Player().transform);
        SelectTool("1002");
        yield return new WaitForSecondsRealtime(1.1f);
        yield return MouseClick();
        yield return MouseClick();
        yield return MouseClick();
        var outsideSample = (GameObject)Prop(hammer, "PendingSample");
        Check(outsideSample != null, "A real sample can be generated outside the current site.");
        PlacePlayer(outsideSample.transform.position + new Vector3(0, -0.35f, -0.6f), outsideSample.transform.position);
        yield return Frames(5);
        yield return Press(Key.E);
        Check(LabelText(GameObject.Find("GameToastCanvas/Toast")).Contains("ここは採集地点ではありません"), "Off-target pickup shows the localized guidance toast.");
        var warningToast = GameObject.Find("GameToastCanvas/Toast");
        Check(warningToast.transform.Find("Check").GetComponent<Text>().text == "!" &&
            warningToast.transform.Find("Check").GetComponent<Text>().color == (Color)Get(T("UISystem.InteractionPrompt"), "Warning") &&
            warningToast.GetComponent<Outline>().effectColor == (Color)Get(T("UISystem.InteractionPrompt"), "Warning"),
            "Off-target toast uses the amber warning icon and border.");
        Check((int)Get(_quests, "_fieldPhaseTargetIndex") == 0 && !StoryActive(), "Off-target pickup does not advance the site or start beat2.");
        yield return Capture("A00-outside-site-toast");
        for (int site = 0; site < sites.Length; site++)
        {
            target = (Component)Get(guidance, "activeTarget");
            Check(target != null && (string)Prop(target, "TargetId") == sites[site], "Blue guide line targets site " + (site + 1) + ".");
            PlaceAtHammerTarget(target);
            Observe("A.site" + (site + 1) + ".ground", Player().transform.position);
            Call(guidance, "RegisterPlayer", Player().transform);
            SelectTool("1002");
            yield return new WaitForSecondsRealtime(1.1f);
            yield return Frames(6);
            string title = GameObject.Find("CollectionGuidanceCanvas/CollectionHint/Title").GetComponent<Text>().text;
            Check(title.Contains("採集地点 " + (site + 1) + "/3"), "Hammer title shows the current site separately from hits.");
            string task = LabelText(GameObject.Find("QuestUICanvas/QuestPanel"));
            Check(task.Contains("3/9") && task.Contains("野外の3か所で岩石を採集する"), "Field task card displays step 3 of 9 and all three sites.");
            Check(!ScreenRect(GameObject.Find("QuestUICanvas/QuestPanel").GetComponent<RectTransform>()).Overlaps(
                ScreenRect(GameObject.Find("CollectionGuidanceCanvas/CollectionHint").GetComponent<RectTransform>())), "Collection card does not overlap the task card.");
            yield return Capture("A01-site-" + (site + 1) + "-guidance");
            yield return MouseClick();
            yield return MouseClick();
            yield return MouseClick();
            var sample = (GameObject)Prop(hammer, "PendingSample");
            Check(sample != null, "Three real hammer hits generate a sample at site " + (site + 1) + ".");
            var collector = sample.GetComponent(T("SampleCollector"));
            Check((string)Prop(Call(_quests, "FieldSampleFeedback", Get(collector, "sampleData")), "Message") == "岩石サンプルを採取できました！（" + (site + 1) + "/3）",
                "Sample lies within the current site's acceptance radius.");
            PlacePlayer(sample.transform.position + new Vector3(0, -0.35f, -0.6f), sample.transform.position);
            yield return Frames(5);
            var pickupText = (Text)Get(collector, "promptText");
            Check(pickupText.text == "岩石サンプルを拾う" && !pickupText.text.Contains("_"), "Pickup action names the rock sample without an internal ID.");
            Check(((GameObject)Get(collector, "interactionPrompt")).activeInHierarchy, "The readable pickup prompt is visible alongside the collection card.");
            Check(pickupText.fontSize * ((Canvas)Get(collector, "promptCanvas")).scaleFactor >= 18f, "Actual pickup text is at least 18 pixels at 960x540.");
            yield return Capture("A02-site-" + (site + 1) + "-pickup-prompt");
            before = ((IList)Call(inventory, "GetAllSamples")).Count;
            yield return Press(Key.E);
            Check(((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "E collects site " + (site + 1) + " into the inventory.");
            var toast = GameObject.Find("GameToastCanvas/Toast");
            Check(toast != null && LabelText(toast).Contains("（" + (site + 1) + "/3）"), "Rock pickup toast includes site progress.");
            Check(toast.transform.Find("Check").GetComponent<Text>().text == "✓" &&
                toast.transform.Find("Check").GetComponent<Text>().color == (Color)Get(T("UISystem.GameUI"), "Accent") &&
                toast.GetComponent<Outline>().effectColor == (Color)Get(T("UISystem.GameUI"), "Accent"),
                "Normal pickup restores the success icon and border after a warning.");
            yield return Capture("A03-site-" + (site + 1) + "-pickup-toast");
            if (site < 2)
            {
                Check(QuestStatus("q.field.phase") == "InProgress" && !StoryActive() && !(bool)Get(_quests, "_fieldPhaseSampleCutscenePending"),
                    "Sites 1 and 2 keep the field quest active without playing beat2.");
                Check((string)Prop((Component)Get(guidance, "activeTarget"), "TargetId") == sites[site + 1], "Pickup advances the blue line to the next site.");
                Check(GameObject.Find("CollectionGuidanceCanvas/CollectionHint/Title").GetComponent<Text>().text.Contains("採集地点 " + (site + 2) + "/3"),
                    "Collection title advances to the next site.");
            }
            else Check((bool)Get(_quests, "_fieldPhaseSampleCutscenePending"), "Only the third sample schedules beat2.");
        }

        yield return Drive("A04-field-after-pickup", 6f);
        int fieldQuiz = _lastQuizCount;
        Observe("A.field.quiz_count", fieldQuiz);
        Check(QuestStatus("q.field.phase") == "Completed", "Field sampling dialogue completes the field phase.");
        Check(QuestStatus("q.lab.return") == "InProgress", "The route then asks the player to return to the lab.");
        Expect(fieldQuiz == 0, "Rock identification is no longer quizzed in the field.");

        // ---- B: back to the lab (item 3) ----
        yield return SwitchScene("Laboratory Scene");
        yield return Drive("B01-lab-return", 12f);
        int labQuiz = _lastQuizCount;
        Observe("B.lab_return.quiz_count", labQuiz);
        Check(fieldQuiz + labQuiz == 3, "The three rock-identification questions are each asked once.");
        Expect(labQuiz == 3, "The three rock-identification questions are asked after returning to the lab.");
        Check(QuestStatus("q.lab.return") == "Completed" && QuestStatus("q.chapter4.kaede") == "InProgress",
            "Lab return hands over to the drilling briefing.");

        var npc = UnityEngine.Object.FindObjectsByType(T("QuestSystem.QuestNpcInteraction"), FindObjectsSortMode.None)
            .Cast<Component>().First(c => ((Array)Get(c, "stages")).Cast<object>().Any(s => (string)Get(s, "questId") == "q.chapter4.kaede"));
        Call(npc, "RefreshCurrentStage");
        var npcComponent = (Component)npc;
        PlacePlayer(npcComponent.transform.position + new Vector3(0f, 0f, -0.8f), npcComponent.transform.position + Vector3.up);
        Set(npc, "playerInRange", true);
        Call(npc, "UpdateAvailability");
        yield return Frames(4);
        Check(((GameObject)Get(npc, "promptCanvasGO")).activeInHierarchy, "Kaede's shared prompt is visible before the conversation.");
        yield return Capture("C00-kaede-prompt");
        yield return CaptureKaedeTouchSizes(npc);
        Call(npc, "BeginInteraction");
        yield return Drive("C01-lab-kaede-briefing", 6f);
        Check(QuestStatus("q.chapter4.field") == "InProgress", "Talking to Kaede starts the field drilling task.");

        // ---- D: field drilling (items 4 and 5) ----
        yield return SwitchScene("MainScene");
        yield return Drive("D01-field-drill-intro", 12f);
        Check(QuestStatus("q.chapter4.sample") == "InProgress", "Field briefing starts the core-sampling objective.");
        guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        target = (Component)Get(guidance, "activeTarget");
        Check(target != null, "Drill-tower task has its field guidance target.");
        Vector3 drillSite = GroundAt(target.transform.position);
        PlacePlayer(drillSite + new Vector3(0, 0.15f, -2.3f), drillSite);
        Call(guidance, "RegisterPlayer", Player().transform);
        SelectTool("1001");
        yield return Frames(6);
        var drill = Tool("1001");
        Check((bool)Prop(drill, "CanConfirmPlacement"), "Tower preview is placeable at the drill site.");
        yield return MouseClick();
        yield return Frames(6);
        var tower = (Component)Get(drill, "placedTower");
        Check(tower != null, "Primary click places the drill tower.");
        PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
        yield return Frames(6);
        var towerPrompt = Find("DrillTowerInteractionUI");
        Call(towerPrompt, "ShowInteractionPrompt", tower);
        string normalPrompt = ((Text)Get(towerPrompt, "promptText")).text;
        Check(!normalPrompt.Contains("しまう"), "The tower offers no put-away operation before maximum depth.");
        yield return Capture("D02-tower-ready");
        yield return Press(Key.F);
        Check((bool)Get(tower, "isDrilling"), "F starts drilling.");
        yield return new WaitForSeconds(2.4f);
        var cores = (IList)Get(tower, "collectedSamples");
        Check(cores.Count > 0 && (GameObject)cores[0] != null, "Drilling produces a core.");
        var core = (GameObject)cores[0];
        PlacePlayer(GroundAt(core.transform.position) + new Vector3(0, 0.15f, -0.6f), core.transform.position);
        yield return Frames(5);
        yield return Press(Key.E);
        var coreToast = GameObject.Find("GameToastCanvas/Toast");
        string coreToastText = coreToast != null && coreToast.activeInHierarchy ? LabelText(coreToast) : "(none)";
        Observe("D.pickup.toast", coreToastText);
        Expect(coreToastText.Contains("コア"), "Picking up the drill core shows a success message.");
        yield return Drive("D03-core-return", 6f);
        string coreLine = _dialogue.LastOrDefault(l => l.StartsWith("D03-core-return")) ?? "";
        Observe("D.core_return.last_line", coreLine);
        Expect(coreLine.Contains("うまくできたね") && !coreLine.Contains("よくやった"), "Core pickup praise is friendly (うまくできたね！).");
        Check(QuestStatus("q.chapter4.return") == "InProgress", "Core pickup asks the player to return to the lab.");

        PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
        yield return new WaitForSecondsRealtime(0.6f);
        var prompt = GameObject.Find("DrillTowerInteractionCanvas/InteractionPrompt");
        string promptText = prompt != null && prompt.activeInHierarchy ? prompt.transform.Find("PromptText").GetComponent<Text>().text : "(hidden)";
        Observe("D.tower_prompt", promptText.Replace("\n", " / "));
        Check(prompt != null && prompt.activeInHierarchy, "Near the tower after sampling, its interaction prompt is visible.");
        Expect(!promptText.Contains("回収"), "The tower prompt no longer offers recovering the tower.");
        yield return Capture("D04-tower-prompt-after-core");
        yield return Press(Key.G);
        yield return Frames(4);
        Observe("D.tower_after_G", Get(drill, "placedTower") != null ? "still placed" : "recovered");
        Expect(Get(drill, "placedTower") != null, "G no longer recovers the tower during the teaching route.");

        // Finish all five drilling operations with another tool equipped; cores remain uncollected.
        SelectTool("1002");
        for (int depth = 1; depth < 5; depth++)
        {
            PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -0.6f), tower.transform.position + Vector3.up * 1.5f);
            yield return Frames(4);
            yield return Press(Key.F);
            Check((bool)Get(tower, "isDrilling"), "F drills with the hammer equipped at depth " + depth + ".");
            yield return Press(Key.F);
            Check(Get(drill, "placedTower") != null, "F cannot put away a tower while drilling.");
            yield return new WaitForSeconds(2.4f);
            Check((int)Get(tower, "currentDrillCount") == depth + 1, "Actual drilling completes depth " + (depth + 1) + ".");
        }
        Call(towerPrompt, "ShowInteractionPrompt", tower);
        Check(((Text)Get(towerPrompt, "promptText")).text.Contains("ドリルタワーをしまう"), "Maximum depth adds the F put-away action.");
        yield return Capture("D05-maximum-depth-put-away");
        yield return Press(Key.G);
        Check(Get(drill, "placedTower") != null, "G remains disabled at maximum depth.");
        var remaining = ((IList)Get(tower, "collectedSamples")).Cast<GameObject>().Where(c => c != null).ToArray();
        Check(remaining.Length == 4, "Four uncollected cores remain after five drilling operations.");
        yield return Press(Key.F);
        Check(tower == null && Get(drill, "placedTower") == null && !(bool)Get(drill, "hasPlacedObject") && (bool)Get(drill, "canUse"),
            "F destroys only the tower and resets its tool for placement.");
        Check(remaining.All(c => c != null && c.GetComponent(T("SampleCollector")) != null), "Putting away the tower preserves every uncollected core.");
        Check(LabelText(GameObject.Find("GameToastCanvas/Toast")).Contains("ドリルタワーをしまいました"), "Putting away the tower shows a toast.");
        yield return Capture("D06-tower-put-away-cores-remain");
        var savedCore = remaining[0];
        PlacePlayer(GroundAt(savedCore.transform.position) + new Vector3(0, 0.15f, -0.6f), savedCore.transform.position);
        yield return Frames(5);
        before = ((IList)Call(inventory, "GetAllSamples")).Count;
        Observe("D.saved_core.position", savedCore.transform.position);
        yield return Capture("D07-preserved-core-pickup-prompt");
        yield return Press(Key.E);
        Observe("D.saved_core.after_E", savedCore == null ? "picked up" : "still present");
        Observe("D.saved_core.inventory_delta", ((IList)Call(inventory, "GetAllSamples")).Count - before);
        Check(savedCore == null && ((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "A preserved core can still be picked up with E.");
        PlacePlayer(drillSite + new Vector3(0, 0.15f, -2.3f), drillSite);
        SelectTool("1001");
        yield return Frames(6);
        Check((bool)Prop(drill, "CanConfirmPlacement"), "After putting away the tower its placement preview is valid again.");
        yield return MouseClick();
        yield return Frames(4);
        Check(Get(drill, "placedTower") != null, "The drill tower can be placed again.");

        // ---- E: lab analysis (item 6) and report ----
        yield return SwitchScene("Laboratory Scene");
        yield return Drive("E01-lab-analysis", 12f);
        var analysis = _dialogue.Where(l => l.StartsWith("E01-lab-analysis")).ToList();
        int coralQuiz = analysis.FindIndex(l => l.Contains("QUIZ") && l.Contains("どんな環境"));
        int coralFound = analysis.FindIndex(l => l.Contains("サンゴ") && l.Contains("化石") && l.Contains("岩芯"));
        Observe("E.coral_discovery_line", coralFound >= 0 ? analysis[coralFound] : "(none)");
        Check(coralQuiz >= 0, "Lab analysis asks the coral environment question.");
        Expect(coralFound >= 0 && coralFound < coralQuiz, "The lab dialogue reveals a coral fossil inside the collected core before the coral question.");
        Check((bool)Prop(T("StorySystem.InvestigationProgress"), "IsComplete"), "The route reaches investigation completion.");
        yield return new WaitForSecondsRealtime(1f);
        yield return Capture("F01-report");
        wheel = Find("InventoryUISystem");
        Call(wheel, "InitializeTools");
        Check(((IList)Get(wheel, "availableTools")).Cast<object>().Select(t => (string)Get(t, "toolID"))
            .SequenceEqual(new[] { "0", "999", "1002", "1001" }), "Completed investigation still hides the vehicles and preserves teaching order.");
        Call(_quests, "StartQuest", "q.chapter5.kaede");
        Call(wheel, "InitializeTools");
        Check(!((IList)Get(wheel, "availableTools")).Cast<object>().Any(t => new[] { "1000", "1100", "1101" }.Contains((string)Get(t, "toolID"))),
            "Legacy chapter 5 also keeps all three excluded tools out of the wheel.");
        var quiz = Prop(T("StorySystem.QuizScoreManager"), "Instance");
        Observe("F.first_try_correct", ((IList)Prop(quiz, "Attempts")).Cast<object>().Count(a => (bool)Get(a, "isCorrect")));
        File.WriteAllLines(Path.Combine(_output, "dialogue.txt"), _dialogue);
    }

    private IEnumerator CaptureKaedeTouchSizes(object npc)
    {
        var input = Prop(T("MobileInputManager"), "Instance");
        var controls = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        bool forceShow = controls != null && (bool)Get(controls, "forceShowOnDesktop");
        Call(input, "EnableDesktopTestMode", true);
        Call(input, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        // The lab was entered as a desktop player; initialize its actual mobile path after switching modes.
        Call(T("LaboratoryMobileUIHelper"), "InitializeLaboratoryMobileUI");
        yield return Frames(8);
        controls = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        Check(controls != null, "The laboratory mobile initializer supplies real controls.");
        Set(controls, "forceShowOnDesktop", true);
        controls.gameObject.SetActive(true);
        if (Get(controls, "interactButton") == null) Call(controls, "StartOriginalLogic");
        Call(controls, "SetVirtualControlsVisible", true);
        foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(844, 390), new Vector2Int(1024, 768) })
        {
            typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
                .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { size.x, size.y });
            yield return Frames(8);
            Call(npc, "UpdatePromptLocalization");
            Call(npc, "UpdateAvailability");
            Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)Get(npc, "promptPanel");
            var text = (Text)Get(npc, "promptText");
            var hint = panel.Find("TouchControl");
            Check(Screen.width == size.x && Screen.height == size.y && panel.gameObject.activeInHierarchy,
                "Kaede touch prompt is visible in the real lab at " + size + ".");
            Check(hint.gameObject.activeSelf && hint.Find("Visual/Icon").GetComponent<Image>().sprite ==
                ((Button)Get(controls, "interactButton")).transform.Find("Icon").GetComponent<Image>().sprite,
                "Kaede uses the real Inspect button icon at " + size + ".");
            Check(panel.Find("TouchInstruction").gameObject.activeSelf && !text.text.Contains("［E］"),
                "Kaede retains the touch instruction and hides the keyboard prefix.");
            Check(text.preferredHeight <= text.rectTransform.rect.height + 1f &&
                !ScreenRect((RectTransform)hint).Overlaps(ScreenRect(text.rectTransform)), "Kaede's action and icon fit without overlap at " + size + ".");
            Check(!ScreenRect(panel).Overlaps(ScreenRect(GameObject.Find("CurrentToolCanvas/CurrentTool").GetComponent<RectTransform>())),
                "Kaede prompt does not overlap the tool HUD at " + size + ".");
            foreach (string field in new[] { "interactButton", "secondaryInteractButton", "toolWheelButton", "inventoryButton" })
                Check(!ScreenRect(panel).Overlaps(ScreenRect(((Button)Get(controls, field)).GetComponent<RectTransform>())),
                    "Kaede prompt does not cover " + field + " at " + size + ".");
            if (size.x == 960) Check(text.fontSize * ((GameObject)Get(npc, "promptCanvasGO")).GetComponent<Canvas>().scaleFactor >= 18f,
                "Kaede's real touch action is at least 18 pixels at 960x540.");
            yield return Capture("npc-touch-" + size.x + "x" + size.y);
        }
        typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
            .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { 960, 540 });
        Call(input, "EnableDesktopTestMode", false);
        Call(input, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        Set(controls, "forceShowOnDesktop", forceShow);
        Call(controls, "SetVirtualControlsVisible", false);
        yield return Frames(8);
        Call(npc, "UpdatePromptLocalization");
    }

    // ---------------------------------------------------------------- dialogue driver
    private int _lastQuizCount;
    private string _toastSeen;

    private IEnumerator Drive(string segment, float startTimeout)
    {
        _lastQuizCount = 0;
        float start = Time.realtimeSinceStartup;
        while (!StoryActive() && Time.realtimeSinceStartup - start < startTimeout) yield return null;
        if (!StoryActive())
        {
            _dialogue.Add(segment + " (no dialogue)");
            yield break;
        }
        string last = null;
        int n = 0;
        float idleSince = -1f;
        for (int guard = 0; guard < 3000; guard++)
        {
            if (!StoryActive())
            {
                if (idleSince < 0) idleSince = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - idleSince > 1.5f) break;
                yield return null;
                continue;
            }
            idleSince = -1f;
            var panel = GameObject.Find("SubtitleCanvas/ChoicePanel");
            if (panel != null)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                string prompt = Plain(DialogueText());
                var labels = Enumerable.Range(0, 4).Select(i => panel.transform.Find("Choice" + i)).Where(t => t != null)
                    .Select(t => Plain(TmpText(t.gameObject))).ToArray();
                var answer = Answers.First(a => prompt.Contains(a.prompt));
                if (answer.answer == "しゅう曲")
                {
                    Check(prompt.Contains("野外で見たがけ"), "The folding question recalls the cliff seen in the field.");
                    yield return Capture("E02-beat4-folding-question");
                }
                n++;
                _lastQuizCount++;
                string name = $"{segment}-{n:00}-quiz";
                _dialogue.Add($"{name} [{SceneManager.GetActiveScene().name}] QUIZ {prompt} :: {string.Join(" | ", labels)}");
                yield return Capture(name);
                Check(labels[answer.correct].Contains(answer.answer), "Quiz '" + answer.answer + "' shows its correct choice at position " + (answer.correct + 1) + ".");
                panel.transform.Find("Choice" + answer.correct).GetComponent<Button>().onClick.Invoke();
                yield return Frames(3);
                last = null;
                continue;
            }
            string text = DialogueText();
            if (text == null) { yield return null; continue; }
            string key = Plain(Speaker()) + "|" + Plain(text);
            if (key != last)
            {
                yield return new WaitForSecondsRealtime(0.45f);
                n++;
                string name = $"{segment}-{n:00}";
                _dialogue.Add($"{name} [{SceneManager.GetActiveScene().name}] {Plain(Speaker())}: {Plain(DialogueText())}");
                File.WriteAllLines(Path.Combine(_output, "dialogue.txt"), _dialogue);
                yield return Capture(name);
                last = key;
            }
            var bg = GameObject.Find("SubtitleCanvas/BG");
            var button = bg != null ? bg.GetComponent<Button>() : null;
            if (button != null && button.interactable && !(bool)Prop(T("Core.GameInputState"), "IsModalOpen")) button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.12f);
        }
    }

    private bool StoryActive() => (bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive") ||
        GameObject.Find("SubtitleCanvas/BG") != null;
    private string DialogueText() { var go = GameObject.Find("SubtitleCanvas/BG/Text"); return go == null ? null : TmpText(go); }
    private string Speaker() { var go = GameObject.Find("SubtitleCanvas/BG/Speaker"); return go == null ? "" : TmpText(go); }
    private static Component TmpComponent(GameObject go) => go.GetComponentsInChildren<Component>(true).FirstOrDefault(c => c != null && c.GetType().Name == "TextMeshProUGUI");
    private static string TmpText(GameObject go) { var c = TmpComponent(go); return c == null ? "" : (string)Prop(c, "text"); }
    private static string LabelText(GameObject go) => string.Join(" ", go.GetComponentsInChildren<Text>(true).Select(t => t.text));
    /// <summary>Drops ruby readings and TMP tags so logs show the base text only.</summary>
    private static string Plain(string s) => s == null ? "" :
        Regex.Replace(Regex.Replace(s, "<voffset[^>]*>.*?</voffset>", ""), "<[^>]+>", "").Replace("\n", " ").Trim();

    // ---------------------------------------------------------------- quest / scene helpers
    private string QuestStatus(string id) => Call(_quests, "GetQuestStatus", id).ToString();

    private void CompleteQuest(string id)
    {
        Call(_quests, "StartQuest", id);
        var quest = Call(_quests, "GetQuest", id);
        foreach (var objective in (IEnumerable)Get(quest, "objectives")) Call(_quests, "CompleteObjective", (string)Get(objective, "id"));
    }

    private IEnumerator SwitchScene(string scene)
    {
        var manager = Prop(T("GameSceneManager"), "Instance");
        Call(manager, "SwitchToScene", scene);
        float start = Time.realtimeSinceStartup;
        while ((SceneManager.GetActiveScene().name != scene || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) &&
               Time.realtimeSinceStartup - start < 40f) yield return null;
        Check(SceneManager.GetActiveScene().name == scene, "Scene switcher reaches " + scene + ".");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
    }

    private void StopDialogue()
    {
        Call(_director, "CancelPlayback");
        Call(_quests, "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
    }

    private Component Player() => Find("FirstPersonController");
    private object _tools() => Find("ToolManager");
    private Component Tool(string id) => (Component)((Array)Get(_tools(), "availableTools")).Cast<object>()
        .First(t => t != null && (string)Get(t, "toolID") == id);

    private void SelectTool(string id)
    {
        var wheel = Find("InventoryUISystem");
        Call(wheel, "InitializeTools");
        var tools = (IList)Get(wheel, "availableTools");
        int index = Enumerable.Range(0, tools.Count).First(i => (string)Get(tools[i], "toolID") == id);
        Call(wheel, "OpenWheel");
        Call(wheel, "SetSelectedSlot", index);
        Assert.AreEqual(index, Get(wheel, "selectedSlot"));
        Assert.AreEqual(Get(wheel, "selectedColor"), ((Image[])Get(wheel, "slotImages"))[index].color,
            "Selection highlight follows the reordered slot.");
        var slots = (RectTransform[])Get(wheel, "wheelSlots");
        Assert.IsTrue((bool)Call(wheel, "TrySelectToolAtScreenPoint", ScreenRect(slots[index]).center, "鼠标"),
            "The mouse path reaches the reordered " + id + " slot.");
        Assert.AreSame(tools[index], Call(_tools(), "GetCurrentTool"), "Wheel selection equips " + id + ".");
    }

    private void PlaceAtHammerTarget(Component target)
    {
        string id = (string)Prop(target, "TargetId");
        int index = Array.IndexOf(new[]
        {
            "chapter3.field.sample_site_a", "chapter3.field.sample_site_b", "chapter3.field.sample_site_c"
        }, id);
        Assert.GreaterOrEqual(index, 0, "The hammer fixture must use a configured field site.");
        var sceneTargets = FieldSiteTestData.GetHammerTargetsFromMainScene();
        Assert.AreSame(sceneTargets[index], target.transform, "The active hammer target belongs to the loaded MainScene asset.");
        Vector3 foot = sceneTargets[index].position;
        var faces = FieldSiteTestData.HammerFacePoints;
        // Route walking has its own CharacterController acceptance test; this fixture
        // keeps the existing UI/tool capture flow at each real exposed cliff face.
        PlacePlayer(foot + Vector3.up * 0.08f, faces[index]);
        Vector3 eye = foot + Vector3.up * 1.08f;
        Camera.main.transform.position = eye + (faces[index] - eye).normalized * 0.065f;
        Camera.main.transform.LookAt(faces[index]);
    }

    private Vector3 GroundAt(Vector3 site)
    {
        foreach (var hit in Physics.RaycastAll(site + Vector3.up * 50f, Vector3.down, 100f).OrderBy(h => h.distance))
            if (!hit.collider.isTrigger && (hit.collider.GetComponent(T("GeologyLayer")) != null || hit.collider.gameObject.name.ToLower().Contains("terrain"))) return hit.point;
        Assert.Fail("No geological ground under " + site);
        return site;
    }

    private void PlacePlayer(Vector3 feet, Vector3 lookAt)
    {
        var player = Player();
        ((Behaviour)player).enabled = false; // Keep the scripted view; tools and pickups do not need mouse look.
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = feet;
        Camera.main.transform.position = feet + Vector3.up * 1.5f;
        Camera.main.transform.LookAt(lookAt);
        if (controller != null) controller.enabled = true;
        Physics.SyncTransforms();
    }

    private static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private IEnumerator MouseClick()
    {
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f), buttons = 1 });
        yield return null;
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
        yield return new WaitForSecondsRealtime(0.15f);
    }

    private IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return Frames(3);
    }

    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }

    // ---------------------------------------------------------------- capture
    /// <summary>Frame sequence for a short screen recording; also notes any toast shown meanwhile.</summary>
    private IEnumerator Burst(string name, float seconds, int fps)
    {
        _toastSeen = null;
        string folder = Path.Combine(_output, name + "-frames");
        Directory.CreateDirectory(folder);
        int frames = Mathf.CeilToInt(seconds * fps);
        for (int i = 0; i < frames; i++)
        {
            float next = Time.realtimeSinceStartup + 1f / fps;
            var toast = GameObject.Find("GameToastCanvas/Toast");
            if (toast != null && toast.activeInHierarchy && _toastSeen == null) _toastSeen = LabelText(toast);
            yield return CaptureTo(Path.Combine(folder, $"{i:000}.png"));
            if (i == Mathf.RoundToInt(fps * 0.6f)) File.Copy(Path.Combine(folder, $"{i:000}.png"), Path.Combine(_output, name + ".png"), true);
            while (Time.realtimeSinceStartup < next) yield return null;
        }
    }

    private IEnumerator Capture(string name) => CaptureTo(Path.Combine(_output, name + ".png"));

    private IEnumerator CaptureTo(string path)
    {
        var camera = Camera.main;
        if (!Application.isBatchMode || camera == null)
        {
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            yield break;
        }
        // Batch mode: render the live camera and overlay canvases, as FieldFeedbackAcceptanceTests does.
        yield return null;
        int width = Screen.width, height = Screen.height;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
            .OrderBy(c => c.sortingOrder).ToArray();
        var previousCameras = canvases.Select(c => c.worldCamera).ToArray();
        var previousDistances = canvases.Select(c => c.planeDistance).ToArray();
        var target = new RenderTexture(width, height, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D capture = null;
        try
        {
            camera.targetTexture = target;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                // Higher sorting orders sit closer to the camera so they draw on top.
                canvases[i].planeDistance = camera.nearClipPlane + 0.1f + 0.001f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            capture = new Texture2D(width, height, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = previousCameras[i];
                canvases[i].planeDistance = previousDistances[i];
            }
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.Destroy(target);
            if (capture != null) UnityEngine.Object.Destroy(capture);
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (!string.IsNullOrEmpty(_output))
        {
            if (_dialogue.Count > 0) File.WriteAllLines(Path.Combine(_output, "dialogue.txt"), _dialogue);
            if (_director != null && _quests != null) StopDialogue();
            var input = Prop(T("MobileInputManager"), "Instance");
            Call(input, "EnableDesktopTestMode", false);
            Call(input, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
            if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            InputSystem.settings.updateMode = _inputMode;
            InputSystem.settings.backgroundBehavior = _background;
        }
        yield return null;
    }
}
