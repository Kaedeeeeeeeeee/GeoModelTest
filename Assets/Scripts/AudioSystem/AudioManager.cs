using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoModel.AudioSystem
{
    /// <summary>
    /// 集中式音频管理器。单例 + DontDestroyOnLoad，参照 LocalizationManager 模式。
    ///
    /// 公开 API：
    ///   PlayBGM(key, fade)         切换 BGM 并交叉淡化
    ///   PlaySFX(key)               2D 音效（短促，UI 类）
    ///   PlaySFX3D(key, pos)        3D 空间音效（钻探/锤击等）
    ///   PlayUI(key)                走 UI 通道
    ///   StartLoopSFX3D(key, t)     返回 AudioSource，挂在 transform 上循环
    ///   StopLoop(source, fade)     淡出并销毁循环源
    ///   SetVolume(channel, 0..1)   设置音量，写入 PlayerPrefs
    ///   GetVolume(channel)
    ///
    /// 资源约定：
    ///   BGM 文件放 Resources/Audio/BGM/<key>.{mp3,ogg,wav}
    ///   SFX 文件放 Resources/Audio/SFX/<key>.{ogg,wav}（启动时整目录预加载）
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region 单例
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 字段
        [Header("默认音量")]
        [Range(0f, 1f)] public float defaultMaster = 0.8f;
        [Range(0f, 1f)] public float defaultBGM    = 0.55f;
        [Range(0f, 1f)] public float defaultSFX    = 0.85f;
        [Range(0f, 1f)] public float defaultUI     = 0.8f;

        [Header("淡入淡出")]
        public float defaultBgmFade = 1.5f;

        [Header("3D 默认参数")]
        public float sfx3DMinDistance = 1f;
        public float sfx3DMaxDistance = 30f;

        [Header("调试")]
        public bool enableDebugLog = false;

        // 音量字典（线性 0-1）
        private readonly Dictionary<AudioChannel, float> volumes = new();

        // BGM 双源交叉淡化
        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource activeBgmSource;
        private bool useSourceA = true;
        private Coroutine bgmFadeCoroutine;
        private string currentBgmKey = "";

        // UI 通道源（2D，忽略暂停）
        private AudioSource uiSource;

        // SFX 2D 池
        private readonly List<AudioSource> sfxPool = new();
        private const int SfxPoolSize = 8;

        // 缓存
        private readonly Dictionary<string, AudioClip> sfxCache = new();
        private readonly Dictionary<string, AudioClip> bgmCache = new();

        // 场景 → BGM 映射
        private static readonly Dictionary<string, string> SceneBgmMap = new()
        {
            { "MainScene",        AudioKeys.BGM.Field },
            { "Laboratory Scene", AudioKeys.BGM.Lab   }
        };

        private bool initialized;
        #endregion

        #region Unity 生命周期
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            // 强制在第一个场景加载前创建实例，确保 sceneLoaded 事件能被捕获
            var _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Init();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion

        #region 初始化
        private void Init()
        {
            if (initialized) return;

            // 默认音量
            volumes[AudioChannel.Master] = defaultMaster;
            volumes[AudioChannel.BGM]    = defaultBGM;
            volumes[AudioChannel.SFX]    = defaultSFX;
            volumes[AudioChannel.UI]     = defaultUI;

            LoadVolumesFromPrefs();
            BuildSources();
            PreloadSFX();

            initialized = true;
            Log($"初始化完成。SFX 缓存 {sfxCache.Count} 条。");

            // 启动时给当前场景一个机会触发 BGM（Awake 早于 sceneLoaded 事件订阅时机）
            EnsureAudioListener();
            string sceneName = SceneManager.GetActiveScene().name;
            TryPlaySceneBgm(sceneName);
        }

        private void BuildSources()
        {
            bgmSourceA = CreateSource("BGM_A", loop: true, ignorePause: true);
            bgmSourceB = CreateSource("BGM_B", loop: true, ignorePause: true);
            activeBgmSource = bgmSourceA;

            uiSource = CreateSource("UI", loop: false, ignorePause: true);

            for (int i = 0; i < SfxPoolSize; i++)
            {
                var src = CreateSource($"SFX_{i}", loop: false, ignorePause: false);
                sfxPool.Add(src);
            }
        }

        private AudioSource CreateSource(string name, bool loop, bool ignorePause)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = ignorePause;
            return src;
        }

        private void PreloadSFX()
        {
            var clips = Resources.LoadAll<AudioClip>("Audio/SFX");
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                sfxCache[clip.name] = clip;
            }
        }
        #endregion

        #region BGM
        public void PlayBGM(string key, float fadeDuration = -1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                StopBGM(fadeDuration);
                return;
            }

            if (key == currentBgmKey && activeBgmSource != null && activeBgmSource.isPlaying)
            {
                return;
            }

            if (fadeDuration < 0f) fadeDuration = defaultBgmFade;

            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(key, fadeDuration));
        }

        public void StopBGM(float fadeDuration = -1f)
        {
            if (fadeDuration < 0f) fadeDuration = defaultBgmFade;
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
            currentBgmKey = "";
        }

        private IEnumerator CrossfadeBGM(string key, float duration)
        {
            // 加载 clip（带缓存）
            AudioClip clip;
            if (!bgmCache.TryGetValue(key, out clip))
            {
                var request = Resources.LoadAsync<AudioClip>($"Audio/BGM/{key}");
                yield return request;
                clip = request.asset as AudioClip;
                if (clip == null)
                {
                    Warn($"BGM clip 未找到: Audio/BGM/{key}");
                    yield break;
                }
                bgmCache[key] = clip;
            }

            var newSource = useSourceA ? bgmSourceB : bgmSourceA;
            var oldSource = useSourceA ? bgmSourceA : bgmSourceB;
            useSourceA = !useSourceA;

            float targetVol = EffectiveBgmVolume();
            float oldStartVol = oldSource.volume;

            newSource.clip = clip;
            newSource.volume = 0f;
            newSource.Play();

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float n = t / duration;
                newSource.volume = Mathf.Lerp(0f, targetVol, n);
                if (oldSource.isPlaying) oldSource.volume = Mathf.Lerp(oldStartVol, 0f, n);
                yield return null;
            }

            newSource.volume = targetVol;
            oldSource.Stop();
            oldSource.clip = null;
            activeBgmSource = newSource;
            currentBgmKey = key;
            bgmFadeCoroutine = null;

            Log($"BGM 切换到 {key}");
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startA = bgmSourceA.volume;
            float startB = bgmSourceB.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float n = 1f - (t / duration);
                bgmSourceA.volume = startA * n;
                bgmSourceB.volume = startB * n;
                yield return null;
            }
            bgmSourceA.Stop();
            bgmSourceB.Stop();
            bgmFadeCoroutine = null;
        }
        #endregion

        #region SFX
        public void PlaySFX(string key, float volumeScale = 1f)
        {
            if (!sfxCache.TryGetValue(key, out var clip))
            {
                Warn($"SFX 未找到: {key}");
                return;
            }

            var src = GetFreeSfxSource();
            src.transform.position = Vector3.zero;
            src.spatialBlend = 0f;
            src.volume = volumeScale * EffectiveSfxVolume();
            src.PlayOneShot(clip);
        }

        public void PlaySFX3D(string key, Vector3 position, float volumeScale = 1f)
        {
            if (!sfxCache.TryGetValue(key, out var clip))
            {
                Warn($"SFX 未找到: {key}");
                return;
            }

            var src = GetFreeSfxSource();
            src.transform.position = position;
            src.spatialBlend = 1f;
            src.minDistance = sfx3DMinDistance;
            src.maxDistance = sfx3DMaxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.volume = volumeScale * EffectiveSfxVolume();
            src.PlayOneShot(clip);
        }

        public void PlaySFXRandom(string[] keys, Vector3 position, float volumeScale = 1f)
        {
            if (keys == null || keys.Length == 0) return;
            var k = keys[Random.Range(0, keys.Length)];
            PlaySFX3D(k, position, volumeScale);
        }

        public void PlayUI(string key, float volumeScale = 1f)
        {
            if (!sfxCache.TryGetValue(key, out var clip))
            {
                Warn($"UI SFX 未找到: {key}");
                return;
            }
            uiSource.PlayOneShot(clip, volumeScale * EffectiveUiVolume());
        }

        /// <summary>
        /// 开启循环 3D 音效（钻探/引擎类）。挂在 attachTo 下，返回 AudioSource 供调用方持有。
        /// </summary>
        public AudioSource StartLoopSFX3D(string key, Transform attachTo, float volumeScale = 1f)
        {
            if (!sfxCache.TryGetValue(key, out var clip))
            {
                Warn($"Loop SFX 未找到: {key}");
                return null;
            }

            var go = new GameObject($"LoopSFX_{key}");
            if (attachTo != null) go.transform.SetParent(attachTo);
            go.transform.localPosition = Vector3.zero;

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;
            src.minDistance = sfx3DMinDistance;
            src.maxDistance = sfx3DMaxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.playOnAwake = false;
            src.volume = volumeScale * EffectiveSfxVolume();
            src.Play();
            return src;
        }

        public void StopLoop(AudioSource source, float fadeOut = 0.3f)
        {
            if (source == null) return;
            StartCoroutine(FadeOutAndDestroy(source, fadeOut));
        }

        private IEnumerator FadeOutAndDestroy(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float startVol = src.volume;
            float t = 0f;
            while (t < duration && src != null)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            foreach (var src in sfxPool)
            {
                if (!src.isPlaying) return src;
            }
            // 全在播：复用第一个（声音会被打断，但优于丢失）
            return sfxPool[0];
        }
        #endregion

        #region 音量
        public void SetVolume(AudioChannel channel, float value01)
        {
            float v = Mathf.Clamp01(value01);
            volumes[channel] = v;
            PlayerPrefs.SetFloat(PrefKey(channel), v);
            PlayerPrefs.Save();
            ApplyLiveVolumes();
        }

        public float GetVolume(AudioChannel channel)
        {
            return volumes.TryGetValue(channel, out var v) ? v : 0.8f;
        }

        private void LoadVolumesFromPrefs()
        {
            foreach (AudioChannel ch in System.Enum.GetValues(typeof(AudioChannel)))
            {
                string key = PrefKey(ch);
                if (PlayerPrefs.HasKey(key))
                {
                    volumes[ch] = PlayerPrefs.GetFloat(key);
                }
            }
        }

        private void ApplyLiveVolumes()
        {
            // 实时把音量应用到正在播放的 BGM 与 UI 源
            float bgmVol = EffectiveBgmVolume();
            if (bgmSourceA != null && bgmSourceA.isPlaying && bgmFadeCoroutine == null) bgmSourceA.volume = bgmVol;
            if (bgmSourceB != null && bgmSourceB.isPlaying && bgmFadeCoroutine == null) bgmSourceB.volume = bgmVol;
            // SFX/UI 是 one-shot，新触发的会用新音量，无需更新已在播的
        }

        private float EffectiveBgmVolume() => GetVolume(AudioChannel.Master) * GetVolume(AudioChannel.BGM);
        private float EffectiveSfxVolume() => GetVolume(AudioChannel.Master) * GetVolume(AudioChannel.SFX);
        private float EffectiveUiVolume()  => GetVolume(AudioChannel.Master) * GetVolume(AudioChannel.UI);

        private static string PrefKey(AudioChannel ch) => $"audio.{ch.ToString().ToLower()}";
        #endregion

        #region 场景集成
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryPlaySceneBgm(scene.name);
        }

        private void TryPlaySceneBgm(string sceneName)
        {
            EnsureAudioListener();
            if (SceneBgmMap.TryGetValue(sceneName, out var bgmKey))
            {
                PlayBGM(bgmKey);
            }
        }

        /// <summary>
        /// 确保场景中有且仅有一个 AudioListener（自动挂到 Main Camera）。
        /// 避免手编 .unity scene YAML。
        /// </summary>
        private void EnsureAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length > 0) return;

            var cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Log($"AudioListener 自动挂到 {cam.name}");
            }
            else
            {
                Warn("场景中找不到摄像机，无法添加 AudioListener");
            }
        }

        /// <summary>剧情触发时手动调，结束后用 ResumeSceneBgm 恢复。</summary>
        public void PlayStoryBgm()
        {
            PlayBGM(AudioKeys.BGM.Story);
        }

        public void ResumeSceneBgm()
        {
            TryPlaySceneBgm(SceneManager.GetActiveScene().name);
        }
        #endregion

        #region 调试
        private void Log(string msg)
        {
            if (enableDebugLog) Debug.Log($"[AudioManager] {msg}");
        }

        private void Warn(string msg)
        {
            Debug.LogWarning($"[AudioManager] {msg}");
        }
        #endregion
    }
}
