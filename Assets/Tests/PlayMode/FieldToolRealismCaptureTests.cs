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

/// <summary>
/// Opt-in visual check of the realistic hammer and core-drilling tools in the shipped MainScene and lab.
/// Set GEOMODEL_REALISM_CAPTURE to an output folder; screenshots, timings and layout checks are written there.
/// </summary>
public class FieldToolRealismCaptureTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    private string _output;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private object _quests, _director;
    private InputSettings.UpdateMode _inputMode;
    private InputSettings.BackgroundBehavior _background;
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private int _viewSizeIndex = -1;
    private readonly RealSceneFixtureCleanup _cleanup = new RealSceneFixtureCleanup();
    private readonly SortedDictionary<string, string> _notes = new SortedDictionary<string, string>();

    private void Note(string key, object value)
    {
        _notes[key] = value?.ToString() ?? "";
        File.WriteAllLines(Path.Combine(_output, "notes.txt"), _notes.Select(kv => kv.Key + " = " + kv.Value));
    }

    [UnityTest]
    public IEnumerator HammerAndCoreDrilling_ShouldLookLikeFieldwork()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_REALISM_CAPTURE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_REALISM_CAPTURE to opt into the field-tool realism capture.");
        Directory.CreateDirectory(_output);
        foreach (var file in Directory.GetFiles(_output, "*.png")) File.Delete(file);

        _cleanup.Capture();
        _viewSizeIndex = GameViewSelection.Index;
        SetViewSize(960, 540);
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
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        Call(Prop(T("LocalizationManager"), "Instance"), "SwitchLanguage", Enum.Parse(T("LanguageSettings+Language"), "Japanese"));
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        StopDialogue();
        Call(_director, "ReloadFlags");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        yield return Frames(3);

        // ---------------------------------------------------------------- hammer
        foreach (string id in new[] { "q.lab.intro", "q.lab.drkaede" }) CompleteQuest(id);
        Call(_quests, "StartQuest", "q.field.phase");
        Call(_quests, "CompleteObjective", "q.field.phase.enter_field");
        Set(_quests, "_fieldPhaseTargetIndex", 0);
        Call(_quests, "ActivateCurrentFieldTarget");
        Call(_quests, "TryBindSampleEvents");
        var guidance = Prop(T("GuidanceSystem.GuidanceManager"), "Instance");
        var site = (Component)Get(guidance, "activeTarget");
        Assert.IsNotNull(site, "Hammer site A is the active target.");
        Vector3 face = FieldSiteTestData.HammerFacePoints[0];
        PlaceAtHammerSite(0);
        Call(guidance, "RegisterPlayer", Player().transform);
        SelectTool("1002");
        var hammer = Tool("1002");
        yield return new WaitForSecondsRealtime(0.3f);
        Note("hammer.goggles_showing_at_0.3s", Prop(T("UISystem.SafetyGogglesOverlay"), "IsShowing"));
        yield return Capture("01-goggles-flash");
        yield return new WaitForSecondsRealtime(1.6f);
        var held = GameObject.Find("EquippedRockHammer");
        Note("hammer.model", held != null ? held.name + " local=" + held.transform.localPosition + " rot=" + held.transform.localEulerAngles : "(missing)");
        yield return Capture("02-hammer-in-hand");
        string variants = Environment.GetEnvironmentVariable("GEOMODEL_REALISM_HAMMER_VARIANTS");
        if (held != null && !string.IsNullOrEmpty(variants))
        {
            // Format: "px,py,pz,rx,ry,rz;..." to compare candidate in-hand poses from the same eye.
            Vector3 restP = held.transform.localPosition;
            Quaternion restR = held.transform.localRotation;
            int n = 0;
            foreach (string variant in variants.Split(';'))
            {
                var v = variant.Split(',').Select(float.Parse).ToArray();
                held.transform.localPosition = new Vector3(v[0], v[1], v[2]);
                held.transform.localRotation = Quaternion.Euler(v[3], v[4], v[5]);
                yield return Capture("02v" + (n++) + "-hammer-" + variant.Replace(",", "_"));
            }
            held.transform.localPosition = restP;
            held.transform.localRotation = restR;
        }
        // A slightly wider view of the hammer in hand, from the same eye.
        Camera.main.transform.position -= Camera.main.transform.forward * 0.4f;
        yield return Capture("02b-hammer-in-hand-wide");
        PlaceAtHammerSite(0);

        float hammerStart = Time.realtimeSinceStartup;
        for (int hit = 1; hit <= 3; hit++)
        {
            yield return MouseClick(0f);
            yield return Frames(4);
            yield return CaptureFrom("0" + (2 + hit) + "a-hit" + hit + "-debris", face, 0.75f, 0.25f);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return CaptureFrom("0" + (2 + hit) + "b-hit" + hit + "-marks", face, 0.6f, 0.15f);
            if (hit == 1) yield return Capture("03c-hit1-player-view");
        }
        var sample = (GameObject)Prop(hammer, "PendingSample");
        Assert.IsNotNull(sample, "Three hits break off a rock sample.");
        var drop = sample.GetComponent(T("SampleDropController"));
        float landed = -1f;
        while (Time.realtimeSinceStartup - hammerStart < 12f)
        {
            if (drop == null || (bool)Call(drop, "HasLanded")) { landed = Time.realtimeSinceStartup - hammerStart; break; }
            yield return null;
        }
        Note("hammer.seconds_first_click_to_landed", landed.ToString("F2"));
        var bounds = sample.GetComponent<Renderer>().bounds;
        Note("hammer.rock_size_m", bounds.size.ToString("F3"));
        yield return CaptureFrom("06-rock-on-ground", sample.transform.position, 0.7f, 0.45f);
        PlacePlayer(GroundAt(sample.transform.position) + new Vector3(0, 0.1f, -0.7f), sample.transform.position);
        yield return Frames(6);
        yield return Capture("07-rock-pickup-prompt");
        var inventory = Find("SampleInventory");
        int before = ((IList)Call(inventory, "GetAllSamples")).Count;
        float pickupStart = Time.realtimeSinceStartup;
        yield return Press(Key.E);
        Note("hammer.picked_up", ((IList)Call(inventory, "GetAllSamples")).Count == before + 1);
        Note("hammer.seconds_first_click_to_pickup_done", (Time.realtimeSinceStartup - hammerStart).ToString("F2"));
        yield return new WaitForSecondsRealtime(0.3f);
        Note("bag.title", Prop(T("UISystem.SampleBagOverlay"), "LastTitle"));
        Note("bag.detail", Prop(T("UISystem.SampleBagOverlay"), "LastDetail"));
        yield return Capture("08-sample-bag-label");
        CheckOverlaps("desktop.bag", "SampleBagCanvas/SampleBag");
        Note("hammer.pickup_blocking_seconds", (Time.realtimeSinceStartup - pickupStart).ToString("F2"));

        // ---------------------------------------------------------------- boring rig
        Vector3 drillSite = GroundAt((Vector3)T("FieldSiteLayout").GetField("DrillPosition", Flags).GetValue(null));
        PlacePlayer(drillSite + new Vector3(0, 0.15f, -2.6f), drillSite);
        SelectTool("1001");
        yield return Frames(8);
        var drill = Tool("1001");
        Note("rig.preview_valid", Prop(drill, "CanConfirmPlacement"));
        yield return Capture("10-rig-preview-player");
        yield return CaptureFrom("10b-rig-preview-overview", drillSite + new Vector3(1f, 1f, 0f), 6.5f, 3.2f);
        yield return MouseClick(0.15f);
        yield return Frames(6);
        var tower = (Component)Get(drill, "placedTower");
        Assert.IsNotNull(tower, "Primary click sets up the boring rig.");
        yield return new WaitForSecondsRealtime(0.4f);
        yield return CaptureFrom("11a-rig-unfolding", tower.transform.position + new Vector3(1f, 1.4f, 0f), 6.5f, 2.6f);
        yield return new WaitForSecondsRealtime(1.0f);
        yield return CaptureFrom("11b-rig-panorama", tower.transform.position + new Vector3(1f, 1.4f, 0f), 6.5f, 2.6f);
        yield return CaptureFrom("11c-rig-side", tower.transform.position + new Vector3(0.5f, 1.4f, 0f), 6f, 1.2f, 90f);
        yield return CaptureFrom("11d-rig-feet", tower.transform.position + Vector3.up * 0.2f, 2.6f, 0.6f, 30f);
        LogRig(tower);
        PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
        yield return Frames(6);
        yield return Capture("12-rig-closeup-player");

        // First drilling cycle, timed from the F press until the boxed core can be picked up.
        var cores = (IList)Get(tower, "collectedSamples");
        float drillStart = Time.realtimeSinceStartup;
        yield return Press(Key.F);
        Assert.IsTrue((bool)Get(tower, "isDrilling"), "F starts drilling.");
        yield return WaitUntilElapsed(drillStart, 1.2f);
        yield return Capture("13-drilling-player-gauge");
        yield return WaitUntilElapsed(drillStart, 2.6f);
        yield return CaptureFrom("14-drilling-rod-and-cuttings", tower.transform.position + Vector3.up * 0.9f, 3.2f, 1.2f, 20f);
        yield return WaitUntilElapsed(drillStart, 4.8f);
        yield return CaptureFrom("15-core-hoist", tower.transform.position + new Vector3(0.9f, 1.6f, 0f), 5f, 1.4f);
        yield return WaitUntilElapsed(drillStart, 5.55f);
        yield return CaptureFrom("16-core-into-box", tower.transform.position + new Vector3(1.4f, 0.6f, 0f), 4.5f, 2.2f);
        float pickable = -1f;
        while (Time.realtimeSinceStartup - drillStart < 20f)
        {
            if (cores.Count > 0 && cores[0] is GameObject core0 && core0 != null && ((Behaviour)core0.GetComponent(T("SampleCollector"))).enabled)
            {
                pickable = Time.realtimeSinceStartup - drillStart;
                break;
            }
            yield return null;
        }
        Note("rig.seconds_F_to_core_pickable", pickable.ToString("F2"));
        var box = (Component)Get(tower, "coreBox");
        Vector3 boxCenter = box != null ? box.transform.position : tower.transform.position;
        yield return CaptureFrom("17-core-in-box", boxCenter + Vector3.up * 0.1f, 2.4f, 2.0f, 15f);

        // Mobile portrait: second cycle with the depth gauge, then pick up its core for the bag label.
        var mobile = Prop(T("MobileInputManager"), "Instance");
        Call(mobile, "EnableDesktopTestMode", true);
        Call(mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        var controls = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        if (controls != null)
        {
            Set(controls, "forceShowOnDesktop", true);
            controls.gameObject.SetActive(true);
            if (Get(controls, "interactButton") == null) Call(controls, "StartOriginalLogic");
        }
        foreach (var size in new[] { new Vector2Int(390, 844), new Vector2Int(844, 390) })
        {
            string tag = size.x < size.y ? "portrait" : "landscape";
            SetViewSize(size.x, size.y);
            yield return Frames(8);
            int index = (int)Get(tower, "currentDrillCount");
            PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
            yield return Frames(4);
            Call(tower, "StartDrilling");
            yield return new WaitForSecondsRealtime(2f);
            Note("mobile." + tag + ".screen", Screen.width + "x" + Screen.height);
            yield return Capture("18-mobile-" + tag + "-gauge");
            CheckOverlaps("mobile." + tag + ".gauge", "DrillDepthGaugeCanvas/Panel");
            yield return WaitForDrill(tower);
            var core = cores.Cast<GameObject>().LastOrDefault(c => c != null);
            Assert.IsNotNull(core, "Cycle " + (index + 1) + " boxes a core.");
            PlacePlayer(GroundAt(core.transform.position) + new Vector3(0, 0.1f, -1.0f), core.transform.position);
            yield return Frames(6);
            var collector = core.GetComponent(T("SampleCollector"));
            Call(collector, "CollectSample");
            yield return new WaitForSecondsRealtime(0.35f);
            Note("mobile." + tag + ".bag", Prop(T("UISystem.SampleBagOverlay"), "LastTitle") + " / " + Prop(T("UISystem.SampleBagOverlay"), "LastDetail"));
            yield return Capture("19-mobile-" + tag + "-bag-label");
            CheckOverlaps("mobile." + tag + ".bag", "SampleBagCanvas/SampleBag");
        }
        Call(mobile, "EnableDesktopTestMode", false);
        Call(mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        if (controls != null) { Set(controls, "forceShowOnDesktop", false); Call(controls, "SetVirtualControlsVisible", false); }
        SetViewSize(960, 540);
        yield return Frames(6);

        while ((bool)Call(tower, "CanDrill"))
        {
            PlacePlayer(tower.transform.position + new Vector3(0, 0.1f, -2.3f), tower.transform.position + Vector3.up * 1.5f);
            Call(tower, "StartDrilling");
            yield return WaitForDrill(tower);
        }
        Note("rig.drill_count", Get(tower, "currentDrillCount"));
        yield return CaptureFrom("20-core-box-after-five", boxCenter + Vector3.up * 0.1f, 2.4f, 2.2f, 10f);
        yield return CaptureFrom("20b-core-box-labels", boxCenter + Vector3.up * 0.1f, 1.6f, 1.4f, 70f);
        yield return CaptureFrom("21-rig-after-five", tower.transform.position + new Vector3(1f, 1.4f, 0f), 6.5f, 2.6f);

        // ---------------------------------------------------------------- lab
        yield return SwitchScene("Laboratory Scene");
        yield return new WaitForSecondsRealtime(2f);
        StopDialogue();
        var labInventory = Find("SampleInventory");
        var all = ((IList)Call(labInventory, "GetAllSamples")).Cast<object>().ToList();
        Note("lab.samples", string.Join(", ", all.Select(s => Get(s, "sourceToolID") + ":" + Get(s, "displayName"))));
        var rock = all.FirstOrDefault(s => (string)Get(s, "sourceToolID") == "1002");
        Assert.IsNotNull(rock, "The hammer sample reaches the lab inventory.");
        var icon = (Sprite)Get(rock, "previewIcon");
        if (icon == null) { icon = (Sprite)Call(Prop(T("SampleIconGenerator"), "Instance"), "GenerateIconForSample", rock); }
        Note("lab.rock_icon", icon != null ? icon.texture.width + "x" + icon.texture.height : "(none)");
        if (icon != null) SaveSprite(icon, "22a-rock-icon.png");
        var inventoryUI = Find("InventoryUI");
        if (inventoryUI != null)
        {
            Call(inventoryUI, "OpenInventory");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Capture("22-lab-inventory-icons");
            Call(inventoryUI, "CloseInventory");
            yield return Frames(4);
        }
        yield return CaptureLabCutting(rock);
    }

    private IEnumerator CaptureLabCutting(object rock)
    {
        var viewer = (Component)UnityEngine.Object.FindFirstObjectByType(T("SampleCuttingSystem.Sample3DModelViewer"), FindObjectsInactive.Include);
        Note("lab.viewer", viewer != null ? viewer.name : "(none)");
        var stations = UnityEngine.Object.FindObjectsByType(T("SampleCuttingSystem.CuttingStationInteraction"), FindObjectsInactive.Include, FindObjectsSortMode.None);
        Note("lab.cutting_stations", stations.Length);
        if (stations.Length == 0) yield break;
        var station = (Component)stations[0];
        PlacePlayer(GroundAt(station.transform.position + station.transform.forward * -1.4f) + Vector3.up * 0.05f, station.transform.position + Vector3.up * 0.9f);
        yield return Frames(6);
        yield return Capture("23-lab-cutting-station");
        var open = station.GetType().GetMethods(Flags).FirstOrDefault(m => m.GetParameters().Length == 0 &&
            (m.Name == "OpenCuttingInterface" || m.Name == "StartInteraction" || m.Name == "OpenCuttingUI" || m.Name == "ShowCuttingUI"));
        Note("lab.cutting_open_method", open != null ? open.Name : "(none)");
        if (open == null) yield break;
        open.Invoke(station, null);
        yield return new WaitForSecondsRealtime(1.2f);
        yield return Capture("24-lab-cutting-ui");
        var handlers = UnityEngine.Object.FindObjectsByType(T("SampleCuttingSystem.SampleDragHandler"), FindObjectsSortMode.None).Cast<Component>().ToArray();
        var zone = (Component)UnityEngine.Object.FindFirstObjectByType(T("SampleCuttingSystem.SampleDropZone"));
        Note("lab.drag_handlers", handlers.Length + " zone=" + (zone != null));
        var rockHandler = handlers.FirstOrDefault(h =>
        {
            var data = Call(h, "GetSampleData");
            return data != null && Get(data, "name")?.ToString() == Get(rock, "displayName")?.ToString();
        }) ?? handlers.FirstOrDefault();
        if (zone != null && rockHandler != null)
        {
            Call(zone, "AcceptSample", rockHandler);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("25-lab-rock-on-cutter");
        }
        if (viewer != null)
        {
            Call(viewer, "ShowRealSampleModel", rock);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("26-lab-rock-3d-viewer");
        }
    }

    // ---------------------------------------------------------------- helpers
    private void LogRig(Component tower)
    {
        var root = tower.transform.Find("RigVisual");
        if (root == null) { Note("rig.visual", "(missing)"); return; }
        var lines = new List<string>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = string.Join("|", r.sharedMaterials.Select(m => m == null ? "null" : m.name + "@" + (m.shader != null ? m.shader.name : "noshader")));
            lines.Add(r.name + " " + r.bounds.center.ToString("F2") + " size=" + r.bounds.size.ToString("F2") + " " + mats);
        }
        for (int i = 0; i < 3; i++)
        {
            var leg = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RigLeg" + i);
            if (leg == null) continue;
            var legRenderer = leg.GetComponentInChildren<Renderer>();
            float foot = legRenderer.bounds.min.y;
            Vector3 probe = legRenderer.bounds.center; probe.y = foot;
            float ground = GroundAt(probe).y;
            lines.Add("FOOT RigLeg" + i + " bottom=" + foot.ToString("F3") + " ground=" + ground.ToString("F3") + " gap=" + (foot - ground).ToString("F3"));
        }
        File.WriteAllLines(Path.Combine(_output, "rig-parts.txt"), lines);
    }

    private void CheckOverlaps(string key, string path)
    {
        var go = GameObject.Find(path);
        if (go == null || !go.activeInHierarchy) { Note(key + ".overlaps", "(not visible)"); return; }
        Rect mine = ScreenRect(go.GetComponent<RectTransform>());
        var hits = new List<string>();
        foreach (var graphic in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
        {
            if (!graphic.isActiveAndEnabled || graphic.transform.IsChildOf(go.transform) || go.transform.IsChildOf(graphic.transform)) continue;
            var canvas = graphic.canvas;
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (!(graphic is Text || graphic is Image img && (img.raycastTarget || graphic.GetComponent<Button>() != null))) continue;
            if (graphic is Text text && string.IsNullOrWhiteSpace(text.text)) continue;
            if (graphic.color.a < 0.05f) continue;
            Rect other = ScreenRect(graphic.rectTransform);
            if (other.width >= Screen.width * 0.9f && other.height >= Screen.height * 0.9f) continue;
            if (mine.Overlaps(other)) hits.Add(Path_(graphic.transform));
        }
        Note(key + ".rect", mine);
        Note(key + ".overlaps", hits.Count == 0 ? "none" : string.Join("; ", hits.Distinct().Take(12)));
    }

    private static string Path_(Transform t) => t.parent == null ? t.name : Path_(t.parent) + "/" + t.name;

    private void SaveSprite(Sprite sprite, string file)
    {
        var tex = sprite.texture;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(Path.Combine(_output, file), copy.EncodeToPNG());
        UnityEngine.Object.Destroy(copy);
    }

    private static object Get(object target, string field)
    {
        for (var type = target as Type ?? target.GetType(); type != null; type = type.BaseType)
        {
            var f = type.GetField(field, Flags | BindingFlags.DeclaredOnly);
            if (f != null) return f.GetValue(target is Type ? null : target);
            var p = type.GetProperty(field, Flags | BindingFlags.DeclaredOnly);
            if (p != null) return p.GetValue(target is Type ? null : target);
        }
        throw new MissingFieldException(field);
    }
    private static void Set(object target, string field, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var f = type.GetField(field, Flags | BindingFlags.DeclaredOnly);
            if (f != null) { f.SetValue(target, value); return; }
        }
        throw new MissingFieldException(field);
    }
    private static object Prop(object target, string name) => Get(target, name);
    private static object Call(object target, string name, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        var method = type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
            m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
        return method.Invoke(target is Type ? null : target, args);
    }
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));
    private Component Player() => Find("FirstPersonController");
    private Component Tool(string id) => (Component)((Array)Get(Find("ToolManager"), "availableTools")).Cast<object>()
        .First(t => t != null && (string)Get(t, "toolID") == id);

    private void CompleteQuest(string id)
    {
        Call(_quests, "StartQuest", id);
        var quest = Call(_quests, "GetQuest", id);
        foreach (var objective in (IEnumerable)Get(quest, "objectives")) Call(_quests, "CompleteObjective", (string)Get(objective, "id"));
    }

    private void StopDialogue()
    {
        Call(_director, "CancelPlayback");
        Call(_quests, "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
    }

    private IEnumerator SwitchScene(string scene)
    {
        var manager = Prop(T("GameSceneManager"), "Instance");
        Call(manager, "SwitchToScene", scene);
        float start = Time.realtimeSinceStartup;
        while ((SceneManager.GetActiveScene().name != scene || (bool)Prop(T("GameSceneManager"), "IsLoadingScene")) &&
               Time.realtimeSinceStartup - start < 40f) yield return null;
        Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        _quests = Prop(T("QuestSystem.QuestManager"), "Instance");
    }

    private void SelectTool(string id)
    {
        var wheel = Find("InventoryUISystem");
        Call(wheel, "InitializeTools");
        var tools = (IList)Get(wheel, "availableTools");
        int index = Enumerable.Range(0, tools.Count).First(i => (string)Get(tools[i], "toolID") == id);
        Call(wheel, "OpenWheel");
        Call(wheel, "SetSelectedSlot", index);
        var slots = (RectTransform[])Get(wheel, "wheelSlots");
        Assert.IsTrue((bool)Call(wheel, "TrySelectToolAtScreenPoint", ScreenRect(slots[index]).center, "鼠标"));
    }

    private void PlaceAtHammerSite(int index)
    {
        var sceneTargets = FieldSiteTestData.GetHammerTargetsFromMainScene();
        Vector3 foot = sceneTargets[index].position;
        Vector3 face = FieldSiteTestData.HammerFacePoints[index];
        PlacePlayer(foot + Vector3.up * 0.08f, face);
        Vector3 eye = foot + Vector3.up * 1.08f;
        Camera.main.transform.position = eye + (face - eye).normalized * 0.065f;
        Camera.main.transform.LookAt(face);
    }

    private Vector3 GroundAt(Vector3 site)
    {
        foreach (var hit in Physics.RaycastAll(site + Vector3.up * 50f, Vector3.down, 100f).OrderBy(h => h.distance))
            if (!hit.collider.isTrigger && (hit.collider.GetComponent(T("GeologyLayer")) != null ||
                hit.collider.gameObject.name.ToLower().Contains("terrain") || hit.collider.gameObject.name.ToLower().Contains("floor") ||
                hit.collider.gameObject.name.ToLower().Contains("ground"))) return hit.point;
        return site;
    }

    private void PlacePlayer(Vector3 feet, Vector3 lookAt)
    {
        var player = Player();
        ((Behaviour)player).enabled = false;
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

    private IEnumerator MouseClick(float wait)
    {
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f), buttons = 1 });
        yield return null;
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
    }

    private IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return Frames(3);
    }

    private static IEnumerator WaitForDrill(Component tower)
    {
        float deadline = Time.realtimeSinceStartup + 20f;
        while (tower != null && (bool)Get(tower, "isDrilling") && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
    }

    private static IEnumerator WaitUntilElapsed(float start, float seconds)
    {
        while (Time.realtimeSinceStartup - start < seconds) yield return null;
    }

    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }

    private void SetViewSize(int width, int height)
    {
        typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
            .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { width, height });
    }

    // ---------------------------------------------------------------- capture
    private IEnumerator Capture(string name) => Render(Camera.main, Path.Combine(_output, name + ".png"), true);

    /// <summary>Third-person view of a point from a helper camera; the player's camera and HUD are untouched.</summary>
    private IEnumerator CaptureFrom(string name, Vector3 focus, float distance, float height, float yawOffset = 0f)
    {
        var main = Camera.main;
        Vector3 back = Vector3.ProjectOnPlane(-main.transform.forward, Vector3.up);
        if (back.sqrMagnitude < 0.001f) back = Vector3.back;
        back = Quaternion.Euler(0f, yawOffset, 0f) * back.normalized;
        var go = new GameObject("RealismCaptureCamera");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(main);
        camera.nearClipPlane = 0.02f;
        camera.fieldOfView = 55f;
        go.transform.position = focus + back * distance + Vector3.up * height;
        go.transform.LookAt(focus);
        camera.enabled = false;
        yield return Render(camera, Path.Combine(_output, name + ".png"), false);
        UnityEngine.Object.Destroy(go);
    }

    private IEnumerator Render(Camera camera, string path, bool withOverlays)
    {
        yield return null;
        int width = Screen.width, height = Screen.height;
        var canvases = withOverlays
            ? UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
                .OrderBy(c => c.sortingOrder).ToArray()
            : new Canvas[0];
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
        if (string.IsNullOrEmpty(_output)) yield break;
        var input = Prop(T("MobileInputManager"), "Instance");
        if (input != null)
        {
            Call(input, "EnableDesktopTestMode", false);
            Call(input, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        }
        if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        InputSystem.settings.updateMode = _inputMode;
        InputSystem.settings.backgroundBehavior = _background;
        if (_viewSizeIndex >= 0) GameViewSelection.Index = _viewSizeIndex;
        Call(T("ProgressResetService"), "ResetAll");
        // Scene singletons kept alive by DontDestroyOnLoad (input, quests, tools) must not leak into later tests.
        yield return _cleanup.Restore();
    }
}
