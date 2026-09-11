using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class InvestigationReportTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private const string Server = "https://survey-preview-test.invalid";
    private readonly Dictionary<string, string> _prefs = new Dictionary<string, string>();
    private readonly string[] _keys =
    {
        "StorySystem.Investigation.v1", "StorySystem.QuizAttemptState.v2", "Backend.SurveyCompletion.v1",
        "Backend.ResearchParticipantId", "Backend.SurveyTicket.v1", "Backend.AccessToken",
        "Backend.RefreshToken", "Backend.AccessTokenExpiresAtUnix"
    };
    private UnityEngine.Object _settings;
    private string _settingsJson;
    private IEnumerator _report;
    private bool _closed;
    private Keyboard _keyboard;
    private InputSettings.UpdateMode _updateMode;
    private InputSettings.BackgroundBehavior _backgroundBehavior;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
#endif

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Instance(string name) => RuntimeType(name).GetProperty("Instance", Flags).GetValue(null);
    private static object Call(string name, string method, object target = null, params object[] args) =>
        RuntimeType(name).GetMethod(method, Flags).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private Transform Card => ((Canvas)RuntimeType("StorySystem.InvestigationReport").GetField("_activeCanvas", Flags).GetValue(null)).transform.GetChild(1);
    private Button Return => Card.Find("ReturnToTitle").GetComponent<Button>();
    private Button Survey => Card.Find("AnswerSurvey").GetComponent<Button>();

    [SetUp]
    public void SetUp()
    {
        foreach (string key in _keys)
        {
            _prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            PlayerPrefs.DeleteKey(key);
        }
        RuntimeType("StorySystem.QuizScoreManager").GetField("_instance", Flags).SetValue(null, null);
        RuntimeType("StorySystem.InvestigationProgress").GetField("_state", Flags).SetValue(null, null);
        _settings = Resources.Load("BackendSettings");
        _settingsJson = JsonUtility.ToJson(_settings);
        Set(_settings, "supabaseUrl", Server);
        Set(_settings, "publishableKey", "local-ui-test-key");
        // Stop the unrelated scene transition after a successful close assertion.
        Set(Instance("SceneSystem.GameSession"), "_returning", true);
        Call("StorySystem.InvestigationProgress", "MarkComplete");
        _updateMode = InputSystem.settings.updateMode;
        _backgroundBehavior = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        _editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        _keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.EnableDevice(_keyboard);
        _keyboard.MakeCurrent();
        _closed = false;
    }

    private void OpenReport(bool research)
    {
        if (research)
        {
            string run = (string)Instance("StorySystem.QuizScoreManager").GetType().GetProperty("RunId").GetValue(Instance("StorySystem.QuizScoreManager"));
            string participant = "11111111-1111-4111-8111-111111111111";
            PlayerPrefs.SetString("Backend.ResearchParticipantId", participant);
            PlayerPrefs.SetString("Backend.SurveyCompletion.v1", "{\"participantId\":\"" + participant + "\",\"sessionId\":\"22222222-2222-4222-8222-222222222222\",\"runId\":\"" + run + "\",\"server\":\"" + Server + "\"}");
        }
        Assert.AreEqual(research, RuntimeType("Backend.SurveyGateway").GetProperty("IsEligible").GetValue(null));
        _report = (IEnumerator)Call("StorySystem.InvestigationReport", "Show", null, (Action)(() => _closed = true));
        Assert.IsTrue(_report.MoveNext());
    }

    private void Press(Key key)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        InputSystem.Update();
    }

    [UnityTest]
    public IEnumerator Report_Should_BlockButtonCardAndKeys_BeforeResearchSurveyClick()
    {
        OpenReport(true);
        Assert.IsFalse(Return.interactable, "Return must be disabled before the first rendered frame.");
        Assert.IsTrue(Survey.interactable);
        Assert.Less(Return.GetComponentInChildren<Text>().color.r, 0.6f, "The label must also look disabled.");
        Return.onClick.Invoke();
        Card.GetComponent<Button>().onClick.Invoke();
        foreach (Key key in new[] { Key.Space, Key.Enter })
        {
            Press(key);
            Assert.IsTrue(_report.MoveNext());
            Assert.IsFalse(_closed);
            Assert.IsFalse(Return.interactable);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator Report_Should_UnlockAfterFailedSurveyAttempt_WithoutCompletingQuestionnaire()
    {
        OpenReport(true);
        Survey.onClick.Invoke();
        // The real gateway has a completed run but no auth tokens. It returns a
        // re-entry error locally without requesting any research API.
        yield return null;
        Assert.IsTrue(_report.MoveNext());
        Assert.IsFalse((bool)Instance("Backend.SurveyGateway").GetType().GetProperty("IsBusy").GetValue(Instance("Backend.SurveyGateway")));
        Assert.IsTrue(Return.interactable);
        Assert.IsTrue(Survey.interactable, "A failed attempt must remain retryable.");
        Assert.IsNotEmpty(Card.Find("SurveyStatus").GetComponent<Text>().text);
        Return.onClick.Invoke();
        Assert.IsFalse(_report.MoveNext());
        Assert.IsTrue(_closed);
    }

    [UnityTest]
    public IEnumerator Report_Should_KeepReturnAvailable_ForOrdinaryPlay()
    {
        OpenReport(false);
        Assert.IsTrue(Return.interactable);
        Assert.IsFalse(Survey.interactable);
        Return.onClick.Invoke();
        Assert.IsFalse(_report.MoveNext());
        Assert.IsTrue(_closed);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Report_Should_NotTreatSurveySubmitKey_AsReturnShortcut()
    {
        OpenReport(true);
        EventSystem.current.SetSelectedGameObject(Survey.gameObject);
        Press(Key.Enter);
        Survey.onClick.Invoke();
        Assert.IsTrue(_report.MoveNext(), "The click frame must not also exit the report.");
        yield return null;
        Press(Key.Space);
        Assert.IsTrue(_report.MoveNext(), "A focused survey button must not trigger the global exit shortcut.");
        Assert.IsFalse(_closed);
    }

    [TearDown]
    public void TearDown()
    {
        (_report as IDisposable)?.Dispose();
        var canvas = (Canvas)RuntimeType("StorySystem.InvestigationReport").GetField("_activeCanvas", Flags).GetValue(null);
        if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
        RuntimeType("StorySystem.InvestigationReport").GetField("_activeCanvas", Flags).SetValue(null, null);
        Call("Core.GameInputState", "ReleaseAll");
        Set(Instance("SceneSystem.GameSession"), "_returning", false);
        JsonUtility.FromJsonOverwrite(_settingsJson, _settings);
        foreach (var pair in _prefs)
        {
            if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key);
            else PlayerPrefs.SetString(pair.Key, pair.Value);
        }
        PlayerPrefs.Save();
        RuntimeType("StorySystem.QuizScoreManager").GetField("_instance", Flags).SetValue(null, null);
        RuntimeType("StorySystem.InvestigationProgress").GetField("_state", Flags).SetValue(null, null);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        InputSystem.settings.updateMode = _updateMode;
        InputSystem.settings.backgroundBehavior = _backgroundBehavior;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
#endif
        Time.timeScale = 1f;
    }
}
