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

public class ToolWheelSceneTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => (target as Type ?? target.GetType()).GetField(field, Flags).GetValue(target is Type ? null : target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Prop(object target, string name) => (target as Type ?? target.GetType()).GetProperty(name, Flags).GetValue(target is Type ? null : target);
    private static object Call(object target, string name, params object[] args) =>
        (target as Type ?? target.GetType()).GetMethod(name, Flags).Invoke(target is Type ? null : target, args);
    private static Component Find(string name) => (Component)UnityEngine.Object.FindFirstObjectByType(T(name));
    private object _wheel, _mobile, _director;
    private Keyboard _keyboard;
    private Mouse _mouse;
    private Touchscreen _touchscreen;
    private InputSettings.UpdateMode _updateMode;
    private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
#endif
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private string _output;
    private readonly List<string> _checks = new List<string>();
    private readonly List<string> _geometry = new List<string> { "context\tsector\tgraphic\tseparator_margin_px\tdead_zone_clearance_px\touter_clearance_px" };
    private readonly List<GameObject> _fixtures = new List<GameObject>();

    private void Check(bool condition, string message)
    {
        Assert.IsTrue(condition, message);
        _checks.Add("PASS " + message);
        File.WriteAllLines(Path.Combine(_output, "checks.txt"), _checks);
    }

    [UnityTest]
    public IEnumerator ActualWheel_ShouldSelectEqualSectorsWithMouseAndTouchAndCaptureLayouts()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_TOOL_WHEEL_CAPTURE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_TOOL_WHEEL_CAPTURE to capture the actual wheel.");
        Directory.CreateDirectory(_output);
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
        _touchscreen = InputSystem.AddDevice<Touchscreen>();
        Call(T("ProgressResetService"), "ResetAll");
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro");
        PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        Call(_director, "CancelPlayback");
        Call(Prop(T("QuestSystem.QuestManager"), "Instance"), "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Time.timeScale = 1f;
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        Set(quests, "autoStartIntroQuestIfNone", false);
        Call(quests, "ResetProgressForNewGame");
        Call(_director, "CancelPlayback");
        Call(_director, "ReloadFlags");
        Call(quests, "CancelPendingPlayback");
        Call(T("Core.GameInputState"), "ReleaseAll");
        _wheel = Find("InventoryUISystem");
        _mobile = Prop(T("MobileInputManager"), "Instance");
        Call(_mobile, "EnableDesktopTestMode", false);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        var persistent = Find("PlayerPersistentData");
        foreach (string id in new[] { "999", "1002", "1001" }) Call(persistent, "MarkToolUnlocked", id);
        Call(persistent, "ApplyUnlockedToolsToScene");
        Call(_wheel, "InitializeTools");
        var tools = Find("ToolManager");
        var teaching = ((IList)Get(_wheel, "availableTools")).Cast<object>().ToArray();
        Check(teaching.Select(t => (string)Get(t, "toolID")).SequenceEqual(new[] { "0", "999", "1002", "1001" }), "Four teaching tools retain their order.");
        SwitchLanguage("Japanese");
        yield return Frames(4);
        MoveMouse(Center());
        Check(!(bool)Prop(T("StorySystem.StoryDirector"), "IsStoryPlaybackActive") && !(bool)Prop(T("Core.GameInputState"), "IsModalOpen"), "Gameplay input is unblocked before real Tab.");
        yield return Tap(Key.Tab);
        yield return Frames(4);
        Check((bool)Prop(_wheel, "IsWheelOpen"), "Real Tab opens the wheel.");
        Check((int)Get(_wheel, "selectedSlot") == -1, "Mouse in the circular dead zone selects nothing.");
        Check(((Graphic[])Get(_wheel, "sectorGraphics")).Length == 4 && ((RectTransform[])Get(_wheel, "wheelSlots")).All(r => r.GetComponent<Image>() == null),
            "Exactly four sectors are rendered, with no square or empty slots.");
        Check((string)Prop(T("UISystem.CollectionGuidanceHUD"), "RecommendedToolId") == null, "Neutral capture has no recommendation.");
        VerifyNamesAndSize("Japanese 960x540");
        DumpWheel();
        yield return Capture("01-desktop-960x540-neutral");
        var graphics = (Graphic[])Get(_wheel, "sectorGraphics");
        for (int i = 0; i < 4; i++)
        {
            MoveMouse(Point(i));
            yield return Frames(3);
            Check((int)Get(_wheel, "selectedSlot") == i && graphics[i].color == (Color)Get(_wheel, "selectedSlotBackgroundColor"),
                "Real mouse hover highlights full sector " + i + ".");
            Check(((Image[])Get(_wheel, "slotImages"))[i].color == (Color)Get(_wheel, "selectedColor") &&
                ((Text[])Get(_wheel, "slotTexts"))[i].fontStyle == FontStyle.Bold, "Icon and name highlight together for sector " + i + ".");
        }
        MoveMouse(Point(2));
        yield return Frames(3);
        yield return Capture("02-desktop-960x540-hover-hammer");
        MoveMouse(OutsidePoint(2));
        yield return Frames(3);
        Check((int)Get(_wheel, "selectedSlot") == 2 && graphics[2].color == (Color)Get(_wheel, "selectedSlotBackgroundColor"),
            "Mouse outside the circle still highlights the hammer sector by direction.");
        yield return Capture("02b-desktop-960x540-outside-hover-hammer");
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = OutsidePoint(2), buttons = 1 });
        yield return Frames(2);
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = OutsidePoint(2) });
        yield return Frames(2);
        Check(!(bool)Prop(_wheel, "IsWheelOpen") && (string)Get(Call(tools, "GetCurrentTool"), "toolID") == "1002",
            "Clicking outside the circle in the hammer direction equips the actual hammer and closes the wheel.");
        yield return Tap(Key.Tab);
        MoveMouse(Center());
        yield return Frames(3);
        var markers = (Image[])Get(_wheel, "equippedMarkers");
        Check(markers[2].gameObject.activeSelf && markers.Where((m, i) => i != 2).All(m => !m.gameObject.activeSelf),
            "Only the equipped hammer has a small static marker.");
        yield return Tap(Key.Tab);

        // 实际任务的推荐状态，而非手工改颜色。
        Call(_wheel, "SelectToolAndStartPreview", 0);
        Call(quests, "StartQuest", "q.field.phase");
        Call(quests, "CompleteObjective", "q.field.phase.enter_field");
        Set(quests, "_fieldPhaseTargetIndex", 0);
        Call(quests, "ActivateCurrentFieldTarget");
        var target = UnityEngine.Object.FindObjectsByType(T("GuidanceSystem.GuidanceTarget"), FindObjectsSortMode.None)
            .Cast<Component>().First(t => (string)Get(t, "targetId") == "chapter3.field.sample_site_a");
        var player = Find("FirstPersonController");
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = target.transform.position + Vector3.up * 1.5f;
        controller.enabled = true;
        Call(Prop(T("GuidanceSystem.GuidanceManager"), "Instance"), "RegisterPlayer", player.transform);
        yield return Frames(4);
        yield return Tap(Key.Tab);
        MoveMouse(Center());
        yield return Frames(4);
        Check((string)Prop(T("UISystem.CollectionGuidanceHUD"), "RecommendedToolId") == "1002", "The actual hammer quest recommends the hammer sector: " +
            Prop(T("UISystem.CollectionGuidanceHUD"), "RecommendedToolId") + ", quest=" + Call(quests, "GetQuestStatus", "q.field.phase"));
        graphics = (Graphic[])Get(_wheel, "sectorGraphics");
        Color first = graphics[2].color;
        yield return new WaitForSecondsRealtime(0.3f);
        Check(graphics[2].color != first && graphics[2].color != (Color)Get(_wheel, "slotBackgroundColor"), "Recommended sector brightness breathes over time.");
        Check(graphics[0].color == (Color)Get(_wheel, "slotBackgroundColor") && ((Image[])Get(_wheel, "equippedMarkers"))[0].gameObject.activeSelf,
            "Equipped marker remains distinct from the hammer recommendation.");
        yield return Capture("03-desktop-960x540-recommended-hammer");
        Call(_wheel, "CloseWheel", false);

        foreach (string language in new[] { "English", "ChineseSimplified" })
        {
            SwitchLanguage(language);
            yield return Tap(Key.Tab);
            MoveMouse(Center());
            yield return Frames(4);
            VerifyNamesAndSize(language + " 960x540");
            yield return Capture(language == "English" ? "04-desktop-960x540-english" : "05-desktop-960x540-chinese");
            Call(_wheel, "CloseWheel", false);
        }
        yield return VerifyGeometryMatrix();
        SwitchLanguage("Japanese");
        Call(_mobile, "EnableDesktopTestMode", true);
        Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        var controls = (Component)UnityEngine.Object.FindFirstObjectByType(T("MobileControlsUI"), FindObjectsInactive.Include);
        Set(controls, "forceShowOnDesktop", true);
        controls.gameObject.SetActive(true);
        if (Get(controls, "interactButton") == null) Call(controls, "StartOriginalLogic");
        foreach (var size in new[] { new Vector2Int(844, 390), new Vector2Int(1024, 768) })
        {
            SetSize(size.x, size.y);
            yield return Frames(5);
            yield return OpenTouchWheel(controls);
            Check((bool)Prop(_wheel, "IsWheelOpen"), "Real mobile Tools button opens wheel at " + size + ".");
            VerifyNamesAndSize("Touch " + size);
            yield return Capture("06-touch-" + size.x + "x" + size.y);
            Touch(UnityEngine.InputSystem.TouchPhase.Began, Center());
            yield return Frames(2);
            Touch(UnityEngine.InputSystem.TouchPhase.Moved, Point(3));
            yield return Frames(3);
            Check((int)Get(_wheel, "selectedSlot") == 3, "Held touch drag highlights the tower sector.");
            Touch(UnityEngine.InputSystem.TouchPhase.Moved, OutsidePoint(3));
            yield return Frames(3);
            Check((int)Get(_wheel, "selectedSlot") == 3, "Held touch outside the circle still highlights the tower sector by direction.");
            Touch(UnityEngine.InputSystem.TouchPhase.Ended, OutsidePoint(3));
            yield return Frames(3);
            Check(!(bool)Prop(_wheel, "IsWheelOpen") && (string)Get(Call(tools, "GetCurrentTool"), "toolID") == "1001",
                "Releasing touch outside the circle in the tower direction equips the tower.");
            yield return OpenTouchWheel(controls);
            Touch(UnityEngine.InputSystem.TouchPhase.Began, Point(2));
            yield return Frames(2);
            // 松手事件本身到达死区，也必须覆盖上一帧的有效悬停。
            Touch(UnityEngine.InputSystem.TouchPhase.Ended, Center());
            yield return Frames(3);
            Check((bool)Prop(_wheel, "IsWheelOpen") && (int)Get(_wheel, "selectedSlot") == -1 &&
                (string)Get(Call(tools, "GetCurrentTool"), "toolID") == "1001", "Releasing touch in the dead zone preserves equipped tower.");
            Call(_wheel, "CloseWheel", false);
        }

        // 将来增加道具时，创建数量和命中规则也保持一致。
        SetSize(960, 540);
        yield return Frames(5);
        Call(_wheel, "OpenWheel");
        var list = (IList)Get(_wheel, "availableTools");
        var extraHost = new GameObject("FutureWheelTools");
        _fixtures.Add(extraHost);
        var extras = Enumerable.Range(0, 4).Select(i =>
        {
            var tool = extraHost.AddComponent(T("EmptyHandTool"));
            Set(tool, "toolID", (2000 + i).ToString());
            return tool;
        }).ToArray();
        for (int count = 1; count <= 8; count++)
        {
            list.Clear();
            foreach (var tool in teaching.Concat(extras).Take(count)) list.Add(tool);
            Call(_wheel, "UpdateWheelDisplay");
            yield return Frames(2);
            Check(((Graphic[])Get(_wheel, "sectorGraphics")).Length == count &&
                ((Image[])Get(_wheel, "slotSeparators")).Length == (count == 1 ? 0 : count), "N=" + count + " renders exactly N equal sectors and correct dividers.");
            for (int i = 0; i < count; i++)
                Check((int)Call(_wheel, "GetWheelSlotAtScreenPoint", Point(i)) == i, "N=" + count + " sector center " + i + " matches angular selection.");
        }
        yield return Capture("07-desktop-eight-sectors");
        Call(_wheel, "CloseWheel", false);
    }

    private IEnumerator OpenTouchWheel(object controls)
    {
        var button = (Button)Get(controls, "toolWheelButton");
        var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        Touch(UnityEngine.InputSystem.TouchPhase.Began, point);
        yield return Frames(2);
        Touch(UnityEngine.InputSystem.TouchPhase.Ended, point);
        yield return Frames(4);
    }

    private IEnumerator VerifyGeometryMatrix()
    {
        foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(844, 390), new Vector2Int(1024, 768) })
        {
            SetSize(size.x, size.y);
            yield return Frames(5);
            Call(_wheel, "OpenWheel");
            foreach (string language in new[] { "Japanese", "ChineseSimplified", "English" })
            {
                SwitchLanguage(language);
                MoveMouse(Center());
                yield return Frames(3);
                string context = size.x + "x" + size.y + " " + language;
                VerifyNamesAndSize(context);
                VerifyContentGeometry(context + " normal");
                for (int i = 0; i < 4; i++)
                {
                    MoveMouse(Point(i));
                    yield return Frames(2);
                    VerifyContentGeometry(context + " hover " + i);
                }
            }
            Call(_wheel, "CloseWheel", false);
        }
        SetSize(960, 540);
        yield return Frames(5);
    }

    private void VerifyContentGeometry(string context)
    {
        Canvas.ForceUpdateCanvases();
        var wheel = (RectTransform)((GameObject)Get(_wheel, "wheelUI")).transform;
        float scale = wheel.GetComponentInParent<Canvas>().scaleFactor;
        float inner = (float)Get(_wheel, "selectionRadius");
        float outer = wheel.rect.width * 0.5f;
        var names = (Text[])Get(_wheel, "slotTexts");
        var icons = (Image[])Get(_wheel, "slotImages");
        for (int i = 0; i < 4; i++)
        {
            foreach (var graphic in new Graphic[] { names[i], icons[i] })
            {
                // 使用最终字形/图标网格，排除 Text RectTransform 的空白区域。
                var mesh = graphic.canvasRenderer.GetMesh();
                Assert.IsNotNull(mesh, context + ": rendered mesh exists for " + graphic.name);
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                var points = new List<Vector2>();
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                    if (Vector3.Cross(b - a, c - a).sqrMagnitude < 0.000001f) continue;
                    foreach (var vertex in new[] { a, b, c })
                        points.Add(wheel.InverseTransformPoint(graphic.transform.TransformPoint(vertex)));
                }
                Check(points.Count > 0, context + ": visible geometry exists for sector " + i + " " + graphic.name);
                if (graphic is Text text)
                {
                    Check(points.Count / 6 >= text.text.Count(c => !char.IsWhiteSpace(c)), context + ": every character renders in " + text.text);
                    Check(text.cachedTextGenerator.lineCount <= 2, context + ": tool name uses at most two lines: " + text.text);
                }
                float xMin = points.Min(p => p.x), xMax = points.Max(p => p.x);
                float yMin = points.Min(p => p.y), yMax = points.Max(p => p.y);
                var corners = new[] { new Vector2(xMin, yMin), new Vector2(xMin, yMax), new Vector2(xMax, yMin), new Vector2(xMax, yMax) };
                float angle = i * Mathf.PI * 0.5f;
                var radial = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                var tangent = new Vector2(radial.y, -radial.x);
                float margin = (corners.Min(p => (Vector2.Dot(p, radial) - Mathf.Abs(Vector2.Dot(p, tangent))) / Mathf.Sqrt(2f)) -
                    (float)Get(_wheel, "separatorWidth") * 0.5f) * scale;
                float deadClearance = new Vector2(Mathf.Clamp(0f, xMin, xMax), Mathf.Clamp(0f, yMin, yMax)).magnitude - inner;
                float outerClearance = outer - corners.Max(p => p.magnitude);
                string item = context + " sector " + i + " " + graphic.name;
                _geometry.Add(context + "\t" + i + "\t" + graphic.name + "\t" + margin.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    "\t" + (deadClearance * scale).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    "\t" + (outerClearance * scale).ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                File.WriteAllLines(Path.Combine(_output, "geometry.tsv"), _geometry);
                Check(margin >= 6f, item + ": actual rendered bounds clear both dividers by >=6px (" + margin + ").");
                Check(deadClearance > 0f && outerClearance > 0f, item + ": rendered bounds stay outside the dead zone and inside the rim.");
            }
        }
    }

    private void DumpWheel()
    {
        var lines = new List<string>();
        foreach (var g in ((GameObject)Get(_wheel, "wheelUI")).GetComponentsInChildren<Graphic>())
        {
            var mesh = g.canvasRenderer.GetMesh();
            lines.Add(g.name + ": rect=" + g.rectTransform.rect + ", color=" + g.color + ", cull=" + g.canvasRenderer.cull + ", mesh=" + (mesh == null ? 0 : mesh.vertexCount) +
                (g.GetType().Name == "WheelSectorGraphic" ? ", outer=" + Get(g, "outerRadius") + ", span=" + Get(g, "span") : ""));

        }
        File.WriteAllLines(Path.Combine(_output, "mesh-diagnostic.txt"), lines);
    }

    private void VerifyNamesAndSize(string context)
    {
        var rect = (RectTransform)((GameObject)Get(_wheel, "wheelUI")).transform;
        var canvas = rect.GetComponentInParent<Canvas>();
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var safe = Screen.safeArea;
        Check(corners.All(p => p.x >= safe.xMin - 1f && p.x <= safe.xMax + 1f && p.y >= safe.yMin - 1f && p.y <= safe.yMax + 1f), context + ": whole wheel fits the safe area.");
        Check(rect.rect.width * canvas.scaleFactor <= Mathf.Min(safe.width, safe.height) * 0.86f + 1f && rect.rect.width <= 921f,
            context + ": wheel respects 86% short-side and 920-unit limits.");
        foreach (var text in (Text[])Get(_wheel, "slotTexts"))
        {
            Check(text.fontSize * canvas.scaleFactor >= 18f && !text.resizeTextForBestFit, context + ": label is at least 18px: " + text.text);
            Check(text.preferredHeight <= text.rectTransform.rect.height + 1f && !string.IsNullOrEmpty(text.text), context + ": complete tool name fits: " + text.text);
        }
    }

    private Vector2 Center() => RectTransformUtility.WorldToScreenPoint(null, ((GameObject)Get(_wheel, "wheelUI")).transform.position);
    private Vector2 Point(int index)
    {
        var rect = ((RectTransform[])Get(_wheel, "wheelSlots"))[index];
        return RectTransformUtility.WorldToScreenPoint(null, rect.position);
    }
    private Vector2 OutsidePoint(int index)
    {
        var rect = (RectTransform)((GameObject)Get(_wheel, "wheelUI")).transform;
        float radius = rect.rect.width * rect.GetComponentInParent<Canvas>().scaleFactor * 0.5f;
        var point = Center() + (Point(index) - Center()).normalized * (radius + 12f);
        Check(Vector2.Distance(point, Center()) > radius, "Input position is beyond the wheel rim.");
        return point;
    }
    private void MoveMouse(Vector2 point) => InputSystem.QueueStateEvent(_mouse, new MouseState { position = point });
    private void Touch(UnityEngine.InputSystem.TouchPhase phase, Vector2 position) =>
        InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = 1, phase = phase, position = position });
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
        if (_wheel != null) Call(_wheel, "CloseWheel", false);
        if (_mobile != null)
        {
            Call(_mobile, "EnableDesktopTestMode", false);
            Call(_mobile, "SwitchInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        }
        foreach (var fixture in _fixtures) UnityEngine.Object.Destroy(fixture);
        if (_backend != null) Set(_backend, "enableBackend", _backendEnabled);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        if (_touchscreen != null) InputSystem.RemoveDevice(_touchscreen);
        InputSystem.settings.updateMode = _updateMode;
        InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
#endif
        SwitchLanguage("Japanese");
        SetSize(960, 540);
        yield return null;
    }
}
