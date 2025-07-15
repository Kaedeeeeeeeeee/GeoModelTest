using UnityEngine;

/// <summary>
/// 采集目标标记组件 - 显示采集进度和目标位置
/// </summary>
public class CollectionTargetMarker : MonoBehaviour
{
    [Header("标记设置")]
    public float markerRadius = 0.5f; // 标记半径
    public float pulseSpeed = 2f; // 脉冲速度
    public Color activeColor = Color.green; // 激活颜色
    public Color progressColor = Color.yellow; // 进度颜色
    public Color completeColor = Color.blue; // 完成颜色
    
    [Header("进度环设置")]
    public int ringSegments = 32; // 环的分段数
    public float ringThickness = 0.05f; // 环的厚度
    public float heightOffset = 0.02f; // 高度偏移
    
    private LineRenderer progressRing; // 进度环
    private LineRenderer baseRing; // 基础环
    private Material progressMaterial;
    private Material baseMaterial;
    
    // 进度状态
    private int currentHits = 0;
    private int requiredHits = 3;
    private bool isActive = false;
    
    void Awake()
    {
        CreateMarkerComponents();
    }
    
    /// <summary>
    /// 创建标记组件
    /// </summary>
    void CreateMarkerComponents()
    {
        // 创建基础环（灰色底环）
        GameObject baseRingObj = new GameObject("BaseRing");
        baseRingObj.transform.SetParent(transform);
        baseRingObj.transform.localPosition = Vector3.zero;
        
        baseRing = baseRingObj.AddComponent<LineRenderer>();
        SetupLineRenderer(baseRing, Color.gray * 0.5f);
        
        // 创建进度环（彩色进度显示）
        GameObject progressRingObj = new GameObject("ProgressRing");
        progressRingObj.transform.SetParent(transform);
        progressRingObj.transform.localPosition = Vector3.up * heightOffset;
        
        progressRing = progressRingObj.AddComponent<LineRenderer>();
        SetupLineRenderer(progressRing, activeColor);
        
        // 生成环形顶点
        GenerateRingVertices();
        
        // 初始时隐藏
        SetVisible(false);
    }
    
    /// <summary>
    /// 设置LineRenderer基础属性
    /// </summary>
    void SetupLineRenderer(LineRenderer lr, Color color)
    {
        lr.material = CreateLineMaterial(color);
        lr.widthMultiplier = ringThickness;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = ringSegments;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }
    
