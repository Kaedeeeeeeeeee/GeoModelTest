using UnityEngine;
using System.Collections;

/// <summary>
/// 样本掉落控制器 - 让样本有重量感地掉落到地面
/// </summary>
public class SampleDropController : MonoBehaviour
{
    [Header("掉落设置")]
    public float dropHeight = 2.5f; // 初始掉落高度（地面上方2.5m）
    public float gravity = 9.8f; // 重力加速度
    public float additionalGravity = 15f; // 额外重力，防止漂浮
    public float bounceReduction = 0.3f; // 弹跳衰减
    public int maxBounces = 2; // 最大弹跳次数
    
    [Header("着陆检测")]
    public LayerMask groundLayer = -1; // 地面层级
    public float groundCheckDistance = 0.1f; // 地面检测距离
    public float settleThreshold = 0.5f; // 稳定阈值
    
    
    [Header("音效")]
    public AudioClip[] dropSounds; // 掉落音效
    public AudioClip[] bounceSounds; // 弹跳音效
    [Range(0f, 1f)] public float soundVolume = 0.7f;
    
    [Header("视觉效果")]
    public bool enableDustEffect = true; // 着陆灰尘效果
    public GameObject dustEffectPrefab; // 灰尘特效预制体
    
    private Rigidbody rb;
    private Collider[] colliders;
    private AudioSource audioSource;
    private Vector3 initialPosition;
    private bool isDropping = false;
    private bool hasLanded = false;
    private int bounceCount = 0;
    private float timeSinceLastBounce = 0f;
    
    // 落地检测
    private bool isGrounded = false;
    private float groundedTime = 0f;
    
    
    void Start()
    {
        InitializeDropController();
    }
    
    void InitializeDropController()
    {
        // 获取物理组件
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 记录初始位置
        initialPosition = transform.position;
        
        // 设置物理属性
        SetupPhysics();
        
        // 开始掉落
        StartDrop();
    }
    
    void SetupPhysics()
    {
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 启用重力和物理模拟
        rb.useGravity = true;
        rb.isKinematic = false;
        
        // 设置物理属性以增加重量感
        rb.mass = Random.Range(1.5f, 3.0f); // 随机质量，增加真实感
        rb.linearDamping = 0.1f; // 轻微空气阻力
        rb.angularDamping = 2.0f; // 角速度阻力
        
        // 冻结XZ轴移动和所有旋转，保持垂直状态
        rb.freezeRotation = true; // 完全禁止旋转
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        
        // 替换为MeshCollider以获得更精确的碰撞
        SetupMeshColliders();
        
        // 添加物理材质以增加真实感
        PhysicsMaterial sampleMaterial = new PhysicsMaterial("SampleMaterial");
        sampleMaterial.dynamicFriction = 0.6f;
        sampleMaterial.staticFriction = 0.8f;
        sampleMaterial.bounciness = bounceReduction;
        sampleMaterial.frictionCombine = PhysicsMaterialCombine.Average;
        sampleMaterial.bounceCombine = PhysicsMaterialCombine.Average;
        
        // 在设置MeshCollider后重新获取碰撞器列表并应用物理材质
        ApplyPhysicsMaterial(sampleMaterial);
    }
    
