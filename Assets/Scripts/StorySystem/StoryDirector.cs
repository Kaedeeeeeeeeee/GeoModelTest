using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Core;
using GeoModel.AudioSystem;

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
            // 强制日志（不受 enableDebugLog 控制），方便 WebGL 用户从 Console 直接定位
            Debug.Log($"[StoryDirector] OnSceneLoaded: {sceneName} | flags={string.Join(",", _flags)} | _isRunningCinematic={_isRunningCinematic}");

            // 场景1：首次进入 MainScene → 地震演出 + 对白
            if (sceneName == "MainScene")
            {
                bool hasMainRescueFlag = HasFlag("story.main.rescue");
                Debug.Log($"[StoryDirector] MainScene 分支 | story.main.rescue flag={hasMainRescueFlag}");
                if (!hasMainRescueFlag)
                {
                    if (!_isRunningCinematic)
                    {
                        Debug.Log("[StoryDirector] 启动 Run_MainScene_Rescue 协程");
                        StartCoroutine(Run_MainScene_Rescue());
                    }
                    else
                    {
                        Debug.LogWarning("[StoryDirector] MainScene 演出被 _isRunningCinematic=true 阻止");
                    }
                    return;
                }
                Debug.Log("[StoryDirector] story.main.rescue flag 已设置，跳过演出（如要重播：URL 加 ?resetstory=1）");
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

        /// <summary>
        /// 强制从 PlayerPrefs 重新载入剧情 flags。给 ProgressResetService.ResetAll() 用：
        /// StoryDirector 是 DontDestroyOnLoad 单例，启动时已 load 过 flags 到 _flags HashSet；
        /// 即便外部清掉 PlayerPrefs key，内存里的 _flags 也不变 → HasFlag 仍返回 true → 跳过演出。
        /// 调这个方法可以让 _flags 重新跟 PlayerPrefs 对齐（如果 key 不存在，结果是空 HashSet）。
        /// </summary>
        public void ReloadFlags()
        {
            _flags = ProgressPersistence.LoadFlags();
            _isRunningCinematic = false; // 顺便重置正在演出的标志，防止某种残留
            Debug.Log($"[StoryDirector] ReloadFlags 完成，当前 flags 数量: {_flags.Count}");
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
            QuizScoreManager.Instance.Reset(); // 新一轮游玩开始，清空测验成绩
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

        /// <summary>
        /// 显示结尾调查报告（A/B/C 评级 + 正解数），用于最终 beat 结束时。
        /// </summary>
        public void PlayReport(Action onComplete = null)
        {
            StartCoroutine(SubtitleUI.ShowReport(onComplete));
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
                    float volumeScale = Mathf.Clamp01(amplitude);
                    PersistentEarthquakeController.Instance?.PlayEarthquakeSfxOneShot(volumeScale);
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
                        float volumeScale = Mathf.Clamp01(
                            line.ShakeAmplitudeOverride > 0f ? line.ShakeAmplitudeOverride : shakeAmplitude);
                        PersistentEarthquakeController.Instance?.PlayEarthquakeSfxOneShot(volumeScale);
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

        /// <summary>
        /// 运行时已解析的选择题选项（文本已本地化）。
        /// </summary>
        public class SubtitleChoice
        {
            public string Text;
            public bool IsCorrect;
            public string Feedback;
        }

        public struct SubtitleLine
        {
            public string Speaker;
            public string Text;
            public bool TriggerCameraShake;
            public float ShakeAmplitudeOverride;

            // 选择题支持（可选）：QuestionId 用于计分/报告，Choices 为 null/空表示普通线性台词。
            public string QuestionId;
            public System.Collections.Generic.List<SubtitleChoice> Choices;
            public string Hint; // 选择题"ヒント"按钮显示的提示文本（可选）

            public bool HasChoices => Choices != null && Choices.Count > 0;
            public bool HasHint => !string.IsNullOrEmpty(Hint);

            public SubtitleLine(string speaker, string text, bool triggerCameraShake = false, float shakeAmplitudeOverride = 0f)
            {
                Speaker = speaker;
                Text = text ?? string.Empty;
                TriggerCameraShake = triggerCameraShake;
                ShakeAmplitudeOverride = shakeAmplitudeOverride;
                QuestionId = null;
                Choices = null;
                Hint = null;
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

            // 立绘槽（要在 bg 之前创建，让对话框渲染在立绘之上）
            LoadPortraits();
            Image portraitLeft = _spritePlayer != null ? CreatePortrait(canvasGO.transform, _spritePlayer, false) : null;
            Image portraitRight = _spriteKaede != null ? CreatePortrait(canvasGO.transform, _spriteKaede, true) : null;
            bool leftAppeared = false;
            bool rightAppeared = false;

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
            var txt = textGO.AddComponent<TextMeshProUGUI>();
            ApplyTmpFont(txt);
            txt.fontSize = 34;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.lineSpacing = RubyLineSpacing; // 给头顶注音留行距
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
            var speakerTxt = speakerGO.AddComponent<TextMeshProUGUI>();
            ApplyTmpFont(speakerTxt);
            speakerTxt.fontSize = 24;
            speakerTxt.color = new Color(1f, 1f, 1f, 0.85f);
            speakerTxt.alignment = TextAlignmentOptions.TopLeft;

            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(bg.transform, false);
            var hintRt = hintGO.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 6f);
            hintRt.sizeDelta = new Vector2(0f, 24f);
            var hintTxt = hintGO.AddComponent<TextMeshProUGUI>();
            ApplyTmpFont(hintTxt);
            hintTxt.fontSize = 20;
            hintTxt.color = new Color(1f, 1f, 1f, 0.7f);
            hintTxt.alignment = TextAlignmentOptions.Center;
            string hintContinue = FuriganaProcessor.Process(LocalizedOr("ui.dialog.continue", "ダイアログをクリックして続ける"));
            hintTxt.text = hintContinue;

            bool advanceRequested = false;
            void RequestAdvance() => advanceRequested = true;
            button.onClick.AddListener(RequestAdvance);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                onLineDisplayed?.Invoke(line, i);

                // 立绘 alpha 渐变：当前说话者全显示，已露过脸但不在说话则半透明
                var spKind = ClassifySpeaker(line.Speaker);
                if (portraitLeft != null)
                {
                    if (spKind == SpeakerKind.Player) leftAppeared = true;
                    float targetL = leftAppeared ? (spKind == SpeakerKind.Player ? 1f : 0.55f) : 0f;
                    if (_fadeLeft != null) StoryDirectorRunner.Instance.StopCoroutine(_fadeLeft);
                    _fadeLeft = StoryDirectorRunner.Instance.StartCoroutine(FadeAlpha(portraitLeft, targetL, PortraitFadeSec));
                }
                if (portraitRight != null)
                {
                    if (spKind == SpeakerKind.Kaede) rightAppeared = true;
                    float targetR = rightAppeared ? (spKind == SpeakerKind.Kaede ? 1f : 0.55f) : 0f;
                    if (_fadeRight != null) StoryDirectorRunner.Instance.StopCoroutine(_fadeRight);
                    _fadeRight = StoryDirectorRunner.Instance.StartCoroutine(FadeAlpha(portraitRight, targetR, PortraitFadeSec));
                }

                speakerGO.SetActive(!string.IsNullOrEmpty(line.Speaker));
                speakerTxt.text = FuriganaProcessor.Process(line.Speaker ?? string.Empty);
                txt.text = FuriganaProcessor.Process(line.Text ?? string.Empty);
                advanceRequested = false;
                yield return null; // 避免前一次点击连带跳过

                // 选择题分支：显示答案按钮，记录得分，再播放反馈
                if (line.HasChoices)
                {
                    // E: 答错可重答。已错过的选项灰掉禁用；最终必须答对才推进。
                    var disabledIdx = new HashSet<int>();
                    bool correctlyAnswered = false;
                    button.interactable = false; // 选择期间屏蔽背景点击推进

                    while (!correctlyAnswered)
                    {
                        // 每次循环（含重答）恢复 speaker + prompt
                        speakerGO.SetActive(!string.IsNullOrEmpty(line.Speaker));
                        speakerTxt.text = FuriganaProcessor.Process(line.Speaker ?? string.Empty);
                        txt.text = FuriganaProcessor.Process(line.Text ?? string.Empty);

                        int picked = -1;
                        bool showHintRequested = false;
                        var choicePanel = BuildChoicePanel(
                            canvasGO.transform, line.Choices, disabledIdx,
                            idx => picked = idx,
                            line.HasHint ? () => showHintRequested = true : (System.Action)null);
                        hintTxt.text = FuriganaProcessor.Process(LocalizedOr("ui.dialog.choose", "答えをえらんでね"));

                        while (picked < 0 && !showHintRequested)
                        {
                            if (Input.GetKeyDown(KeyCode.Alpha1) && line.Choices.Count >= 1 && !disabledIdx.Contains(0)) picked = 0;
                            else if (Input.GetKeyDown(KeyCode.Alpha2) && line.Choices.Count >= 2 && !disabledIdx.Contains(1)) picked = 1;
                            else if (Input.GetKeyDown(KeyCode.Alpha3) && line.Choices.Count >= 3 && !disabledIdx.Contains(2)) picked = 2;
                            else if (Input.GetKeyDown(KeyCode.Alpha4) && line.Choices.Count >= 4 && !disabledIdx.Contains(3)) picked = 3;
                            yield return null;
                        }

                        if (choicePanel != null) UnityEngine.Object.Destroy(choicePanel);

                        // D: 点了 ヒント → 显示提示，按推进键后重建选择面板（不计分、不算尝试）
                        if (showHintRequested)
                        {
                            speakerGO.SetActive(false);
                            txt.text = FuriganaProcessor.Process(line.Hint);
                            hintTxt.text = hintContinue;
                            advanceRequested = false;
                            yield return null;
                            while (!advanceRequested)
                            {
                                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                                {
                                    advanceRequested = true;
                                    break;
                                }
                                yield return null;
                            }
                            continue;
                        }

                        var chosen = line.Choices[Mathf.Clamp(picked, 0, line.Choices.Count - 1)];
                        QuizScoreManager.Instance.Record(line.QuestionId, chosen.IsCorrect); // dedup → 最终算最后一次
                        if (chosen.IsCorrect)
                            AudioManager.Instance?.PlaySFX(AudioKeys.SFX.SamplePickup);
                        else
                            AudioManager.Instance?.PlayUI(AudioKeys.UI.PanelClose);

                        string feedback = !string.IsNullOrEmpty(chosen.Feedback)
                            ? chosen.Feedback
                            : LocalizedOr(chosen.IsCorrect ? "quiz.correct" : "quiz.incorrect",
                                          chosen.IsCorrect ? "正解（せいかい）！" : "ざんねん、もう一度（いちど）かんがえてみよう。");

                        speakerGO.SetActive(false);
                        txt.text = FuriganaProcessor.Process(feedback);
                        hintTxt.text = hintContinue;
                        advanceRequested = false;
                        yield return null;
                        while (!advanceRequested)
                        {
                            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                            {
                                advanceRequested = true;
                                break;
                            }
                            yield return null;
                        }

                        if (chosen.IsCorrect)
                            correctlyAnswered = true;
                        else
                            disabledIdx.Add(picked);
                    }

                    button.interactable = true;
                    continue;
                }

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

            // 停掉挂着的立绘渐变协程，避免 Canvas/Image 销毁后悬挂
            if (_fadeLeft != null) { StoryDirectorRunner.Instance.StopCoroutine(_fadeLeft); _fadeLeft = null; }
            if (_fadeRight != null) { StoryDirectorRunner.Instance.StopCoroutine(_fadeRight); _fadeRight = null; }

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

        // 头顶注音(ruby)需要额外的行距留白；看到效果后可微调。
        private const float RubyLineSpacing = 24f;

        private static TMP_FontAsset _tmpFont;
        private static bool _tmpFontTried;

        /// <summary>
        /// 运行时从内嵌的 NotoSansCJKjp 动态生成 TMP 字体资源（Dynamic SDF），
        /// 这样无需在编辑器里手动烘焙字体资源。失败则回退到 TMP 默认字体。
        /// </summary>
        private static TMP_FontAsset GetTmpFont()
        {
            if (_tmpFont != null) return _tmpFont;
            if (_tmpFontTried) return _tmpFont;
            _tmpFontTried = true;

            try
            {
                var src = UIFontResolver.GetUIFont();
                if (src != null)
                {
                    _tmpFont = TMP_FontAsset.CreateFontAsset(src);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SubtitleUI] 运行时创建 TMP 字体失败，回退默认字体: {e.Message}");
            }

            if (_tmpFont == null && TMP_Settings.defaultFontAsset != null)
            {
                _tmpFont = TMP_Settings.defaultFontAsset;
            }
            return _tmpFont;
        }

        private static void ApplyTmpFont(TMP_Text t)
        {
            var f = GetTmpFont();
            if (f != null) t.font = f;
            t.richText = true;
        }

        // ===== 立绘 =====

        private static Sprite _spritePlayer;
        private static Sprite _spriteKaede;
        private static bool _portraitsTried;

        private enum SpeakerKind { Other, Player, Kaede }

        /// <summary>
        /// 从 Assets/Resources/Tachie/ 加载立绘。优先 Sprite 导入；若以 Texture2D 导入则运行时包成 Sprite。
        /// </summary>
        private static void LoadPortraits()
        {
            if (_portraitsTried) return;
            _portraitsTried = true;
            try
            {
                _spritePlayer = Resources.Load<Sprite>("Tachie/player");
                if (_spritePlayer == null)
                {
                    var tex = Resources.Load<Texture2D>("Tachie/player");
                    if (tex != null) _spritePlayer = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                _spriteKaede = Resources.Load<Sprite>("Tachie/kaede");
                if (_spriteKaede == null)
                {
                    var tex = Resources.Load<Texture2D>("Tachie/kaede");
                    if (tex != null) _spriteKaede = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SubtitleUI] 立绘加载失败: {e.Message}");
            }
        }

        private static SpeakerKind ClassifySpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return SpeakerKind.Other;
            if (speaker.IndexOf("Kaede", System.StringComparison.OrdinalIgnoreCase) >= 0
                || speaker.IndexOf("カエデ", System.StringComparison.Ordinal) >= 0
                || speaker == "枫")
            {
                return SpeakerKind.Kaede;
            }
            if (speaker == "主人公" || speaker == "Lily" || speaker == "みなと"
                || speaker.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) >= 0
                || speaker.IndexOf("プレイヤー", System.StringComparison.Ordinal) >= 0)
            {
                return SpeakerKind.Player;
            }
            return SpeakerKind.Other;
        }

        /// <summary>
        /// 创建一个立绘槽：外层 RectMask2D 限定可见区，内层 Image 比容器高，向下溢出被遮住 → 只露上半身。
        /// </summary>
        private static Image CreatePortrait(Transform parent, Sprite sprite, bool right)
        {
            var go = new GameObject(right ? "PortraitRight" : "PortraitLeft");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(right ? 1f : 0f, 0f);
            rt.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rt.pivot = new Vector2(right ? 1f : 0f, 0f);
            rt.anchoredPosition = new Vector2(right ? -40f : 40f, 240f);
            rt.sizeDelta = new Vector2(480f, 600f);
            go.AddComponent<RectMask2D>();

            var inner = new GameObject("Image");
            inner.transform.SetParent(go.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 1f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(0f, 720f);
            var img = inner.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var c = img.color; c.a = 0f; img.color = c;
            // 右侧立绘水平镜像 → 让 Kaede 面朝左(主人公)，形成对视感
            if (right) inner.transform.localScale = new Vector3(-1f, 1f, 1f);
            return img;
        }

        // ===== 立绘 alpha 渐变 =====

        private const float PortraitFadeSec = 0.25f;
        private static Coroutine _fadeLeft, _fadeRight;

        /// <summary>
        /// 缓动立绘 alpha 到目标值；img 为 null（Canvas 已销毁）则安全退出。
        /// </summary>
        private static IEnumerator FadeAlpha(Image img, float target, float duration)
        {
            if (img == null) yield break;
            float start = img.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                if (img == null) yield break;
                var c = img.color;
                c.a = Mathf.Lerp(start, target, t / duration);
                img.color = c;
                yield return null;
            }
            if (img != null)
            {
                var fc = img.color; fc.a = target; img.color = fc;
            }
        }

        private static string LocalizedOr(string key, string fallback)
        {
            var lm = LocalizationManager.Instance;
            if (lm != null && lm.IsInitialized && !string.IsNullOrEmpty(key) && lm.HasText(key))
            {
                return lm.GetText(key);
            }
            return fallback;
        }

        /// <summary>
        /// 动态生成选择题答案按钮（垂直排列，叠在对话框上方）。点击或数字键 1-4 选择。
        /// </summary>
        private static GameObject BuildChoicePanel(Transform parent, List<SubtitleChoice> choices, HashSet<int> disabledIndices, System.Action<int> onPick, System.Action onHintClicked)
        {
            var panel = new GameObject("ChoicePanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.26f);
            rt.anchorMax = new Vector2(0.85f, 0.64f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                var btnGO = new GameObject($"Choice{i}");
                btnGO.transform.SetParent(panel.transform, false);
                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);

                var le = btnGO.AddComponent<LayoutElement>();
                le.minHeight = 52f;
                le.preferredHeight = 56f;

                bool isDisabled = disabledIndices != null && disabledIndices.Contains(idx);
                var btn = btnGO.AddComponent<Button>();
                btn.targetGraphic = img;
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.6f, 0.8f, 1f, 1f);
                colors.pressedColor = new Color(0.4f, 0.65f, 0.95f, 1f);
                btn.colors = colors;
                if (isDisabled)
                {
                    btn.interactable = false;
                    img.color = new Color(0.08f, 0.10f, 0.14f, 0.55f);
                }
                btn.onClick.AddListener(() => { AudioManager.Instance?.PlayUI(AudioKeys.UI.Click); onPick(idx); });

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var ltr = labelGO.AddComponent<RectTransform>();
                ltr.anchorMin = Vector2.zero;
                ltr.anchorMax = Vector2.one;
                ltr.offsetMin = new Vector2(18f, 4f);
                ltr.offsetMax = new Vector2(-18f, -4f);
                var label = labelGO.AddComponent<TextMeshProUGUI>();
                ApplyTmpFont(label);
                label.fontSize = 30;
                label.color = isDisabled ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.lineSpacing = RubyLineSpacing;
                label.text = FuriganaProcessor.Process($"{idx + 1}. {choices[idx].Text}");
            }

            // D: 可选「ヒント」按钮 (黄色, 小字号) — 仅当传入 onHintClicked
            if (onHintClicked != null)
            {
                var hintBtnGO = new GameObject("HintBtn");
                hintBtnGO.transform.SetParent(panel.transform, false);
                var hintImg = hintBtnGO.AddComponent<Image>();
                hintImg.color = new Color(0.85f, 0.72f, 0.30f, 0.92f);
                var hintLE = hintBtnGO.AddComponent<LayoutElement>();
                hintLE.minHeight = 42f;
                hintLE.preferredHeight = 46f;
                var hintBtn = hintBtnGO.AddComponent<Button>();
                hintBtn.targetGraphic = hintImg;
                var hintColors = hintBtn.colors;
                hintColors.normalColor = Color.white;
                hintColors.highlightedColor = new Color(0.95f, 0.85f, 0.50f, 1f);
                hintColors.pressedColor = new Color(0.70f, 0.58f, 0.22f, 1f);
                hintBtn.colors = hintColors;
                hintBtn.onClick.AddListener(() => { AudioManager.Instance?.PlayUI(AudioKeys.UI.Click); onHintClicked(); });

                var hintLabelGO = new GameObject("Label");
                hintLabelGO.transform.SetParent(hintBtnGO.transform, false);
                var hintLrt = hintLabelGO.AddComponent<RectTransform>();
                hintLrt.anchorMin = Vector2.zero;
                hintLrt.anchorMax = Vector2.one;
                hintLrt.offsetMin = new Vector2(14f, 2f);
                hintLrt.offsetMax = new Vector2(-14f, -2f);
                var hintLabel = hintLabelGO.AddComponent<TextMeshProUGUI>();
                ApplyTmpFont(hintLabel);
                hintLabel.fontSize = 22;
                hintLabel.color = new Color(0.20f, 0.15f, 0.05f, 1f);
                hintLabel.alignment = TextAlignmentOptions.Center;
                hintLabel.text = "ヒント (?)";
            }

            return panel;
        }

        /// <summary>
        /// 结尾调查报告界面：显示 A/B/C 评级与正解数。点击或空格/回车继续。
        /// </summary>
        public static IEnumerator ShowReport(System.Action onComplete = null)
        {
            MarkDialogOpened();

            var score = QuizScoreManager.Instance;
            string grade = score.Grade;

            var canvasGO = new GameObject("ReportCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
            EnsureEventSystem(canvasGO.transform);

            var dim = new GameObject("Dim");
            dim.transform.SetParent(canvasGO.transform, false);
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.75f);
            bool advance = false;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener(() => advance = true);

            var card = new GameObject("Card");
            card.transform.SetParent(canvasGO.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.25f, 0.22f);
            cardRt.anchorMax = new Vector2(0.75f, 0.78f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            CreateReportText(card.transform, LocalizedOr("report.title", "ちょうさ報告（ほうこく）"), 40, new Vector2(0f, 0.78f), new Vector2(1f, 0.98f));
            CreateReportText(card.transform, $"{LocalizedOr("report.grade", "そうごう評価（ひょうか）")}: {grade}", 64, new Vector2(0f, 0.42f), new Vector2(1f, 0.74f));
            CreateReportText(card.transform, $"{LocalizedOr("report.score", "せいかい数（すう）")}: {score.CorrectCount} / {score.Total}", 32, new Vector2(0f, 0.24f), new Vector2(1f, 0.40f));
            CreateReportText(card.transform, LocalizedOr("ui.dialog.continue", "ダイアログをクリックして続ける"), 22, new Vector2(0f, 0.04f), new Vector2(1f, 0.16f));

            yield return null;
            while (!advance)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) break;
                yield return null;
            }

            MarkDialogClosed();
            StoryDirectorRunner.Instance.Run(DestroyCanvasNextFrame(canvasGO));
            onComplete?.Invoke();
        }

        private static void CreateReportText(Transform parent, string content, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("ReportText");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(24f, 0f);
            rt.offsetMax = new Vector2(-24f, 0f);
            var t = go.AddComponent<TextMeshProUGUI>();
            ApplyTmpFont(t);
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.lineSpacing = RubyLineSpacing;
            t.text = FuriganaProcessor.Process(content);
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
