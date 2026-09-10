using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StorySystem;
using Core;

/// <summary>Explicit, editor-only review driver. Never included in player builds.</summary>
[InitializeOnLoad]
public static class RemediationReview
{
    private const string CommandPath = "Logs/remediation/review-command.json";
    private const string ResultPath = "Logs/remediation/review-result.json";
    [Serializable] private sealed class Command { public string op; public string value; public int width = 1366; public int height = 768; }
    static RemediationReview() { EditorApplication.update += Poll; }

    [MenuItem("GeoModel/Review/Close review editor")]
    public static void CloseReview() { EditorApplication.Exit(0); }

    public static void BuildValidation()
    {
        RetainRuntimeShaders();
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            "Build/RemediationWebGL", BuildTarget.WebGL, BuildOptions.Development);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("WebGL validation build failed: " + result.summary.result);
    }

    [MenuItem("GeoModel/Setup/Retain runtime shaders")]
    public static void RetainRuntimeShaders()
    {
        // These shaders are found by name when constructing tools and samples at runtime.
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
        foreach (string name in new[] { "Unlit/Color", "Unlit/Transparent", "Legacy Shaders/Transparent/Diffuse" })
        {
            var shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Required runtime shader is missing: " + name);
            bool included = false;
            for (int i = 0; i < shaders.arraySize; i++) included |= shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader;
            if (!included)
            {
                int index = shaders.arraySize++;
                shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            }
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("GeoModel/Review/Start UI review")]
    public static void StartReview()
    {
        if (EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        SetSize(1366, 768);
        EditorApplication.isPlaying = true;
    }

    private static void SetSize(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupProperty = sizesType.GetProperty("currentGroupType", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object groupType = groupProperty?.GetValue(sizes) ?? 0;
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        Type modeType = assembly.GetType("UnityEditor.GameViewSizeType");
        object size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(modeType, 1), width, height, "Review " + width + "x" + height });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        Type gameViewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(view, new object[] { count - 1, null });
        view.Repaint();
        view.Focus();
    }

    private static void Poll()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !File.Exists(CommandPath)) return;
        try
        {
            var command = JsonUtility.FromJson<Command>(File.ReadAllText(CommandPath));
            File.Delete(CommandPath);
            Execute(command);
            File.WriteAllText(ResultPath, "{\"ok\":true,\"op\":\"" + command.op + "\"}");
        }
        catch (Exception ex)
        {
            File.WriteAllText(ResultPath, JsonUtility.ToJson(new Error { error = ex.ToString() }));
            Debug.LogException(ex);
        }
    }
    [Serializable] private sealed class Error { public string error; }
    private static void Execute(Command c)
    {
        switch (c.op)
        {
            case "size": SetSize(c.width, c.height); break;
            case "capture":
                Directory.CreateDirectory("Docs/reports/2026-09-10/screenshots");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/reports/2026-09-10/screenshots/" + c.value + ".png"));
                break;
            case "language": LocalizationManager.Instance.SwitchLanguage((LanguageSettings.Language)Enum.Parse(typeof(LanguageSettings.Language), c.value)); break;
            case "reset": ProgressResetService.ResetAll(); break;
            case "backend_local":
            case "survey_local":
                var local = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(c.op == "survey_local" ? "Logs/remediation/survey/fixture.json" : "Logs/remediation/backend-ui-fixture.json"));
                if ((string)local["url"] != "http://127.0.0.1:55321") throw new InvalidOperationException("Local QA server required.");
                var backendSettings = Backend.BackendSettingsProvider.Load();
                typeof(Backend.BackendSettings).GetField("supabaseUrl", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(backendSettings, (string)local["url"]);
                typeof(Backend.BackendSettings).GetField("publishableKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(backendSettings, (string)local["key"]);
                typeof(Backend.BackendSettings).GetField("enableBackend", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(backendSettings, true);
                if (c.op == "survey_local")
                {
                    var surveySettings = Resources.Load<Backend.SurveySettings>("SurveySettings");
                    typeof(Backend.SurveySettings).GetField("_pageUrl", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(surveySettings, "http://127.0.0.1:55883/");
                    Backend.SurveyGateway.ReviewOpenUrl = url => File.WriteAllText("Logs/remediation/survey/opened-url.txt", url);
                }
                break;
            case "clear_backend_fixture":
                if (Backend.ResearchParticipationCoordinator.Instance.IsResearchActive) throw new InvalidOperationException("End the local test session first.");
                foreach (string key in new[] { "Backend.PendingTelemetry", "Backend.PendingSessionEnd.v1", "Backend.PendingProgressSnapshot.v2", "Backend.CurrentCodeHash.v1", "Backend.CurrentCodeVerified.v1", "Backend.LastVerifiedCodeHash.v1", "Backend.AccessToken", "Backend.RefreshToken", "Backend.UserId", "Backend.AccessTokenExpiresAtUnix", "Backend.ResearchParticipantId", "Backend.ResearchStudyId", "Backend.ResearchCondition", "Backend.ResearchProtocolVersion" }) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                break;
            case "end_research": Backend.ResearchParticipationCoordinator.Instance.EndSession("local_review"); break;
            case "settings": SettingsManager.Instance.OpenSettings(); break;
            case "close_settings": SettingsManager.Instance.CloseSettings(); break;
            case "history": StoryHistoryUI.Show(); break;
            case "close_history": StoryHistoryUI.CloseCurrent(); break;
            case "return": SceneSystem.GameSession.Instance.ReturnToTitle(); break;
            case "continue": SceneSystem.GameSession.Instance.ContinueGame(); break;
            case "scene":
                StoryDirector.Instance.CancelPlayback(); GameInputState.ReleaseAll(); Time.timeScale = 1;
                GameSceneManager.Instance.SwitchToScene(c.value); break;
            case "skip_intro":
                PlayerPrefs.SetString("StoryFlags", "story.main.rescue|story.lab.intro|story.field.phase_intro|story.chapter4.sample_intro");
                PlayerPrefs.SetInt("MainScene.ClassRoom.Hidden", 1);
                StoryDirector.Instance.ReloadFlags(); break;
            case "story": StoryDirector.Instance.PlaySequence(c.value); break;
            case "cancel_story": StoryDirector.Instance.CancelPlayback(); break;
            case "wheel":
                var wheel = UnityEngine.Object.FindFirstObjectByType<InventoryUISystem>();
                Invoke(wheel, "OpenWheel"); break;
            case "equip":
                var tools = UnityEngine.Object.FindFirstObjectByType<ToolManager>();
                int index = Array.FindIndex(tools.availableTools, t => t != null && t.toolID == c.value);
                if (index < 0) throw new InvalidOperationException("Tool not unlocked: " + c.value);
                tools.EquipTool(index); break;
            case "field_pose":
                var data = GameSceneManager.Instance.GetComponent<PlayerPersistentData>();
                PlayerPrefs.DeleteKey("PlayerPersistentData.Scene.MainScene");
                data.ClearSceneData("MainScene"); data.RestoreSceneData("MainScene"); break;
            case "unlock":
                var persistent = GameSceneManager.Instance.GetComponent<PlayerPersistentData>();
                foreach (string id in new[] { "1002", "1000", "1001", "999" }) persistent.MarkToolUnlocked(id);
                persistent.ApplyUnlockedToolsToScene(); break;
            case "core":
                QuestSystem.QuestManager.Instance.StartQuest("q.chapter4.sample");
                Invoke(QuestSystem.QuestManager.Instance, "OnSampleAdded", new SampleItem { sampleID = "review-core", sourceToolID = "1001", displayName = "Review core" });
                break;
            case "prepare_core":
                StoryDirector.Instance.CancelPlayback();
                var quests = QuestSystem.QuestManager.Instance;
                foreach (string id in new[] { "q.lab.intro", "q.lab.drkaede", "q.lab.anomaly", "q.field.phase", "q.lab.return", "q.chapter4.kaede", "q.chapter4.field" })
                {
                    quests.StartQuest(id);
                    foreach (var objective in quests.GetQuest(id).objectives) quests.CompleteObjective(objective.id);
                }
                quests.StartQuest("q.chapter4.sample");
                InvestigationProgress.MarkFieldSample();
                break;
            case "long_dialogue":
                StoryDirector.Instance.CancelPlayback();
                var longText = string.Join("\n", Enumerable.Repeat(LocalizationManager.Instance.GetText("story.beat3.q1.fb_ng"), 12));
                if (longText.Contains("[")) longText = string.Join("\n", Enumerable.Repeat("柱状図（ちゅうじょうず）と化石（かせき）の記録（きろく）を調（しら）べて、地層（ちそう）ができた環境（かんきょう）を考（かんが）えよう。", 14));
                StoryDirector.Instance.StartCoroutine(StoryDirector.SubtitleUI.ShowSequence(new[] { new StoryDirector.SubtitleUI.SubtitleLine("Dr.Kaede", longText) }));
                break;
            case "input":
                UnityEngine.Object.FindFirstObjectByType<InputField>().text = c.value; break;
            case "report":
                StoryDirector.Instance.CancelPlayback(); GameInputState.ReleaseAll();
                InvestigationProgress.MarkFieldSample(); InvestigationProgress.AcceptCoreSample("review-core", "1001", true, true);
                InvestigationProgress.MarkLabAnalysis(); InvestigationProgress.MarkComplete();
                StoryDirector.Instance.PlayReport(); break;
            case "exit": EditorApplication.Exit(0); break;
            case "click":
                var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.gameObject.activeInHierarchy &&
                    (b.name == c.value || b.GetComponentInChildren<Text>()?.text == c.value || b.GetComponentInChildren<TMPro.TMP_Text>()?.text == c.value));
                button.onClick.Invoke(); break;
            case "state":
                var state = new
                {
                    scene = SceneManager.GetActiveScene().name, timeScale = Time.timeScale,
                    modal = GameInputState.IsModalOpen, story = StoryDirector.IsStoryPlaybackActive,
                    width = Screen.width, height = Screen.height,
                    buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    sizeGroups = Enum.GetNames(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType")),
                    sizeProperties = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes").GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(p => p.Name),
                    gameViewProperties = typeof(Editor).Assembly.GetType("UnityEditor.GameView").GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.Name.ToLowerInvariant().Contains("size") || p.Name.ToLowerInvariant().Contains("group")).Select(p => p.Name),
                    canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(v => new { v.name, active = v.gameObject.activeInHierarchy, v.sortingOrder }),
                    tools = UnityEngine.Object.FindObjectsByType<ToolManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(v => new { v.name, v.enabled, hud = v.GetComponent<UISystem.CurrentToolHUD>() != null, active = v.gameObject.activeInHierarchy }),
                    buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy).Select(b => new { b.name, text = b.GetComponentInChildren<Text>()?.text ?? b.GetComponentInChildren<TMPro.TMP_Text>()?.text, b.interactable }),
                    text = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => new { t.name, t.text, t.pageToDisplay, pages = t.textInfo.pageCount }),
                    quests = QuestSystem.QuestManager.Instance.GetAllQuests().Select(q => new { q.id, status = q.status.ToString() })
                };
                File.WriteAllText("Logs/remediation/ui-state.json", Newtonsoft.Json.JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented));
                break;
        }
    }
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
}
