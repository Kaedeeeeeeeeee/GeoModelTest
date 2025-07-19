using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 仓库系统交互触发器 - 可调节位置的F键交互触发器
/// </summary>
public class WarehouseTrigger : MonoBehaviour
{
    [Header("触发器设置")]
    public float interactionRange = 3f;
    public LayerMask playerLayer = 1;
    
    [Header("位置调节 (可在Inspector中调整)")]
    public Vector3 triggerPosition = new Vector3(-1.924f, 1.06f, 0.662f);
    public Vector3 triggerRotation = Vector3.zero;
    public Vector3 triggerScale = Vector3.one;
    
    [Header("UI提示")]
    public GameObject interactionPrompt;
    public Text promptText;
    public Canvas promptCanvas;
    
    [Header("视觉反馈")]
    public GameObject highlightEffect;
    public Material highlightMaterial;
    public Color highlightColor = Color.cyan;
    
    // 私有成员
    private bool playerInRange = false;
    private GameObject nearbyPlayer;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isHighlighted = false;
    
    void Start()
    {
        // 强制确保使用正确的位置
        EnsureCorrectPosition();
        
        ApplyTransformSettings();
        SetupInteractionUI();
        SetupVisualComponents();
        SetupCollider();
        
        Debug.Log($"仓库触发器初始化完成，位置: {transform.position}");
    }
    
    void Update()
    {
        CheckPlayerInteraction();
        HandleInput();
        UpdatePromptPosition();
        
        // 实时应用Inspector中的变换设置
        if (transform.position != triggerPosition || 
            transform.eulerAngles != triggerRotation || 
            transform.localScale != triggerScale)
        {
            ApplyTransformSettings();
        }
    }
    
    /// <summary>
    /// 应用Inspector中的变换设置
    /// </summary>
    void ApplyTransformSettings()
    {
        transform.position = triggerPosition;
        transform.eulerAngles = triggerRotation;
        transform.localScale = triggerScale;
    }
    
    /// <summary>
    /// 确保使用正确的位置
    /// </summary>
    void EnsureCorrectPosition()
    {
        Vector3 correctPosition = new Vector3(-1.924f, 1.06f, 0.662f);
        
        // 检查是否为旧的错误值
        if (triggerPosition == Vector3.zero || triggerPosition == new Vector3(0, 1, 0))
        {
            Debug.LogWarning($"WarehouseTrigger检测到错误位置值: {triggerPosition}，修正为: {correctPosition}");
            triggerPosition = correctPosition;
        }
        
        // 确保位置正确
        if (triggerPosition != correctPosition)
        {
            Debug.LogWarning($"WarehouseTrigger位置不正确: {triggerPosition}，修正为: {correctPosition}");
            triggerPosition = correctPosition;
        }
    }
    
    /// <summary>
    /// 强制更新触发器位置（外部调用）
    /// </summary>
    public void ForceUpdatePosition(Vector3 newPosition, Vector3 newRotation, Vector3 newScale)
    {
        triggerPosition = newPosition;
        triggerRotation = newRotation;
        triggerScale = newScale;
        
        ApplyTransformSettings();
        
        Debug.Log($"WarehouseTrigger位置已强制更新为: {newPosition}");
    }
    
    /// <summary>
    /// 设置碰撞器
    /// </summary>
    void SetupCollider()
    {
        // 确保有碰撞器用于交互检测
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = interactionRange;
            Debug.Log("为仓库触发器添加了SphereCollider");
        }
        else
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
    
    /// <summary>
    /// 设置交互UI
    /// </summary>
    void SetupInteractionUI()
    {
        if (interactionPrompt == null)
        {
            CreateInteractionPrompt();
        }
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    /// <summary>
    /// 创建交互提示UI
    /// </summary>
    void CreateInteractionPrompt()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("WarehousePromptCanvas");
        
        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 150; // 高于样本收集系统提示
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建交互提示面板
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasObj.transform);
        
