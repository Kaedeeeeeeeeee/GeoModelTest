using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class QuestAttentionTests
{
    private GameObject _host;
    private Component _effect;
    private RectTransform _card;
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    internal static object Get(object o, string field) => o.GetType().GetField(field, Flags).GetValue(o);
    internal static void Set(object o, string field, object value) => o.GetType().GetField(field, Flags).SetValue(o, value);
    internal static object Prop(object o, string property) => (o as Type ?? o.GetType()).GetProperty(property, Flags).GetValue(o is Type ? null : o);
    internal static object Call(object o, string method, params object[] args) => (o as Type ?? o.GetType()).GetMethod(method, Flags).Invoke(o is Type ? null : o, args);

    [SetUp]
    public void Setup()
    {
        _host = new GameObject("AttentionTest", typeof(RectTransform), typeof(Canvas));
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(_host.transform, false);
        _card = card.GetComponent<RectTransform>();
        _card.sizeDelta = new Vector2(540, 142);
        _effect = _host.AddComponent(T("UISystem.QuestAttentionEffect"));
        Call(_effect, "Initialize", _card);
    }

    [Test]
    public void Cue_ShouldWaitForGameplayAndMergeRapidTaskChanges()
    {
        Call(_effect, "NotifyTaskChanged", false);
        Call(_effect, "Tick", 10f, false);
        Assert.IsFalse((bool)Prop(_effect, "IsAnimating"));
        Assert.AreEqual(0, Prop(_effect, "NotificationCount"));
        Call(_effect, "NotifyTaskChanged", false);
        Call(_effect, "Tick", 0.3f, true);
        Assert.IsTrue((bool)Prop(_effect, "IsAnimating"));
        Assert.AreEqual(1, Prop(_effect, "NotificationCount"));
        Assert.IsTrue(_card.Find("NewTaskCue").gameObject.activeSelf);
        Assert.Greater(_card.localScale.x, 1);
        foreach (var graphic in _card.Find("NewTaskCue").GetComponentsInChildren<Graphic>())
            Assert.IsFalse(graphic.raycastTarget, "The cue must not intercept gameplay clicks.");
        Call(_effect, "Tick", 4f, true);
        Assert.IsFalse((bool)Prop(_effect, "IsAnimating"));
        Assert.AreEqual(Vector3.one, _card.localScale);
        Assert.IsFalse(_card.Find("NewTaskCue").gameObject.activeSelf);
        Assert.IsFalse(_card.Find("TaskAttentionRing").gameObject.activeSelf);
    }

    [Test]
    public void Cue_ShouldResumeAfterModalInterruptsIt()
    {
        Call(_effect, "NotifyTaskChanged", false);
        Call(_effect, "Tick", 0.4f, true);
        Call(_effect, "Tick", 8f, false);
        Assert.IsFalse(_card.Find("NewTaskCue").gameObject.activeSelf);
        Call(_effect, "Tick", 0.3f, true);
        Assert.IsTrue((bool)Prop(_effect, "IsAnimating"));
        Assert.AreEqual(2, Prop(_effect, "NotificationCount"));
    }

    [Test]
    public void Completion_ShouldUseCompletionLabelInsteadOfNewTask()
    {
        Call(_effect, "NotifyTaskChanged", true);
        Call(_effect, "Tick", 0.3f, true);
        Assert.AreEqual(Call(T("UISystem.GameUI"), "L", "quest.ui.completed_cue"),
            _card.Find("NewTaskCue/Label").GetComponent<Text>().text);
    }

    [TearDown]
    public void Cleanup() => UnityEngine.Object.DestroyImmediate(_host);
}

