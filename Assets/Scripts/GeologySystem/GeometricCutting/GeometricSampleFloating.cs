using UnityEngine;

/// <summary>
/// 样本显示模式
/// </summary>
public enum SampleDisplayMode
{
    Floating,   // 悬浮模式（梦幻效果）
    Realistic   // 现实模式（重力掉落）
}

/// <summary>
/// 几何样本显示组件
/// 为真实几何切割的样本提供悬浮动画效果或掉落物理效果
/// </summary>
public class GeometricSampleFloating : MonoBehaviour
{
    [Header("显示模式")]
    public SampleDisplayMode displayMode = SampleDisplayMode.Realistic;
    
    [Header("悬浮动画（仅在浮动模式下生效）")]
    public float floatingAmplitude = 0.2f;
    public float floatingSpeed = 1.0f;
    public bool enableFloating = true;
    
    [Header("旋转动画")]
    public Vector3 rotationSpeed = new Vector3(0, 15f, 0);
    public bool enableRotation = true;
    
    [Header("缩放脉冲")]
    public bool enablePulse = false;
    public float pulseAmplitude = 0.05f;
    public float pulseSpeed = 2.0f;
    
    [Header("高级效果")]
    public bool enableBobbing = true;
    public float bobbingFrequency = 0.5f;
    public bool enableOrbiting = false;
    public float orbitRadius = 0.3f;
    public float orbitSpeed = 0.2f;
    
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private float timeOffset;
    private bool isFloating = false;
    
    // 物理组件引用
    private Rigidbody rb;
    private Collider[] colliders;
    
    void Start()
    {
        InitializeFloating();
    }
    
    void InitializeFloating()
    {
        // 记录初始状态
        initialPosition = transform.position;
        initialScale = transform.localScale;
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // 随机时间偏移避免同步
        
        // 获取物理组件
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        
        // 根据显示模式初始化
        switch (displayMode)
        {
            case SampleDisplayMode.Floating:
                SetupFloatingPhysics();
                if (enableFloating)
                {
                    StartFloating();
                }
                break;
                
            case SampleDisplayMode.Realistic:
                SetupRealisticPhysics();
                break;
        }
    }
    