        RectTransform rectTransform = promptObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.3f); // 显示在屏幕中下方
        rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(350, 80); // 稍大一点适合仓库文本
        
        // 添加背景 - 橙色以区别于其他交互
        Image background = promptObj.AddComponent<Image>();
        background.color = new Color(0.9f, 0.5f, 0.1f, 0.8f); // 橙色背景
        
        // 创建文本
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        promptText = textObj.AddComponent<Text>();
        promptText.text = "[F] 打开仓库";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 22;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedPrompt = textObj.AddComponent<LocalizedText>();
        localizedPrompt.TextKey = "warehouse.interaction.prompt";
        
        // 初始隐藏
        promptObj.SetActive(false);
        
        interactionPrompt = promptObj;
        
        Debug.Log("仓库交互提示UI创建完成");
    }
    
    /// <summary>
    /// 设置视觉组件
    /// </summary>
    void SetupVisualComponents()
    {
        // 获取所有渲染器
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    originalMaterials[i] = renderers[i].material;
                }
            }
        }
        else
        {
            // 如果没有渲染器，创建一个简单的立方体作为视觉指示
            CreateVisualIndicator();
        }
    }
    
    /// <summary>
    /// 创建视觉指示器
    /// </summary>
    void CreateVisualIndicator()
    {
        GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualObj.name = "WarehouseVisualIndicator";
        visualObj.transform.SetParent(transform);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localScale = new Vector3(1f, 2f, 1f); // 高一点的立方体
        
        // 设置材质
        Renderer renderer = visualObj.GetComponent<Renderer>();
        Material warehouseMaterial = new Material(Shader.Find("Standard"));
        warehouseMaterial.color = new Color(0.6f, 0.8f, 1f, 0.7f); // 淡蓝色半透明
        warehouseMaterial.SetFloat("_Mode", 3); // 设置为透明模式
        warehouseMaterial.SetFloat("_Metallic", 0.2f);
        warehouseMaterial.SetFloat("_Glossiness", 0.8f);
        renderer.material = warehouseMaterial;
        
        // 移除碰撞器（父对象已有）
        Collider visualCollider = visualObj.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // 更新渲染器数组
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
        
        Debug.Log("创建了仓库视觉指示器");
    }
    
    /// <summary>
    /// 检查玩家交互
    /// </summary>
    void CheckPlayerInteraction()
    {
        // 方法1：使用OverlapSphere检测
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        
        bool foundPlayer = false;
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player") || collider.name.Contains("Lily"))
            {
                nearbyPlayer = collider.gameObject;
                foundPlayer = true;
                break;
            }
        }
        
        // 方法2：直接查找玩家位置（备用）
        if (!foundPlayer)
        {
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= interactionRange)
                {
                    nearbyPlayer = player.gameObject;
                    foundPlayer = true;
                }
            }
        }
        
        if (foundPlayer && !playerInRange)
        {
            OnPlayerEnter();
        }
        else if (!foundPlayer && playerInRange)
        {
            OnPlayerExit();
        }
    }
    
    /// <summary>
    /// 玩家进入交互范围
    /// </summary>
    void OnPlayerEnter()
    {
        playerInRange = true;
        ShowInteractionPrompt();
        EnableHighlight();
        Debug.Log("玩家进入仓库交互范围");
    }
    
    /// <summary>
    /// 玩家离开交互范围
    /// </summary>
    void OnPlayerExit()
    {
        playerInRange = false;
        HideInteractionPrompt();
        DisableHighlight();
        Debug.Log("玩家离开仓库交互范围");
    }
    
    /// <summary>
    /// 显示交互提示
    /// </summary>
    void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏交互提示
    /// </summary>
    void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    /// <summary>
    /// 启用高亮效果
    /// </summary>
    void EnableHighlight()
    {
        if (isHighlighted || renderers == null) return;
        
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                if (highlightMaterial != null)
                {
                    renderer.material = highlightMaterial;
                }
                else
                {
                    // 使用颜色高亮
                    Material tempMaterial = new Material(renderer.material);
                    tempMaterial.color = highlightColor;
                    renderer.material = tempMaterial;
                }
            }
        }
        
        isHighlighted = true;
    }
    
    /// <summary>
    /// 禁用高亮效果
    /// </summary>
    void DisableHighlight()
    {
        if (!isHighlighted || renderers == null || originalMaterials == null) return;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
        
        isHighlighted = false;
    }
    
    /// <summary>
    /// 更新提示位置
    /// </summary>
    void UpdatePromptPosition()
    {
        // UI固定显示在屏幕下方，不需要位置计算
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        if (!playerInRange) return;
        
        // F键打开仓库
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            OpenWarehouse();
        }
    }
    
    /// <summary>
    /// 打开仓库界面
    /// </summary>
    void OpenWarehouse()
    {
        Debug.Log("F键按下 - 准备打开仓库界面");
        
        // 查找或创建仓库UI
        WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI == null)
        {
            Debug.Log("未找到WarehouseUI，需要创建");
            // TODO: 创建WarehouseUI
        }
        else
        {
            warehouseUI.OpenWarehouseInterface();
        }
        
        // 隐藏交互提示
        HideInteractionPrompt();
    }
    
    /// <summary>
    /// 在Scene视图中绘制交互范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = new Color(0.9f, 0.5f, 0.1f, 0.3f); // 橙色半透明
        Gizmos.DrawSphere(transform.position, interactionRange);
        
        // 绘制交互范围边界
        Gizmos.color = new Color(0.9f, 0.5f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 绘制高亮指示
        if (isHighlighted)
        {
            Gizmos.color = highlightColor;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
    
    /// <summary>
    /// 在Inspector中显示触发器信息
    /// </summary>
    [ContextMenu("显示触发器状态")]
    void ShowTriggerStatus()
    {
        Debug.Log("=== 仓库触发器状态 ===");
        Debug.Log($"playerInRange: {playerInRange}");
        Debug.Log($"交互范围: {interactionRange}m");
        Debug.Log($"位置: {transform.position}");
        Debug.Log($"触发器设置位置: {triggerPosition}");
        
        if (nearbyPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, nearbyPlayer.transform.position);
            Debug.Log($"玩家距离: {distance:F2}m");
        }
        else
        {
            Debug.Log("未检测到附近玩家");
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        if (interactionPrompt != null)
        {
            Destroy(interactionPrompt);
        }
        
        if (promptCanvas != null)
        {
            Destroy(promptCanvas.gameObject);
        }
    }
}