/// <summary>Opt-in real MainScene capture. Run alone with GEOMODEL_QUEST_CLIP set to a frame output directory.</summary>
public class QuestAttentionSceneTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private UnityEngine.Object _backend;
    private bool _backendEnabled;
    private object _director;
    private IDisposable _modal;
    private Camera _camera;
    private RenderTexture _target;
    private Texture2D _frame;
    private string _output;
    private static Type T(string n) => QuestAttentionTests.T(n);
    private static object Call(object o, string m, params object[] a) => QuestAttentionTests.Call(o, m, a);
    private static object Prop(object o, string p) => QuestAttentionTests.Prop(o, p);

    [UnityTest]
    public IEnumerator NewObjective_ShouldCueOnceAndRecordRealScene()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_QUEST_CLIP");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_QUEST_CLIP to record the real scene.");
        Directory.CreateDirectory(_output);
        // Reuse the same GameView resolution setup as the field acceptance harness.
        typeof(FieldFeedbackAcceptanceTests).GetMethod("SetGameViewSize", Flags)
            .Invoke(new FieldFeedbackAcceptanceTests(), new object[] { 1280, 720 });
        _backend = Resources.Load("BackendSettings");
        _backendEnabled = (bool)QuestAttentionTests.Get(_backend, "enableBackend");
        QuestAttentionTests.Set(_backend, "enableBackend", false);
        PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
        PlayerPrefs.SetInt("FirstControlGuide.Completed.v2", 1);
        PlayerPrefs.SetInt("FirstControlGuide.FieldCompleted.v1", 1);
        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4);
        _director = Prop(T("StorySystem.StoryDirector"), "Instance");
        Call(_director, "CancelPlayback");
        Call(_director, "ReloadFlags");
        var quests = Prop(T("QuestSystem.QuestManager"), "Instance");
        Call(quests, "CancelPendingPlayback");
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        Call(T("Core.GameInputState"), "ReleaseAll");
        Call(T("StorySystem.InvestigationProgress"), "Reset");
        Call(T("QuestSystem.QuestUI"), "SetForceHidden", false);
        Time.timeScale = 1;
        var player = (Behaviour)UnityEngine.Object.FindFirstObjectByType(T("FirstPersonController"));
        player.enabled = false;
        _camera = Camera.main;
        var ground = Physics.RaycastAll(new Vector3(-10, 70, -33), Vector3.down, 120)
            .OrderBy(h => h.distance).First(h => h.collider.GetComponent(T("GeologyLayer")) != null).point;
        _camera.transform.position = ground + Vector3.up * 1.7f;
        _camera.transform.LookAt(_camera.transform.position + new Vector3(12, -0.3f, 28));
        foreach (var quest in ((IDictionary)QuestAttentionTests.Get(quests, "_quests")).Values)
            QuestAttentionTests.Set(quest, "status", Enum.Parse(T("QuestSystem.QuestStatus"), "NotStarted"));
        Call(quests, "StartQuest", "q.lab.anomaly");
        Call(T("QuestSystem.QuestUI"), "RefreshAll");
        yield return new WaitForSecondsRealtime(4);
        var ui = (Component)UnityEngine.Object.FindFirstObjectByType(T("QuestSystem.QuestUI"));
        var effect = ui.GetComponent(T("UISystem.QuestAttentionEffect"));
        int previousCount = (int)Prop(effect, "NotificationCount");
        Assert.IsFalse((bool)Prop(effect, "IsAnimating"));
        _target = new RenderTexture(Screen.width, Screen.height, 24);
        _frame = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        var times = new List<string>();
        float begin = Time.realtimeSinceStartup;
        bool started = false;
        int frameIndex = 0;
        while (Time.realtimeSinceStartup - begin < 7)
        {
            float elapsed = Time.realtimeSinceStartup - begin;
            if (!started && elapsed >= 1)
            {
                Call(quests, "StartQuest", "q.field.phase");
                started = true;
            }
            CaptureFrame(Path.Combine(_output, "frame-" + frameIndex.ToString("D4") + ".jpg"));
            times.Add(elapsed.ToString("F6", CultureInfo.InvariantCulture));
            frameIndex++;
            yield return new WaitForSecondsRealtime(1f / 30);
        }
        File.WriteAllLines(Path.Combine(_output, "times.txt"), times);
        Assert.AreEqual(previousCount + 1, Prop(effect, "NotificationCount"), "A real quest-start event plays one cue.");
        Assert.IsFalse((bool)Prop(effect, "IsAnimating"), "The cue ends by itself.");
        for (int i = 0; i < 5; i++) Call(T("QuestSystem.QuestUI"), "RefreshAll");
        Call(ui, "SceneChanged", SceneManager.GetActiveScene(), LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.AreEqual(previousCount + 1, Prop(effect, "NotificationCount"), "Ordinary UI refreshes do not replay the cue.");
        Assert.IsFalse((bool)Prop(T("Core.GameInputState"), "IsModalOpen"), "The cue never acquires an input lock.");

        _modal = (IDisposable)Call(T("Core.GameInputState"), "Acquire", new object[] { null });
        Call(quests, "StartQuest", "q.lab.return");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreEqual(previousCount + 1, Prop(effect, "NotificationCount"), "A menu defers the next cue.");
        _modal.Dispose();
        _modal = null;
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreEqual(previousCount + 2, Prop(effect, "NotificationCount"), "Closing the menu reveals the pending cue.");
    }

    private void CaptureFrame(string path)
    {
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var distances = canvases.Select(c => c.planeDistance).ToArray();
        var previousTarget = _camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            _camera.targetTexture = _target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                canvas.planeDistance = _camera.nearClipPlane + 0.1f;
            }
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            RenderTexture.active = _target;
            _frame.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            _frame.Apply();
            File.WriteAllBytes(path, _frame.EncodeToJPG(92));
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = cameras[i];
                canvases[i].planeDistance = distances[i];
            }
            _camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        _modal?.Dispose();
        if (_director != null) Call(_director, "CancelPlayback");
        if (_backend != null) QuestAttentionTests.Set(_backend, "enableBackend", _backendEnabled);
        if (_target != null) { _target.Release(); UnityEngine.Object.Destroy(_target); }
        if (_frame != null) UnityEngine.Object.Destroy(_frame);
        yield return null;
    }
}
