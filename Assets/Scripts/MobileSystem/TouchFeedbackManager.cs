using UnityEngine;
using System.Collections;

/// <summary>
/// 触觉反馈管理器（简化版）
/// 提供基础的触觉反馈功能，专注于按钮确认和重要操作反馈
/// </summary>
public class TouchFeedbackManager : MonoBehaviour
{
    [Header("基础设置")]
    public bool enableVibration = true;
    public bool enableSoundFeedback = true;
    public bool adaptToSettings = true; // 适配系统设置
    
    [Header("震动强度设置")]
    [Range(0f, 1f)]
    public float lightVibrationIntensity = 0.3f;
    [Range(0f, 1f)]
    public float mediumVibrationIntensity = 0.6f;
    [Range(0f, 1f)]
    public float strongVibrationIntensity = 1.0f;
    
    [Header("震动时长设置（毫秒）")]
    public int shortVibrationDuration = 50;
    public int mediumVibrationDuration = 100;
    public int longVibrationDuration = 200;
    
    [Header("音频反馈")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip warningSound;
    
    [Header("反馈间隔")]
    public float minVibrationInterval = 0.1f; // 最小震动间隔，防止过度震动
    
    [Header("调试设置")]
    public bool enableDebugLog = false;
    
    // 单例模式
    public static TouchFeedbackManager Instance { get; private set; }
    
    // 私有变量
    private float lastVibrationTime = 0f;
    private bool systemVibrationEnabled = true;
    
    public enum FeedbackType
    {
        ButtonClick,    // 按钮点击
        Success,        // 成功操作
        Error,          // 错误操作
        Warning,        // 警告
        Selection,      // 选择
        Confirmation,   // 确认
        Cancel,         // 取消
        ToolSwitch,     // 工具切换
        SampleCollect,  // 样本收集
        Achievement     // 成就获得
    }
    
    public enum VibrationIntensity
    {
        Light,
        Medium,
        Strong
    }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeFeedbackSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 检查系统设置
        CheckSystemSettings();
        
        // 初始化音频组件
        InitializeAudioFeedback();
        
        Debug.Log("[TouchFeedbackManager] 触觉反馈系统初始化完成");
    }
    
    /// <summary>
    /// 初始化反馈系统
    /// </summary>
    void InitializeFeedbackSystem()
    {
        // 检查设备是否支持震动
        if (!SystemInfo.supportsVibration)
        {
            enableVibration = false;
            Debug.LogWarning("[TouchFeedbackManager] 设备不支持震动功能");
        }
        
        // 只在移动设备上启用震动
        if (!Application.isMobilePlatform)
        {
            enableVibration = false;
            Debug.Log("[TouchFeedbackManager] 非移动设备，震动功能已禁用");
        }
    }
    
    /// <summary>
    /// 检查系统设置
    /// </summary>
    void CheckSystemSettings()
    {
        if (adaptToSettings)
        {
            // 这里可以检查系统的震动设置
            // 在某些平台上可以通过SystemInfo或其他API获取
            systemVibrationEnabled = true; // 默认启用，实际项目中可能需要更复杂的检测
        }
    }
    
    /// <summary>
    /// 初始化音频反馈
    /// </summary>
    void InitializeAudioFeedback()
    {
        if (audioSource == null)
        {
            // 创建音频源
            GameObject audioObj = new GameObject("FeedbackAudioSource");
            audioObj.transform.SetParent(transform);
            audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }
        
        // 如果没有音频剪辑，创建简单的音效
        if (clickSound == null)
        {
            // 可以在这里生成程序化音效或从Resources加载
        }
    }
    
    #region 公共接口
    
    /// <summary>
    /// 触发反馈
    /// </summary>
    public void TriggerFeedback(FeedbackType feedbackType)
    {
        switch (feedbackType)
        {
            case FeedbackType.ButtonClick:
                TriggerButtonClickFeedback();
                break;
                
            case FeedbackType.Success:
                TriggerSuccessFeedback();
                break;
                
            case FeedbackType.Error:
                TriggerErrorFeedback();
                break;
                
            case FeedbackType.Warning:
                TriggerWarningFeedback();
                break;
                
            case FeedbackType.Selection:
                TriggerSelectionFeedback();
                break;
                
            case FeedbackType.Confirmation:
                TriggerConfirmationFeedback();
                break;
                
            case FeedbackType.Cancel:
                TriggerCancelFeedback();
                break;
                
            case FeedbackType.ToolSwitch:
                TriggerToolSwitchFeedback();
                break;
                
            case FeedbackType.SampleCollect:
                TriggerSampleCollectFeedback();
                break;
                
            case FeedbackType.Achievement:
                TriggerAchievementFeedback();
                break;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[TouchFeedbackManager] 反馈触发: {feedbackType}");
        }
    }
    
