using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        [SerializeField] private string mainRescueSequenceResource = "Story/quest1.1";
        [SerializeField] private string labIntroSequenceResource = "Story/quest1.2";
        [SerializeField] private string phaseShifterSequenceResource = "Story/quest3.2";
        [SerializeField] private string labReturnSequenceResource = "Story/quest3.4";
        [SerializeField] private string chapter4FieldSequenceResource = "Story/quest4.2";
        [SerializeField] private string chapter4ReturnSequenceResource = "Story/quest4.4";
        [SerializeField] private string chapter5FieldSequenceResource = "Story/quest5.2";
        private bool _isRunningCinematic = false;

        private static readonly List<SubtitleUI.SubtitleLine> DefaultMainRescueLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("这是哪里…？地面在震动！"),
            new SubtitleUI.SubtitleLine("糟了！滑坡？震源很近…"),
            new SubtitleUI.SubtitleLine("有人吗！……救命！", triggerCameraShake: true)
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultLabIntroLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("旁白", "已经安全抵达实验室。"),
            new SubtitleUI.SubtitleLine("旁白", "接下来领取基础工具，开始研究。"),
            new SubtitleUI.SubtitleLine("系统", "领取任务：实验室引导")
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultPhaseShifterLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("Dr.Kaede", "フェーズシフターの調整が完了したわ。転送前に最終チェックをしましょう。"),
            new SubtitleUI.SubtitleLine("ナレーション", "静かな研究室で、淡い光が転送装置を包み込む。")
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultLabReturnLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("ナレーション", "研究室に戻った。報告と次の準備を進めよう。"),
            new SubtitleUI.SubtitleLine("主人公", "ただいま戻りました。次の段階に進みましょう。")
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultChapter4ReturnLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("ナレーション", "再びG-Labへ戻った。調査結果を整理しよう。"),
            new SubtitleUI.SubtitleLine("Dr.Kaede", "ボーリングサンプルの解析を始めよう。準備ができたら声をかけてくれ。")
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultChapter4FieldLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("ナレーション", "目的地点に転送された。空気が張りつめている。"),
            new SubtitleUI.SubtitleLine("Dr.Kaede", "推奨装置かドリルタワーでサンプルを採取して。深さと圧力のログも忘れずに。")
        };

        private static readonly List<SubtitleUI.SubtitleLine> DefaultChapter5FieldLines = new List<SubtitleUI.SubtitleLine>
        {
            new SubtitleUI.SubtitleLine("ナレーション", "野外に到着した。新しい指示を確認しよう。"),
            new SubtitleUI.SubtitleLine("Dr.Kaede", "ドローンを活用して周囲を調べてくれ。異常があればすぐに報告だ。")
        };

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
            if (sceneName == "Laboratory Scene")
            {
                if (!HasFlag("story.lab.intro"))
                {
                    if (!_isRunningCinematic) StartCoroutine(Run_Laboratory_Intro());
                    return;
                }

                var qm = QuestSystem.QuestManager.Instance;
                if (qm != null)
                {
                    if (qm.GetQuestStatus("q.lab.return") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.lab.return.enter_lab") &&
                        !HasFlag("story.lab.return"))
                    {
                        if (!_isRunningCinematic) StartCoroutine(Run_Laboratory_Return());
                        return;
                    }

                    if (qm.GetQuestStatus("q.chapter4.return") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.chapter4.return.enter_lab") &&
                        !HasFlag("story.chapter4.return"))
                    {
                        if (!_isRunningCinematic) StartCoroutine(Run_Laboratory_Return_Chapter4());
                        return;
                    }
                    if (qm.GetQuestStatus("q.chapter5.return") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.chapter5.return.enter_lab") &&
                        !HasFlag("story.chapter5.return"))
                    {
                        qm.CompleteObjective("q.chapter5.return.enter_lab");
                        SetFlag("story.chapter5.return");
                        return;
                    }
                }
            }

            if (sceneName == "MainScene")
            {
                var qm = QuestSystem.QuestManager.Instance;
                if (qm != null)
                {
                    if (qm.GetQuestStatus("q.chapter4.sample") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.chapter4.sample.collect") &&
                        !HasFlag("story.chapter4.sample_intro"))
                    {
                        if (!_isRunningCinematic) StartCoroutine(Run_Chapter4_FieldSequence());
                        return;
                    }

                    if (qm.GetQuestStatus("q.field.phase") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.field.phase.enter_field") &&
                        !HasFlag("story.field.phase_intro"))
                    {
                        if (!_isRunningCinematic) StartCoroutine(Run_Field_PhaseShifterIntro());
                        return;
                    }

                    if (qm.GetQuestStatus("q.chapter5.field") == QuestSystem.QuestStatus.InProgress &&
                        !qm.IsObjectiveCompleted("q.chapter5.field.enter_field") &&
                        !HasFlag("story.chapter5.field"))
                    {
                        if (!_isRunningCinematic) StartCoroutine(Run_Chapter5_FieldSequence());
                        return;
                    }
                }
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

        private List<SubtitleUI.SubtitleLine> LoadStoryLines(string resourcePath, IReadOnlyList<SubtitleUI.SubtitleLine> fallback)
        {
            var sequence = StorySequenceLoader.LoadFromResources(resourcePath, enableDebugLog);
            if (sequence != null)
            {
                var loadedLines = sequence.ToSubtitleLines();
                if (loadedLines.Count > 0) return loadedLines;
            }

            return fallback != null ? new List<SubtitleUI.SubtitleLine>(fallback) : new List<SubtitleUI.SubtitleLine>();
        }

        private IEnumerator Run_MainScene_Rescue()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_MainScene_Rescue 开始");
            // 进入场景即切第三人称，正面朝向角色，并在期间保持震动与字幕推进
            var lines = LoadStoryLines(mainRescueSequenceResource, DefaultMainRescueLines);
            yield return StartCoroutine(ThirdPersonCinematicAction.ExecuteWithShakeFacingFront(lines, 0.6f));

            SetFlag("story.main.rescue");
            // 演出结束后，自动切换到实验室，触发第二段剧情
            yield return new WaitForSeconds(0.35f);
            if (enableDebugLog) Debug.Log("[StoryDirector] 切换到 Laboratory Scene 以进入第二段剧情");
            var gsm = GameSceneManager.Instance;
            if (gsm != null)
            {
                gsm.SwitchToScene("Laboratory Scene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Laboratory Scene");
            }

            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_MainScene_Rescue 结束");
        }

        private IEnumerator Run_Laboratory_Intro()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Intro 开始");

            FirstPersonController controller = null;
            bool previousControllerState = false;
            controller = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if (controller != null)
            {
                previousControllerState = controller.enabled;
                controller.enabled = false;
            }

            // 简易对白（可复用字幕UI）
            var lines = LoadStoryLines(labIntroSequenceResource, DefaultLabIntroLines);
            yield return PlayLinesWithCameraShake(lines);

            // 发起/推进首个任务（奖励在任务完成时发放）
            QuestSystem.QuestManager.Instance.StartQuest("q.lab.intro");
            QuestSystem.QuestManager.Instance.CompleteObjective("q.lab.intro.intro_done");

            if (controller != null)
            {
                controller.enabled = previousControllerState;
            }

            SetFlag("story.lab.intro");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Intro 结束");
        }

        public void PlaySequence(string resourcePath, Action onComplete = null, bool disablePlayerControl = true)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlaySequenceRoutine(resourcePath, onComplete, disablePlayerControl));
        }

        private IEnumerator PlaySequenceRoutine(string resourcePath, Action onComplete, bool disablePlayerControl)
        {
            while (_isRunningCinematic)
            {
                yield return null;
            }

            _isRunningCinematic = true;

            FirstPersonController controller = null;
            bool previousControllerState = false;
            if (disablePlayerControl)
            {
                controller = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
                if (controller != null)
                {
                    previousControllerState = controller.enabled;
                    controller.enabled = false;
                }
            }

            var lines = LoadStoryLines(resourcePath, null);
            yield return PlayLinesWithCameraShake(lines);

            if (controller != null)
            {
                controller.enabled = previousControllerState;
            }

            _isRunningCinematic = false;
            onComplete?.Invoke();
        }

        private IEnumerator PlayLinesWithCameraShake(IReadOnlyList<SubtitleUI.SubtitleLine> lines)
        {
            yield return SubtitleUI.ShowSequence(
                lines,
                null,
                (line, _) =>
                {
                    if (!line.TriggerCameraShake) return;
                    float amplitude = line.ShakeAmplitudeOverride > 0f ? line.ShakeAmplitudeOverride : 0.6f;
                    StoryDirectorRunner.Instance.Run(CameraShakeAction.Execute(0.9f, amplitude));
                });
        }

        private IEnumerator Run_Field_PhaseShifterIntro()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Field_PhaseShifterIntro 开始");

            var lines = LoadStoryLines(phaseShifterSequenceResource, DefaultPhaseShifterLines);
            yield return PlayLinesWithCameraShake(lines);

            var qm = QuestSystem.QuestManager.Instance;
            if (qm != null)
            {
                qm.CompleteObjective("q.field.phase.enter_field");
            }

            SetFlag("story.field.phase_intro");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Field_PhaseShifterIntro 结束");
        }

        private IEnumerator Run_Chapter4_FieldSequence()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Chapter4_FieldSequence 开始");

            var lines = LoadStoryLines(chapter4FieldSequenceResource, DefaultChapter4FieldLines);
            yield return PlayLinesWithCameraShake(lines);

            QuestSystem.QuestManager.Instance?.MarkChapter4SampleIntroPlayed();
            SetFlag("story.chapter4.sample_intro");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Chapter4_FieldSequence 结束");
        }

        public void MarkChapter4FieldIntroPlayed()
        {
            SetFlag("story.chapter4.sample_intro");
        }

        private IEnumerator Run_Chapter5_FieldSequence()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Chapter5_FieldSequence 开始");

            var lines = LoadStoryLines(chapter5FieldSequenceResource, DefaultChapter5FieldLines);
            yield return PlayLinesWithCameraShake(lines);

            var qm = QuestSystem.QuestManager.Instance;
            if (qm != null)
            {
                qm.CompleteObjective("q.chapter5.field.enter_field");
            }

            SetFlag("story.chapter5.field");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Chapter5_FieldSequence 结束");
        }

        private IEnumerator Run_Laboratory_Return()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Return 开始");

            var lines = LoadStoryLines(labReturnSequenceResource, DefaultLabReturnLines);
            yield return PlayLinesWithCameraShake(lines);

            var qm = QuestSystem.QuestManager.Instance;
            if (qm != null)
            {
                qm.CompleteObjective("q.lab.return.enter_lab");
            }

            SetFlag("story.lab.return");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Return 结束");
        }

        private IEnumerator Run_Laboratory_Return_Chapter4()
        {
            _isRunningCinematic = true;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Return_Chapter4 开始");

            var lines = LoadStoryLines(chapter4ReturnSequenceResource, DefaultChapter4ReturnLines);
            yield return PlayLinesWithCameraShake(lines);

            var qm = QuestSystem.QuestManager.Instance;
            if (qm != null)
            {
                qm.CompleteObjective("q.chapter4.return.enter_lab");
            }

            SetFlag("story.chapter4.return");
            _isRunningCinematic = false;
            if (enableDebugLog) Debug.Log("[StoryDirector] Run_Laboratory_Return_Chapter4 结束");
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
            var seed = UnityEngine.Random.value * 1000f;
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
        public static IEnumerator ExecuteWithShakeFacingFront(IReadOnlyList<SubtitleUI.SubtitleLine> lines, float shakeAmplitude)
        {
            var player = GameObject.Find("Lily");
            var mainCam = Camera.main;
            const float cameraDistance = 1.8f;
            const float cameraHeight = 1.6f;
            const float focusHeight = 1.1f; // chest-level focus so the shot tilts slightly downward

            // 禁用玩家控制（有则禁用）
            var fpc = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            bool fpcPrev = fpc != null && fpc.enabled;
            if (fpc != null) fpc.enabled = false;

            if (player == null || mainCam == null)
            {
                // 兜底：仅播放字幕
                yield return SubtitleUI.ShowSequence(lines);
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
            bool subtitlesFinished = lines == null || lines.Count == 0;
            bool shakeActive = false;
            float activeShakeAmplitude = shakeAmplitude;
            var subtitleRoutine = SubtitleUI.ShowSequence(
                lines,
                () => subtitlesFinished = true,
                (line, index) =>
                {
                    if (!shakeActive && line.TriggerCameraShake)
                    {
                        shakeActive = true;
                        if (line.ShakeAmplitudeOverride > 0f)
                        {
                            activeShakeAmplitude = line.ShakeAmplitudeOverride;
                        }
                    }
                });
            var subtitleRunner = StoryDirectorRunner.Instance; // 用于驱动并行协程
            subtitleRunner.Run(subtitleRoutine);

            // 抖动噪声种子
            float seed = UnityEngine.Random.value * 1000f;
            Vector3 desiredPos = target.position;
            // 初始化镜头位置避免第一帧跳变
            var initialFocus = target.position + Vector3.up * focusHeight;
            cam.transform.position = target.position + Vector3.up * cameraHeight + target.forward * cameraDistance;
            cam.transform.LookAt(initialFocus);

            while (!subtitlesFinished)
            {
                // 角色正面：在角色前方一定距离，看向头部
                var focus = target.position + Vector3.up * focusHeight;
                var inFront = target.position + Vector3.up * cameraHeight + target.forward * cameraDistance;
                desiredPos = inFront;

                // 叠加震动（对当前镜头生效）
                Vector3 shake = Vector3.zero;
                if (shakeActive && activeShakeAmplitude > 0f)
                {
                    float x = (Mathf.PerlinNoise(seed, Time.time * 10f) - 0.5f) * 2f * activeShakeAmplitude * 0.6f;
                    float y = (Mathf.PerlinNoise(Time.time * 10f, seed) - 0.5f) * 2f * activeShakeAmplitude;
                    shake = new Vector3(x, y, 0f);
                }

                var currentPos = cam.transform.position == Vector3.zero ? desiredPos : cam.transform.position;
                cam.transform.position = Vector3.Lerp(currentPos, desiredPos, 0.35f) + shake;
                cam.transform.LookAt(focus);
                yield return null;
            }

            // 清理
            UnityEngine.Object.Destroy(cine);
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
        private static int activeSequenceCount;
        private static int inputBlockReleaseFrame = -1;

        /// <summary>
        /// 是否有剧情对话框正在显示。
        /// </summary>
        public static bool IsDialogOpen => activeSequenceCount > 0;

        /// <summary>
        /// 玩家输入是否应该被阻止（对话进行中或刚关闭的下一帧）。
        /// </summary>
        public static bool IsPlayerInputBlocked =>
            IsDialogOpen || Time.frameCount <= inputBlockReleaseFrame;

        private static void MarkDialogOpened()
        {
            activeSequenceCount++;
        }

        private static void MarkDialogClosed()
        {
            activeSequenceCount = Mathf.Max(0, activeSequenceCount - 1);
            inputBlockReleaseFrame = Time.frameCount + 1;
        }

        public struct SubtitleLine
        {
            public string Speaker;
            public string Text;
            public bool TriggerCameraShake;
            public float ShakeAmplitudeOverride;

            public SubtitleLine(string speaker, string text, bool triggerCameraShake = false, float shakeAmplitudeOverride = 0f)
            {
                Speaker = speaker;
                Text = text ?? string.Empty;
                TriggerCameraShake = triggerCameraShake;
                ShakeAmplitudeOverride = shakeAmplitudeOverride;
            }

            public SubtitleLine(string text, bool triggerCameraShake = false, float shakeAmplitudeOverride = 0f)
                : this(string.Empty, text, triggerCameraShake, shakeAmplitudeOverride)
            {
            }
        }

        public static IEnumerator ShowSequence(
            IReadOnlyList<SubtitleLine> lines,
            System.Action onComplete = null,
            System.Action<SubtitleLine, int> onLineDisplayed = null)
        {
            if (lines == null || lines.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            MarkDialogOpened();

            var canvasGO = new GameObject("SubtitleCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            EnsureEventSystem(canvasGO.transform);

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.1f, 0.08f);
            bgRt.anchorMax = new Vector2(0.9f, 0.22f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);

            var button = bg.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bgImg;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(bg.transform, false);
            var tr = textGO.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            var txt = textGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.UpperLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            tr.offsetMin = new Vector2(24f, 32f);
            tr.offsetMax = new Vector2(-24f, -64f);

            var speakerGO = new GameObject("Speaker");
            speakerGO.transform.SetParent(bg.transform, false);
            var speakerRt = speakerGO.AddComponent<RectTransform>();
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0.5f, 1f);
            speakerRt.anchoredPosition = new Vector2(0f, 0f);
            speakerRt.offsetMin = new Vector2(24f, -40f);
            speakerRt.offsetMax = new Vector2(-24f, -12f);
            var speakerTxt = speakerGO.AddComponent<Text>();
            speakerTxt.font = txt.font;
            speakerTxt.fontSize = 24;
            speakerTxt.color = new Color(1f, 1f, 1f, 0.85f);
            speakerTxt.alignment = TextAnchor.UpperLeft;
            speakerTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            speakerTxt.verticalOverflow = VerticalWrapMode.Overflow;

            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(bg.transform, false);
            var hintRt = hintGO.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 6f);
            hintRt.sizeDelta = new Vector2(0f, 24f);
            var hintTxt = hintGO.AddComponent<Text>();
            hintTxt.font = txt.font;
            hintTxt.fontSize = 20;
            hintTxt.color = new Color(1f, 1f, 1f, 0.7f);
            hintTxt.alignment = TextAnchor.MiddleCenter;
            hintTxt.text = "ダイアログをクリックして続ける";

            bool advanceRequested = false;
            void RequestAdvance() => advanceRequested = true;
            button.onClick.AddListener(RequestAdvance);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                onLineDisplayed?.Invoke(line, i);
                speakerGO.SetActive(!string.IsNullOrEmpty(line.Speaker));
                speakerTxt.text = line.Speaker ?? string.Empty;
                txt.text = line.Text ?? string.Empty; // 可后续替换为本地化Key
                advanceRequested = false;
                yield return null; // 避免前一次点击连带跳过

                while (!advanceRequested)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        advanceRequested = true;
                        break;
                    }
                    yield return null;
                }
            }

            button.onClick.RemoveListener(RequestAdvance);
            MarkDialogClosed();
            
            // Delay destruction to next frame to avoid Assertion failed errors in ProcessEvent
            StoryDirectorRunner.Instance.Run(DestroyCanvasNextFrame(canvasGO));
            
            onComplete?.Invoke();
        }

        private static IEnumerator DestroyCanvasNextFrame(GameObject canvasGO)
        {
            yield return null;
            if (canvasGO != null)
            {
                UnityEngine.Object.Destroy(canvasGO);
            }
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null) return;

            var es = new GameObject("EventSystem");
            if (parent != null)
            {
                es.transform.SetParent(parent, false);
            }
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
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
