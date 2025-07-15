using UnityEngine;

public class SampleFloatingDisplay : MonoBehaviour
{
    [Header("悬浮设置")]
    public float floatingHeight = 1.5f; // 悬浮高度
    public float floatingAmplitude = 0.3f; // 上下浮动幅度
    public float floatingSpeed = 1.0f; // 上下浮动速度
    
    [Header("旋转设置")]
    public bool enableRotation = true;
    public Vector3 rotationSpeed = new Vector3(0, 30f, 0); // 每秒旋转度数
    
    [Header("缩放脉冲")]
    public bool enablePulse = false;
    public float pulseAmplitude = 0.1f; // 缩放脉冲幅度
    public float pulseSpeed = 2.0f; // 缩放脉冲速度
    
    [Header("材质保持")]
    public bool preserveOriginalMaterials = true;
    
    private Vector3 initialPosition;
    private Vector3 baseScale;
    private float floatingTimer = 0f;
    private bool isFloating = false;
    
    // 组件引用
    private Rigidbody rb;
    private Collider[] colliders;
    
    void Start()
    {
        InitializeFloatingDisplay();
    }
    
    void InitializeFloatingDisplay()
    {
        // 记录初始位置和缩放
        initialPosition = transform.position;
        baseScale = transform.localScale;
        
        // 设置悬浮位置
        Vector3 floatingPos = initialPosition + Vector3.up * floatingHeight;
        transform.position = floatingPos;
        initialPosition = floatingPos; // 更新参考位置
        
        // 获取物理组件
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();
        
        // 设置物理状态
        SetupPhysicsForFloating();
        
        // 开始悬浮（保持原始材质）
        isFloating = true;
        
        
    }
    
    void SetupPhysicsForFloating()
    {
        if (rb != null)
        {
            rb.useGravity = false; // 禁用重力
            rb.isKinematic = true; // 设为运动学，不受物理影响
        }
        
        // 保持碰撞器用于交互，但设为触发器
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true; // 设为触发器，允许玩家穿过但仍能检测
            }
        }
    }
    
    // 移除发光效果，保持原始材质
    
    void Update()
    {
        if (!isFloating) return;
        
        floatingTimer += Time.deltaTime;
        
        // 上下浮动
        UpdateFloatingMovement();
        
        // 旋转动画
        if (enableRotation)
        {
            UpdateRotation();
        }
        
        // 缩放脉冲
        if (enablePulse)
        {
            UpdatePulseEffect();
        }
    }
    
    void UpdateFloatingMovement()
    {
        // 使用正弦波实现平滑的上下浮动
        float floatingOffset = Mathf.Sin(floatingTimer * floatingSpeed) * floatingAmplitude;
        Vector3 newPosition = initialPosition + Vector3.up * floatingOffset;
        transform.position = newPosition;
    }
    
    void UpdateRotation()
    {
        // 持续旋转
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
    
    void UpdatePulseEffect()
    {
        // 使用正弦波实现缩放脉冲
        float pulseScale = 1f + Mathf.Sin(floatingTimer * pulseSpeed) * pulseAmplitude;
        transform.localScale = baseScale * pulseScale;
    }
    
    // 公共方法：停止悬浮，恢复正常物理
    public void StopFloating()
    {
        if (!isFloating) return;
        
        isFloating = false;
        
        // 恢复物理状态
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 1.0f;
            rb.angularDamping = 1.0f;
        }
        
        // 恢复正常碰撞器
        foreach (Collider col in colliders)
        {
            if (col.isTrigger && !col.name.Contains("trigger"))
            {
                col.isTrigger = false;
            }
        }
        
        // 恢复正常缩放
        transform.localScale = baseScale;
        
        
    }
    
    // 公共方法：开始悬浮
    public void StartFloating()
    {
        if (isFloating) return;
        
        SetupPhysicsForFloating();
        isFloating = true;
        floatingTimer = 0f;
        
        
    }
    
    // 公共方法：设置悬浮参数
    public void SetFloatingParameters(float height, float amplitude, float speed)
    {
        floatingHeight = height;
        floatingAmplitude = amplitude;
        floatingSpeed = speed;
        
        // 更新位置
        if (isFloating)
        {
            Vector3 basePos = transform.position;
            basePos.y = initialPosition.y + height;
            initialPosition = basePos;
        }
    }
    
    // 鼠标交互
    void OnMouseEnter()
    {
        if (isFloating)
        {
            // 悬停时加快旋转
            rotationSpeed *= 1.5f;
        }
    }
    
    void OnMouseExit()
    {
        if (isFloating)
        {
            // 恢复正常旋转速度
            rotationSpeed /= 1.5f;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家靠近时稍微加快浮动
            floatingSpeed *= 1.1f;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 玩家离开时恢复正常速度
            floatingSpeed /= 1.1f;
        }
    }
}