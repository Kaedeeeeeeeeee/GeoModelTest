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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>Opt-in acceptance run against the shipped MainScene, with real tools, physics, samples and UI.</summary>
public class FieldFeedbackAcceptanceTests
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
    private string _output;
    private object _quests, _director, _tools, _wheel, _mobile;
    private Component _player;
    private Camera _camera;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private InputSettings.UpdateMode _inputMode;
    private InputSettings.BackgroundBehavior _background;
    private readonly List<string> _checks = new List<string>();
    private UnityEngine.Object _backend;
    private bool _backendEnabled;

    private void Check(bool condition, string message)
    {
        Assert.IsTrue(condition, message);
        _checks.Add(message);
        File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
    }

    [UnityTest]
    public IEnumerator ActualFieldScene_ShouldCompleteHammerTowerAndQuizWithScreenshots()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_FIELD_ACCEPTANCE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_FIELD_ACCEPTANCE to opt into the full-scene acceptance run.");
        Directory.CreateDirectory(_output);
        SetGameViewSize(1600, 900);
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
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        Call(Prop(T("StorySystem.QuizScoreManager"), "Instance"), "StartNewRun");
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        StopDialogue();
        Call(_director, "ReloadFlags");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        _player = Find("FirstPersonController");
        Check(_player != null, "Actual MainScene player exists.");
        _camera = Camera.main;
        _tools = Find("ToolManager");
        _wheel = Find("InventoryUISystem");
        _mobile = Prop(T("MobileInputManager"), "Instance");
        Check(_camera != null && _tools != null && _wheel != null && _mobile != null, "Scene camera, tool manager, inventory and input services exist.");
        ((Behaviour)_player).enabled = false; // Position the fixture at the real task site without replaying the commute.
        foreach (string id in new[] { "1002", "1001" }) Call(T("ToolUnlockService"), "UnlockToolById", id);
        yield return null;
        yield return null;
        PrepareQuest("q.field.phase");
        ((HashSet<string>)Get(_quests, "_completedObjectives")).Add("q.field.phase.enter_field");
        Set(_quests, "_fieldPhaseTargetIndex", 0);
        Call(_quests, "ActivateCurrentFieldTarget");
        Call(_quests, "TryBindSampleEvents");
        var guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        var target = (Component)Get(guidance, "activeTarget");
        Check(target != null, "Hammer task uses its real configured guidance target.");
        Vector3 rock = GroundAt(target.transform.position);
        PlacePlayer(rock + new Vector3(0, 0.15f, -1.0f), rock);
        Call(guidance, "RegisterPlayer", _player.transform);
        Call(_tools, "UnequipCurrentTool");
        yield return Frames(5);
        Check(Hint().Contains("Tab"), "Unequipped hammer shows the tool-selection instruction at the site.");
        yield return Capture("01-hammer-select");
        Call(_wheel, "OpenWheel");
        yield return Frames(3);
        Check((string)Prop(T("UISystem.CollectionGuidanceHUD"), "RecommendedToolId") == "1002", "The hammer is highlighted in the tool menu.");
        yield return Capture("02-hammer-tool-menu");
        SelectTool("1002");
        yield return Frames(5);
        Check(Hint().Contains("3"), "Equipped hammer explains three hits on one spot.");
        yield return MouseClick();
        yield return MouseClick();
        var hammer = (Component)((Array)Get(_tools, "availableTools")).Cast<object>().First(t => t != null && (string)Get(t, "toolID") == "1002");
        Check((int)Prop(hammer, "CurrentHitCount") == 2, "Two real primary clicks reach 2/3 hammer hits.");
        yield return Capture("03-hammer-progress");
        yield return MouseClick();
        var sample = (GameObject)Prop(hammer, "PendingSample");
        Check(sample != null && sample.GetComponent(T("SampleCollector")) != null, "Third hammer hit generates a collectible world sample.");
        yield return Frames(4);
        Check(Hint().Contains("E"), "Completed hammer sampling prompts the player to collect the sample.");
        yield return Capture("04-hammer-pickup");
        var inventory = Find("SampleInventory");
        int before = ((IList)Call(inventory, "GetAllSamples")).Count;
        PlacePlayer(sample.transform.position + new Vector3(0, -0.35f, -0.6f), sample.transform.position);
        yield return Frames(4);
        yield return Press(Key.E);
        yield return Frames(8);
        Check(sample == null && ((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "E collects the actual hammer sample into inventory exactly once.");
        // Sampling naturally opens beat2. Check the picture while the teaching line is visible.
        yield return AdvanceUntilPicture();
        yield return Capture("05-picture-hint");
        var banner = GameObject.Find("SubtitleCanvas/IllustrationBanner");
        ClickVisibleButton(banner.GetComponent<Button>(), (RectTransform)banner.transform.Find("ZoomHint"));
        yield return Frames(3);
        Check((bool)Prop(T("Core.GameInputState"), "IsModalOpen"), "Opening the actual illustration blocks underlying controls.");
        yield return Capture("06-picture-enlarged");
        ClickVisibleButton(GameObject.Find("SubtitleCanvas/IllustrationFullscreen/CloseButton").GetComponent<Button>());
        yield return Frames(3);
        Check(!(bool)Prop(T("Core.GameInputState"), "IsModalOpen"), "Close button restores dialogue interaction.");
        yield return AdvanceUntilChoices();
        Check(GameObject.Find("SubtitleCanvas/ChoicePanel") != null, "Hammer collection advances to the rock-identification quiz.");
        yield return Capture("07-quiz-fixed-options");
        yield return VerifyWrongThenCorrect();
        StopDialogue();
        // Switch to the actual chapter-four sampling checkpoint, preserving real field geometry.
        PrepareQuest("q.chapter4.sample");
        Call(T("StorySystem.InvestigationProgress"), "Reset");
        Call(_quests, "MarkChapter4SampleIntroPlayed");
        target = (Component)Get(guidance, "activeTarget");
        Check(target != null, "Drill-tower task uses its configured field target.");
        Vector3 drillSite = GroundAt(target.transform.position);
        PlacePlayer(drillSite + new Vector3(0, 0.15f, -2.3f), drillSite);
        Call(_tools, "UnequipCurrentTool");
        yield return Frames(5);
        yield return Capture("07-tower-select");
        SelectTool("1001");
        yield return Frames(6);
        var drill = (Component)((Array)Get(_tools, "availableTools")).Cast<object>().First(t => t != null && (string)Get(t, "toolID") == "1001");
        Check((bool)Prop(drill, "IsPlacing") && (bool)Prop(drill, "CanConfirmPlacement"), "Selecting the tower opens a valid placement preview at the real site.");
        yield return Capture("08-tower-placement");
        yield return MouseClick();
        yield return Frames(6);
        var tower = (Component)Get(drill, "placedTower");
        Check(tower != null, "Primary click places a real drill tower.");
        PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
        yield return Frames(5);
        Check(Hint().Contains("F"), "Near the placed tower, the prompt switches to F to drill.");
        yield return Capture("09-tower-ready");
        yield return Press(Key.F);
        Check((bool)Get(tower, "isDrilling"), "F starts the actual drill coroutine.");
        yield return Capture("10-tower-drilling");
        yield return new WaitForSeconds(2.3f);
        var cores = (IList)Get(tower, "collectedSamples");
        Check(cores.Count > 0 && (GameObject)cores[0] != null, "Drilling generates an actual core sample.");
        yield return Capture("11-tower-core-pickup");
        var core = (GameObject)cores[0];
        PlacePlayer(GroundAt(core.transform.position) + new Vector3(0, 0.15f, -0.6f), core.transform.position);
        yield return Frames(5);
        before = ((IList)Call(inventory, "GetAllSamples")).Count;
        yield return Press(Key.E);
        yield return Frames(8);
        Check(core == null && ((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "E collects the drilled core exactly once.");
        Check((bool)Prop(T("StorySystem.InvestigationProgress"), "HasCore"), "Core pickup updates the actual investigation progress.");
        StopDialogue();
        // Exercise the virtual-control routing in Unity's supported desktop touch-test mode.
        Call(T("StorySystem.InvestigationProgress"), "Reset");
        PrepareQuest("q.field.phase");
        ((HashSet<string>)Get(_quests, "_completedObjectives")).Add("q.field.phase.enter_field");
        Set(_quests, "_fieldPhaseTargetIndex", 0);
        Call(_quests, "ActivateCurrentFieldTarget");
        PlacePlayer(rock + new Vector3(0, 0.15f, -1.0f), rock);
        Call(_mobile, "EnableDesktopTestMode", true);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        var mobileUI = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        Check(mobileUI != null, "The scene contains its real virtual controls.");
        Set(mobileUI, "forceShowOnDesktop", true);
        mobileUI.gameObject.SetActive(true);
        if (Get(mobileUI, "interactButton") == null) Call(mobileUI, "StartOriginalLogic");
        Call(_tools, "UnequipCurrentTool");
        yield return Frames(8);
        Check(!Hint().Contains("Tab"), "Touch-mode guidance uses the tool button instead of Tab.");
        var hintRect = ScreenRect(GameObject.Find("CollectionGuidanceCanvas/CollectionHint").GetComponent<RectTransform>());
        foreach (string field in new[] { "inventoryButton", "encyclopediaButton", "toolWheelButton", "interactButton", "secondaryInteractButton" })
            Check(!hintRect.Overlaps(ScreenRect(((Button)Get(mobileUI, field)).GetComponent<RectTransform>())), "The touch guidance does not cover " + field + ".");
        yield return Capture("12-touch-hammer-select");
        SelectTool("1002");
        yield return Frames(6);
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        Check((int)Prop(hammer, "CurrentHitCount") == 1, "The actual virtual Inspect button routes to one hammer hit.");
        yield return Capture("13-touch-hammer-progress");
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        Check((int)Prop(hammer, "CurrentHitCount") == 2, "Second virtual Inspect press reaches 2/3.");
        before = ((IList)Call(inventory, "GetAllSamples")).Count;
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        sample = (GameObject)Prop(hammer, "PendingSample");
        Check(sample != null && ((IList)Call(inventory, "GetAllSamples")).Count == before,
            "Third virtual hammer press creates a sample without also collecting it.");
        yield return Capture("13b-touch-hammer-pickup");
        PlacePlayer(GroundAt(sample.transform.position) + new Vector3(0, 0.15f, -0.6f), sample.transform.position);
        yield return Frames(5);
        before = ((IList)Call(inventory, "GetAllSamples")).Count;
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        Check(sample == null && ((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "Virtual Inspect collects the sample exactly once.");
        StopDialogue();
        PrepareQuest("q.chapter4.sample");
        Call(T("StorySystem.InvestigationProgress"), "Reset");
        Call(_quests, "MarkChapter4SampleIntroPlayed");
        Call(drill, "RecallTower");
        PlacePlayer(drillSite + new Vector3(0, 0.15f, -2.3f), drillSite);
        SelectTool("1001");
        yield return Frames(6);
        Check((bool)Prop(drill, "CanConfirmPlacement"), "The virtual-control tower preview can be placed.");
        yield return Capture("14-touch-tower-placement");
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        tower = (Component)Get(drill, "placedTower");
        Check(tower != null, "Virtual Inspect places the tower.");
        PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
        yield return Frames(5);
        yield return Capture("15-touch-tower-ready");
        yield return TouchAction(mobileUI, "OnSecondaryInteractButtonDown", "OnSecondaryInteractButtonUp");
        Check((bool)Get(tower, "isDrilling"), "Virtual Use starts drilling.");
        yield return new WaitForSeconds(2.3f);
        cores = (IList)Get(tower, "collectedSamples");
        Check(cores.Count > 0 && (GameObject)cores[0] != null, "Touch-mode drilling generates a core.");
        core = (GameObject)cores[0];
        PlacePlayer(GroundAt(core.transform.position) + new Vector3(0, 0.15f, -0.6f), core.transform.position);
        yield return Frames(5);
        yield return Capture("16-touch-core-pickup");
        before = ((IList)Call(inventory, "GetAllSamples")).Count;
        yield return TouchAction(mobileUI, "OnInteractButtonDown", "OnInteractButtonUp");
        Check(core == null && ((IList)Call(inventory, "GetAllSamples")).Count == before + 1, "Virtual Inspect collects the core from ground level.");
        Check((bool)Prop(T("StorySystem.InvestigationProgress"), "HasCore"), "Touch-mode core pickup advances investigation progress.");
    }

    private void StopDialogue()
    {
        Call(_director, "CancelPlayback");
        Call(_quests, "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
    }

    private void PrepareQuest(string id)
    {
        StopDialogue();
        foreach (var quest in ((IDictionary)Get(_quests, "_quests")).Values)
            Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), (string)Get(quest, "id") == id ? "InProgress" : "NotStarted"));
        ((HashSet<string>)Get(_quests, "_completedObjectives")).Clear();
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
    }

    private string Hint() => GameObject.Find("CollectionGuidanceCanvas/CollectionHint/NextAction")?.GetComponent<Text>().text ?? "";
    private static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private void ClickVisibleButton(Button button, RectTransform area = null)
    {
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = ScreenRect(area != null ? area : button.GetComponent<RectTransform>()).center,
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0, "UI raycast reaches " + button.name + ".");
        var handler = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check(handler == button.gameObject, "The visible " + button.name + " receives the click without an overlay blocking it.");
    }
    private void SelectTool(string id)
    {
        Call(_wheel, "InitializeTools");
        var tools = (IList)Get(_wheel, "availableTools");
        int index = Enumerable.Range(0, tools.Count).First(i => (string)Get(tools[i], "toolID") == id);
        Call(_wheel, "SelectToolAndStartPreview", index);
        Call(_wheel, "CloseWheel", false);
    }
    private Vector3 GroundAt(Vector3 site)
    {
        var hits = Physics.RaycastAll(site + Vector3.up * 50f, Vector3.down, 100f).OrderBy(h => h.distance);
        foreach (var hit in hits)
            if (!hit.collider.isTrigger && (hit.collider.GetComponent(T("GeologyLayer")) != null || hit.collider.gameObject.name.ToLower().Contains("terrain"))) return hit.point;
        Assert.Fail("No actual geological ground under target " + site);
        return site;
    }
    private void PlacePlayer(Vector3 feet, Vector3 lookAt)
    {
        var controller = _player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        _player.transform.position = feet;
        _camera.transform.position = feet + Vector3.up * 1.5f;
        _camera.transform.LookAt(lookAt);
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
    private IEnumerator TouchAction(object ui, string down, string up)
    {
        Call(ui, down);
        yield return Frames(3);
        Call(ui, up);
        yield return Frames(3);
    }
    private IEnumerator AdvanceUntilPicture()
    {
        for (int i = 0; i < 60; i++)
        {
            var banner = GameObject.Find("SubtitleCanvas/IllustrationBanner");
            if (banner != null && banner.GetComponent<Image>().sprite != null)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Check(banner.transform.Find("ZoomHint").gameObject.activeSelf, "The real teaching picture shows its enlarge hint.");
                yield break;
            }
            var bg = GameObject.Find("SubtitleCanvas/BG");
            if (bg != null) bg.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.1f);
        }
        Assert.Fail("No teaching picture appeared.");
    }

    private void SetGameViewSize(int width, int height)
    {
#if UNITY_EDITOR
        var editor = typeof(UnityEditor.Editor).Assembly;
        var sizesType = editor.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance", Flags).GetValue(null);
        var groupType = editor.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var sizeType = editor.GetType("UnityEditor.GameViewSize");
        var sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, Flags, null,
            new[] { Enum.Parse(sizeKind, "FixedResolution"), (object)width, height, "Feedback acceptance" }, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        var viewType = editor.GetType("UnityEditor.GameView");
        var view = UnityEditor.EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", Flags).SetValue(view, index);
        view.Focus();
#endif
    }

    private IEnumerator AdvanceUntilChoices()
    {
        for (int i = 0; i < 90; i++)
        {
            if (GameObject.Find("SubtitleCanvas/ChoicePanel") != null) yield break;
            var bg = GameObject.Find("SubtitleCanvas/BG");
            if (bg != null) bg.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.08f);
        }
        Assert.Fail("Dialogue did not reach a question.");
    }
    private IEnumerator VerifyWrongThenCorrect()
    {
        var panel = GameObject.Find("SubtitleCanvas/ChoicePanel");
        var labels = panel.GetComponentsInChildren<Component>().Where(c => c.GetType().Name == "TextMeshProUGUI")
            .Select(c => (string)Prop(c, "text")).ToArray();
        var quiz = Prop(T("StorySystem.QuizScoreManager"), "Instance");
        int count = ((IList)Prop(quiz, "Attempts")).Count;
        var seq = Call(T("StorySystem.StorySequenceLoader"), "LoadFromResources", "Story/beat2", false);
        var line = ((IList)Get(seq, "dialogues")).Cast<object>().First(l => (string)Get(l, "questionId") == "q.rock_mudstone");
        var choices = ((IList)Get(line, "choices")).Cast<object>().ToArray();
        int correct = Array.FindIndex(choices, c => (bool)Get(c, "isCorrect"));
        int wrong = (correct + 1) % choices.Length;
        panel.transform.Find("Choice" + wrong).GetComponent<Button>().onClick.Invoke();
        yield return Frames(3);
        yield return AdvanceUntilChoices();
        var retry = GameObject.Find("SubtitleCanvas/ChoicePanel");
        var retryLabels = retry.GetComponentsInChildren<Component>().Where(c => c.GetType().Name == "TextMeshProUGUI")
            .Select(c => (string)Prop(c, "text")).ToArray();
        CollectionAssert.AreEqual(labels, retryLabels, "Wrong-answer retry preserves all displayed option positions.");
        retry.transform.Find("Choice" + correct).GetComponent<Button>().onClick.Invoke();
        yield return Frames(3);
        var attempts = (IList)Prop(quiz, "Attempts");
        Check(attempts.Count == count + 2, "Wrong then correct UI answers record two attempts.");
        Check(!(bool)Get(attempts[count], "isCorrect") && (bool)Get(attempts[count + 1], "isCorrect"), "Scoring follows the actual choice ID after fixed reordering.");
    }
    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    private IEnumerator Capture(string name)
    {
        foreach (var dust in UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None).Where(r => r.name == "DustEffect"))
            Assert.IsTrue(dust.sharedMaterial != null && dust.sharedMaterial.shader.isSupported, "Sample dust renders with a supported material.");
        if (!Application.isBatchMode)
        {
            string path = Path.Combine(_output, name + ".png");
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(File.Exists(path), "Game-view capture completed: " + name);
            yield return null;
            yield break;
        }
        // Batch fallback renders the same live camera and canvases.
        yield return null;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var previousCameras = canvases.Select(c => c.worldCamera).ToArray();
        var previousDistances = canvases.Select(c => c.planeDistance).ToArray();
        var target = new RenderTexture(Screen.width, Screen.height, 24);
        var previousTarget = _camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D capture = null;
        try
        {
            _camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                canvas.planeDistance = _camera.nearClipPlane + 0.1f;
            }
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            RenderTexture.active = target;
            capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(Path.Combine(_output, name + ".png"), capture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = previousCameras[i];
                canvases[i].planeDistance = previousDistances[i];
            }
            _camera.targetTexture = previousTarget;
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
