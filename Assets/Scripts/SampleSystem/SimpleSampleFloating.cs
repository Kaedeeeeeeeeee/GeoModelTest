using UnityEngine;

/// <summary>
/// 简单样本显示效果 - 为放置的样本提供悬浮动画或现实物理效果
/// </summary>
public class SimpleSampleFloating : MonoBehaviour
{
    [Header("显示模式")]
    public SampleDisplayMode displayMode = SampleDisplayMode.Realistic;
    
    [Header("悬浮设置（仅在浮动模式下生效）")]
    public bool enableFloating = true;
    public float floatHeight = 0.2f;
    public float floatSpeed = 1f;
    
    [Header("旋转设置")]
    public bool enableRotation = true;
    public float rotationSpeed = 20f;
    public Vector3 rotationAxis = Vector3.up;
    
    [Header("缩放动画")]
    public bool enableScaling = false;
    public float scaleAmount = 0.05f;
    public float scaleSpeed = 2f;
    
    private Vector3 startPosition;
    private Vector3 originalScale;
    private float timeOffset;
    
    void Start()
    {
        // 记录初始位置和缩放
        startPosition = transform.position;
        originalScale = transform.localScale;
        
        // 添加随机时间偏移以避免所有样本同步动画
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // 根据显示模式初始化
        InitializeDisplayMode();
    }
    
    void InitializeDisplayMode()
    {
        switch (displayMode)
        {
            case SampleDisplayMode.Floating:
                SetupFloatingMode();
                break;
            case SampleDisplayMode.Realistic:
                SetupRealisticMode();
                break;
        }
    }
    
    void SetupFloatingMode()
    {
        // 确保样本位置稍微离地面
        startPosition.y += 0.1f;
        transform.position = startPosition;
        
        // 设置浮动物理
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
    
    void SetupRealisticMode()
    {
        // 添加掉落控制器
        SampleDropController dropController = GetComponent<SampleDropController>();
        if (dropController == null)
        {
            dropController = gameObject.AddComponent<SampleDropController>();
        }
        
        // 禁用浮动动画
        enableFloating = false;
    }
    
    void Update()
    {
        // 只有在浮动模式下才执行动画
        if (displayMode != SampleDisplayMode.Floating) return;
        
        float time = Time.time + timeOffset;
        
        // 悬浮动画
        if (enableFloating)
        {
            Vector3 pos = startPosition;
            pos.y += Mathf.Sin(time * floatSpeed) * floatHeight;
            transform.position = pos;
        }
        
        // 旋转动画
        if (enableRotation)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
        
        // 缩放动画
        if (enableScaling)
        {
            float scaleFactor = 1f + Mathf.Sin(time * scaleSpeed) * scaleAmount;
            transform.localScale = originalScale * scaleFactor;
        }
    }
    
    /// <summary>
    /// 设置悬浮参数
    /// </summary>
    public void SetFloatingParams(float height, float speed)
    {
        floatHeight = height;
        floatSpeed = speed;
    }
    
    /// <summary>
    /// 设置旋转参数
    /// </summary>
    public void SetRotationParams(float speed, Vector3 axis)
    {
        rotationSpeed = speed;
        rotationAxis = axis.normalized;
    }
    
    /// <summary>
    /// 启用/禁用动画
    /// </summary>
    public void SetAnimationEnabled(bool floating, bool rotation, bool scaling = false)
    {
        enableFloating = floating;
        enableRotation = rotation;
        enableScaling = scaling;
    }
    
    /// <summary>
    /// 重置到初始状态
    /// </summary>
    public void ResetToInitialState()
    {
        transform.position = startPosition;
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 播放收集动画
    /// </summary>
    public void PlayCollectionAnimation()
    {
        StartCoroutine(CollectionAnimationCoroutine());
    }
    
    /// <summary>
    /// 收集动画协程
    /// </summary>
    System.Collections.IEnumerator CollectionAnimationCoroutine()
    {
        // 禁用悬浮动画
        enableFloating = false;
        enableRotation = false;
        
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        Vector3 targetPos = startPos + Vector3.up * 2f;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 向上移动并缩小
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // 加速旋转
            transform.Rotate(Vector3.up * rotationSpeed * 5f * Time.deltaTime);
            
            yield return null;
        }
        
        // 动画完成后销毁对象
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 播放高亮动画
    /// </summary>
    public void PlayHighlightAnimation(float duration = 1f)
    {
        StartCoroutine(HighlightAnimationCoroutine(duration));
    }
    
    /// <summary>
    /// 高亮动画协程
    /// </summary>
    System.Collections.IEnumerator HighlightAnimationCoroutine(float duration)
    {
        float originalScaleSpeed = scaleSpeed;
        float originalScaleAmount = scaleAmount;
        bool originalScalingEnabled = enableScaling;
        
        // 临时启用缩放动画
        enableScaling = true;
        scaleSpeed = 4f;
        scaleAmount = 0.1f;
        
        yield return new WaitForSeconds(duration);
        
        // 恢复原始设置
        enableScaling = originalScalingEnabled;
        scaleSpeed = originalScaleSpeed;
        scaleAmount = originalScaleAmount;
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制悬浮范围
        if (enableFloating && displayMode == SampleDisplayMode.Floating)
        {
            Gizmos.color = Color.cyan;
            Vector3 pos = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawWireSphere(pos + Vector3.up * floatHeight, 0.1f);
            Gizmos.DrawWireSphere(pos - Vector3.up * floatHeight, 0.1f);
            Gizmos.DrawLine(pos + Vector3.up * floatHeight, pos - Vector3.up * floatHeight);
        }
    }
    
    /// <summary>
    /// 切换显示模式
    /// </summary>
    public void SwitchDisplayMode(SampleDisplayMode newMode)
    {
        if (displayMode == newMode) return;
        
        Debug.Log($"简单样本显示模式切换: {displayMode} -> {newMode}");
        
        displayMode = newMode;
        InitializeDisplayMode();
    }
    
    /// <summary>
    /// 获取当前显示模式
    /// </summary>
    public SampleDisplayMode GetDisplayMode()
    {
        return displayMode;
    }
    
    /// <summary>
    /// 设置为现实物理模式（静态方法，便于外部调用）
    /// </summary>
    public static void SetSampleToRealistic(GameObject sampleObject)
    {
        SimpleSampleFloating floatingComponent = sampleObject.GetComponent<SimpleSampleFloating>();
        if (floatingComponent != null)
        {
            floatingComponent.SwitchDisplayMode(SampleDisplayMode.Realistic);
        }
        else
        {
            // 如果没有浮动组件，直接添加掉落控制器
            SampleDropController dropController = sampleObject.GetComponent<SampleDropController>();
            if (dropController == null)
            {
                sampleObject.AddComponent<SampleDropController>();
            }
        }
    }
}