    void SetupMeshColliders()
    {
        // 获取所有现有的碰撞器
        Collider[] existingColliders = GetComponentsInChildren<Collider>();
        
        // 移除非触发器的原始碰撞器，保留交互触发器
        foreach (Collider col in existingColliders)
        {
            if (!col.isTrigger && !col.name.Contains("Trigger") && !col.name.Contains("Interaction"))
            {
                DestroyImmediate(col);
            }
        }
        
        // 为所有有网格的对象添加MeshCollider
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // 检查是否已经有MeshCollider
                MeshCollider existingMeshCollider = renderer.GetComponent<MeshCollider>();
                if (existingMeshCollider == null)
                {
                    MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.convex = true; // 启用凸形以支持Rigidbody
                    meshCollider.isTrigger = false;
                    
                    Debug.Log($"为 {renderer.gameObject.name} 添加了MeshCollider");
                }
                else
                {
                    // 更新现有的MeshCollider设置
                    existingMeshCollider.sharedMesh = meshFilter.sharedMesh;
                    existingMeshCollider.convex = true;
                    existingMeshCollider.isTrigger = false;
                    
                    Debug.Log($"更新了 {renderer.gameObject.name} 的MeshCollider设置");
                }
            }
        }
        
        // 如果没有找到任何网格，为主对象添加一个基础MeshCollider
        if (meshRenderers.Length == 0)
        {
            MeshFilter mainMeshFilter = GetComponent<MeshFilter>();
            if (mainMeshFilter != null && mainMeshFilter.sharedMesh != null)
            {
                MeshCollider mainCollider = GetComponent<MeshCollider>();
                if (mainCollider == null)
                {
                    mainCollider = gameObject.AddComponent<MeshCollider>();
                }
                mainCollider.sharedMesh = mainMeshFilter.sharedMesh;
                mainCollider.convex = true;
                mainCollider.isTrigger = false;
                
                Debug.Log($"为主对象 {gameObject.name} 添加了MeshCollider");
            }
        }
        
        // 重新获取更新后的碰撞器列表
        colliders = GetComponentsInChildren<Collider>();
        Debug.Log($"样本 {gameObject.name} 现在有 {colliders.Length} 个碰撞器");
    }
    
    void ApplyPhysicsMaterial(PhysicsMaterial material)
    {
        // 重新获取最新的碰撞器列表
        colliders = GetComponentsInChildren<Collider>();
        
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                col.material = material;
                Debug.Log($"为碰撞器 {col.gameObject.name} 应用了物理材质 ({col.GetType().Name})");
            }
        }
    }
    
    public void StartDrop()
    {
        if (isDropping) return;
        
        isDropping = true;
        hasLanded = false;
        bounceCount = 0;
        
        // 设置初始位置（在地面上方2.5m）
        Vector3 dropPosition = FindDropPosition();
        transform.position = dropPosition;
        
        // 只设置垂直掉落，不添加水平速度
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // 纯重力掉落
            rb.angularVelocity = Vector3.zero; // 不旋转
        }
        
        // 播放掉落音效
        PlayDropSound();
        
        Debug.Log($"样本开始垂直掉落: {gameObject.name} 从高度 {dropPosition.y:F1}m");
    }
    
    Vector3 FindDropPosition()
    {
        Vector3 dropPos = initialPosition;
        
        // 射线检测找到地面
        RaycastHit hit;
        if (Physics.Raycast(initialPosition, Vector3.down, out hit, 100f, groundLayer))
        {
            // 在地面上方设置掉落高度（2.5m）
            dropPos.y = hit.point.y + dropHeight;
        }
        else
        {
            // 如果找不到地面，使用当前位置上方
            dropPos.y = initialPosition.y + dropHeight;
        }
        
        return dropPos;
    }
    
    void Update()
    {
        if (!isDropping) return;
        
        CheckGroundStatus();
        UpdateLandingLogic();
    }
    
    void FixedUpdate()
    {
        // 在掉落过程中持续施加额外的向下重力
        if (isDropping && !hasLanded && rb != null)
        {
            // 施加额外的向下力，确保样本快速下降
            rb.AddForce(Vector3.down * additionalGravity, ForceMode.Acceleration);
        }
    }
    
    void CheckGroundStatus()
    {
        // 地面检测
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.2f, groundLayer))
        {
            if (!isGrounded)
            {
                isGrounded = true;
                groundedTime = 0f;
            }
            else
            {
                groundedTime += Time.deltaTime;
            }
        }
        else
        {
            isGrounded = false;
            groundedTime = 0f;
        }
    }
    
    void UpdateLandingLogic()
    {
        timeSinceLastBounce += Time.deltaTime;
        
        // 检查是否已经稳定着陆
        if (isGrounded && rb.linearVelocity.magnitude < settleThreshold && groundedTime > 1.0f)
        {
            if (!hasLanded)
            {
                OnLanded();
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!isDropping || hasLanded) return;
        
        // 检查是否撞击地面
        if (IsGroundCollision(collision))
        {
            HandleGroundImpact(collision);
        }
    }
    
    bool IsGroundCollision(Collision collision)
    {
        // 检查碰撞对象是否在地面层级中
        return ((1 << collision.gameObject.layer) & groundLayer) != 0;
    }
    
    void HandleGroundImpact(Collision collision)
    {
        Vector3 impactVelocity = rb.linearVelocity;
        float impactForce = impactVelocity.magnitude;
        
        // 播放弹跳音效
        if (bounceCount < maxBounces && impactForce > 1.0f)
        {
            PlayBounceSound(impactForce);
            bounceCount++;
            timeSinceLastBounce = 0f;
        }
        
        // 生成灰尘效果
        if (enableDustEffect && impactForce > 2.0f)
        {
            CreateDustEffect(collision.contacts[0].point);
        }
        
        Debug.Log($"样本撞击地面: 力度 {impactForce:F1}, 弹跳次数 {bounceCount}");
    }
    
    void OnLanded()
    {
        hasLanded = true;
        isDropping = false;
        
        Debug.Log($"样本着陆: {gameObject.name} 在位置 {transform.position}");
        
        // 稍微减少物理计算以优化性能
        if (rb != null)
        {
            rb.linearDamping = 2.0f; // 增加阻力以快速稳定
            rb.angularDamping = 5.0f;
        }
        
        // 启动收集系统
        EnableCollectionSystem();
    }
    
    void EnableCollectionSystem()
    {
        // 添加样本收集组件
        SampleCollector collector = GetComponent<SampleCollector>();
        if (collector == null)
        {
            collector = gameObject.AddComponent<SampleCollector>();
        }
        
        // 确保有交互碰撞器
        EnsureInteractionCollider();
        
        Debug.Log($"样本收集系统已启用: {gameObject.name}");
    }
    
    void EnsureInteractionCollider()
    {
        // 检查是否有交互碰撞器
        bool hasInteractionCollider = false;
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                hasInteractionCollider = true;
                break;
            }
        }
        
        // 如果没有交互碰撞器，创建一个
        if (!hasInteractionCollider)
        {
            GameObject interactionTrigger = new GameObject("InteractionTrigger");
            interactionTrigger.transform.SetParent(transform);
            interactionTrigger.transform.localPosition = Vector3.zero;
            
            SphereCollider interactionCollider = interactionTrigger.AddComponent<SphereCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.radius = 1.5f; // 交互范围
        }
    }
    
    void PlayDropSound()
    {
        if (dropSounds != null && dropSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = dropSounds[Random.Range(0, dropSounds.Length)];
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
    
    void PlayBounceSound(float intensity)
    {
        if (bounceSounds != null && bounceSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = bounceSounds[Random.Range(0, bounceSounds.Length)];
            float volume = Mathf.Clamp01(soundVolume * (intensity / 5f)); // 根据撞击力度调整音量
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    void CreateDustEffect(Vector3 position)
    {
        if (dustEffectPrefab != null)
        {
            GameObject dustEffect = Instantiate(dustEffectPrefab, position, Quaternion.identity);
            Destroy(dustEffect, 2f); // 2秒后销毁特效
        }
        else
        {
            // 简单的粒子效果（如果没有预制体）
            CreateSimpleDustEffect(position);
        }
    }
    
    void CreateSimpleDustEffect(Vector3 position)
    {
        // 创建简单的灰尘粒子效果
        GameObject dustParticles = new GameObject("DustEffect");
        dustParticles.transform.position = position;
        
        ParticleSystem particles = dustParticles.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 1.0f;
        main.startSpeed = 2.0f;
        main.startSize = 0.1f;
        main.startColor = new Color(0.7f, 0.6f, 0.4f, 0.8f); // 土黄色
        main.maxParticles = 20;
        
        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 20)
        });
        emission.enabled = true;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        Destroy(dustParticles, 2f);
    }
    
    /// <summary>
    /// 获取当前掉落状态
    /// </summary>
    public bool IsDropping() => isDropping;
    
    /// <summary>
    /// 获取是否已着陆
    /// </summary>
    public bool HasLanded() => hasLanded;
    
    /// <summary>
    /// 强制停止掉落并着陆
    /// </summary>
    public void ForceLanding()
    {
        if (isDropping && !hasLanded)
        {
            // 停止物理运动
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // 放置在地面上
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f, groundLayer))
            {
                transform.position = hit.point + Vector3.up * 0.1f;
            }
            
            OnLanded();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制地面检测射线
        Gizmos.color = Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.2f));
        
        // 绘制掉落起始位置
        if (Application.isPlaying && isDropping)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(initialPosition + Vector3.up * dropHeight, 0.2f);
        }
    }
}