    /// <summary>
    /// 创建线条材质
    /// </summary>
    Material CreateLineMaterial(Color color)
    {
        // 使用支持透明度的着色器
        Material material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        if (material.shader == null)
        {
            // 备用方案
            material = new Material(Shader.Find("Sprites/Default"));
        }
        
        material.color = color;
        material.SetFloat("_ZWrite", 0);
        material.renderQueue = 3000; // 确保在地面之上渲染
        
        // 启用透明度混合
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3); // Transparent mode
        }
        
        return material;
    }
    
    /// <summary>
    /// 生成环形顶点
    /// </summary>
    void GenerateRingVertices()
    {
        Vector3[] positions = new Vector3[ringSegments];
        
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (float)i / ringSegments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * markerRadius;
            float z = Mathf.Sin(angle) * markerRadius;
            positions[i] = new Vector3(x, 0, z);
        }
        
        baseRing.SetPositions(positions);
        progressRing.SetPositions(positions);
    }
    
    /// <summary>
    /// 显示标记并开始采集
    /// </summary>
    public void StartCollection(Vector3 worldPosition, int requiredHitCount)
    {
        transform.position = worldPosition;
        requiredHits = requiredHitCount;
        currentHits = 0;
        isActive = true;
        
        SetVisible(true);
        UpdateProgress();
        
        Debug.Log($"开始采集标记 - 位置: {worldPosition}, 需要敲击: {requiredHits}次");
    }
    
    /// <summary>
    /// 更新采集进度
    /// </summary>
    public void UpdateProgress(int hits)
    {
        currentHits = hits;
        UpdateProgress();
    }
    
    /// <summary>
    /// 更新进度显示
    /// </summary>
    void UpdateProgress()
    {
        if (!isActive) return;
        
        float progress = (float)currentHits / requiredHits;
        
        // 更新进度环长度和颜色
        UpdateProgressRing(progress);
        
        Debug.Log($"采集进度更新: {currentHits}/{requiredHits} ({progress:P0})");
    }
    
    /// <summary>
    /// 更新进度环的显示长度
    /// </summary>
    void UpdateProgressRing(float progress)
    {
        // 始终显示完整的圆圈，通过颜色和透明度来表示进度
        progressRing.positionCount = ringSegments;
        
        Vector3[] positions = new Vector3[ringSegments];
        
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (float)i / ringSegments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * markerRadius;
            float z = Mathf.Sin(angle) * markerRadius;
            positions[i] = new Vector3(x, 0, z);
        }
        
        progressRing.SetPositions(positions);
        
        // 通过颜色透明度来显示进度，而不是通过截断圆圈
        Color currentColor = Color.Lerp(activeColor, completeColor, progress);
        currentColor.a = 0.3f + (progress * 0.7f); // 透明度从30%到100%
        
        if (progressRing.material != null)
        {
            progressRing.material.color = currentColor;
        }
    }
    
    /// <summary>
    /// 完成采集
    /// </summary>
    public void CompleteCollection()
    {
        currentHits = requiredHits;
        UpdateProgress();
        
        // 播放完成动画
        StartCoroutine(CompleteAnimation());
    }
    
    /// <summary>
    /// 完成动画协程
    /// </summary>
    System.Collections.IEnumerator CompleteAnimation()
    {
        float animationTime = 1f;
        float elapsed = 0f;
        
        Color startColor = progressRing.material.color;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;
        
        while (elapsed < animationTime)
        {
            float t = elapsed / animationTime;
            
            // 颜色闪烁
            Color flashColor = Color.Lerp(startColor, Color.white, Mathf.PingPong(t * 6f, 1f));
            if (progressRing.material != null)
            {
                progressRing.material.color = flashColor;
            }
            
            // 尺寸变化
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 恢复并隐藏
        transform.localScale = startScale;
        SetVisible(false);
        isActive = false;
    }
    
    /// <summary>
    /// 取消采集
    /// </summary>
    public void CancelCollection()
    {
        isActive = false;
        SetVisible(false);
        StopAllCoroutines();
        
        Debug.Log("采集标记已取消");
    }
    
    /// <summary>
    /// 设置标记可见性
    /// </summary>
    void SetVisible(bool visible)
    {
        if (baseRing != null)
            baseRing.enabled = visible;
        
        if (progressRing != null)
            progressRing.enabled = visible;
    }
    
    /// <summary>
    /// 检查点是否在标记范围内
    /// </summary>
    public bool IsPointInRange(Vector3 worldPoint, float tolerance = 0.5f)
    {
        if (!isActive) return false;
        
        float distance = Vector3.Distance(worldPoint, transform.position);
        return distance <= tolerance;
    }
    
    void Update()
    {
        if (isActive)
        {
            // 添加轻微的脉冲效果
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
            if (progressRing != null)
            {
                progressRing.widthMultiplier = ringThickness * pulse;
            }
        }
    }
    
    void OnDestroy()
    {
        // 清理材质资源
        if (progressMaterial != null)
            DestroyImmediate(progressMaterial);
        
        if (baseMaterial != null)
            DestroyImmediate(baseMaterial);
    }
    
    /// <summary>
    /// 在Scene视图中显示调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (isActive)
        {
            // 绘制采集范围
            Gizmos.color = Color.yellow * 0.3f;
            Gizmos.DrawWireSphere(transform.position, markerRadius);
            
            // 绘制进度信息
            Gizmos.color = Color.white;
            Vector3 labelPos = transform.position + Vector3.up * 0.5f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"采集进度: {currentHits}/{requiredHits}");
            #endif
        }
    }
}