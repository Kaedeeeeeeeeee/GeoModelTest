using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>Real MainScene geology and CharacterController acceptance for the mountain field route.</summary>
public class FieldOutcropSceneTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly string[] SiteNames = { "FieldGuidanceTarget_A", "FieldGuidanceTarget_B", "FieldGuidanceTarget_C" };
    private static readonly string[] LayerNames = { "向山層", "大年寺層", "青葉山層" };
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private readonly List<string> _evidence = new List<string>();
    private readonly List<Behaviour> _disabledPlayers = new List<Behaviour>();
    private readonly HashSet<int> _persistentObjectsBefore = new HashSet<int>();
    private readonly Dictionary<string, string> _stringPrefsBefore = new Dictionary<string, string>();
    private readonly Dictionary<string, int?> _intPrefsBefore = new Dictionary<string, int?>();
    private readonly Dictionary<object, object> _questStatusesBefore = new Dictionary<object, object>();
    private readonly Dictionary<object, bool> _objectivesBefore = new Dictionary<object, bool>();
    private readonly Dictionary<Type, object> _storyStatesBefore = new Dictionary<Type, object>();
    private Component _questsBefore, _mobileBefore;
    private HashSet<string> _completedQuestsBefore, _completedObjectivesBefore;
    private int _fieldTargetIndexBefore;
    private object _quizStateBefore, _mobileModeBefore;
    private bool _mobileDesktopBefore;
    private float _timeScaleBefore;
    private CursorLockMode _cursorLockBefore;
    private bool _cursorVisibleBefore;
    private float _walkedLength;

    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => (target as Type ?? target.GetType()).GetField(name, Flags).GetValue(target is Type ? null : target);
    private static void Set(object target, string name, object value) => (target as Type ?? target.GetType()).GetField(name, Flags).SetValue(target is Type ? null : target, value);
    private static object Prop(object target, string name) => (target as Type ?? target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        var method = type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
            m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
        return method.Invoke(target is Type ? null : target, args);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        CaptureOriginalEnvironment();
        _backend = Resources.Load("BackendSettings");
        if (_backend != null)
        {
            _backendEnabled = (bool)Get(_backend, "enableBackend");
            Set(_backend, "enableBackend", false);
        }
        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
        // ResetAll reloads this persistent singleton before the fixture writes its completed chapter flags.
        // Reload before scene events, otherwise the cached empty flags replay the opening third-person camera.
        Call(Prop(T("StorySystem.StoryDirector"), "Instance"), "ReloadFlags");
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(3f);
        StopDialogue();
    }

    [UnityTest]
    public IEnumerator ActualMainSceneTargets_ShouldExposeSuccessivelyYoungerCliffs()
    {
        var sceneTargets = FieldSiteTestData.GetHammerTargetsFromMainScene();
        for (int i = 0; i < SiteNames.Length; i++)
        {
            var target = sceneTargets[i];
            RaycastHit foot = GroundAt(target.position);
            Assert.LessOrEqual(Vector3.Angle(foot.normal, Vector3.up), 20f, SiteNames[i] + " has a standable cliff foot.");
            Assert.GreaterOrEqual(EdgeDistance(foot.point), 15f, SiteNames[i] + " stays inside the map margin.");
            bool exposed = false;
            var player = Find("FirstPersonController");
            var controller = player.GetComponent<CharacterController>();
            float eyeHeight = controller.height * 0.5f - controller.center.y + controller.skinWidth +
                ((Vector3)Get(player, "cameraPosition")).y;
            Vector3 eyes = foot.point + Vector3.up * eyeHeight;
            for (int yaw = 0; yaw < 360 && !exposed; yaw += 3)
            {
                foreach (float pitch in new[] { -25f, -12f, 0f, 12f })
                {
                    Vector3 direction = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                    if (!Physics.Raycast(eyes, direction, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    var layer = hit.collider.GetComponent(T("GeologyLayer"));
                    exposed = layer != null && ((string)Get(layer, "layerName")).Contains(LayerNames[i]) &&
                        Vector3.Angle(hit.normal, Vector3.up) >= 40f;
                    if (exposed) break;
                }
            }
            Assert.IsTrue(exposed, SiteNames[i] + " must expose " + LayerNames[i] +
                " on the first eye-height hit within hammer range, with cliff slope >= 40 degrees; actual foot=" + foot.point + ", ground=" + foot.collider.name);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator PhaseShifterReturn_ShouldWalkYoungerOutcropsAndDrillWithoutChangingNewGame()
    {
        // Exercise the title-menu session path: this clears saved field entrances before the manager opens MainScene.
        yield return StartNewGameFromTitle();
        var player = Find("FirstPersonController");
        Assert.IsNotNull(player, "New Game creates the real MainScene player.");
        Assert.Less(Vector2.Distance(XZ(player.transform.position), new Vector2(-29.923f, -20.96f)), 0.5f,
            "New Game keeps Lily's classroom entrance.");
        Assert.IsNotNull(GameObject.Find("ClassRoom"), "New Game keeps the classroom visible.");
        Assert.AreEqual(0, PlayerPrefs.GetInt("MainScene.ClassRoom.Hidden", 0), "New Game keeps classroom-hidden state clear.");

        // This geometry/tool test starts at the field chapters after verifying the real New Game entrance.
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
        Call(Prop(T("StorySystem.StoryDirector"), "Instance"), "ReloadFlags");
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);

        yield return PhaseSwitch("Laboratory Scene");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        PrepareQuest(quests, "q.field.phase");
        yield return PhaseSwitch("MainScene");
        yield return CheckArrival("step 3");

        var layout = T("FieldSiteLayout");
        var paths = FieldSiteTestData.WalkingPaths;
        var faces = FieldSiteTestData.HammerFacePoints;
        var sceneTargets = FieldSiteTestData.GetHammerTargetsFromMainScene();
        var feet = sceneTargets.Select(target => target.position).ToArray();
        FieldSiteTestData.AssertWalkingPathAnchors((Vector3)Get(layout, "ArrivalPosition"), feet);
        Assert.AreEqual(3, paths.Length, "The field route defines three consecutive walking segments.");
        Assert.AreEqual(3, faces.Length);
        Assert.AreEqual(3, feet.Length);
        Assert.LessOrEqual(paths.Sum(PathLength), 90f, "The designed arrival -> A -> B -> C route is at most 90 m.");
        Assert.AreEqual("InProgress", Call(quests, "GetQuestStatus", "q.field.phase").ToString(),
            "Step 3 returns to the field with the actual hammer quest already active.");
        ((HashSet<string>)Get(quests, "_completedObjectives")).Add("q.field.phase.enter_field");
        Set(quests, "_fieldPhaseTargetIndex", 0);
        Call(quests, "ActivateCurrentFieldTarget");
        Call(quests, "TryBindSampleEvents");
        foreach (string id in new[] { "999", "1002", "1001" }) Call(T("ToolUnlockService"), "UnlockToolById", id);
        yield return new WaitForSecondsRealtime(0.8f);
        var hammer = Tool("1002");
        var tools = Find("ToolManager");
        Call(tools, "EquipTool", hammer);
        yield return new WaitForSecondsRealtime(1.1f);

        _walkedLength = 0f;
        for (int site = 0; site < 3; site++)
        {
            yield return Walk(paths[site], "arrival-A-B-C segment " + (site + 1));
            StopDialogue();
            yield return Frames(2);
            player = Find("FirstPersonController");
            var target = sceneTargets[site];
            Assert.Less(Vector3.Distance(player.transform.position, target.position),
                (float)Prop(target.GetComponent(T("GuidanceSystem.GuidanceTarget")), "DetectionRadius"),
                SiteNames[site] + " is reached using CharacterController.Move.");
            Assert.Less(Vector2.Distance(XZ(player.transform.position), XZ(feet[site])), 0.3f,
                "The controller reaches the standable foot of " + SiteNames[site] + ".");
            var camera = player.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(camera, "The real player camera exists after reaching the outcrop.");
            Assert.IsTrue(camera.gameObject.activeInHierarchy, "Field dialogue restores the player's camera before sampling.");
            camera.transform.LookAt(faces[site]);
            Physics.SyncTransforms();
            var ray = camera.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            Assert.IsTrue(Physics.Raycast(ray, out RaycastHit face, (float)Get(hammer, "useRange"), ~0, QueryTriggerInteraction.Ignore),
                "Real player eye-height ray reaches the " + SiteNames[site] + " cliff within hammer range.");
            var layer = face.collider.GetComponent(T("GeologyLayer"));
            Assert.IsNotNull(layer, "The first eye-height hit is a geological layer.");
            StringAssert.Contains(LayerNames[site], (string)Get(layer, "layerName"));
            Assert.GreaterOrEqual(Vector3.Angle(face.normal, Vector3.up), 40f, "The hammer hits a natural steep cliff.");
            for (int hit = 0; hit < 3; hit++)
            {
                Assert.IsTrue((bool)Call(hammer, "RequestPrimaryUse"), "Hammer accepts real primary use " + (hit + 1) + ".");
                yield return new WaitForSecondsRealtime(0.7f);
            }
            var sample = (GameObject)Prop(hammer, "PendingSample");
            Assert.IsNotNull(sample, "Three real hammer uses generate a world sample at " + SiteNames[site] + ".");
            var collector = sample.GetComponent(T("SampleCollector"));
            Assert.IsNotNull(collector);
            var sampleData = Get(collector, "sampleData");
            var sampleLayers = SampleLayers(sampleData);
            Assert.IsNotEmpty(sampleLayers, "The actual hammer sample retains geological metadata.");
            Assert.IsTrue(sampleLayers.All(n => n.Contains(LayerNames[site])),
                "Hammer sample must belong to " + LayerNames[site] + "; actual=" + string.Join(" | ", sampleLayers));
            Assert.IsTrue((bool)Call(quests, "IsSampleFromCurrentFieldTarget", sampleData),
                "Actual hammer source position is within the current target's 5 m acceptance radius.");
            Call(collector, "CheckPlayerInteraction");
            Assert.IsTrue((bool)Get(collector, "playerInRange"),
                "The actual collector detects the scene CharacterController within its pickup radius at the cliff foot.");
            int before = ((IList)Call(Find("SampleInventory"), "GetAllSamples")).Count;
            Call(collector, "CollectSample");
            yield return null;
            Assert.AreEqual(before + 1, ((IList)Call(Find("SampleInventory"), "GetAllSamples")).Count,
                "The real collector inserts the rock exactly once into inventory.");
            Assert.AreEqual(site + 1, (int)Get(quests, "_fieldPhaseTargetIndex"),
                "The real inventory event counts the rock for current field site " + (site + 1) + ".");
            Record("HAMMER " + SiteNames[site] + " layer=" + string.Join(" | ", sampleLayers) + " hit=" + face.point +
                " slope=" + Vector3.Angle(face.normal, Vector3.up).ToString("F2") + " quest_count=" + Get(quests, "_fieldPhaseTargetIndex"));
            StopDialogue();
            yield return new WaitForSecondsRealtime(1.1f);
        }
        Assert.LessOrEqual(_walkedLength, 90f, "The measured CharacterController arrival -> A -> B -> C walk stays within 90 m.");
        Record("HAMMER_ROUTE actual_length=" + _walkedLength.ToString("F2") + " m");

        // A second real phase-shifter return must also discard the last saved field position.
        yield return PhaseSwitch("Laboratory Scene");
        PrepareQuest(quests, "q.chapter4.sample");
        yield return PhaseSwitch("MainScene");
        yield return CheckArrival("step 6");
        yield return WalkAndDrillMountainTerrace();
    }

    [UnityTest]
    public IEnumerator MountainDrill_ShouldWalkPlaceAndCrossLayers()
    {
        yield return PhaseSwitch("Laboratory Scene");
        PrepareQuest(Prop(T("QuestSystem.QuestManager"), "Instance"), "q.chapter4.sample");
        yield return PhaseSwitch("MainScene");
        yield return CheckArrival("drill regression");
        yield return WalkAndDrillMountainTerrace();
    }

    private IEnumerator WalkAndDrillMountainTerrace()
    {
        var layout = T("FieldSiteLayout");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        Assert.AreEqual("InProgress", Call(quests, "GetQuestStatus", "q.chapter4.sample").ToString(),
            "The drill quest was active before the real laboratory -> field scene event.");
        var guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        var target = (Component)Get(guidance, "activeTarget");
        Assert.IsNotNull(target, "The real QuestManager scene event activates the production drill guidance anchor.");
        Assert.AreEqual("chapter4.sample.site", (string)Prop(target, "TargetId"));
        Assert.Less(Vector3.Distance((Vector3)Prop(target, "WorldPosition"), (Vector3)Get(layout, "DrillPosition")), 0.01f,
            "The production Chapter4SampleTargetPosition points to the same mountain terrace as the tested drill layout.");
        Assert.AreEqual(5f, (float)Prop(target, "DetectionRadius"), 0.01f,
            "The production drill target retains its actual 5 m detection radius.");
        Call(T("ToolUnlockService"), "UnlockToolById", "1001");
        yield return Frames(5);
        var drillPath = FieldSiteTestData.DrillWalkingPath;
        var drillPosition = (Vector3)Get(layout, "DrillPosition");
        FieldSiteTestData.AssertDrillWalkingPathAnchors((Vector3)Get(layout, "ArrivalPosition"), drillPosition,
            FieldSiteTestData.GetHammerFootPositionsFromMainScene());
        Vector3 drillStandingPoint = drillPath[drillPath.Length - 1];
        Assert.AreEqual(GroundAt(drillStandingPoint).point.y, drillStandingPoint.y, 0.01f,
            "The drill route's standing endpoint matches the real terrace surface.");
        Assert.LessOrEqual(PathLength(drillPath), 60f, "Arrival -> drill walking route is at most 60 m.");
        _walkedLength = 0f;
        yield return Walk(drillPath, "arrival-drill");
        StopDialogue();
        yield return Frames(2);
        Assert.LessOrEqual(_walkedLength, 60f, "The measured CharacterController arrival -> drill walk stays within 60 m.");
        var drill = Tool("1001");
        var tools = Find("ToolManager");
        Call(tools, "EquipTool", drill);
        yield return Frames(3); // Equip deliberately suppresses the selecting frame and the next frame.
        var player = Find("FirstPersonController");
        var drillCamera = player.GetComponentInChildren<Camera>(true);
        Assert.IsNotNull(drillCamera, "The real player camera exists after walking to the drill terrace.");
        Assert.IsTrue(drillCamera.gameObject.activeInHierarchy, "Field dialogue restores the player's camera before drill placement.");
        drillCamera.transform.LookAt(drillPosition);
        Physics.SyncTransforms();
        Assert.IsTrue((bool)Call(drill, "RequestPrimaryUse"), "The real drill-tower primary use enters its placement preview.");
        yield return Frames(8);
        Assert.IsTrue((bool)Prop(drill, "CanConfirmPlacement"), "The real drill-tower preview is placeable on the mountain terrace.");
        Assert.IsTrue((bool)Call(drill, "RequestPrimaryUse"), "The real drill-tower primary use confirms placement.");
        yield return Frames(8);
        var tower = (Component)Get(drill, "placedTower");
        Assert.IsNotNull(tower, "The drill tower is instantiated by the actual placeable tool.");
        Assert.Less(Vector2.Distance(XZ(tower.transform.position), XZ(drillPosition)), 1f,
            "The real tower placement reaches the specified terrace.");
        Assert.LessOrEqual(Vector3.Angle(GroundAt(tower.transform.position).normal, Vector3.up), 20f,
            "The tower terrace has a standable surface.");
        Record("DRILL_PLACEMENT can_confirm_placement=True tower=" + tower.transform.position +
            " ground_slope=" + Vector3.Angle(GroundAt(tower.transform.position).normal, Vector3.up).ToString("F2") + " deg");
        Assert.IsTrue((bool)Call(tower, "CanDrill"));
        Call(tower, "StartDrilling");
        yield return new WaitForSecondsRealtime(2.5f);
        Assert.AreEqual(1, (int)Get(tower, "currentDrillCount"), "One actual drilling operation completes.");
        var cores = (IList)Get(tower, "collectedSamples");
        Assert.AreEqual(1, cores.Count, "One actual drilling operation creates one core.");
        var core = (GameObject)cores[0];
        var coreData = Get(core.GetComponent(T("SampleCollector")), "sampleData");
        var coreLayers = SampleLayers(coreData);
        Assert.GreaterOrEqual(coreLayers.Length, 2, "The first 2 m core crosses the terrace's real layer boundary.");
        StringAssert.Contains("大年寺層", coreLayers[0], "The first core starts in the L7 mountain terrace stratum.");
        StringAssert.Contains("向山層", coreLayers[1], "The first core descends from L7 into L6.");
        Assert.IsTrue(coreLayers.All(n => new[] { "向山層", "大年寺層", "青葉山層" }.Any(n.Contains)),
            "The first core contains the mountain strata; actual=" + string.Join(" | ", coreLayers));
        var surface = GroundAt(tower.transform.position).point;
        var downLayers = Physics.RaycastAll(surface + Vector3.up * 0.05f, Vector3.down, 10.05f, ~0, QueryTriggerInteraction.Ignore)
            .OrderBy(h => h.distance).Select(h => h.collider.GetComponent(T("GeologyLayer"))).Where(l => l != null)
            .Select(l => (string)Get(l, "layerName")).Distinct().ToArray();
        Assert.GreaterOrEqual(downLayers.Length, 2, "The first 10 m below the actual tower exposes alternating strata.");
        Record("DRILL route_length=" + _walkedLength.ToString("F2") + " m core_0-2m=" + string.Join(" | ", coreLayers) +
            " physical_0-10m=" + string.Join(" | ", downLayers));

        var ordinarySavedPosition = player.transform.position;
        yield return SwitchScene("Laboratory Scene", false);
        yield return SwitchScene("MainScene", false);
        Assert.Less(Vector3.Distance(Find("FirstPersonController").transform.position, ordinarySavedPosition), 0.3f,
            "An ordinary laboratory -> MainScene switch preserves the saved field position; only the phase shifter uses the new arrival.");
        Record("ORDINARY_RETURN saved=" + ordinarySavedPosition + " restored=" + Find("FirstPersonController").transform.position);
    }

    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));
    private static Vector2 XZ(Vector3 point) => new Vector2(point.x, point.z);
    private static float PathLength(Vector3[] path)
    {
        float length = 0f;
        for (int i = 1; i < path.Length; i++) length += Vector3.Distance(path[i - 1], path[i]);
        return length;
    }

    private static string[] SampleLayers(object sample) => ((IEnumerable)Get(sample, "geologicalLayers")).Cast<object>()
        .Select(layer => (string)Get(layer, "layerName")).ToArray();

    private static Component Tool(string id) => (Component)((Array)Get(Find("ToolManager"), "availableTools")).Cast<object>()
        .First(tool => tool != null && (string)Get(tool, "toolID") == id);

    private static void PrepareQuest(object quests, string id)
    {
        StopDialogue();
        foreach (var quest in ((IDictionary)Get(quests, "_quests")).Values)
            Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), (string)Get(quest, "id") == id ? "InProgress" : "NotStarted"));
        ((HashSet<string>)Get(quests, "_completedObjectives")).Clear();
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
    }

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    private IEnumerator PhaseSwitch(string scene) => SwitchScene(scene, true);

    private IEnumerator StartNewGameFromTitle()
    {
        yield return SwitchScene("StartScene", false);
        var session = Prop(T("SceneSystem.GameSession"), "Instance");
        Call(session, "StartNewGame");
        float started = Time.realtimeSinceStartup;
        while ((SceneManager.GetActiveScene().name != "MainScene" || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) &&
            Time.realtimeSinceStartup - started < 40f) yield return null;
        Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name, "The title GameSession.StartNewGame route reaches MainScene.");
        Assert.IsFalse((bool)Prop(T("GameSceneManager"), "IsLoadingScene"));
        yield return new WaitForSecondsRealtime(1.2f);
        // Complete the camera-owning coroutine through its real dialogue buttons before canceling the later automatic trip.
        yield return FinishOpeningCinematic();
        StopDialogue();
        Record("NEW_GAME GameSession.StartNewGame -> GameSceneManager -> MainScene player=" + Find("FirstPersonController").transform.position);
    }

    private IEnumerator SwitchScene(string scene, bool phaseShifter)
    {
        yield return FinishOpeningCinematic();
        StopDialogue();
        var manager = Prop(T("GameSceneManager"), "Instance");
        if (phaseShifter)
        {
            // Use the shipped owner-bound button listener, so regression in the phase-shifter UI route is observable.
            Call(T("ToolUnlockService"), "UnlockToolById", "999");
            yield return Frames(5);
            var tool = Tool("999");
            Call(Find("ToolManager"), "EquipTool", tool);
            yield return Frames(3);
            Call(manager, "ShowSceneSelectionUI", tool);
            yield return Frames(2);
            var selection = (GameObject)Get(manager, "sceneSelectionUI");
            Assert.IsNotNull(selection, "The real phase-shifter opens scene selection.");
            Assert.IsTrue(selection.activeInHierarchy);
            Assert.AreSame(tool, Get(manager, "selectionOwner"), "Scene selection retains its equipped phase-shifter owner.");
            var button = selection.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "SceneButton_" + scene);
            Assert.IsNotNull(button, "The real scene selection provides the " + scene + " button.");
            Assert.IsTrue(button.interactable, "The requested destination is enabled in the real phase-shifter UI.");
            button.onClick.Invoke();
            Record("PHASE_UI owner=" + tool.name + " button=" + button.name);
        }
        else
        {
            Call(manager, "SwitchToScene", scene);
        }
        float started = Time.realtimeSinceStartup;
        while ((SceneManager.GetActiveScene().name != scene || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) &&
            Time.realtimeSinceStartup - started < 40f) yield return null;
        Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "The real scene route reaches " + scene + ".");
        yield return new WaitForSecondsRealtime(1.2f);
        yield return FinishOpeningCinematic();
        StopDialogue();
    }

    private IEnumerator FinishOpeningCinematic()
    {
        if (GameObject.Find("CinematicCamera") == null) yield break;
        var player = Find("FirstPersonController");
        Assert.IsNotNull(player, "The opening cinematic retains its scene player.");
        var camera = player.GetComponentInChildren<Camera>(true);
        Assert.IsNotNull(camera);
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Time.timeScale = 1f;
        float started = Time.realtimeSinceStartup;
        float nextClick = started;
        while (GameObject.Find("CinematicCamera") != null || !camera.gameObject.activeInHierarchy)
        {
            Assert.Less(Time.realtimeSinceStartup - started, 15f,
                "The real opening dialogue must finish and restore its first-person camera before fixture cancellation.");
            Assert.AreEqual("MainScene", SceneManager.GetActiveScene().name,
                "Verify the classroom entrance before the opening's automatic laboratory trip.");
            var background = GameObject.Find("SubtitleCanvas/BG");
            var advance = background != null ? background.GetComponent<Button>() : null;
            if (advance != null && advance.interactable && Time.realtimeSinceStartup >= nextClick)
            {
                Assert.IsFalse((bool)Prop(T("Core.GameInputState"), "IsModalOpen"), "No modal obscures the opening dialogue's real advance action.");
                advance.onClick.Invoke();
                nextClick = Time.realtimeSinceStartup + 0.08f;
            }
            yield return null;
        }
        Assert.IsTrue(camera.gameObject.activeInHierarchy, "Normal cinematic cleanup restores the player camera.");
        Record("OPENING_CINEMATIC normal_dialogue_completion restored_player_camera=True");
    }

    private IEnumerator CheckArrival(string returnName)
    {
        var player = Find("FirstPersonController");
        Assert.IsNotNull(player);
        var layout = T("FieldSiteLayout");
        var arrival = (Vector3)Get(layout, "ArrivalPosition");
        Assert.Less(Vector3.Distance(player.transform.position, arrival), 0.3f,
            returnName + " phase-shifter return uses the fixed mountain arrival rather than the saved MainScene position.");
        Assert.Less(Quaternion.Angle(player.transform.rotation, (Quaternion)Get(layout, "ArrivalRotation")), 0.5f,
            returnName + " return faces the first outcrop.");
        Assert.AreEqual(1, PlayerPrefs.GetInt("MainScene.ClassRoom.Hidden", 0), "Leaving MainScene still hides the classroom.");
        var classroom = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "ClassRoom");
        Assert.IsNotNull(classroom);
        Assert.IsFalse(classroom.gameObject.activeSelf, "Classroom hiding still applies on the field return.");
        var behaviour = (Behaviour)player;
        _disabledPlayers.Add(behaviour);
        behaviour.enabled = false; // Movement below is performed by this same scene CharacterController, without input jitter.
        var controller = player.GetComponent<CharacterController>();
        Assert.IsNotNull(controller);
        Assert.IsTrue(controller.enabled);
        controller.slopeLimit = 45f;
        yield return Frames(2);
        Record("ARRIVAL " + returnName + " player=" + player.transform.position + " yaw=" + player.transform.eulerAngles.y.ToString("F2"));
    }

    private IEnumerator Walk(Vector3[] path, string segment)
    {
        Assert.IsNotEmpty(path, segment + " supplies physical walking waypoints.");
        var player = Find("FirstPersonController");
        var controller = player.GetComponent<CharacterController>();
        Assert.Less(Vector2.Distance(XZ(player.transform.position), XZ(path[0])), 0.5f, segment + " starts at the player's current position.");
        const float step = 1f / 60f;
        float verticalSpeed = -2f;
        float measured = 0f;
        float maxSlope = 0f;
        for (int waypoint = 1; waypoint < path.Length; waypoint++)
        {
            int stalledSteps = 0;
            float bestDistance = float.MaxValue;
            for (int guard = 0; guard < 12000; guard++)
            {
                Vector3 delta = path[waypoint] - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.15f) break;
                if (delta.magnitude < bestDistance - 0.02f) { bestDistance = delta.magnitude; stalledSteps = 0; }
                else stalledSteps++;
                Assert.Less(stalledSteps, 240, segment + " stalls before waypoint " + waypoint + " at " + player.transform.position);
                Vector3 before = player.transform.position;
                verticalSpeed = controller.isGrounded ? -2f : Mathf.Max(-15f, verticalSpeed - 15f * step);
                Vector3 movement = Vector3.ClampMagnitude(delta, 3f * step) + Vector3.up * (verticalSpeed * step);
                controller.Move(movement);
                Physics.SyncTransforms();
                measured += Vector3.Distance(before, player.transform.position);
                Assert.GreaterOrEqual(EdgeDistance(player.transform.position), 0f, segment + " never leaves the field map.");
                var ground = GroundAt(player.transform.position);
                float slope = Vector3.Angle(ground.normal, Vector3.up);
                maxSlope = Mathf.Max(maxSlope, slope);
                Assert.LessOrEqual(slope, 40.01f, segment + " remains below the 45-degree controller limit with margin at " + player.transform.position);
                Assert.GreaterOrEqual(controller.bounds.min.y - ground.point.y, -0.4f, segment + " never falls below the geological ground.");
                Assert.LessOrEqual(controller.bounds.min.y - ground.point.y, 1f, segment + " never crosses an unsupported drop.");
                yield return null;
                if (guard == 11999) Assert.Fail(segment + " times out before waypoint " + waypoint);
            }
        }
        _walkedLength += measured;
        Record("WALK " + segment + " measured=" + measured.ToString("F2") + " m planned=" + PathLength(path).ToString("F2") +
            " m maximum_ground_slope=" + maxSlope.ToString("F2") + " deg end=" + player.transform.position);
    }

    private void Record(string value)
    {
        _evidence.Add(value);
        Debug.Log("[FieldOutcropSceneTests] " + value);
        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "move-sites", "acceptance");
        Directory.CreateDirectory(output);
        File.WriteAllLines(Path.Combine(output, "route-and-samples.txt"), _evidence);
    }

    private static RaycastHit GroundAt(Vector3 point)
    {
        foreach (var hit in Physics.RaycastAll(new Vector3(point.x, 100f, point.z), Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            if (hit.collider.GetComponent(T("GeologyLayer")) != null || hit.collider.name.Contains("0629final")) return hit;
        Assert.Fail("No geological ground under " + point);
        return default;
    }

    private static float EdgeDistance(Vector3 point) => Mathf.Min(point.x + 75f, 224f - point.x, point.z + 75f, 224f - point.z);

    private static void StopDialogue()
    {
        var director = Prop(T("StorySystem.StoryDirector"), "Instance");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        if (director != null) Call(director, "CancelPlayback");
        if (quests != null) Call(quests, "CancelPendingPlayback");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
    }

    private void CaptureOriginalEnvironment()
    {
        _persistentObjectsBefore.Clear();
        var probe = new GameObject("FieldOutcropPersistentSceneProbe");
        UnityEngine.Object.DontDestroyOnLoad(probe);
        foreach (var root in probe.scene.GetRootGameObjects())
            if (root != probe)
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    _persistentObjectsBefore.Add(child.gameObject.GetInstanceID());
        UnityEngine.Object.DestroyImmediate(probe);
        _timeScaleBefore = Time.timeScale;
        _cursorLockBefore = Cursor.lockState;
        _cursorVisibleBefore = Cursor.visible;
        _mobileBefore = Find("MobileInputManager");
        if (_mobileBefore != null)
        {
            _mobileModeBefore = Get(_mobileBefore, "currentInputMode");
            _mobileDesktopBefore = (bool)Get(_mobileBefore, "desktopTestMode");
        }
        _stringPrefsBefore.Clear();
        foreach (string key in new[]
        {
            "StoryFlags", "QuestSystem.CompletedQuests", "QuestSystem.CompletedObjectives", "GameSession.ResumeScene.v1",
            "PlayerPersistentData.Inventory", "PlayerPersistentData.UnlockedToolIds", "PlayerPersistentData.Scene.MainScene",
            "PlayerPersistentData.Scene.Laboratory Scene", "StorySystem.QuizAttemptState.v2", "StorySystem.Investigation.v1",
            "StorySystem.Checkpoints.v1", "StorySystem.History.v1", "Backend.SurveyCompletion.v1", "Backend.SurveyTicket.v1"
        }) _stringPrefsBefore[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        _intPrefsBefore.Clear();
        foreach (string key in new[] { "FirstControlGuide.Completed.v2", "FirstControlGuide.FieldCompleted.v1", "MainScene.ClassRoom.Hidden" })
            _intPrefsBefore[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
        _storyStatesBefore.Clear();
        foreach (string name in new[] { "StorySystem.InvestigationProgress", "StorySystem.StoryCheckpoint", "StorySystem.StoryHistory" })
            _storyStatesBefore[T(name)] = Get(T(name), "_state");
        _quizStateBefore = Get(Prop(T("StorySystem.QuizScoreManager"), "Instance"), "_state");
        _questsBefore = Find("QuestSystem.QuestManager");
        _questStatusesBefore.Clear();
        _objectivesBefore.Clear();
        if (_questsBefore != null)
        {
            _completedQuestsBefore = new HashSet<string>((HashSet<string>)Get(_questsBefore, "_completedQuests"));
            _completedObjectivesBefore = new HashSet<string>((HashSet<string>)Get(_questsBefore, "_completedObjectives"));
            _fieldTargetIndexBefore = (int)Get(_questsBefore, "_fieldPhaseTargetIndex");
            foreach (var quest in ((IDictionary)Get(_questsBefore, "_quests")).Values)
            {
                _questStatusesBefore[quest] = Get(quest, "status");
                foreach (var objective in (IEnumerable)Get(quest, "objectives"))
                    _objectivesBefore[objective] = (bool)Get(objective, "completed");
            }
        }
    }

    private void RestoreOriginalProgress()
    {
        foreach (var pair in _stringPrefsBefore)
        {
            if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key);
            else PlayerPrefs.SetString(pair.Key, pair.Value);
        }
        foreach (var pair in _intPrefsBefore)
        {
            if (!pair.Value.HasValue) PlayerPrefs.DeleteKey(pair.Key);
            else PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
        }
        PlayerPrefs.Save();
        foreach (var pair in _storyStatesBefore) Set(pair.Key, "_state", pair.Value);
        Set(Prop(T("StorySystem.QuizScoreManager"), "Instance"), "_state", _quizStateBefore);
        var director = Find("StorySystem.StoryDirector");
        if (director != null) Call(director, "ReloadFlags");
        if (_questsBefore != null)
        {
            Set(_questsBefore, "_completedQuests", _completedQuestsBefore);
            Set(_questsBefore, "_completedObjectives", _completedObjectivesBefore);
            Set(_questsBefore, "_fieldPhaseTargetIndex", _fieldTargetIndexBefore);
            foreach (var pair in _questStatusesBefore) Set(pair.Key, "status", pair.Value);
            foreach (var pair in _objectivesBefore) Set(pair.Key, "completed", pair.Value);
        }
        if (_mobileBefore != null)
        {
            Call(_mobileBefore, "EnableDesktopTestMode", _mobileDesktopBefore);
            Call(_mobileBefore, "SwitchInputMode", _mobileModeBefore);
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        StopDialogue();
        _disabledPlayers.Clear();
        // Unload the real field through an asset scene, then leave the next synthetic fixture an empty surface.
        // Keeping MainScene alive lets its Lily and mobile singleton win FindFirstObjectByType and singleton Awake.
        yield return SceneManager.LoadSceneAsync("StartScene");
        Call(T("UISystem.ResearchConsentDialog"), "CloseCurrent");
        StopDialogue();
        var title = SceneManager.GetActiveScene();
        var empty = SceneManager.CreateScene("FieldOutcropSceneTests_Empty_" + Time.frameCount);
        SceneManager.SetActiveScene(empty);
        yield return SceneManager.UnloadSceneAsync(title);
        var probe = new GameObject("FieldOutcropPersistentSceneProbe");
        UnityEngine.Object.DontDestroyOnLoad(probe);
        foreach (var root in probe.scene.GetRootGameObjects())
        {
            if (root == probe) continue;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child != null && !_persistentObjectsBefore.Contains(child.gameObject.GetInstanceID()))
                    UnityEngine.Object.Destroy(child.gameObject);
        }
        UnityEngine.Object.Destroy(probe);
        yield return null;
        RestoreOriginalProgress();
        Call(T("Core.GameInputState"), "ReleaseAll");
        if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
        Time.timeScale = _timeScaleBefore;
        Cursor.lockState = _cursorLockBefore;
        Cursor.visible = _cursorVisibleBefore;
        yield return null;
        yield return null;
    }
}