    void SetupFloatingPhysics()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        
        // 设置碰撞器为触发器以便交互
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger && !col.name.Contains("Trigger"))
            {
                col.isTrigger = true;
            }
        }
    }
    
    void SetupRealisticPhysics()
    {
        // 移除当前的浮动组件，添加掉落控制器
        SampleDropController dropController = GetComponent<SampleDropController>();
        if (dropController == null)
        {
            dropController = gameObject.AddComponent<SampleDropController>();
        }
        
        // 禁用浮动逻辑
        isFloating = false;
        
        Debug.Log($"样本切换到现实物理模式: {gameObject.name}");
    }
    
    public void StartFloating()
    {
        isFloating = true;
        
    }
    
    public void StopFloating()
    {
        isFloating = false;
        
        // 恢复物理状态
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        // 恢复碰撞器
        foreach (Collider col in colliders)
        {
            if (col.isTrigger && !col.name.Contains("Trigger"))
            {
                col.isTrigger = false;
            }
        }
        
        
    }
    
    void Update()
    {
        // 只有在浮动模式下才执行浮动逻辑
        if (displayMode != SampleDisplayMode.Floating || !isFloating) return;
        
        float currentTime = Time.time + timeOffset;
        
        // 计算新位置
        Vector3 newPosition = CalculateFloatingPosition(currentTime);
        transform.position = newPosition;
        
        // 应用旋转
        if (enableRotation)
        {
            ApplyRotation();
        }
        
        // 应用缩放脉冲
        if (enablePulse)
        {
            ApplyPulse(currentTime);
        }
    }
    
    Vector3 CalculateFloatingPosition(float time)
    {
        Vector3 position = initialPosition;
        
        // 基础上下浮动
        if (enableFloating)
        {
            float verticalOffset = Mathf.Sin(time * floatingSpeed) * floatingAmplitude;
            position.y += verticalOffset;
        }
        
        // 微妙的左右摆动
        if (enableBobbing)
        {
            float horizontalOffset = Mathf.Sin(time * bobbingFrequency * 1.3f) * floatingAmplitude * 0.3f;
            position.x += horizontalOffset;
            
            float depthOffset = Mathf.Cos(time * bobbingFrequency * 0.7f) * floatingAmplitude * 0.2f;
            position.z += depthOffset;
        }
        
        // 轨道运动
        if (enableOrbiting)
        {
            float orbitX = Mathf.Cos(time * orbitSpeed) * orbitRadius;
            float orbitZ = Mathf.Sin(time * orbitSpeed) * orbitRadius;
            position.x += orbitX;
            position.z += orbitZ;
        }
        
        return position;
    }
    
    void ApplyRotation()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
    
    void ApplyPulse(float time)
    {
        float pulseScale = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmplitude;
        transform.localScale = initialScale * pulseScale;
    }
    
    /// <summary>
    /// 设置悬浮参数
    /// </summary>
    public void SetFloatingParameters(float amplitude, float speed, Vector3 rotation)
    {
        floatingAmplitude = amplitude;
        floatingSpeed = speed;
        rotationSpeed = rotation;
    }
    
    /// <summary>
    /// 设置悬浮高度
    /// </summary>
    public void SetFloatingHeight(float height)
    {
        Vector3 targetPosition = initialPosition;
        targetPosition.y += height;
        
        // 平滑移动到新高度
        StartCoroutine(MoveToHeight(targetPosition, 1.0f));
    }
    
    System.Collections.IEnumerator MoveToHeight(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = initialPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            initialPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        initialPosition = targetPosition;
    }
    
    /// <summary>
    /// 播放特殊动画效果
    /// </summary>
    public void PlayCollectionAnimation()
    {
        StartCoroutine(CollectionAnimationCoroutine());
    }
    
    System.Collections.IEnumerator CollectionAnimationCoroutine()
    {
        // 保存原始状态
        bool wasFloating = isFloating;
        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        
        isFloating = false;
        
        float animationDuration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // 缩小并上升
            float scale = Mathf.Lerp(1f, 0f, t);
            transform.localScale = originalScale * scale;
            
            // 上升动画
            Vector3 currentPos = originalPosition + Vector3.up * t * 3f;
            transform.position = currentPos;
            
            // 旋转加速
            transform.Rotate(rotationSpeed * Time.deltaTime * (1f + t * 3f));
            
            yield return null;
        }
        
        // 销毁对象
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 播放强调动画
    /// </summary>
    public void PlayHighlightAnimation(float duration = 2f)
    {
        StartCoroutine(HighlightAnimationCoroutine(duration));
    }
    
    System.Collections.IEnumerator HighlightAnimationCoroutine(float duration)
    {
        // 临时增强动画效果
        float originalAmplitude = floatingAmplitude;
        float originalSpeed = floatingSpeed;
        Vector3 originalRotationSpeed = rotationSpeed;
        
        floatingAmplitude *= 1.5f;
        floatingSpeed *= 1.3f;
        rotationSpeed *= 1.5f;
        
        yield return new WaitForSeconds(duration);
        
        // 恢复原始参数
        floatingAmplitude = originalAmplitude;
        floatingSpeed = originalSpeed;
        rotationSpeed = originalRotationSpeed;
    }
    
    // 交互事件
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家靠近时增强动画
            PlayHighlightAnimation(1f);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家离开时恢复正常
            // 动画会自动在HighlightAnimation结束时恢复
        }
    }
    
    // 鼠标交互
    void OnMouseEnter()
    {
        if (isFloating)
        {
            // 鼠标悬停时稍微加快旋转
            rotationSpeed *= 1.2f;
        }
    }
    
    void OnMouseExit()
    {
        if (isFloating)
        {
            // 恢复正常旋转速度
            rotationSpeed /= 1.2f;
        }
    }
    
    void OnMouseDown()
    {
        if (isFloating)
        {
            // 点击时播放强调动画
            PlayHighlightAnimation(0.5f);
        }
    }
    
    /// <summary>
    /// 获取当前悬浮状态
    /// </summary>
    public bool IsFloating()
    {
        return isFloating;
    }
    
    /// <summary>
    /// 重置到初始状态
    /// </summary>
    public void ResetToInitialState()
    {
        transform.position = initialPosition;
        transform.localScale = initialScale;
        transform.rotation = Quaternion.identity;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
        if (enableFloating)
        {
            StartFloating();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // 绘制悬浮范围
            Gizmos.color = Color.cyan;
            Vector3 center = initialPosition;
            Gizmos.DrawWireSphere(center + Vector3.up * floatingAmplitude, 0.1f);
            Gizmos.DrawWireSphere(center - Vector3.up * floatingAmplitude, 0.1f);
            Gizmos.DrawLine(center - Vector3.up * floatingAmplitude, center + Vector3.up * floatingAmplitude);
            
            // 绘制轨道
            if (enableOrbiting)
            {
                Gizmos.color = Color.yellow;
                DrawWireCircle(center, orbitRadius);
            }
        }
    }
    
    /// <summary>
    /// 绘制线框圆圈
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            
            Gizmos.DrawLine(point1, point2);
        }
    }
    
    /// <summary>
    /// 切换显示模式
    /// </summary>
    public void SwitchDisplayMode(SampleDisplayMode newMode)
    {
        if (displayMode == newMode) return;
        
        Debug.Log($"样本显示模式切换: {displayMode} -> {newMode}");
        
        // 停止当前模式
        switch (displayMode)
        {
            case SampleDisplayMode.Floating:
                StopFloating();
                break;
            case SampleDisplayMode.Realistic:
                // 移除掉落控制器
                SampleDropController dropController = GetComponent<SampleDropController>();
                if (dropController != null)
                {
                    Destroy(dropController);
                }
                break;
        }
        
        // 切换到新模式
        displayMode = newMode;
        
        // 重新初始化
        switch (newMode)
        {
            case SampleDisplayMode.Floating:
                SetupFloatingPhysics();
                StartFloating();
                break;
            case SampleDisplayMode.Realistic:
                SetupRealisticPhysics();
                break;
        }
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
        GeometricSampleFloating floatingComponent = sampleObject.GetComponent<GeometricSampleFloating>();
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