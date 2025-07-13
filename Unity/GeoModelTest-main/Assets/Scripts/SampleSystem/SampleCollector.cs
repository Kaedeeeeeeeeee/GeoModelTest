using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 样本采集交互组件 - 附加到可采集的地质样本上
/// </summary>
public class SampleCollector : MonoBehaviour
{
    [Header("交互设置")]
    public KeyCode collectKey = KeyCode.E;
    public float interactionRange = 2f;
    public LayerMask playerLayer = 1;
    
    [Header("UI提示")]
    public GameObject interactionPrompt;
    public Text promptText;
    public Canvas promptCanvas;
    
    [Header("样本数据")]
    public SampleItem sampleData;
    public string sourceToolID = "";
    
    [Header("视觉反馈")]
    public GameObject highlightEffect;
    public Material highlightMaterial;
    
    private bool playerInRange = false;
    private GameObject nearbyPlayer;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    
    void Start()
    {
        SetupSampleData();
        SetupInteractionUI();
        SetupVisualComponents();
    }
    
    void Update()
    {
        CheckPlayerInteraction();
        HandleInput();
        UpdatePromptPosition();
    }
    
    /// <summary>
    /// 设置样本数据
    /// </summary>
    public void Setup(SampleItem sample)
    {
        sampleData = sample;
        if (sampleData != null)
        {
            sourceToolID = sampleData.sourceToolID;
        }
    }
    
    /// <summary>
    /// 设置样本数据（如果还没有）
    /// </summary>
    void SetupSampleData()
    {
        if (sampleData == null)
        {
            // 从现有的地质样本GameObject创建SampleItem
            sampleData = SampleItem.CreateFromGeologicalSample(gameObject, sourceToolID);
            Debug.Log($"自动为样本生成数据: {sampleData.displayName}");
        }
    }
    
    /// <summary>
    /// 设置交互UI
    /// </summary>
    void SetupInteractionUI()
    {
        // 先清理可能存在的旧UI
        CleanupExistingUI();
        
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
    /// 清理现有UI
    /// </summary>
    void CleanupExistingUI()
    {
        if (interactionPrompt != null)
        {
            DestroyImmediate(interactionPrompt);
            interactionPrompt = null;
        }
        
        if (promptCanvas != null)
        {
            DestroyImmediate(promptCanvas.gameObject);
            promptCanvas = null;
        }
        
        // 清理场景中可能存在的重复Canvas
        Canvas[] existingCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in existingCanvases)
        {
            if (canvas.gameObject.name.Contains("SamplePromptCanvas"))
            {
                DestroyImmediate(canvas.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 创建交互提示UI（模仿钻塔UI风格）
    /// </summary>
    void CreateInteractionPrompt()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("SamplePromptCanvas");
        
        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 95; // 比钻塔UI稍低
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建交互提示面板
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasObj.transform);
        
        RectTransform rectTransform = promptObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.2f); // 显示在屏幕下方
        rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(300, 80); // 适合样本信息的大小
        
        // 添加背景
        Image background = promptObj.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.7f);
        
        // 创建文本
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        promptText = textObj.AddComponent<Text>();
        promptText.text = $"[E] 采集 {sampleData?.displayName ?? "地质样本"}";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 20; // 和钻塔UI类似的字体大小
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        
        // 初始隐藏
        promptObj.SetActive(false);
        
        interactionPrompt = promptObj;
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
                originalMaterials[i] = renderers[i].material;
            }
        }
        
        // 添加碰撞器用于交互检测
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = interactionRange;
        }
    }
    
    /// <summary>
    /// 检查玩家交互
    /// </summary>
    void CheckPlayerInteraction()
    {
        // 查找附近的玩家
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
    }
    
    /// <summary>
    /// 玩家离开交互范围
    /// </summary>
    void OnPlayerExit()
    {
        playerInRange = false;
        HideInteractionPrompt();
        DisableHighlight();
    }
    
    /// <summary>
    /// 显示交互提示
    /// </summary>
    void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (promptText != null && sampleData != null)
            {
                promptText.text = $"[E] 采集 {sampleData.displayName}";
            }
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
        if (renderers != null && highlightMaterial != null)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = highlightMaterial;
                }
            }
        }
    }
    
    /// <summary>
    /// 禁用高亮效果
    /// </summary>
    void DisableHighlight()
    {
        if (renderers != null && originalMaterials != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    renderers[i].material = originalMaterials[i];
                }
            }
        }
    }
    
    /// <summary>
    /// 更新提示位置（固定在屏幕下方显示）
    /// </summary>
    void UpdatePromptPosition()
    {
        // UI现在固定显示在屏幕下方，不需要位置计算
        // 只需要更新文本内容
        if (interactionPrompt != null && promptText != null && sampleData != null)
        {
            promptText.text = $"[E] 采集 {sampleData.displayName}";
        }
    }
    
    /// <summary>
    /// 获取玩家摄像机
    /// </summary>
    Camera GetPlayerCamera()
    {
        // 首先尝试 Camera.main
        if (Camera.main != null)
        {
            return Camera.main;
        }
        
        // 如果没有找到，搜索所有摄像机
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            // 查找包含 "Player", "Main", "FPS" 等关键词的摄像机
            if (cam.gameObject.name.ToLower().Contains("player") ||
                cam.gameObject.name.ToLower().Contains("main") ||
                cam.gameObject.name.ToLower().Contains("fps") ||
                cam.gameObject.name.ToLower().Contains("camera"))
            {
                return cam;
            }
            
            // 或者查找在玩家对象下的摄像机
            if (cam.transform.parent != null)
            {
                string parentName = cam.transform.parent.name.ToLower();
                if (parentName.Contains("lily") || parentName.Contains("player"))
                {
                    return cam;
                }
            }
        }
        
        // 最后返回第一个启用的摄像机
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                return cam;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            CollectSample();
        }
    }
    
    /// <summary>
    /// 采集样本
    /// </summary>
    void CollectSample()
    {
        if (sampleData == null)
        {
            Debug.LogWarning("样本数据为空，无法采集");
            return;
        }
        
        // 查找样本背包
        var inventory = FindFirstObjectByType<SampleInventory>();
        if (inventory == null)
        {
            ShowMessage("未找到样本背包系统！");
            return;
        }
        
        // 尝试添加到背包
        if (inventory.TryAddSample(sampleData))
        {
            ShowCollectionFeedback();
            // 采集成功，销毁世界中的样本
            Destroy(gameObject);
        }
        else
        {
            ShowMessage("背包已满或无法添加样本！");
        }
    }
    
    /// <summary>
    /// 显示采集反馈
    /// </summary>
    void ShowCollectionFeedback()
    {
        ShowMessage($"已采集样本: {sampleData.displayName}");
        
        // 播放采集音效（如果有）
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        Debug.Log($"样本已采集: {sampleData.displayName} (ID: {sampleData.sampleID})");
    }
    
    /// <summary>
    /// 显示消息（临时实现）
    /// </summary>
    void ShowMessage(string message)
    {
        Debug.Log($"[SampleCollector] {message}");
        // TODO: 实现屏幕消息显示系统
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
    }
    
    /// <summary>
    /// 在Scene视图中绘制交互范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}