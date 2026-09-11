using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime holder for player-facing performance and control settings.
/// Keeps the UI, mobile auto-detection, and player controller on the same persisted values.
/// </summary>
public class GamePerformanceSettings : MonoBehaviour
{
    public const string ManualQualityEnabledKey = "GeoModel.ManualQualityEnabled";
    public const string QualityLevelKey = "GeoModel.QualityLevel";
    public const string LookSensitivityKey = "GeoModel.LookSensitivity";

    public const float DefaultLookSensitivity = 8f;
    public const float MinLookSensitivity = 2f;
    public const float MaxLookSensitivity = 18f;

    private static GamePerformanceSettings instance;
    private AutomaticQualityPolicy _automaticQuality;
    private bool _isMobile;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int GeoModel_IsPerformanceSampleEligible();
    [DllImport("__Internal")] private static extern void GeoModel_SetRenderResolution(int width, int height, int quality, int manual);
#endif

    public static event Action<float> LookSensitivityChanged;
    public static event Action<int, bool> QualityChanged;

    public static GamePerformanceSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GamePerformanceSettings>();
                if (instance == null)
                {
                    GameObject host = new GameObject("GamePerformanceSettings");
                    instance = host.AddComponent<GamePerformanceSettings>();
                }
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            _isMobile = MobileInputManager.IsRuntimeMobileDevice();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyQualityPreference(false);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool IsManualQualityEnabled => PlayerPrefs.GetInt(ManualQualityEnabledKey, 0) == 1;

    public int CurrentQualityLevel => QualitySettings.GetQualityLevel();
    public float MeasuredFramesPerSecond => _automaticQuality?.AverageFps ?? 0f;
    public int MicroscopePreviewSize => CurrentQualityLevel <= FindQualityLevel("Low", 1) ? 512 : 1024;
    public int MicroscopeAntiAliasing => CurrentQualityLevel <= FindQualityLevel("Medium", 2) ? 1 : 2;

    private void Update()
    {
        if (IsManualQualityEnabled || _automaticQuality == null) return;

        bool eligible = Application.isFocused && Time.timeScale > 0f &&
            !Core.GameInputState.GameplayBlocked && !GameSceneManager.IsLoadingScene &&
            !StorySystem.StoryDirector.IsStoryPlaybackActive &&
            SceneManager.GetActiveScene().name != "StartScene";
#if UNITY_WEBGL && !UNITY_EDITOR
        eligible = eligible && GeoModel_IsPerformanceSampleEligible() == 1;
#endif
        int nextLevel = _automaticQuality.Sample(Time.unscaledDeltaTime, eligible);
        if (nextLevel != CurrentQualityLevel)
        {
            ApplyQualityLevel(nextLevel, true);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _automaticQuality?.Suspend();
    }

    private void OnApplicationFocus(bool focused)
    {
        _automaticQuality?.Suspend();
    }

    private void OnApplicationPause(bool paused)
    {
        _automaticQuality?.Suspend();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    public int SavedManualQualityLevel
    {
        get
        {
            int fallback = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            return Mathf.Clamp(PlayerPrefs.GetInt(QualityLevelKey, fallback), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        }
    }

    public void SetManualQualityLevel(int qualityLevel)
    {
        int clampedLevel = ClampQualityLevel(qualityLevel);
        PlayerPrefs.SetInt(ManualQualityEnabledKey, 1);
        PlayerPrefs.SetInt(QualityLevelKey, clampedLevel);
        PlayerPrefs.Save();

        ApplyQualityLevel(clampedLevel, true);
    }

    public void SetAutoQuality()
    {
        PlayerPrefs.SetInt(ManualQualityEnabledKey, 0);
        PlayerPrefs.Save();
        _automaticQuality = null;
        ApplyQualityPreference(true);
    }

    public void ApplyQualityPreference(bool logChanges = true)
    {
        int qualityLevel = IsManualQualityEnabled ? SavedManualQualityLevel : GetRecommendedQualityLevel();
        ApplyQualityLevel(qualityLevel, logChanges);
    }

    public int GetRecommendedQualityLevel()
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0)
        {
            return 0;
        }

        if (_automaticQuality == null)
        {
            _automaticQuality = new AutomaticQualityPolicy(FindQualityLevel("Low", 1));
        }
        return _automaticQuality.CurrentLevel;
    }

    public string GetQualityDisplayName(int qualityLevel)
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0)
        {
            return "Quality";
        }

