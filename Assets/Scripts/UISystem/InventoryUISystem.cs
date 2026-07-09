using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class InventoryUISystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject wheelUI;
    public Transform wheelCenter;
    public RectTransform[] wheelSlots = new RectTransform[8];
    public Image[] slotImages = new Image[8];
    public Text[] slotTexts = new Text[8];
    public Image wheelBackground;
    public Image[] slotSeparators = new Image[8];
    
    [Header("Selection")]
    public float wheelSizePercent = 90f;
    public float selectionRadius = 100f;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    [Header("Visual Settings")]
    public Color wheelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color separatorColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    public float separatorWidth = 4f;
    public Color slotBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    public Color selectedSlotBackgroundColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);
    public Color textShadowColor = new Color(0f, 0f, 0f, 0.8f);
    
    private bool isWheelOpen = false;
    private bool wheelOpenedByMobileInput = false; // 标记轮盘是否由移动端输入打开
    private int selectedSlot = -1;
    private int mobileWheelCandidateSlot = -1;
    private Camera playerCamera;
    private FirstPersonController fpController;
    private Canvas canvas;
    
    private List<CollectionTool> availableTools = new List<CollectionTool>();
    
    [Header("移动端适配")]
    public bool enableMobileAdaptation = true;
    public bool showMobileToolbar = false; // legacy: mobile now uses the same radial wheel as desktop Tab
    public float mobileToolbarHeight = 120f;
    public int maxVisibleTools = 5; // 移动端一次显示的最大工具数
    
    // 移动端相关组件
    private MobileInputManager mobileInputManager;
    private GameObject mobileToolbar;
    private List<Button> mobileToolButtons = new List<Button>();
    private ScrollRect mobileScrollRect;
    private RectTransform mobileToolbarContent;
    private bool isMobileMode = false;
    private RectTransform mobileToolbarRect;
    private Rect lastMobileToolbarSafeArea;
    private Vector2 lastMobileToolbarScreenSize;


    /// <summary>
    /// 初始化移动端输入事件监听
    /// </summary>
    void InitializeMobileInputEvents()
    {
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager != null)
        {
            // 监听移动端输入事件
            mobileInputManager.OnToolWheelInput += HandleToolWheelInput;
            mobileInputManager.OnInventoryInput += HandleInventoryInput;
            mobileInputManager.OnWarehouseInput += HandleWarehouseInput;
            mobileInputManager.OnEncyclopediaInput += HandleEncyclopediaInput;
            mobileInputManager.OnInteractInput += HandleMobileInteractInput;

            Debug.Log("[InventoryUISystem] 移动端输入事件监听已设置");
        }
        else
        {
            Debug.LogWarning("[InventoryUISystem] 未找到MobileInputManager，移动端UI事件不可用");
        }
    }

    /// <summary>
    /// 处理工具轮盘输入
    /// </summary>
    void HandleToolWheelInput()
    {
        RefreshMobileModeState();

        CloseLegacyMobileToolbar();
        ToggleWheel(true);
    }

    void HandleMobileInteractInput(bool isPressed)
    {
        RefreshMobileModeState();

        if (!isPressed || !isMobileMode || IsToolMenuOpen())
        {
            return;
        }

        ToolManager toolManager = FindFirstObjectByType<ToolManager>(FindObjectsInactive.Include);
        CollectionTool currentTool = toolManager != null ? toolManager.GetCurrentTool() : null;
        if (currentTool != null && currentTool.RequestPrimaryUse())
        {
            Debug.Log($"[InventoryUISystem] 移动端使用工具: {currentTool.toolName} ({currentTool.toolID})");
        }
    }

    bool IsToolMenuOpen()
    {
        return isWheelOpen || (mobileToolbar != null && mobileToolbar.activeSelf);
    }

    bool ShouldUseMobileToolbar()
    {
        return false;
    }

    void ToggleWheel(bool openedByMobileInput)
    {
        if (isWheelOpen)
        {
            CloseWheel(false);
        }
        else
        {
            wheelOpenedByMobileInput = openedByMobileInput;
            OpenWheel();
        }
    }

    void CloseLegacyMobileToolbar()
    {
        if (mobileToolbar != null && mobileToolbar.activeSelf)
        {
            mobileToolbar.SetActive(false);
        }
    }

    /// <summary>
    /// 处理背包输入
    /// </summary>
    void HandleInventoryInput()
    {
        // Debug.Log("[InventoryUISystem] 收到背包输入事件");

        // 查找背包UI并切换状态
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            // 检查背包是否已打开，实现切换功能
            if (inventoryUI.IsInventoryOpen())
            {
                inventoryUI.CloseInventory();
                Debug.Log("[InventoryUISystem] 背包界面已关闭");
            }
            else
            {
                inventoryUI.OpenInventory();
                Debug.Log("[InventoryUISystem] 背包界面已打开");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryUISystem] 未找到InventoryUI组件");
        }
    }

    /// <summary>
    /// 处理仓库输入
    /// </summary>
    void HandleWarehouseInput()
    {
        // Debug.Log("[InventoryUISystem] 收到仓库输入事件");

        // 查找并打开仓库UI
        WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI != null)
        {
            warehouseUI.OpenWarehouseInterface();
            Debug.Log("[InventoryUISystem] 仓库界面已打开");
        }
        else
        {
            Debug.LogWarning("[InventoryUISystem] 未找到WarehouseUI组件");
        }
    }

    /// <summary>
    /// 处理图鉴输入
    /// </summary>
    void HandleEncyclopediaInput()
    {
        Debug.Log("[InventoryUISystem] 收到图鉴输入事件");

        // 查找图鉴UI并切换状态
        Debug.Log("[InventoryUISystem] 开始查找图鉴组件...");

        // 先尝试直接查找EncyclopediaUI
        Encyclopedia.EncyclopediaUI encyclopediaUI = FindFirstObjectByType<Encyclopedia.EncyclopediaUI>();

        if (encyclopediaUI != null)
        {
            Debug.Log("[InventoryUISystem] 找到EncyclopediaUI，准备调用ToggleEncyclopedia()");
            encyclopediaUI.ToggleEncyclopedia();

            // 根据图鉴状态控制鼠标
            HandleEncyclopediaCursor(encyclopediaUI);
            Debug.Log("[InventoryUISystem] 图鉴界面已切换");
            return;
        }

        // 如果没找到EncyclopediaUI，尝试通过EncyclopediaInitializer获取
        Encyclopedia.EncyclopediaInitializer initializer = FindFirstObjectByType<Encyclopedia.EncyclopediaInitializer>();
        if (initializer != null)
        {
            Debug.Log("[InventoryUISystem] 找到EncyclopediaInitializer，尝试通过反射获取EncyclopediaUI");

            // 使用反射获取private字段encyclopediaUI
            var field = initializer.GetType().GetField("encyclopediaUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var ui = field.GetValue(initializer) as Encyclopedia.EncyclopediaUI;
                if (ui != null)
                {
                    Debug.Log("[InventoryUISystem] 通过反射找到EncyclopediaUI，准备调用ToggleEncyclopedia()");
                    ui.ToggleEncyclopedia();

                    // 根据图鉴状态控制鼠标
                    HandleEncyclopediaCursor(ui);
                    Debug.Log("[InventoryUISystem] 图鉴界面已切换");
                    return;
                }
            }
        }

        Debug.LogWarning("[InventoryUISystem] 无法找到可用的图鉴组件");

        // 显示场景中的所有图鉴组件用于调试
        var allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var encyclopediaComponents = allManagers.Where(m => m.GetType().Name.Contains("Encyclopedia")).ToArray();
        Debug.Log($"[InventoryUISystem] 场景中包含Encyclopedia的组件数量: {encyclopediaComponents.Length}");

        foreach (var comp in encyclopediaComponents)
        {
            Debug.Log($"[InventoryUISystem] 找到组件: {comp.GetType().FullName}");
        }
    }

    /// <summary>
    /// 根据图鉴状态控制鼠标显示/隐藏
    /// </summary>
    void HandleEncyclopediaCursor(Encyclopedia.EncyclopediaUI encyclopediaUI)
    {
        // 使用反射获取图鉴的isOpen状态
        var isOpenField = encyclopediaUI.GetType().GetField("isOpen",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (isOpenField != null)
        {
            bool isOpen = (bool)isOpenField.GetValue(encyclopediaUI);

            if (isOpen)
            {
                // 图鉴打开时显示鼠标
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // 禁用第一人称视角控制
                FirstPersonController controller = GetFirstPersonController();
                if (controller != null)
                {
                    controller.SetMouseLookEnabled(false);
                }

                Debug.Log("[InventoryUISystem] 图鉴打开 - 鼠标已显示，视角控制已禁用");
            }
            else
            {
                // 图鉴关闭时隐藏鼠标
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 重新启用第一人称视角控制
                FirstPersonController controller = GetFirstPersonController();
                if (controller != null)
                {
                    controller.SetMouseLookEnabled(true);
                }

                Debug.Log("[InventoryUISystem] 图鉴关闭 - 鼠标已隐藏，视角控制已启用");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryUISystem] 无法获取图鉴isOpen状态，使用默认鼠标设置");
        }
    }

    void CreateWheelUI()
    {
        // 创建圆形轮盘背景
        GameObject wheelBG = new GameObject("WheelBackground");
        wheelBG.transform.SetParent(transform);
        
        RectTransform bgRect = wheelBG.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(300, 300);
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.localPosition = Vector3.zero;
        
        // 创建圆形背景图像
        UnityEngine.UI.Image bgImage = wheelBG.AddComponent<UnityEngine.UI.Image>();
        bgImage.sprite = CreateCircleSprite(256);
        bgImage.color = wheelBackgroundColor;
        bgImage.type = UnityEngine.UI.Image.Type.Simple;
        
        // 初始化数组
        wheelSlots = new RectTransform[8];
        slotImages = new Image[8];
        slotTexts = new Text[8];
        
        // 创建8个轮盘槽位
        for (int i = 0; i < 8; i++)
        {
            // 创建槽位容器
            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(wheelBG.transform);
            
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(80, 80);
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            
            // 添加槽位背景
            UnityEngine.UI.Image slotBG = slot.AddComponent<UnityEngine.UI.Image>();
            slotBG.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            
            // 创建工具图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(50, 50);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            
            UnityEngine.UI.Image iconImage = iconObj.AddComponent<UnityEngine.UI.Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            
            // 创建文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(slot.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100, 20);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, -50);
            
            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "";
            
            // 安全获取字体
            try
            {
                text.font = UIFontResolver.GetUIFont();
            }
            catch
            {
                text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            }
            
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // 保存引用
            wheelSlots[i] = slotRect;
            slotImages[i] = iconImage;
            slotTexts[i] = text;
        }
        
        // 设置引用
        wheelUI = wheelBG;
        wheelBackground = bgImage;
        wheelCenter = wheelBG.transform;
        
        // 立即设置为隐藏状态，避免意外显示
        wheelUI.SetActive(false);
    }

    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ToolWheelCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;
        float feather = Mathf.Max(2f, size * 0.025f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / feather);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    /// <summary>
    /// 加载自定义背景图片
    /// </summary>
    Sprite LoadCustomBackgroundSprite()
    {
        try
        {
            // 尝试从AssetDatabase加载（Editor模式）
#if UNITY_EDITOR
            Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Picture/Image.png");
            if (texture != null)
            {
                // 创建Sprite
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                Debug.Log("✅ 成功加载自定义TabUI背景图片");
                return sprite;
            }
#endif
            
            // 尝试从Resources加载（运行时）
            Texture2D resourceTexture = Resources.Load<Texture2D>("Picture/Image");
            if (resourceTexture != null)
            {
                Sprite sprite = Sprite.Create(resourceTexture, new Rect(0, 0, resourceTexture.width, resourceTexture.height), new Vector2(0.5f, 0.5f));
                Debug.Log("✅ 从Resources加载自定义TabUI背景图片");
                return sprite;
            }
            
            Debug.LogWarning("❌ 无法找到自定义背景图片，使用默认背景");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 加载自定义背景图片时出错: {e.Message}");
            return null;
        }
    }
    
    void Start()
    {
        // 初始化移动端输入管理器连接
        InitializeMobileInputEvents();

        playerCamera = Camera.main;
        fpController = GetFirstPersonController();
        canvas = GetComponent<Canvas>();

        // 强制创建标准的UI结构
        Debug.Log("创建标准的圆形UI");
        DestroyOldUI();
        CreateWheelUI();

        if (wheelUI != null)
        {
            // 确保UI处于隐藏状态
            wheelUI.SetActive(false);
            SetupWheelAppearance();
            UpdateWheelSize();
            Debug.Log("✅ TabUI已初始化并设置为隐藏状态");
        }
        else
        {
            Debug.LogError("❌ wheelUI为null，TabUI初始化失败");
        }

        // 初始化移动端支持
        InitializeMobileSupport();

        StartCoroutine(DelayedInitialize());

        // 额外的安全检查：确保UI在一秒后仍然是隐藏状态
        StartCoroutine(SafetyCheck());
    }

    FirstPersonController GetFirstPersonController()
    {
        if (fpController == null)
        {
            fpController = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        }

        return fpController;
    }
    
    /// <summary>
    /// 安全检查：确保UI在初始化后处于正确的隐藏状态
    /// </summary>
    IEnumerator SafetyCheck()
    {
        yield return new WaitForSeconds(1f);
        
        if (wheelUI != null && wheelUI.activeSelf && !isWheelOpen)
        {
            Debug.LogWarning("⚠️ 检测到TabUI意外显示，强制隐藏");
            wheelUI.SetActive(false);
        }
    }
    
    bool DetectExistingUI()
    {
        // 查找Cycle对象（你创建的圆形背景）
        Transform cycleTransform = transform.Find("Cycle");
        if (cycleTransform != null)
        {
            Debug.Log("找到Cycle背景，设置为wheelUI");
            wheelUI = cycleTransform.gameObject;
            wheelBackground = cycleTransform.GetComponent<Image>();
            wheelCenter = cycleTransform;
            
            // 查找Slot对象
            wheelSlots = new RectTransform[8];
            slotImages = new Image[8];
            slotTexts = new Text[8];
            
            for (int i = 0; i < 8; i++)
            {
                Transform slotTransform = cycleTransform.Find($"Slot_{i}");
                if (slotTransform != null)
                {
                    wheelSlots[i] = slotTransform.GetComponent<RectTransform>();
                    
                    // 查找Icon
                    Transform iconTransform = slotTransform.Find("Icon");
                    if (iconTransform != null)
                    {
                        slotImages[i] = iconTransform.GetComponent<Image>();
                    }
                    
                    // 查找Text
                    Transform textTransform = slotTransform.Find("Text");
                    if (textTransform != null)
                    {
                        slotTexts[i] = textTransform.GetComponent<Text>();
                    }
                }
            }
            
            Debug.Log($"成功检测到现有UI: wheelUI={wheelUI.name}, slots={System.Array.FindAll(wheelSlots, s => s != null).Length}");
            return true;
        }
        
        return false;
    }
    
    void DestroyOldUI()
    {
        // 清理数组引用（先清理引用再销毁对象）
        wheelSlots = new RectTransform[8];
        slotImages = new Image[8]; 
        slotTexts = new Text[8];
        wheelBackground = null;
        wheelCenter = null;
        
        // 删除旧的UI元素（安全检查）
        if (wheelUI != null)
        {
            try
            {
                DestroyImmediate(wheelUI);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"销毁wheelUI时出错: {e.Message}");
            }
            wheelUI = null;
        }
        
        // 查找并删除可能存在的旧UI对象（安全检查）
        try
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child != null && child != transform && child.name != null && 
                    (child.name.Contains("Wheel") || child.name.Contains("Slot") || child.name.Contains("Inventory")))
                {
                    if (child.gameObject != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"清理子对象时出错: {e.Message}");
        }
        
        Debug.Log("已清理旧UI元素");
    }
    
    // 调试方法：强制显示圆形布局信息
    void Update()
    {
        HandleInput();
        
        if (isWheelOpen)
        {
            UpdateSelection();
        }

        if (mobileToolbar != null && mobileToolbar.activeSelf)
        {
            ApplyMobileToolbarLayout();
        }
        
        // 持续的安全检查：确保UI状态与isWheelOpen一致
        if (wheelUI != null && wheelUI.activeSelf != isWheelOpen)
        {
            Debug.LogWarning($"⚠️ TabUI状态不一致：wheelUI.activeSelf={wheelUI.activeSelf}, isWheelOpen={isWheelOpen}，正在修复");
            wheelUI.SetActive(isWheelOpen);
        }
        
        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        // F2键：调试圆形布局
        if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
        {
            DebugCircularLayout();
        }
        
        // R键：刷新工具
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            InitializeTools();
        }
        
        UpdateWheelSize();
    }
    
    void DebugCircularLayout()
    {
        Debug.Log("=== 圆形布局调试信息 ===");
        Debug.Log($"wheelSlots数组长度: {(wheelSlots != null ? wheelSlots.Length : 0)}");
        
        if (wheelSlots != null)
        {
            for (int i = 0; i < wheelSlots.Length; i++)
            {
                if (wheelSlots[i] != null)
                {
                    float angle = i * 45f;
                    Vector2 pos = wheelSlots[i].anchoredPosition;
                    Debug.Log($"Slot {i}: 角度={angle}度, 位置=({pos.x:F1}, {pos.y:F1})");
                }
                else
                {
                    Debug.Log($"Slot {i}: null");
                }
            }
        }
        
        Debug.Log($"wheelUI: {(wheelUI != null ? wheelUI.name : "null")}");
        Debug.Log($"圆形布局应该显示8个slot围成圆形");
    }
    
    IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(0.5f);
        
        InitializeTools();
        
        yield return new WaitForSeconds(1f);
        
        InitializeTools();
    }
    
    private float lastScreenSize = 0f;
    
    void UpdateWheelSize()
    {
        if (wheelUI == null) return;
        
        // 使用80%的屏幕大小
        float screenSize = Mathf.Min(Screen.width, Screen.height);
        
        // 只有屏幕大小变化时才更新
        if (Mathf.Abs(screenSize - lastScreenSize) < 1f) return;
        
        float wheelSize = screenSize * 0.9f; // 改为90%屏幕大小，进一步增大轮盘
        
        RectTransform wheelRect = wheelUI.GetComponent<RectTransform>();
        if (wheelRect != null)
        {
            wheelRect.sizeDelta = new Vector2(wheelSize, wheelSize);
            // 确保轮盘居中
            wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
            wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRect.pivot = new Vector2(0.5f, 0.5f);
            wheelRect.anchoredPosition = Vector2.zero;

            // 调试信息（可根据需要开启）
            // Debug.Log($"[InventoryUISystem] 轮盘尺寸更新 - 屏幕大小: {screenSize}, 轮盘大小: {wheelSize}x{wheelSize}");
            // Debug.Log($"[InventoryUISystem] 轮盘位置 - anchoredPosition: {wheelRect.anchoredPosition}, localPosition: {wheelRect.localPosition}");
        }
        
        selectionRadius = wheelSize * 0.2f;
        
        UpdateSlotPositions(wheelSize);
        UpdateSeparators(wheelSize);
        
        lastScreenSize = screenSize;
        // Debug.Log($"轮盘尺寸已更新为: {wheelSize}x{wheelSize} (屏幕大小: {screenSize})");
    }
    
    void SetupWheelAppearance()
    {
        if (wheelBackground != null)
        {
            wheelBackground.color = wheelBackgroundColor;

            // 确保背景圆圈大小与轮盘一致
            RectTransform bgRect = wheelBackground.GetComponent<RectTransform>();
            RectTransform wheelRect = wheelUI.GetComponent<RectTransform>();
            if (bgRect != null && wheelRect != null)
            {
                bgRect.sizeDelta = wheelRect.sizeDelta;
                Debug.Log($"[InventoryUISystem] 背景圆圈大小已更新为: {bgRect.sizeDelta}");
            }

            Debug.Log($"[InventoryUISystem] 设置wheelBackground颜色: {wheelBackgroundColor}");
        }
        else
        {
            Image wheelImg = wheelUI.GetComponent<Image>();
            if (wheelImg != null)
            {
                wheelImg.color = wheelBackgroundColor;
                Debug.Log($"[InventoryUISystem] 设置wheelImg颜色: {wheelBackgroundColor}");
            }
            else
            {
                Debug.LogWarning("[InventoryUISystem] 未找到轮盘背景Image组件");
            }
        }

        // 使用正常的轮盘背景色（深色半透明）
        // Color testColor = new Color(1f, 0f, 0f, 0.8f); // 临时红色背景已移除

        SetupSeparators();
    }
    
    void SetupSeparators()
    {
        for (int i = 0; i < slotSeparators.Length; i++)
        {
            if (slotSeparators[i] != null)
            {
                slotSeparators[i].color = separatorColor;
            }
        }
        
        if (slotSeparators[0] == null)
        {
            
        }
    }
    
    void UpdateSeparators(float wheelSize)
    {
        float separatorRadius = wheelSize * 0.42f;
        float separatorLength = wheelSize * 0.3f;
        
        for (int i = 0; i < slotSeparators.Length; i++)
        {
            if (slotSeparators[i] != null)
            {
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Vector2 separatorPos = new Vector2(Mathf.Sin(angle) * separatorRadius, Mathf.Cos(angle) * separatorRadius);
                
                RectTransform separatorRect = slotSeparators[i].GetComponent<RectTransform>();
                separatorRect.anchoredPosition = separatorPos;
                separatorRect.sizeDelta = new Vector2(separatorWidth, separatorLength);
                separatorRect.rotation = Quaternion.Euler(0, 0, -i * 45f - 22.5f);
                
                slotSeparators[i].color = separatorColor;
                slotSeparators[i].gameObject.SetActive(true);
            }
        }
        
        if (slotSeparators[0] == null)
        {
            
        }
    }
    
    void UpdateSlotPositions(float wheelSize)
    {
        // 调整为合理的参数，确保slot图标在圆圈内部且布局美观
        float slotSize = wheelSize * 0.12f;   // slot大小为轮盘的12%，增大图标
        float slotRadius = (wheelSize * 0.28f); // slot距离圆心28%的轮盘半径
        
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (wheelSlots[i] != null)
            {
                // 计算圆形位置：从顶部开始，顺时针排列
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 slotPos = new Vector2(Mathf.Sin(angle) * slotRadius, Mathf.Cos(angle) * slotRadius);
                wheelSlots[i].anchoredPosition = slotPos;
                wheelSlots[i].sizeDelta = new Vector2(slotSize, slotSize);
                
                if (slotTexts[i] != null)
                {
                    RectTransform textRect = slotTexts[i].GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(slotSize * 1.8f, slotSize * 0.4f); // 文本大小随slot缩放
                    textRect.anchoredPosition = new Vector2(0, -slotSize * 0.8f); // 文本位置随slot缩放
                    slotTexts[i].fontSize = Mathf.RoundToInt(slotSize * 0.25f); // 字体大小为slot的25%，减小文字
                    
                    Outline outline = slotTexts[i].GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = slotTexts[i].gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = textShadowColor;
                    outline.effectDistance = new Vector2(1, -1);
                }
            }
        }
    }
    
    // 原来的Update方法已合并到上面的新Update方法中
    
    void HandleInput()
    {
        if (wheelUI == null) return; // 安全检查
        
        // 处理移动端输入
        bool mobileInputHandled = false;
        if (enableMobileAdaptation && isMobileMode && mobileInputManager != null)
        {
            mobileInputHandled = HandleMobileInput();
        }
        
        // 如果移动端输入未处理，使用传统桌面输入
        if (!mobileInputHandled)
        {
            HandleDesktopInput();
        }
    }
    
    /// <summary>
    /// 处理移动端输入
    /// </summary>
    bool HandleMobileInput()
    {
        if (isWheelOpen)
        {
            HandleMobileWheelTouchInput();
            return true;
        }

        // 在移动端模式下，输入由移动端按钮事件处理
        // 但在桌面测试模式下，仍需允许桌面输入处理Tab键

        // 检查是否在桌面测试模式
        bool isDesktopTestMode = mobileInputManager != null && mobileInputManager.desktopTestMode;

        if (isDesktopTestMode)
        {
            // 桌面测试模式下，移动端和桌面端输入可以共存
            return false; // 允许桌面输入处理
        }

        // 真正的移动设备上，阻止桌面输入
        return true;
    }

    void HandleMobileWheelTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        var touch = touchscreen.primaryTouch;
        if (touch.press.isPressed)
        {
            int slotIndex = GetWheelSlotAtScreenPoint(touch.position.ReadValue());
            mobileWheelCandidateSlot = slotIndex >= 0 && slotIndex < availableTools.Count ? slotIndex : -1;
            SetSelectedSlot(mobileWheelCandidateSlot);
        }

        if (touch.press.wasReleasedThisFrame)
        {
            if (mobileWheelCandidateSlot >= 0 && mobileWheelCandidateSlot < availableTools.Count)
            {
                SelectToolAtSlot(mobileWheelCandidateSlot, "触屏");
            }
            else
            {
                SetSelectedSlot(-1);
            }

            mobileWheelCandidateSlot = -1;
        }
    }
    
    /// <summary>
    /// 处理桌面端输入
    /// </summary>
    void HandleDesktopInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            RefreshMobileModeState();
            CloseLegacyMobileToolbar();
            ToggleWheel(false);
        }

        // 添加I键打开背包功能
        if (keyboard.iKey.wasPressedThisFrame)
        {
            HandleInventoryInput();
        }

        // 添加O键打开图鉴功能
        if (keyboard.oKey.wasPressedThisFrame)
        {
            HandleEncyclopediaInput();
        }

        // 添加触屏点击检测（移动端工具选择）
        if (isWheelOpen && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            HandleTouchSelection();
        }

        // 添加鼠标点击检测（桌面端工具选择）
        var mouse = Mouse.current;
        if (isWheelOpen && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            HandleClickSelection();
        }
    }
    
    void OpenWheel()
    {
        if (wheelUI == null)
        {
            Debug.LogError("❌ 无法打开TabUI：wheelUI为null");
            return;
        }

        // 每次打开前强制应用解锁的工具并刷新列表，避免切场景后列表丢失
        EnsureUnlockedToolsApplied();
        InitializeTools();

        // 确保Canvas设置正确
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000; // 设置很高的层级，确保在所有UI之上
            // Debug.Log($"[InventoryUISystem] Canvas设置 - RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}");
        }

        isWheelOpen = true;
        mobileWheelCandidateSlot = -1;
        selectedSlot = -1;
        ResetSlotColors();
        wheelUI.SetActive(true);
        SetupWheelAppearance();
        UpdateWheelSize();

        // Tab 轮盘打开音效
        GeoModel.AudioSystem.AudioManager.Instance.PlayUI(GeoModel.AudioSystem.AudioKeys.UI.TabOpen);

        // 强制刷新Canvas
        if (canvas != null)
        {
            canvas.enabled = false;
            canvas.enabled = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 只禁用鼠标视角控制，保留键盘移动
        FirstPersonController controller = GetFirstPersonController();
        if (controller != null)
        {
            controller.SetMouseLookEnabled(false);
        }

        // 不暂停游戏，保持正常时间流逝
        Time.timeScale = 1.0f;

        Debug.Log($"📂 TabUI已打开 - wheelUI.activeInHierarchy: {wheelUI.activeInHierarchy}, position: {wheelUI.transform.position}");
    }
    
    void CloseWheel(bool equipSelectedTool)
    {
        if (wheelUI == null) 
        {
            Debug.LogError("❌ 无法关闭TabUI：wheelUI为null");
            return;
        }
        
        if (equipSelectedTool && selectedSlot >= 0 && selectedSlot < availableTools.Count)
        {
            SelectToolAndStartPreview(selectedSlot);
        }
        
        isWheelOpen = false;
        wheelUI.SetActive(false);

        // Tab 轮盘关闭音效
        GeoModel.AudioSystem.AudioManager.Instance.PlayUI(GeoModel.AudioSystem.AudioKeys.UI.TabClose);

        // 简化逻辑：直接恢复鼠标锁定状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 重新启用鼠标视角控制
        FirstPersonController controller = GetFirstPersonController();
        if (controller != null)
        {
            controller.SetMouseLookEnabled(true); // 恢复鼠标视角
        }

        Debug.Log("[InventoryUISystem] TabUI关闭 - 鼠标已重新锁定并隐藏");
        
        selectedSlot = -1;
        mobileWheelCandidateSlot = -1;
        wheelOpenedByMobileInput = false; // 重置标记
        ResetSlotColors();

        Debug.Log("📁 TabUI已关闭");
    }

    /// <summary>
    /// 获取当前输入位置（支持鼠标和触屏）
    /// </summary>
    Vector2 GetInputPosition()
    {
        // 优先检查触屏输入（移动端）
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return touchPosition;
        }

        // 检查鼠标输入（桌面端）
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mousePosition = mouse.position.ReadValue();
            // 鼠标悬停时也返回位置（用于预览选择）
            return mousePosition;
        }

        return Vector2.zero; // 没有有效输入
    }

    /// <summary>
    /// 处理触屏选择
    /// </summary>
    void HandleTouchSelection()
    {
        if (Touchscreen.current == null) return;

        TrySelectToolAtScreenPoint(Touchscreen.current.primaryTouch.position.ReadValue(), "触屏");
    }

    /// <summary>
    /// 处理鼠标点击选择
    /// </summary>
    void HandleClickSelection()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        TrySelectToolAtScreenPoint(mouse.position.ReadValue(), "鼠标");
    }

    bool TrySelectToolAtScreenPoint(Vector2 screenPoint, string source)
    {
        int slotIndex = GetWheelSlotAtScreenPoint(screenPoint);
        return SelectToolAtSlot(slotIndex, source);
    }

    bool SelectToolAtSlot(int slotIndex, string source)
    {
        if (slotIndex >= 0 && slotIndex < availableTools.Count)
        {
            Debug.Log($"[InventoryUISystem] {source}选择工具: {availableTools[slotIndex].toolName}");
            SelectToolAndStartPreview(slotIndex);
            CloseWheel(false);
            return true;
        }

        Debug.Log($"[InventoryUISystem] {source}点击，但未选中有效工具 (slotIndex: {slotIndex}, tools: {availableTools.Count})");
        return false;
    }

    int GetWheelSlotAtScreenPoint(Vector2 screenPoint)
    {
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        for (int i = 0; i < wheelSlots.Length && i < availableTools.Count; i++)
        {
            RectTransform slot = wheelSlots[i];
            if (slot == null || !slot.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(slot, screenPoint, uiCamera))
            {
                return i;
            }
        }

        return GetWheelSlotByAngle(screenPoint);
    }

    int GetWheelSlotByAngle(Vector2 screenPoint)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = screenPoint - screenCenter;
        if (direction.magnitude <= selectionRadius)
        {
            return -1;
        }

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;
        int slotIndex = Mathf.FloorToInt(angle / 45f);
        return slotIndex >= 0 && slotIndex < availableTools.Count ? slotIndex : -1;
    }
    
    void UpdateSelection()
    {
        // 获取输入位置（支持鼠标和触屏）
        Vector2 inputPosition = GetInputPosition();
        if (inputPosition == Vector2.zero) return; // 没有有效输入

        int newSelectedSlot = GetWheelSlotAtScreenPoint(inputPosition);
        SetSelectedSlot(newSelectedSlot >= 0 && newSelectedSlot < availableTools.Count ? newSelectedSlot : -1);
    }

    void SetSelectedSlot(int slotIndex)
    {
        if (slotIndex == selectedSlot)
        {
            return;
        }

        ResetSlotColors();
        selectedSlot = slotIndex;

        if (selectedSlot < 0 || selectedSlot >= wheelSlots.Length)
        {
            return;
        }

        RectTransform slotTransform = wheelSlots[selectedSlot];
        if (slotTransform != null)
        {
            slotTransform.localScale = Vector3.one * 1.12f;

            Image slotBackground = slotTransform.GetComponent<Image>();
            if (slotBackground != null)
            {
                slotBackground.color = selectedSlotBackgroundColor;
            }
        }

        if (selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
        {
            slotImages[selectedSlot].color = selectedColor;
            slotImages[selectedSlot].transform.localScale = Vector3.one * 1.16f;
        }

        if (selectedSlot < slotTexts.Length && slotTexts[selectedSlot] != null)
        {
            slotTexts[selectedSlot].color = selectedColor;
        }
    }
    
    void ResetSlotColors()
    {
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (wheelSlots[i] != null)
            {
                wheelSlots[i].localScale = Vector3.one;

                Image slotBackground = wheelSlots[i].GetComponent<Image>();
                if (slotBackground != null)
                {
                    slotBackground.color = slotBackgroundColor;
                }
            }
        }

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] != null)
            {
                slotImages[i].color = normalColor;
                slotImages[i].transform.localScale = Vector3.one;
            }
        }

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] != null)
            {
                slotTexts[i].color = Color.white;
            }
        }
    }
    
    void SelectTool(int slotIndex)
    {
        
        
        if (slotIndex < availableTools.Count && availableTools[slotIndex] != null)
        {
            var toolManager = GetOrCreateToolManager();
            if (toolManager != null)
            {
                EnsureUnlockedToolsApplied();
                EnsureToolRegistered(toolManager, availableTools[slotIndex]);
                toolManager.EquipTool(availableTools[slotIndex]);
                
            }
            else
            {
                
            }
        }
        else
        {
            
        }
    }

    void SelectToolAndStartPreview(int slotIndex)
    {
        
        
        if (slotIndex < availableTools.Count && availableTools[slotIndex] != null)
        {
            var toolManager = GetOrCreateToolManager();
            if (toolManager != null)
            {
                EnsureUnlockedToolsApplied();
                EnsureToolRegistered(toolManager, availableTools[slotIndex]);
                toolManager.EquipTool(availableTools[slotIndex]);
                
                
                // 检查是否是放置类工具，如果是则自动开始预览
                PlaceableTool placeableTool = availableTools[slotIndex] as PlaceableTool;
                if (placeableTool != null)
                {
                    placeableTool.EnterPlacementMode();
                    
                }
            }
            else
            {
                
            }
        }
        else
        {
            
        }
    }

    /// <summary>
    /// 获取或创建 ToolManager（在必要时挂到玩家身上）
    /// </summary>
    ToolManager GetOrCreateToolManager()
    {
        var toolManager = FindFirstObjectByType<ToolManager>(FindObjectsInactive.Include);
        if (toolManager != null) return toolManager;

        var player = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        if (player != null)
        {
            toolManager = player.gameObject.AddComponent<ToolManager>();
            toolManager.availableTools = toolManager.availableTools ?? new CollectionTool[0];
            Debug.Log("[InventoryUISystem] 兜底创建ToolManager并挂到玩家上");
            return toolManager;
        }

        Debug.LogWarning("[InventoryUISystem] 未找到玩家，无法创建ToolManager");
        return null;
    }

    /// <summary>
    /// 确保选中的工具已登记到 ToolManager
    /// </summary>
    void EnsureToolRegistered(ToolManager toolManager, CollectionTool tool)
    {
        if (toolManager == null || tool == null) return;
        if (!toolManager.HasTool(tool))
        {
            toolManager.AddTool(tool);
            Debug.Log($"[InventoryUISystem] 兜底将工具加入 ToolManager: {tool.toolName} ({tool.toolID})");
        }
    }

    /// <summary>
    /// 兜底应用已解锁的工具（防止场景切换后列表丢失）
    /// </summary>
    void EnsureUnlockedToolsApplied()
    {
        GetOrCreateToolManager();
        var playerData = FindFirstObjectByType<PlayerPersistentData>(FindObjectsInactive.Include);
        playerData?.ApplyUnlockedToolsToScene();
    }
    
    void InitializeTools()
    {
        // 严格使用 ToolManager 的可用工具列表，避免未解锁工具出现在轮盘
        availableTools.Clear();

        var toolManager = FindFirstObjectByType<ToolManager>(FindObjectsInactive.Include);
        if (toolManager != null && toolManager.availableTools != null)
        {
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null)
                {
                    availableTools.Add(tool);
                }
            }
        }
        // 不再从场景中扫描所有 CollectionTool，防止未解锁工具被显示

        // 按toolID排序工具列表（数字ID从小到大排序，按顺时针方向排列）
        availableTools.Sort((a, b) => {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            
            // 尝试将toolID转换为数字进行比较
            if (int.TryParse(a.toolID, out int idA) && int.TryParse(b.toolID, out int idB))
            {
                return idA.CompareTo(idB); // 从小到大排序
            }
            
            // 如果不是数字，使用字符串比较
            return string.Compare(a.toolID, b.toolID);
        });
        
        UpdateWheelDisplay();
    }
    
    public void AddTool(CollectionTool tool)
    {
        if (!availableTools.Contains(tool))
        {
            availableTools.Add(tool);
            
            // 按toolID排序工具列表（数字ID从小到大排序，按顺时针方向排列）
            availableTools.Sort((a, b) => {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                
                // 尝试将toolID转换为数字进行比较
                if (int.TryParse(a.toolID, out int idA) && int.TryParse(b.toolID, out int idB))
                {
                    return idA.CompareTo(idB); // 从小到大排序
                }
                
                // 如果不是数字，使用字符串比较
                return string.Compare(a.toolID, b.toolID);
            });
            
            UpdateWheelDisplay();
            Debug.Log($"工具已添加到UI: {tool.toolName} (ID: {tool.toolID})");
        }
    }
    
    public void RefreshTools()
    {
        InitializeTools();
    }
    
    /// <summary>
    /// 获取可用工具数量
    /// </summary>
    public int GetAvailableToolsCount()
    {
        return availableTools.Count;
    }
    
    /// <summary>
    /// 获取所有可用工具的信息
    /// </summary>
    public void LogAvailableTools()
    {
        Debug.Log($"=== Tab UI 工具列表 (共{availableTools.Count}个) ===");
        for (int i = 0; i < availableTools.Count; i++)
        {
            if (availableTools[i] != null)
            {
                Debug.Log($"Slot {i}: {availableTools[i].toolName} (ID: {availableTools[i].toolID})");
            }
            else
            {
                Debug.Log($"Slot {i}: null");
            }
        }
    }
    
    void UpdateWheelDisplay()
    {
        
        
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (i < availableTools.Count && availableTools[i] != null)
            {
                
                
                if (slotImages[i] != null)
                {
                    Sprite toolIcon = ToolIconResolver.GetIcon(availableTools[i]);
                    slotImages[i].sprite = toolIcon;
                    slotImages[i].preserveAspect = true;
                    slotImages[i].gameObject.SetActive(true);
                    
                    if (toolIcon == null)
                    {
                        
                        slotImages[i].color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    }
                    else
                    {
                        slotImages[i].color = Color.white;
                    }
                }
                else
                {
                    
                }
                
                if (slotTexts[i] != null)
                {
                    // 尝试使用本地化工具名称
                    string localizedToolName = GetLocalizedToolName(availableTools[i]);
                    slotTexts[i].text = localizedToolName;
                    slotTexts[i].gameObject.SetActive(true);
                    
                    // 添加本地化组件（如果还没有）
                    LocalizedText localizedText = slotTexts[i].GetComponent<LocalizedText>();
                    if (localizedText == null)
                    {
                        localizedText = slotTexts[i].gameObject.AddComponent<LocalizedText>();
                    }
                    localizedText.TextKey = GetToolNameKey(availableTools[i]);
                }
                else
                {
                    
                }
            }
            else
            {
                if (slotImages[i] != null)
                {
                    slotImages[i].gameObject.SetActive(false);
                }
                if (slotTexts[i] != null)
                {
                    slotTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 获取本地化工具名称
    /// </summary>
    private string GetLocalizedToolName(CollectionTool tool)
    {
        if (tool == null) return "Unknown Tool";
        
        var localizationManager = LocalizationManager.Instance;
        if (localizationManager != null)
        {
            string key = GetToolNameKey(tool);
            string localizedName = localizationManager.GetText(key);
            
            // 如果本地化文本存在且不是缺失键格式，返回本地化文本
            if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("[") && !localizedName.EndsWith("]"))
            {
                return localizedName;
            }
        }
        
        // 否则返回原始名称
        return tool.toolName;
    }
    
    /// <summary>
    /// 获取工具名称的本地化键
    /// </summary>
    private string GetToolNameKey(CollectionTool tool)
    {
        if (tool == null) return "tool.unknown.name";
        
        // 优先根据工具ID返回对应的本地化键（更可靠的匹配方式）
        if (!string.IsNullOrEmpty(tool.toolID))
        {
            switch (tool.toolID)
            {
                case "999":
                    return "tool.scene_switcher.name";
                case "1000":
                    return "tool.drill.simple.name";
                case "1001":
                    return "tool.drill_tower.name";
                case "1002":
                    return "tool.hammer.name";
            }
        }
        
        // 兼容基于工具名称的匹配（用于没有ID的旧工具）
        switch (tool.toolName)
        {
            case "场景切换器":
            case "Scene Switcher":
                return "tool.scene_switcher.name";
            case "简易钻探":
            case "Simple Drill":
                return "tool.drill.simple.name";
            case "钻塔工具":
            case "Drill Tower":
                return "tool.drill_tower.name";
            case "地质锤":
            case "Geological Hammer":
                return "tool.hammer.name";
            case "无人机":
            case "Drone":
                return "tool.drone.name";
            case "钻探车":
            case "Drill Car":
                return "tool.drill_car.name";
            default:
                // 如果都不匹配，返回未知工具
                return "tool.unknown.name";
        }
    }
    
    #region 移动端适配方法
    
    /// <summary>
    /// 初始化移动端支持
    /// </summary>
    void InitializeMobileSupport()
    {
        if (!enableMobileAdaptation) return;
        
        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        showMobileToolbar = false;
        
        RefreshMobileModeState();
        
        if (isMobileMode && showMobileToolbar)
        {
            CreateMobileToolbar();
        }
        
        Debug.Log($"[InventoryUISystem] 移动端支持初始化完成 - 移动模式: {isMobileMode}");
    }

    void RefreshMobileModeState()
    {
        isMobileMode = Application.isMobilePlatform ||
                       (mobileInputManager != null && mobileInputManager.IsMobileDevice());
    }
    
    /// <summary>
    /// 创建移动端工具栏
    /// </summary>
    void CreateMobileToolbar()
    {
        if (canvas == null) return;
        
        // 创建工具栏容器
        GameObject toolbar = new GameObject("MobileToolbar");
        toolbar.transform.SetParent(canvas.transform, false);
        
        mobileToolbarRect = toolbar.AddComponent<RectTransform>();
        mobileToolbarRect.anchorMin = new Vector2(0, 0);
        mobileToolbarRect.anchorMax = new Vector2(0, 0);
        mobileToolbarRect.pivot = new Vector2(0.5f, 0);
        
        // 添加背景
        Image toolbarBg = toolbar.AddComponent<Image>();
        toolbarBg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        
        // 创建滚动视图
        CreateMobileScrollView(toolbar);
        
        mobileToolbar = toolbar;
        ApplyMobileToolbarLayout(true);
        mobileToolbar.SetActive(false); // 初始隐藏
        
        Debug.Log("[InventoryUISystem] 移动端工具栏创建完成");
    }

    void ApplyMobileToolbarLayout(bool force = false)
    {
        if (mobileToolbarRect == null) return;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;
        if (!force && lastMobileToolbarScreenSize == screenSize && lastMobileToolbarSafeArea == safeArea)
        {
            return;
        }

        float safeLeft = safeArea.xMin;
        float safeRight = Screen.width - safeArea.xMax;
        float safeBottom = safeArea.yMin;

        float sideMargin = Mathf.Clamp(screenSize.x * 0.035f, 24f, 72f);
        float leftControlReserve = Mathf.Clamp(screenSize.x * 0.18f, 180f, 320f);
        float rightControlReserve = Mathf.Clamp(screenSize.x * 0.12f, 140f, 260f);

        float startX = safeLeft + sideMargin + leftControlReserve;
        float endX = Screen.width - safeRight - sideMargin - rightControlReserve;
        float availableWidth = Mathf.Max(280f, endX - startX);
        float toolbarWidth = Mathf.Min(availableWidth, 760f);
        float toolbarHeight = Mathf.Clamp(mobileToolbarHeight, 144f, 176f);

        float centerX = startX + availableWidth * 0.5f;
        float bottomOffset = safeBottom + Mathf.Clamp(screenSize.y * 0.24f, 220f, 340f);
        float maxBottom = Mathf.Max(safeBottom + 24f, Screen.height - toolbarHeight - 24f);
        bottomOffset = Mathf.Min(bottomOffset, maxBottom);

        mobileToolbarRect.anchorMin = new Vector2(0, 0);
        mobileToolbarRect.anchorMax = new Vector2(0, 0);
        mobileToolbarRect.pivot = new Vector2(0.5f, 0);
        mobileToolbarRect.sizeDelta = new Vector2(toolbarWidth, toolbarHeight);
        mobileToolbarRect.anchoredPosition = new Vector2(centerX, bottomOffset);

        lastMobileToolbarSafeArea = safeArea;
        lastMobileToolbarScreenSize = screenSize;
    }
    
    /// <summary>
    /// 创建移动端工具按钮行
    /// </summary>
    void CreateMobileScrollView(GameObject parent)
    {
        // 创建工具按钮可见区域。这里不使用Mask裁剪，避免WebGL/iPad上按钮被裁到只剩黑条。
        GameObject scrollView = new GameObject("ToolButtonTray");
        scrollView.transform.SetParent(parent.transform, false);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(14, 10);
        scrollRect.offsetMax = new Vector2(-14, -10);
        
        // 透明图像只用于接收UI射线，实际按钮挂在Content里。
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
        scrollImage.raycastTarget = true;

        mobileScrollRect = scrollView.AddComponent<ScrollRect>();
        mobileScrollRect.enabled = false;
        mobileScrollRect.horizontal = true;
        mobileScrollRect.vertical = false;
        
        // 创建内容容器
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollView.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        // 添加水平布局组件
        HorizontalLayoutGroup layoutGroup = content.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 14f;
        layoutGroup.padding = new RectOffset(12, 12, 8, 8);
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        
        mobileToolbarContent = contentRect;
        mobileScrollRect.content = contentRect;
        
        Debug.Log("[InventoryUISystem] 移动端工具按钮行创建完成");
    }
    
    /// <summary>
    /// 更新移动端工具栏显示
    /// </summary>
    void UpdateMobileToolbar()
    {
        if (!isMobileMode || mobileToolbarContent == null) return;
        
        // 清空现有按钮
        foreach (Button button in mobileToolButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        mobileToolButtons.Clear();
        
        // 为每个工具创建按钮
        for (int i = 0; i < availableTools.Count; i++)
        {
            if (availableTools[i] != null)
            {
                CreateMobileToolButton(availableTools[i], i);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(mobileToolbarContent);
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"[InventoryUISystem] 移动端工具栏已更新，包含{mobileToolButtons.Count}个工具");
    }
    
    /// <summary>
    /// 创建移动端工具按钮
    /// </summary>
    void CreateMobileToolButton(CollectionTool tool, int index)
    {
        if (mobileToolbarContent == null) return;
        
        float buttonSize = Mathf.Clamp(mobileToolbarRect != null ? mobileToolbarRect.sizeDelta.y - 38f : mobileToolbarHeight - 38f, 96f, 128f);
        
        // 创建按钮对象
        GameObject buttonObj = new GameObject($"MobileTool_{index}");
        buttonObj.transform.SetParent(mobileToolbarContent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);

        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.minWidth = buttonSize;
        layoutElement.preferredWidth = buttonSize;
        layoutElement.minHeight = buttonSize;
        layoutElement.preferredHeight = buttonSize;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();
        
        // 设置按钮样式
        buttonImage.sprite = ToolIconResolver.GetIcon(tool);
        buttonImage.preserveAspect = true;
        buttonImage.color = normalColor;
        buttonImage.raycastTarget = true;
        button.targetGraphic = buttonImage;
        
        // 添加按钮事件
        int toolIndex = index; // 闭包捕获
        button.onClick.AddListener(() => OnMobileToolSelected(toolIndex));
        
        // 添加按钮文本（工具名称）
        GameObject textObj = new GameObject("ToolName");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.sizeDelta = new Vector2(0, 30);
        textRect.anchoredPosition = new Vector2(0, 6);
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = GetLocalizedToolName(tool);
        buttonText.font = UIFontResolver.GetUIFont();
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.raycastTarget = false;
        
        // 添加本地化支持
        LocalizedText localizedText = textObj.AddComponent<LocalizedText>();
        localizedText.TextKey = GetToolNameKey(tool);
        
        mobileToolButtons.Add(button);
    }
    
    /// <summary>
    /// 移动端工具选择事件
    /// </summary>
    void OnMobileToolSelected(int toolIndex)
    {
        if (toolIndex >= 0 && toolIndex < availableTools.Count)
        {
            CollectionTool selectedTool = availableTools[toolIndex];
            
            SelectToolAndStartPreview(toolIndex);
            SetMobileToolbarOpen(false);
            
            Debug.Log($"[InventoryUISystem] 移动端工具选择: {selectedTool.toolName}");
        }
    }
    
    /// <summary>
    /// 切换移动端工具栏显示
    /// </summary>
    void ToggleMobileToolbar()
    {
        if (!isMobileMode || mobileToolbar == null) return;
        
        SetMobileToolbarOpen(!mobileToolbar.activeSelf);
    }

    void SetMobileToolbarOpen(bool open)
    {
        if (mobileToolbar == null) return;

        if (open)
        {
            EnsureUnlockedToolsApplied();
            InitializeTools();

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
            }

            ApplyMobileToolbarLayout(true);
            mobileToolbar.SetActive(true);

            // 激活后再创建按钮并强制重建布局，避免WebGL首帧只显示背景条。
            UpdateMobileToolbar();
            
            // 禁用玩家控制
            FirstPersonController controller = GetFirstPersonController();
            if (controller != null)
            {
                controller.SetMouseLookEnabled(false);
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            mobileToolbar.SetActive(false);

            // 简化逻辑：直接恢复鼠标锁定状态
            FirstPersonController controller = GetFirstPersonController();
            if (controller != null)
            {
                controller.SetMouseLookEnabled(true);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[InventoryUISystem] 移动端工具栏关闭 - 鼠标已重新锁定并隐藏");
        }
        
        Debug.Log($"[InventoryUISystem] 移动端工具栏: {(open ? "显示" : "隐藏")}");
    }
    
    /// <summary>
    /// 移动端工具选择逻辑
    /// </summary>
    void SelectMobileTool(int toolIndex)
    {
        if (toolIndex >= 0 && toolIndex < availableTools.Count)
        {
            CollectionTool selectedTool = availableTools[toolIndex];
            
            // 使用现有的SelectTool方法
            SelectTool(toolIndex);
            
            Debug.Log($"[InventoryUISystem] 移动端工具选择: {selectedTool.toolName} (ID: {selectedTool.toolID})");
        }
    }
    
    /// <summary>
    /// 重写UpdateWheelDisplay以支持移动端
    /// </summary>
    void RefreshDisplay()
    {
        if (isMobileMode && showMobileToolbar)
        {
            UpdateMobileToolbar();
        }
        else
        {
            // 调用原有的轮盘更新逻辑
            UpdateWheelDisplay();
        }
    }
    
    /// <summary>
    /// 获取移动端模式状态
    /// </summary>
    public bool IsMobileMode()
    {
        return isMobileMode;
    }
    
    /// <summary>
    /// 设置移动端模式
    /// </summary>
    public void SetMobileMode(bool enabled)
    {
        if (isMobileMode == enabled) return;
        
        isMobileMode = enabled;
        
        if (enabled && showMobileToolbar)
        {
            CreateMobileToolbar();
            UpdateMobileToolbar();
        }
        else if (mobileToolbar != null)
        {
            mobileToolbar.SetActive(false);
        }
        
        Debug.Log($"[InventoryUISystem] 移动端模式: {(enabled ? "启用" : "禁用")}");
    }
    
    void OnDestroy()
    {
        // 取消事件监听
        if (mobileInputManager != null)
        {
            mobileInputManager.OnToolWheelInput -= HandleToolWheelInput;
            mobileInputManager.OnInventoryInput -= HandleInventoryInput;
            mobileInputManager.OnWarehouseInput -= HandleWarehouseInput;
            mobileInputManager.OnEncyclopediaInput -= HandleEncyclopediaInput;
            mobileInputManager.OnInteractInput -= HandleMobileInteractInput;
        }
    }
    
    #endregion
}
