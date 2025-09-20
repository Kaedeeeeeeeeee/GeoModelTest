using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 已放置样本收集器 - 处理放置在世界中的样本的收回功能
/// </summary>
public class PlacedSampleCollector : MonoBehaviour
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
    public SampleItem originalSampleData;
    
    [Header("视觉反馈")]
    public GameObject highlightEffect;
    public Material highlightMaterial;
    public Color highlightColor = Color.cyan;
    
    // 私有成员
    private bool playerInRange = false;
    private GameObject nearbyPlayer;
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasEKeyPressedLastFrame = false; // 上一帧E键状态
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Color[] originalColors;
    private bool isHighlighted = false;
    
    void Start()
    {
        SetupInteractionUI();
        SetupVisualComponents();
        ValidateSampleData();

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }

        // 启动时检查Input System状态
        StartCoroutine(CheckInputSystemOnStart());
    }
    
    /// <summary>
    /// 启动时检查Input System状态（简化版）
    /// </summary>
    System.Collections.IEnumerator CheckInputSystemOnStart()
    {
        yield return new WaitForSeconds(1f); // 等待1秒确保所有系统初始化完成
        
        #if UNITY_EDITOR
        if (Keyboard.current == null)
        {
            Debug.LogWarning("[PlacedSampleCollector] Input System未正确初始化");
            // 尝试强制启用
            UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.Keyboard.current);
        }
        #endif
    }
    
    void Update()
    {
        CheckPlayerInteraction();
        HandleInput();
        UpdatePromptPosition();
        
        // Input System状态检查已简化，仅在编辑器中输出错误
    }
    
    /// <summary>
    /// 设置样本数据
    /// </summary>
    public void Setup(SampleItem sampleData)
    {
        originalSampleData = sampleData;
        ValidateSampleData();
        
        if (promptText != null && originalSampleData != null)
        {
            promptText.text = $"[E] 收回 {originalSampleData.displayName}";
        }
        
        #if UNITY_EDITOR
        if (originalSampleData != null)
            Debug.Log($"PlacedSampleCollector已设置: {originalSampleData.displayName}");
        #endif
    }
    
    /// <summary>
    /// 验证样本数据
    /// </summary>
    void ValidateSampleData()
    {
        if (originalSampleData == null)
        {
            Debug.LogError($"PlacedSampleCollector ({gameObject.name}) 缺少样本数据！");
        }
        #if UNITY_EDITOR
        else if (originalSampleData.currentLocation != SampleLocation.InWorld)
        {
            Debug.LogWarning($"样本 {originalSampleData.displayName} 的位置状态不正确: {originalSampleData.currentLocation}");
        }
        #endif
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
            if (canvas.gameObject.name.Contains("PlacedSamplePromptCanvas"))
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
        GameObject canvasObj = new GameObject("PlacedSamplePromptCanvas");
        
        promptCanvas = canvasObj.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 96; // 比普通采集提示稍高
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建交互提示面板
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasObj.transform);
        
        RectTransform rectTransform = promptObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.25f); // 显示在屏幕下方，比采集提示稍高
        rectTransform.anchorMax = new Vector2(0.5f, 0.25f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(320, 80); // 稍大一点，适合收回文本
        
        // 添加背景 - 蓝色以区别于普通采集
        Image background = promptObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.4f, 0.8f, 0.8f); // 蓝色背景
        
        // 创建文本
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        promptText = textObj.AddComponent<Text>();
        promptText.text = $"[E] 收回 {originalSampleData?.displayName ?? "样本"}";
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
            originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
                originalColors[i] = renderers[i].material.color;
            }
        }
        
        // 确保有碰撞器用于交互检测
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = interactionRange;
        }
        else
        {
            // 确保现有碰撞器是触发器
            GetComponent<Collider>().isTrigger = true;
        }
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
                // 检测到玩家 - 简化输出
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
                    // 找到玩家
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
            if (promptText != null && originalSampleData != null)
            {
                promptText.text = $"[E] 收回 {originalSampleData.displayName}";
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
        if (isHighlighted) return;
        
        if (renderers != null)
        {
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
        }
        
        isHighlighted = true;
    }
    
    /// <summary>
    /// 禁用高亮效果
    /// </summary>
    void DisableHighlight()
    {
        if (!isHighlighted) return;
        
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
        
        isHighlighted = false;
    }
    
    /// <summary>
    /// 更新提示位置（固定在屏幕下方显示）
    /// </summary>
    void UpdatePromptPosition()
    {
        // UI现在固定显示在屏幕下方，不需要位置计算
        // 只需要更新文本内容
        if (interactionPrompt != null && promptText != null && originalSampleData != null)
        {
            promptText.text = $"[E] 收回 {originalSampleData.displayName}";
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
        if (!playerInRange) return;
        
        // 使用统一的E键检测（支持键盘和移动端）
        if (IsEKeyPressed())
        {
            CollectPlacedSample();
        }
        #if UNITY_EDITOR
        else if (Time.frameCount % 300 == 0) // 每5秒检查一次
        {
            Debug.LogWarning("❌ Keyboard.current为null，Input System可能未正确初始化");
        }
        #endif
    }
    
    /// <summary>
    /// 收回已放置的样本
    /// </summary>
    void CollectPlacedSample()
    {
        // 收回样本逻辑
        
        if (originalSampleData == null)
        {
            ShowMessage("样本数据丢失，无法收回！");
            Debug.LogError("originalSampleData为null");
            return;
        }
        
        
        // 查找样本背包
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            ShowMessage("未找到样本背包系统！");
            Debug.LogError("SampleInventory.Instance为null");
            return;
        }
        
        
        // 检查背包是否还有空间
        if (!inventory.CanAddSample())
        {
            ShowMessage("背包已满，无法收回样本！");
            return;
        }
        
        
        // 将样本重新添加到背包
        if (inventory.AddSampleBackToInventory(originalSampleData))
        {
            ShowCollectionFeedback();
            
            // 从场景跟踪器中注销
            PlacedSampleTracker.UnregisterPlacedSample(gameObject);
            
            // 收回成功，销毁世界中的样本
            Destroy(gameObject);
        }
        else
        {
            ShowMessage("无法收回样本到背包！");
            Debug.LogError("AddSampleBackToInventory失败");
        }
    }
    
    /// <summary>
    /// 显示收回反馈
    /// </summary>
    void ShowCollectionFeedback()
    {
        ShowMessage($"已收回样本: {originalSampleData.displayName}");
        
        // 播放收回音效（如果有）
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
    }
    
    /// <summary>
    /// 显示消息（临时实现）
    /// </summary>
    void ShowMessage(string message)
    {
        Debug.Log($"[PlacedSampleCollector] {message}");
        // TODO: 实现屏幕消息显示系统
    }
    
    /// <summary>
    /// 获取样本信息（调试用）
    /// </summary>
    public string GetSampleInfo()
    {
        if (originalSampleData != null)
        {
            return $"放置的样本: {originalSampleData.displayName} (ID: {originalSampleData.sampleID})";
        }
        return "无样本数据";
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
        
        // 确保从跟踪器中移除
        PlacedSampleTracker.UnregisterPlacedSample(gameObject);
    }
    
    /// <summary>
    /// 在Scene视图中绘制交互范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 绘制高亮指示
        if (isHighlighted)
        {
            Gizmos.color = highlightColor;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
    }
    
    /// <summary>
    /// 在Inspector中显示样本信息
    /// </summary>
    [ContextMenu("显示样本信息")]
    void ShowSampleInfo()
    {
        Debug.Log(GetSampleInfo());
    }
    
    /// <summary>
    /// 调试收回系统状态（仅编辑器模式）
    /// </summary>
    [ContextMenu("调试收回系统")]
    void DebugCollectionSystem()
    {
        #if UNITY_EDITOR
        Debug.Log("=== 收回系统调试信息 ===");
        Debug.Log($"playerInRange: {playerInRange}");
        Debug.Log($"originalSampleData: {(originalSampleData != null ? originalSampleData.displayName : "null")}");
        Debug.Log($"SampleInventory.Instance: {(SampleInventory.Instance != null ? "存在" : "null")}");
        
        if (nearbyPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, nearbyPlayer.transform.position);
            Debug.Log($"玩家距离: {distance:F2}m (交互范围: {interactionRange}m)");
        }
        #endif
    }

    /// <summary>
    /// 检测E键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsEKeyPressed()
    {
        // 键盘E键检测
        bool keyboardEPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        // 移动端E键检测
        bool mobileEPressed = false;
        if (mobileInputManager != null)
        {
            bool currentEKeyState = mobileInputManager.IsInteracting;
            mobileEPressed = currentEKeyState && !wasEKeyPressedLastFrame;
            wasEKeyPressedLastFrame = currentEKeyState;
        }

        return keyboardEPressed || mobileEPressed;
    }
}