        int index = ClampQualityLevel(qualityLevel);
        return QualitySettings.names[index];
    }

    public static float LoadLookSensitivity(float fallback)
    {
        float saved = PlayerPrefs.GetFloat(LookSensitivityKey, fallback);
        return Mathf.Clamp(saved, MinLookSensitivity, MaxLookSensitivity);
    }

    public static void SaveLookSensitivity(float sensitivity)
    {
        float clamped = Mathf.Clamp(sensitivity, MinLookSensitivity, MaxLookSensitivity);
        PlayerPrefs.SetFloat(LookSensitivityKey, clamped);
        PlayerPrefs.Save();
        LookSensitivityChanged?.Invoke(clamped);
    }

    private void ApplyQualityLevel(int qualityLevel, bool logChanges)
    {
        int clampedLevel = ClampQualityLevel(qualityLevel);
        if (QualitySettings.GetQualityLevel() != clampedLevel)
        {
            QualitySettings.SetQualityLevel(clampedLevel, true);
        }

        ApplyRuntimeQualityOverrides(clampedLevel);
        QualityChanged?.Invoke(clampedLevel, IsManualQualityEnabled);

        if (logChanges)
        {
            string mode = IsManualQualityEnabled ? "Manual" : "Auto";
            Debug.Log($"[GamePerformanceSettings] Quality {mode}: {clampedLevel} ({GetQualityDisplayName(clampedLevel)}), targetFrameRate={Application.targetFrameRate}");
        }
    }

    private void ApplyRuntimeQualityOverrides(int qualityLevel)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = !IsManualQualityEnabled || _isMobile || qualityLevel <= FindQualityLevel("Medium", 2) ? 30 : 60;

        bool veryLow = qualityLevel <= FindQualityLevel("Very Low", 0);
#if UNITY_WEBGL && !UNITY_EDITOR
        // ScalableBufferManager is not the WebGL canvas resolution control.
        GeoModel_SetRenderResolution(veryLow ? 960 : 1280, veryLow ? 540 : 720,
            qualityLevel, IsManualQualityEnabled ? 1 : 0);
#else
        QualitySettings.resolutionScalingFixedDPIFactor = _isMobile ? (veryLow ? 0.6f : 0.72f) : 1f;
#endif
        QualitySettings.globalTextureMipmapLimit = veryLow ? 2 : qualityLevel <= FindQualityLevel("Low", 1) ? 1 : 0;

        if (qualityLevel <= FindQualityLevel("Very Low", 0))
        {
            QualitySettings.pixelLightCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.antiAliasing = 0;
        }
        else if (qualityLevel <= FindQualityLevel("Low", Mathf.Min(1, QualitySettings.names.Length - 1)))
        {
            QualitySettings.pixelLightCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 12f);
            QualitySettings.antiAliasing = 0;
        }
        else if (qualityLevel <= FindQualityLevel("Medium", Mathf.Min(2, QualitySettings.names.Length - 1)))
        {
            QualitySettings.pixelLightCount = Mathf.Min(QualitySettings.pixelLightCount, 1);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.antiAliasing = 0;
        }
    }

    private int FindQualityLevel(string qualityName, int fallback)
    {
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], qualityName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return Mathf.Clamp(fallback, 0, names.Length - 1);
    }

    private int ClampQualityLevel(int qualityLevel)
    {
        int maxLevel = QualitySettings.names != null && QualitySettings.names.Length > 0
            ? QualitySettings.names.Length - 1
            : 0;
        return Mathf.Clamp(qualityLevel, 0, maxLevel);
    }

}
