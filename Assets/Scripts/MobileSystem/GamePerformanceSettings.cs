using System;
using UnityEngine;

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
            ApplyQualityPreference(false);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool IsManualQualityEnabled => PlayerPrefs.GetInt(ManualQualityEnabledKey, 0) == 1;

    public int CurrentQualityLevel => QualitySettings.GetQualityLevel();

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

        bool isMobile = MobileInputManager.IsRuntimeMobileDevice();
        if (isMobile)
        {
            if (IsLowEndDevice())
            {
                return FindQualityLevel("Very Low", 0);
            }

            if (IsLikelyTablet() && SystemInfo.systemMemorySize >= 4000)
            {
                return FindQualityLevel("Medium", Mathf.Min(2, QualitySettings.names.Length - 1));
            }

            return FindQualityLevel("Low", Mathf.Min(1, QualitySettings.names.Length - 1));
        }

        return FindQualityLevel("High", Mathf.Min(3, QualitySettings.names.Length - 1));
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
        bool isMobile = MobileInputManager.IsRuntimeMobileDevice();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = isMobile ? GetMobileTargetFrameRate(qualityLevel) : 60;

        float renderScale = GetRenderScale(qualityLevel, isMobile);
        QualitySettings.resolutionScalingFixedDPIFactor = renderScale;
        ScalableBufferManager.ResizeBuffers(renderScale, renderScale);

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
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, isMobile ? 20f : 35f);
            QualitySettings.antiAliasing = 0;
        }
    }

    private int GetMobileTargetFrameRate(int qualityLevel)
    {
        int lowLevel = FindQualityLevel("Low", Mathf.Min(1, QualitySettings.names.Length - 1));
        return qualityLevel <= lowLevel ? 30 : 45;
    }

    private float GetRenderScale(int qualityLevel, bool isMobile)
    {
        if (!isMobile)
        {
            return 1f;
        }

        int veryLow = FindQualityLevel("Very Low", 0);
        int low = FindQualityLevel("Low", Mathf.Min(1, QualitySettings.names.Length - 1));
        int medium = FindQualityLevel("Medium", Mathf.Min(2, QualitySettings.names.Length - 1));

        if (qualityLevel <= veryLow)
        {
            return 0.6f;
        }

        if (qualityLevel <= low)
        {
            return 0.72f;
        }

        if (qualityLevel <= medium)
        {
            return 0.85f;
        }

        return 1f;
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

    private bool IsLowEndDevice()
    {
        bool lowMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000;
        bool lowCpu = SystemInfo.processorCount > 0 && SystemInfo.processorCount < 4;
        bool lowGpuMemory = SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 512;
        return lowMemory || lowCpu || lowGpuMemory;
    }

    private bool IsLikelyTablet()
    {
        float diagonalPixels = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        return diagonalPixels / dpi >= 7f;
    }
}
