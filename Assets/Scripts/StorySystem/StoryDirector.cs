using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace StorySystem
{
    /// <summary>
    /// 简易剧情导演：监听关键事件并触发内置剧情段落（MVP）。
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        private static StoryDirector _instance;
        public static StoryDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StoryDirector");
                    _instance = go.AddComponent<StoryDirector>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private HashSet<string> _flags;
        [SerializeField] private bool enableDebugLog = true;
        private bool _isRunningCinematic = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance; // 确保创建
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _flags = ProgressPersistence.LoadFlags();
            if (enableDebugLog) Debug.Log($"[StoryDirector] 初始化，已载入标记: {_flags.Count}");
            GameEventBus.SceneLoaded += OnSceneLoaded;
            // 兜底：若事件未触发，延迟检查当前场景一次
            StartCoroutine(DelayedCheckCurrentScene());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                GameEventBus.SceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(string sceneName)
        {
            if (enableDebugLog) Debug.Log($"[StoryDirector] OnSceneLoaded: {sceneName}");
            // 场景1：首次进入 MainScene → 地震演出 + 对白
            if (sceneName == "MainScene" && !HasFlag("story.main.rescue"))
            {
                if (!_isRunningCinematic) StartCoroutine(Run_MainScene_Rescue());
                return;
            }

            // 场景2/3：首次进入 Laboratory Scene → 实验室开场对白 + 解锁地质锤
            if (sceneName == "Laboratory Scene" && !HasFlag("story.lab.intro"))
            {
                if (!_isRunningCinematic) StartCoroutine(Run_Laboratory_Intro());
                return;
            }
        }

        private IEnumerator DelayedCheckCurrentScene()
        {
            yield return null; // 下一帧，等待场景稳定
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (enableDebugLog) Debug.Log($"[StoryDirector] DelayedCheckCurrentScene: {active}");
            OnSceneLoaded(active);
        }

        private bool HasFlag(string key) => _flags != null && _flags.Contains(key);
        private void SetFlag(string key)
        {
            if (_flags == null) _flags = new HashSet<string>();
            if (_flags.Add(key))
            {
                ProgressPersistence.SaveFlags(_flags);
            }
        }

        private IEnumerator Run_MainScene_Rescue()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_MainScene_Rescue 开始");
            // 进入场景即切第三人称，正面朝向角色，并在期间保持震动与字幕推进
            yield return StartCoroutine(ThirdPersonCinematicAction.ExecuteWithShakeFacingFront(
                new List<string>
                {
                    "这是哪里…？地面在震动！",
                    "糟了！滑坡？震源很近…",
                    "有人吗！……救命！"
                },
                6.0f,
                0.6f
            ));

            SetFlag("story.main.rescue");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_MainScene_Rescue 结束");
        }

        private IEnumerator Run_Laboratory_Intro()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Intro 开始");
            // 简易对白（可复用字幕UI）
            yield return StartCoroutine(SubtitleUI.ShowSequence(new List<string>
            {
                "已经安全抵达实验室。",
                "接下来领取基础工具，开始研究。",
                "获得：地质锤"
            }, 4.0f));

            // 解锁地质锤（1002）
            ToolUnlockService.UnlockToolById("1002");

            SetFlag("story.lab.intro");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Intro 结束");
        }
    }

    /// <summary>
    /// 简易屏幕震动效果（对主摄像机的位置施加噪声）。
    /// </summary>
    public static class CameraShakeAction
    {
        public static IEnumerator Execute(float duration, float amplitude)
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            var t = 0f;
            var original = cam.transform.localPosition;
            var seed = Random.value * 1000f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float x = (Mathf.PerlinNoise(seed, Time.time * 10f) - 0.5f) * 2f * amplitude;
                float y = (Mathf.PerlinNoise(Time.time * 10f, seed) - 0.5f) * 2f * amplitude * 0.6f;
                cam.transform.localPosition = original + new Vector3(x, y, 0f);
                yield return null;
            }
            cam.transform.localPosition = original;
        }
    }

    /// <summary>
    /// 简易第三人称镜头+字幕演出（临时镜头，结束后还原）。
    /// </summary>
    public static class ThirdPersonCinematicAction
    {
        /// <summary>
        /// 立即切至第三人称，镜头位于角色前方对脸，并在期间对镜头加入震动与字幕推进。
        /// </summary>
        public static IEnumerator ExecuteWithShakeFacingFront(List<string> lines, float totalDuration, float shakeAmplitude)
        {
            var player = GameObject.Find("Lily");
            var mainCam = Camera.main;

            // 禁用玩家控制（有则禁用）
            var fpc = Object.FindFirstObjectByType<FirstPersonController>();
            bool fpcPrev = fpc != null && fpc.enabled;
            if (fpc != null) fpc.enabled = false;

            if (player == null || mainCam == null)
            {
                // 兜底：仅播放字幕
                yield return SubtitleUI.ShowSequence(lines, totalDuration);
                if (fpc != null) fpc.enabled = fpcPrev;
                yield break;
            }

            // 关闭主摄像机
            var mainCamGO = mainCam.gameObject;
            mainCamGO.SetActive(false);

            // 创建临时镜头
            var cine = new GameObject("CinematicCamera");
            var cam = cine.AddComponent<Camera>();
            var target = player.transform;

            // 同步字幕
            var subtitleRoutine = SubtitleUI.ShowSequence(lines, totalDuration);
            var subtitleRunner = StoryDirectorRunner.Instance; // 用于驱动并行协程
            subtitleRunner.Run(subtitleRoutine);

            // 抖动噪声种子
            float seed = Random.value * 1000f;
            float t = 0f;
            Vector3 desiredPos = cam.transform.position;

            while (t < totalDuration)
            {
                t += Time.unscaledDeltaTime;
                // 角色正面：在角色前方一定距离，看向头部
                var head = target.position + Vector3.up * 1.6f;
                var inFront = head + target.forward * 1.8f; // 正面朝向
                desiredPos = inFront;

                // 叠加震动（对当前镜头生效）
                float x = (Mathf.PerlinNoise(seed, Time.time * 10f) - 0.5f) * 2f * shakeAmplitude * 0.6f;
                float y = (Mathf.PerlinNoise(Time.time * 10f, seed) - 0.5f) * 2f * shakeAmplitude;
                Vector3 shake = new Vector3(x, y, 0f);

                cam.transform.position = Vector3.Lerp(cam.transform.position == Vector3.zero ? desiredPos : cam.transform.position, desiredPos, 0.35f) + shake;
                cam.transform.LookAt(head);
                yield return null;
            }

            // 清理
            Object.Destroy(cine);
            mainCamGO.SetActive(true);

            // 恢复玩家控制
            if (fpc != null) fpc.enabled = fpcPrev;
        }
    }

    /// <summary>
    /// 极简字幕 UI（叠加一个最高层 Canvas + Text）。
    /// </summary>
    public static class SubtitleUI
    {
        public static IEnumerator ShowSequence(List<string> lines, float totalDuration)
        {
            if (lines == null || lines.Count == 0 || totalDuration <= 0f) yield break;

            var canvasGO = new GameObject("SubtitleCanvas");
            var canvas = canvasGO.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.1f, 0.08f);
            bgRt.anchorMax = new Vector2(0.9f, 0.22f);
            var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(bg.transform, false);
            var tr = textGO.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            var txt = textGO.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            float per = totalDuration / lines.Count;
            foreach (var line in lines)
            {
                txt.text = line; // 可后续替换为本地化Key
                float t = 0f;
                while (t < per)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            Object.Destroy(canvasGO);
        }
    }

    /// <summary>
    /// 辅助：用于从非 Mono 的静态方法里启动并行协程。
    /// </summary>
    internal class StoryDirectorRunner : MonoBehaviour
    {
        private static StoryDirectorRunner _instance;
        public static StoryDirectorRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StoryDirectorRunner");
                    _instance = go.AddComponent<StoryDirectorRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void Run(IEnumerator routine)
        {
            if (routine != null) StartCoroutine(routine);
        }
    }
}
