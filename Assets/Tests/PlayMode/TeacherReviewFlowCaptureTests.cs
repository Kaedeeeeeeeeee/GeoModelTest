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
/// also enforce the 2026-09-30 teacher-review behaviour.
/// </summary>
public class TeacherReviewFlowCaptureTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
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
        ("凝灰岩", 1, "火山が噴火"), ("傾", 2, "東"), ("あのがけ", 0, "しゅう曲"),
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
        foreach (string id in new[] { "1002", "999", "1000", "1001" }) Call(T("ToolUnlockService"), "UnlockToolById", id);
        yield return Frames(3);

        // ---- A: field hammer sampling (feedback items 1, 2 and 3) ----
        foreach (string id in new[] { "q.lab.intro", "q.lab.drkaede", "q.lab.anomaly" }) CompleteQuest(id);
        Call(_quests, "StartQuest", "q.field.phase");
        Call(_quests, "CompleteObjective", "q.field.phase.enter_field");
        Set(_quests, "_fieldPhaseTargetIndex", 0);
        Call(_quests, "ActivateCurrentFieldTarget");
        Call(_quests, "TryBindSampleEvents");
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
        var guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        var target = (Component)Get(guidance, "activeTarget");
        Check(target != null, "Hammer task uses its configured field guidance target.");
        Vector3 rock = GroundAt(target.transform.position);
        PlacePlayer(rock + new Vector3(0, 0.15f, -1.0f), rock);
        Call(guidance, "RegisterPlayer", Player().transform);
        SelectTool("1002");
        yield return Frames(6);
        var tabKey = GameObject.Find("CurrentToolCanvas/CurrentTool/TabKey");
        Observe("A.hud.tab_key", tabKey != null && tabKey.activeInHierarchy ? LabelText(tabKey) : "(none)");
        Expect(tabKey != null && tabKey.activeInHierarchy && LabelText(tabKey).Contains("TAB"),
            "Desktop current-tool HUD shows a TAB key badge.");
        yield return Capture("A01-hud-hammer-equipped");

        var hammer = Tool("1002");
        yield return MouseClick();
        yield return MouseClick();
        yield return MouseClick();
        var sample = (GameObject)Prop(hammer, "PendingSample");
        Check(sample != null, "Three real hammer hits generate a collectible rock sample.");
        yield return Frames(4);
        yield return Capture("A02-hammer-sample-ready");
        var inventory = Find("SampleInventory");
        int before = ((IList)Call(inventory, "GetAllSamples")).Count;
        PlacePlayer(sample.transform.position + new Vector3(0, -0.35f, -0.6f), sample.transform.position);
        yield return Frames(4);
        yield return Press(Key.E);
        Check(((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "E puts the hammer sample into the inventory.");
        Expect(!(bool)Prop(T("UISystem.CollectionGuidanceHUD"), "IsVisible"),
            "Right after pickup the finished hammer guidance is hidden instead of pointing back to the site.");
        yield return Burst("A03-pickup", 3.2f, 6);
        Observe("A.pickup.toast", _toastSeen ?? "(none)");
        Expect(_toastSeen != null && _toastSeen.Contains("岩石"), "Picking up the rock sample shows a success message.");

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
        string promptText = prompt != null && prompt.activeInHierarchy ? prompt.GetComponentInChildren<Text>().text : "(hidden)";
        Observe("D.tower_prompt", promptText.Replace("\n", " / "));
        Check(prompt != null && prompt.activeInHierarchy, "Near the tower after sampling, its interaction prompt is visible.");
        Expect(!promptText.Contains("回収"), "The tower prompt no longer offers recovering the tower.");
        yield return Capture("D04-tower-prompt-after-core");
        yield return Press(Key.G);
        yield return Frames(4);
        Observe("D.tower_after_G", Get(drill, "placedTower") != null ? "still placed" : "recovered");
        Expect(Get(drill, "placedTower") != null, "G no longer recovers the tower during the teaching route.");

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
        var quiz = Prop(T("StorySystem.QuizScoreManager"), "Instance");
        Observe("F.first_try_correct", ((IList)Prop(quiz, "Attempts")).Cast<object>().Count(a => (bool)Get(a, "isCorrect")));
        File.WriteAllLines(Path.Combine(_output, "dialogue.txt"), _dialogue);
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
        Call(wheel, "SelectToolAndStartPreview", index);
        Call(wheel, "CloseWheel", false);
        Assert.AreSame(tools[index], Call(_tools(), "GetCurrentTool"), "Wheel selection equips " + id + ".");
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
        const int width = 1600, height = 900;
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
            if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            InputSystem.settings.updateMode = _inputMode;
            InputSystem.settings.backgroundBehavior = _background;
        }
        yield return null;
    }
}
