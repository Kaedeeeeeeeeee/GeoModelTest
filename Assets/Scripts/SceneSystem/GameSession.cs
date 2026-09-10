using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using UISystem;

namespace SceneSystem
{
    public sealed class GameSession : MonoBehaviour
    {
        public const string ResumeSceneKey = "GameSession.ResumeScene.v1";
        private static GameSession _instance;
        private bool _returning;
        private bool _quitting;

        public static GameSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("GameSession").AddComponent<GameSession>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        public static bool IsGameplayScene(string scene) => scene == "MainScene" || scene == "Laboratory Scene";

        public static bool HasProgress =>
            !string.IsNullOrWhiteSpace(PlayerPrefs.GetString("StoryFlags", "")) ||
            !string.IsNullOrWhiteSpace(PlayerPrefs.GetString("QuestSystem.CompletedObjectives", "")) ||
            PlayerPrefs.HasKey(ResumeSceneKey);

        public static string ResumeScene
        {
            get
            {
                string saved = PlayerPrefs.GetString(ResumeSceneKey, "MainScene");
                return IsGameplayScene(saved) ? saved : "MainScene";
            }
        }

        public static void SaveCheckpoint()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (IsGameplayScene(scene))
            {
                PlayerPrefs.SetString(ResumeSceneKey, scene);
                var data = GameSceneManager.Instance.GetComponent<PlayerPersistentData>();
                if (data != null)
                {
                    data.SaveCurrentSceneData(scene);
                    data.SaveInventory();
                }
                var warehouse = UnityEngine.Object.FindFirstObjectByType<WarehouseManager>();
                if (warehouse != null) warehouse.SaveWarehouseData();
            }
            PlayerPrefs.Save();
            WebGLFileSync.Flush();
        }

        public void ContinueGame()
        {
            GameInputState.ReleaseAll();
            Time.timeScale = 1f;
            GameSceneManager.Instance.SwitchToScene(ResumeScene);
        }

        public void ConfirmReturnToTitle()
        {
            GameUI.Confirm(GameUI.L("ui.session.return_title"), GameUI.L("ui.session.return_message"),
                GameUI.L("ui.session.save_return"), ReturnToTitle);
        }

        public void ReturnToTitle()
        {
            if (!_returning) StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            _returning = true;
            SaveCheckpoint();
            if (StorySystem.InvestigationProgress.IsComplete && Backend.ResearchParticipationCoordinator.Instance.IsResearchActive)
            {
                bool ended = false;
                Backend.ResearchParticipationCoordinator.Instance.EndSession("teaching_completed", () => ended = true);
                while (!ended) yield return null;
            }
            bool saved = false;
            yield return WebGLFileSync.FlushAndWait(value => saved = value);
            if (!saved)
            {
                _returning = false;
                GameUI.Confirm(GameUI.L("ui.session.save_failed"), GameUI.L("ui.session.save_retry"),
                    GameUI.L("ui.session.retry"), ReturnToTitle);
                yield break;
            }
            SettingsManager.Instance.CloseSettings();
            StorySystem.StoryHistoryUI.CloseCurrent();
            StorySystem.StoryDirector.Instance.CancelPlayback();
            QuestSystem.QuestManager.Instance.CancelPendingPlayback();
            GameInputState.ReleaseAll();
            Time.timeScale = 1f;
            GameSceneManager.Instance.SwitchToScene("StartScene");
            _returning = false;
        }

        public void QuitToWebsite()
        {
            if (!_quitting) StartCoroutine(QuitRoutine());
        }

        private IEnumerator QuitRoutine()
        {
            _quitting = true;
            SaveCheckpoint();
            bool saved = false;
            yield return WebGLFileSync.FlushAndWait(value => saved = value);
            if (!saved)
            {
                _quitting = false;
                GameUI.Confirm(GameUI.L("ui.session.save_failed"), GameUI.L("ui.session.save_retry"),
                    GameUI.L("ui.session.retry"), QuitToWebsite);
                yield break;
            }
            bool ended = false;
            Backend.ResearchParticipationCoordinator.Instance.EndSession("application_quit", () => ended = true);
            while (!ended) yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            yield return WebGLFileSync.FlushAndWait(value => saved = value);
            if (!saved)
            {
                _quitting = false;
                GameUI.Confirm(GameUI.L("ui.session.save_failed"), GameUI.L("ui.session.save_retry"),
                    GameUI.L("ui.session.retry"), QuitToWebsite);
                yield break;
            }
            var config = Resources.Load<WebExitSettings>("WebExitSettings");
            string url = config != null ? config.GamePageUrl : string.Empty;
            GeoModelTest_ReturnToGamePage(url, GameUI.L("ui.start.quit"));
#elif UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void GeoModelTest_ReturnToGamePage(string configuredUrl, string label);
#endif
    }
}
