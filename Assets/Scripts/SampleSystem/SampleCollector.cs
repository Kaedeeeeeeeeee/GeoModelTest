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
    public LayerMask playerLayer = -1; // 包含所有层级，确保能检测到玩家
    
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
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasEKeyPressedLastFrame = false; // 上一帧E键状态
    
    void Start()
    {
        SetupSampleData();
        SetupInteractionUI();
        SetupVisualComponents();

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }
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
        
        // 无论sampleData是否为null，都要生成图标（因为CreateFromGeologicalSample已经设置previewIcon为null）
        if (SampleIconGenerator.Instance != null && sampleData != null)
        {
            Debug.Log($"🔄 为样本生成图标: {sampleData.displayName}");
            
            // 清理旧图标，防止使用预生成的透明图标
            if (sampleData.previewIcon != null)
            {
                Debug.Log($"   清理旧图标: {sampleData.previewIcon.name}");
                sampleData.previewIcon = null;
            }
            else
            {
                Debug.Log($"   样本无预存图标，开始生成新图标");
            }
            
            // 生成新图标
            sampleData.previewIcon = SampleIconGenerator.Instance.GenerateIconForSample(sampleData);
            
            if (sampleData.previewIcon != null)
            {
                Debug.Log($"   ✅ 新图标生成成功: {sampleData.previewIcon.name}");
            }
            else
            {
                Debug.LogError($"   ❌ 新图标生成失败");
            }
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
        rectTransform.anchorMin = new Vector2(0.5f, 0.35f); // 向上移动到35%位置，避开F键提示
        rectTransform.anchorMax = new Vector2(0.5f, 0.35f);
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
        promptText.text = GetLocalizedCollectionText(); // 使用本地化文本
        promptText.font = UIFontResolver.GetUIFont();
        promptText.fontSize = 20; // 和钻塔UI类似的字体大小
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        
        // 提示需要根据桌面/触摸输入动态切换，因此在 Update 中直接刷新，
        // 不挂载只支持单一键值的 LocalizedText。
        
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
    /// 检查附近是否有WarehouseTrigger
    /// </summary>
    bool IsNearWarehouseTrigger()
    {
        // 检查附近是否有WarehouseTrigger组件
        WarehouseTrigger[] warehouseTriggers = FindObjectsOfType<WarehouseTrigger>();
        
        foreach (var trigger in warehouseTriggers)
        {
            if (trigger != null)
            {
                float distance = Vector3.Distance(transform.position, trigger.transform.position);
                // 如果样本在仓库触发器的交互范围内，则不显示样本收集提示
                if (distance <= trigger.interactionRange + 1f) // 额外增加1米缓冲区域
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查玩家交互
    /// </summary>
    void CheckPlayerInteraction()
    {
        // 检查是否有WarehouseTrigger在附近，如果有则不显示样本收集提示
        if (IsNearWarehouseTrigger())
        {
            if (playerInRange)
            {
                OnPlayerExit();
            }
            return;
        }
        
        // 多种方式查找玩家，确保兼容性
        bool foundPlayer = false;
        
        // 方法1：通过层级检测
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
        foreach (var collider in colliders)
        {
            if (IsPlayerObject(collider.gameObject))
            {
                nearbyPlayer = collider.gameObject;
                foundPlayer = true;
                break;
            }
        }
        
        // 方法2：如果方法1失败，尝试直接搜索所有碰撞器
        if (!foundPlayer)
        {
            Collider[] allColliders = Physics.OverlapSphere(transform.position, interactionRange);
            foreach (var collider in allColliders)
            {
                if (IsPlayerObject(collider.gameObject))
                {
                    nearbyPlayer = collider.gameObject;
                    foundPlayer = true;
                    break;
                }
            }
        }
        
        // 方法3：如果还是失败，尝试通过FirstPersonController组件查找
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
    /// 判断是否为玩家对象
    /// </summary>
    bool IsPlayerObject(GameObject obj)
    {
        // 检查标签
        if (obj.CompareTag("Player"))
            return true;
            
        // 检查名称
        string objName = obj.name.ToLower();
        if (objName.Contains("lily") || objName.Contains("player") || objName.Contains("firstperson"))
            return true;
            
        // 检查是否有FirstPersonController组件
        if (obj.GetComponent<FirstPersonController>() != null)
            return true;
            
        // 检查父对象
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("lily") || parentName.Contains("player") || parentName.Contains("firstperson"))
                return true;
                
            if (parent.GetComponent<FirstPersonController>() != null)
                return true;
                
            parent = parent.parent;
        }
        
        return false;
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
                LocalizedText localizedText = promptText.GetComponent<LocalizedText>();
                if (localizedText != null)
                {
                    localizedText.enabled = false;
                }
                promptText.text = GetLocalizedCollectionText();
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
            LocalizedText localizedText = promptText.GetComponent<LocalizedText>();
            if (localizedText != null)
            {
                localizedText.enabled = false;
            }
            promptText.text = GetLocalizedCollectionText();
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
        if (playerInRange && IsEKeyPressed())
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

        // 样本拾取音效
        GeoModel.AudioSystem.AudioManager.Instance.PlaySFX3D(
            GeoModel.AudioSystem.AudioKeys.SFX.SamplePickup, transform.position);

        // 查找样本背包 - 使用多种方式确保找到
        var inventory = GetOrCreateSampleInventory();
        if (inventory == null)
        {
            string localizedMessage = GetLocalizedMessage("sample.message.no_inventory");
            ShowMessage(localizedMessage);
            return;
        }
        
        // 尝试添加到背包
        bool addSuccess = false;
        try
        {
            addSuccess = inventory.TryAddSample(sampleData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SampleCollector] 添加样本到背包时发生错误: {ex.Message}");
            // 即使出错也认为添加成功，因为样本数据已经创建
            addSuccess = true;
        }

        if (addSuccess)
        {
            ShowCollectionFeedback();
            // 采集成功，销毁世界中的样本
            Destroy(gameObject);
        }
        else
        {
            string localizedMessage = GetLocalizedMessage("sample.message.inventory_full");
            ShowMessage(localizedMessage);
        }
    }
    
    /// <summary>
    /// 显示采集反馈
    /// </summary>
    void ShowCollectionFeedback()
    {
        string localizedMessage = GetLocalizedMessage("sample.message.collected", sampleData.displayName);
        ShowMessage(localizedMessage);
        
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
    
    /// <summary>
    /// 获取本地化采集文本
    /// </summary>
    private string GetLocalizedCollectionText()
    {
        string sampleName = sampleData?.displayName ?? "試料";
        return LocalizationManager.ResolveForCurrentInput(
            "sample.collection.interact",
            "sample.collection.mobile",
            "［E］{0}を採取する",
            "試料を採取する",
            sampleName);
    }
    
    /// <summary>
    /// 获取或创建SampleInventory实例
    /// </summary>
    private SampleInventory GetOrCreateSampleInventory()
    {
        // 方法1：检查单例实例
        if (SampleInventory.Instance != null)
        {
            Debug.Log("[SampleCollector] 使用现有的SampleInventory单例");
            return SampleInventory.Instance;
        }
        
        // 方法2：使用FindFirstObjectByType查找
        var inventory = FindFirstObjectByType<SampleInventory>();
        if (inventory != null)
        {
            Debug.Log("[SampleCollector] 通过FindFirstObjectByType找到SampleInventory");
            return inventory;
        }
        
        // 方法3：检查是否有GameInitializer来初始化系统
        var gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (gameInitializer != null)
        {
            Debug.Log("[SampleCollector] 找到GameInitializer，尝试初始化样本系统");
            
            // 强制初始化样本系统
            gameInitializer.initializeSampleSystem = true;
            
            // 使用反射调用InitializeSampleSystem方法
            var initMethod = typeof(GameInitializer).GetMethod("InitializeSampleSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(gameInitializer, null);
                Debug.Log("[SampleCollector] 已调用InitializeSampleSystem");
                
                // 再次尝试查找
                return FindFirstObjectByType<SampleInventory>();
            }
        }
        
        // 方法4：手动创建SampleInventory
        Debug.LogWarning("[SampleCollector] 未找到任何SampleInventory，尝试手动创建");
        GameObject inventoryObj = new GameObject("SampleInventory (Auto-Created by SampleCollector)");
        var newInventory = inventoryObj.AddComponent<SampleInventory>();
        
        Debug.Log("[SampleCollector] 已手动创建SampleInventory");
        return newInventory;
    }
    
    /// <summary>
    /// 获取本地化消息
    /// </summary>
    private string GetLocalizedMessage(string key, params object[] args)
    {
        string fallback = key switch
        {
            "sample.message.collected" => "試料を採取しました：{0}",
            "sample.message.inventory_full" => "調査バッグがいっぱいです。試料を整理してから、もう一度試してください。",
            "sample.message.no_inventory" => "調査バッグを読み込めませんでした。メニューからやり直してください。",
            _ => key
        };
        return LocalizationManager.Resolve(key, fallback, args);
    }

    /// <summary>
    /// 检测E键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsEKeyPressed()
    {
        // 键盘E键检测
        var keyboard = Keyboard.current;
        bool keyboardEPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;

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