    /// <summary>
    /// 直接震动
    /// </summary>
    public void Vibrate(VibrationIntensity intensity, int durationMs = -1)
    {
        if (!CanVibrate()) return;
        
        int duration = durationMs > 0 ? durationMs : GetDurationForIntensity(intensity);
        float intensityValue = GetIntensityValue(intensity);
        
        PerformVibration(intensityValue, duration);
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (!enableSoundFeedback || audioSource == null || clip == null) return;
        
        audioSource.volume = volume * 0.5f; // 基础音量调节
        audioSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// 设置震动启用状态
    /// </summary>
    public void SetVibrationEnabled(bool enabled)
    {
        enableVibration = enabled;
        Debug.Log($"[TouchFeedbackManager] 震动功能: {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 设置音频反馈启用状态
    /// </summary>
    public void SetSoundFeedbackEnabled(bool enabled)
    {
        enableSoundFeedback = enabled;
        Debug.Log($"[TouchFeedbackManager] 音频反馈: {(enabled ? "启用" : "禁用")}");
    }
    
    #endregion
    
    #region 具体反馈实现
    
    /// <summary>
    /// 按钮点击反馈
    /// </summary>
    void TriggerButtonClickFeedback()
    {
        Vibrate(VibrationIntensity.Light, shortVibrationDuration);
        PlaySound(clickSound);
    }
    
    /// <summary>
    /// 成功操作反馈
    /// </summary>
    void TriggerSuccessFeedback()
    {
        StartCoroutine(SuccessVibrationPattern());
        PlaySound(successSound);
    }
    
    /// <summary>
    /// 错误操作反馈
    /// </summary>
    void TriggerErrorFeedback()
    {
        StartCoroutine(ErrorVibrationPattern());
        PlaySound(errorSound);
    }
    
    /// <summary>
    /// 警告反馈
    /// </summary>
    void TriggerWarningFeedback()
    {
        Vibrate(VibrationIntensity.Medium, mediumVibrationDuration);
        PlaySound(warningSound);
    }
    
    /// <summary>
    /// 选择反馈
    /// </summary>
    void TriggerSelectionFeedback()
    {
        Vibrate(VibrationIntensity.Light, shortVibrationDuration);
    }
    
    /// <summary>
    /// 确认反馈
    /// </summary>
    void TriggerConfirmationFeedback()
    {
        Vibrate(VibrationIntensity.Medium, mediumVibrationDuration);
        PlaySound(successSound, 0.7f);
    }
    
    /// <summary>
    /// 取消反馈
    /// </summary>
    void TriggerCancelFeedback()
    {
        Vibrate(VibrationIntensity.Light, shortVibrationDuration);
    }
    
    /// <summary>
    /// 工具切换反馈
    /// </summary>
    void TriggerToolSwitchFeedback()
    {
        StartCoroutine(ToolSwitchVibrationPattern());
    }
    
    /// <summary>
    /// 样本收集反馈
    /// </summary>
    void TriggerSampleCollectFeedback()
    {
        StartCoroutine(SampleCollectVibrationPattern());
        PlaySound(successSound, 0.8f);
    }
    
    /// <summary>
    /// 成就获得反馈
    /// </summary>
    void TriggerAchievementFeedback()
    {
        StartCoroutine(AchievementVibrationPattern());
        PlaySound(successSound, 1.0f);
    }
    
    #endregion
    
    #region 震动模式
    
    /// <summary>
    /// 成功震动模式
    /// </summary>
    IEnumerator SuccessVibrationPattern()
    {
        PerformVibration(lightVibrationIntensity, shortVibrationDuration);
        yield return new WaitForSeconds(0.1f);
        PerformVibration(mediumVibrationIntensity, shortVibrationDuration);
    }
    
    /// <summary>
    /// 错误震动模式
    /// </summary>
    IEnumerator ErrorVibrationPattern()
    {
        for (int i = 0; i < 3; i++)
        {
            PerformVibration(strongVibrationIntensity, shortVibrationDuration);
            yield return new WaitForSeconds(0.08f);
        }
    }
    
    /// <summary>
    /// 工具切换震动模式
    /// </summary>
    IEnumerator ToolSwitchVibrationPattern()
    {
        PerformVibration(mediumVibrationIntensity, shortVibrationDuration);
        yield return new WaitForSeconds(0.05f);
        PerformVibration(lightVibrationIntensity, shortVibrationDuration);
    }
    
    /// <summary>
    /// 样本收集震动模式
    /// </summary>
    IEnumerator SampleCollectVibrationPattern()
    {
        PerformVibration(lightVibrationIntensity, shortVibrationDuration);
        yield return new WaitForSeconds(0.1f);
        PerformVibration(mediumVibrationIntensity, mediumVibrationDuration);
    }
    
    /// <summary>
    /// 成就获得震动模式
    /// </summary>
    IEnumerator AchievementVibrationPattern()
    {
        PerformVibration(mediumVibrationIntensity, shortVibrationDuration);
        yield return new WaitForSeconds(0.1f);
        PerformVibration(strongVibrationIntensity, mediumVibrationDuration);
        yield return new WaitForSeconds(0.1f);
        PerformVibration(mediumVibrationIntensity, shortVibrationDuration);
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 检查是否可以震动
    /// </summary>
    bool CanVibrate()
    {
        if (!enableVibration || !systemVibrationEnabled) return false;
        
        // 检查震动间隔
        float currentTime = Time.time;
        if (currentTime - lastVibrationTime < minVibrationInterval)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 执行震动
    /// </summary>
    void PerformVibration(float intensity, int durationMs)
    {
        if (!CanVibrate()) return;
        
        lastVibrationTime = Time.time;
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Android原生震动
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        
        if (vibrator != null)
        {
            vibrator.Call("vibrate", (long)durationMs);
        }
        #elif UNITY_IOS && !UNITY_EDITOR
        // iOS震动
        Handheld.Vibrate();
        #else
        // 编辑器或其他平台的模拟
        Debug.Log($"[TouchFeedbackManager] 模拟震动 - 强度: {intensity}, 时长: {durationMs}ms");
        #endif
    }
    
    /// <summary>
    /// 根据强度获取持续时间
    /// </summary>
    int GetDurationForIntensity(VibrationIntensity intensity)
    {
        switch (intensity)
        {
            case VibrationIntensity.Light:
                return shortVibrationDuration;
            case VibrationIntensity.Medium:
                return mediumVibrationDuration;
            case VibrationIntensity.Strong:
                return longVibrationDuration;
            default:
                return shortVibrationDuration;
        }
    }
    
    /// <summary>
    /// 根据强度获取强度值
    /// </summary>
    float GetIntensityValue(VibrationIntensity intensity)
    {
        switch (intensity)
        {
            case VibrationIntensity.Light:
                return lightVibrationIntensity;
            case VibrationIntensity.Medium:
                return mediumVibrationIntensity;
            case VibrationIntensity.Strong:
                return strongVibrationIntensity;
            default:
                return lightVibrationIntensity;
        }
    }
    
    #endregion
    
    #region 调试功能
    
    void OnGUI()
    {
        if (!enableDebugLog) return;
        
        GUILayout.BeginArea(new Rect(10, 1030, 300, 150));
        GUILayout.Label("=== 触觉反馈调试 ===");
        GUILayout.Label($"震动启用: {enableVibration}");
        GUILayout.Label($"音频启用: {enableSoundFeedback}");
        GUILayout.Label($"系统震动: {systemVibrationEnabled}");
        GUILayout.Label($"上次震动: {lastVibrationTime:F2}s");
        
        if (GUILayout.Button("测试按钮点击"))
        {
            TriggerFeedback(FeedbackType.ButtonClick);
        }
        
        if (GUILayout.Button("测试成功反馈"))
        {
            TriggerFeedback(FeedbackType.Success);
        }
        
        if (GUILayout.Button("测试错误反馈"))
        {
            TriggerFeedback(FeedbackType.Error);
        }
        
        GUILayout.EndArea();
    }
    
    #endregion
}