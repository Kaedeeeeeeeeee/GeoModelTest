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
            GameEventBus.SceneLoaded += OnSceneLoaded;
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
            // 场景1：首次进入 MainScene → 地震演出 + 对白
            if (sceneName == "MainScene" && !HasFlag("story.main.rescue"))
            {
                StartCoroutine(Run_MainScene_Rescue());
                return;
            }

            // 场景2/3：首次进入 Laboratory Scene → 实验室开场对白 + 解锁地质锤
            if (sceneName == "Laboratory Scene" && !HasFlag("story.lab.intro"))
            {
                StartCoroutine(Run_Laboratory_Intro());
                return;
            }
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
            // 简易：屏幕震动 + 第三人称镜头 + 字幕推进
            yield return StartCoroutine(CameraShakeAction.Execute(3.5f, 0.6f));
            yield return StartCoroutine(ThirdPersonCinematicAction.Execute(
                new List<string>
                {
                    "这是哪里…？地面在震动！",
                    "糟了！滑坡？震源很近…",
                    "有人吗！……救命！"
                },
                5.5f
            ));

            SetFlag("story.main.rescue");
        }

        private IEnumerator Run_Laboratory_Intro()
        {
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
        public static IEnumerator Execute(List<string> lines, float totalDuration)
        {
            var player = GameObject.Find("Lily");
            var mainCam = Camera.main;
            if (player == null || mainCam == null)
            {
                // 兜底：仅播放字幕
                yield return SubtitleUI.ShowSequence(lines, totalDuration);
                yield break;
            }

            // 关闭主摄像机
            var mainCamGO = mainCam.gameObject;
            mainCamGO.SetActive(false);

            // 临时镜头
            var cine = new GameObject("CinematicCamera");
            var cam = cine.AddComponent<Camera>();
            var target = player.transform;

            float t = 0f;
            // 弹字幕协程
            var sub = SubtitleUI.ShowSequence(lines, totalDuration);
            while (t < totalDuration)
            {
                t += Time.unscaledDeltaTime;
                // 跟随玩家后上方，缓慢偏移
                var back = -target.forward;
                var pos = target.position + back * 3.0f + Vector3.up * 1.8f;
                cam.transform.position = Vector3.Lerp(cam.transform.position == Vector3.zero ? pos : cam.transform.position, pos, 0.2f);
                cam.transform.LookAt(target.position + Vector3.up * 1.6f);
                yield return null;
            }

            // 等待字幕结束
            while (sub.MoveNext()) { }

            // 还原
            Object.Destroy(cine);
            mainCamGO.SetActive(true);
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
}

