using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StorySystem;

public class InventoryUISystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject wheelUI;
    public Transform wheelCenter;
    public RectTransform[] wheelSlots = new RectTransform[0];
    public Image[] slotImages = new Image[0];
    public Text[] slotTexts = new Text[0];
    public Image wheelBackground;
    public Image[] slotSeparators = new Image[0];
    public UISystem.WheelSectorGraphic[] sectorGraphics = new UISystem.WheelSectorGraphic[0];
    public Image[] equippedMarkers = new Image[0];
    private RectTransform wheelDim;
    private UISystem.WheelSectorGraphic wheelRim;
    private UISystem.WheelSectorGraphic deadZone;
    private Sprite circleSprite;
    private ToolManager wheelToolManager;
    private LocalizationManager wheelLocalization;
    private Rect lastWheelSafeArea;
    
    [Header("Selection")]
    public float selectionRadius = 100f;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    [Header("Visual Settings")]
    public Color wheelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color separatorColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    public float separatorWidth = 4f;
    public Color slotBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    public Color selectedSlotBackgroundColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);
    
    private static InventoryUISystem activeWheel;
    public static bool IsAnyWheelOpen => activeWheel != null && activeWheel.isWheelOpen;
    private bool isWheelOpen = false;
    private bool wheelOpenedByMobileInput = false; // 标记轮盘是否由移动端输入打开
    private int selectedSlot = -1;
    private int mobileWheelCandidateSlot = -1;
    private double wheelOpenedInputTime;
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
    private Coroutine pendingMobileToolUse;


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
        if (Core.GameInputState.GameplayBlocked || StoryDirector.IsStoryPlaybackActive)
        {
            return;
        }

        RefreshMobileModeState();

        CloseLegacyMobileToolbar();
        ToggleWheel(true);
    }

    void HandleMobileInteractInput(bool isPressed)
    {
        RefreshMobileModeState();

        if (StoryDirector.IsStoryPlaybackActive ||
            !isPressed || !isMobileMode || IsToolMenuOpen())
        {
            return;
        }

        // 同一次触摸也可能被 NPC/剧情交互消费。延后一帧再决定是否使用工具，
        // 让剧情优先打开，避免“点剧情”的手势同时落到手持道具上。
        if (pendingMobileToolUse == null)
        {
            pendingMobileToolUse = StartCoroutine(TryUseCurrentToolAfterInteractionRouting());
        }
    }

    IEnumerator TryUseCurrentToolAfterInteractionRouting()
    {
        yield return null;
        pendingMobileToolUse = null;

        RefreshMobileModeState();
        if (StoryDirector.IsStoryPlaybackActive || !isMobileMode || IsToolMenuOpen())
        {
            yield break;
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
        if (!Core.ResearchExperienceSettings.WarehouseInteractionEnabled || Core.GameInputState.GameplayBlocked) return;
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
        if (!Core.ResearchExperienceSettings.EncyclopediaEnabled) return;
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

    public void RebuildWheelUI()
    {
        DestroyOldUI();
        ConfigureWheelCanvas();
        CreateWheelUI();
        InitializeTools();
    }

    void ConfigureWheelCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void CreateWheelUI()
    {
        circleSprite = CreateCircleSprite(512);
        wheelBackgroundColor = UISystem.GameUI.Surface;
        slotBackgroundColor = UISystem.GameUI.Panel;
        selectedSlotBackgroundColor = Color.Lerp(UISystem.GameUI.Panel, UISystem.GameUI.Accent, 0.55f);
        normalColor = UISystem.GameUI.Ink;
        selectedColor = Color.white;
        separatorColor = new Color(0.65f, 0.76f, 0.77f, 0.55f);
        separatorWidth = 1.5f;
        var wheelRect = UISystem.GameUI.Rect(transform, "WheelBackground", Vector2.one * 0.5f, Vector2.one * 0.5f);
        wheelUI = wheelRect.gameObject;
        wheelCenter = wheelRect;
        wheelBackground = wheelUI.AddComponent<Image>();
        wheelBackground.sprite = circleSprite;
        wheelBackground.color = wheelBackgroundColor;
        wheelBackground.raycastTarget = false;
        wheelDim = UISystem.GameUI.Box(wheelRect, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.68f),
            Vector2.one * 0.5f, Vector2.one * 0.5f).rectTransform;
        wheelDim.GetComponent<Image>().raycastTarget = false;
        // Dim 在圆盘之前绘制，覆盖底下的 HUD，但不拦截右侧触屏按钮。
        wheelBackground.enabled = false;
        wheelUI.SetActive(false);
    }

    void RebuildSectors()
    {
        if (wheelUI == null) return;
        foreach (var child in wheelUI.transform.Cast<Transform>().ToArray())
        {
            if (child == wheelDim) continue;
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        int count = availableTools.Count;
        wheelSlots = new RectTransform[count];
        slotImages = new Image[count];
        slotTexts = new Text[count];
        sectorGraphics = new UISystem.WheelSectorGraphic[count];
        equippedMarkers = new Image[count];
        slotSeparators = new Image[count > 1 ? count : 0];
        for (int i = 0; i < count; i++)
        {
            sectorGraphics[i] = CreateSectorGraphic("Sector_" + i, slotBackgroundColor);
            var content = UISystem.GameUI.Rect(wheelUI.transform, "Tool_" + i, Vector2.one * 0.5f, Vector2.one * 0.5f);
            wheelSlots[i] = content;
            slotImages[i] = UISystem.GameUI.Box(content, "Icon", normalColor, Vector2.one * 0.5f, Vector2.one * 0.5f);
            slotImages[i].preserveAspect = true;
            slotImages[i].raycastTarget = false;
            slotTexts[i] = UISystem.GameUI.Label(content, "Name", "", 36, Vector2.one * 0.5f,
                Vector2.one * 0.5f, TextAnchor.UpperCenter);
            equippedMarkers[i] = UISystem.GameUI.Box(content, "Equipped", UISystem.GameUI.Ink,
                Vector2.one * 0.5f, Vector2.one * 0.5f);
            equippedMarkers[i].sprite = circleSprite;
            equippedMarkers[i].raycastTarget = false;
        }
        // 内容放在所有扇形之上，避免相邻扇区盖住较长的名称。
        foreach (var content in wheelSlots) content.SetAsLastSibling();
        for (int i = 0; i < slotSeparators.Length; i++)
            slotSeparators[i] = UISystem.GameUI.Box(wheelUI.transform, "Divider_" + i, separatorColor,
                Vector2.one * 0.5f, Vector2.one * 0.5f);
        wheelRim = CreateSectorGraphic("Rim", UISystem.GameUI.Muted);
        deadZone = CreateSectorGraphic("DeadZone", UISystem.GameUI.Surface);
        selectedSlot = mobileWheelCandidateSlot = -1;
        lastScreenSize = 0f;
        UpdateWheelSize();
    }

    UISystem.WheelSectorGraphic CreateSectorGraphic(string name, Color color)
    {
        var rect = UISystem.GameUI.Rect(wheelUI.transform, name, Vector2.zero, Vector2.one);
        var graphic = rect.gameObject.AddComponent<UISystem.WheelSectorGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        return graphic;
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
    
    void Start()
    {
        // 初始化移动端输入管理器连接
        InitializeMobileInputEvents();

        playerCamera = Camera.main;
        fpController = GetFirstPersonController();
        ConfigureWheelCanvas();

        // 强制创建标准的UI结构
        Debug.Log("创建标准的圆形UI");
        DestroyOldUI();
        CreateWheelUI();
        wheelLocalization = LocalizationManager.Instance;
        if (wheelLocalization != null) wheelLocalization.OnLanguageChanged += HandleWheelLanguageChanged;

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
    
    void DestroyOldUI()
    {
        wheelSlots = new RectTransform[0];
        slotImages = new Image[0];
        slotTexts = new Text[0];
        slotSeparators = new Image[0];
        sectorGraphics = new UISystem.WheelSectorGraphic[0];
        equippedMarkers = new Image[0];
        wheelBackground = null;
        wheelCenter = null;
        if (wheelUI != null) DestroyImmediate(wheelUI);
        // 只清理轮盘的旧根节点，不碰背包、仓库等系统的 Slot。
        foreach (string name in new[] { "Cycle", "WheelBackground" })
        {
            var old = transform.Find(name);
            if (old != null) DestroyImmediate(old.gameObject);
        }
        wheelUI = null;
        if (circleSprite != null)
        {
            DestroyImmediate(circleSprite.texture);
            DestroyImmediate(circleSprite);
        }
    }

    // 调试方法：强制显示圆形布局信息
    void Update()
    {
        if (StoryDirector.IsStoryPlaybackActive)
        {
            if (isWheelOpen) CloseWheel(false);
            return;
        }
        if (Core.GameInputState.IsModalOpen) return;
        if (isWheelOpen && Core.GameInputState.TryConsumeEscape()) { CloseWheel(false); return; }
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
        Debug.Log($"轮盘：{availableTools.Count} 个等分扇区，死区半径 {selectionRadius:F1}");
        for (int i = 0; i < wheelSlots.Length; i++)
            Debug.Log($"扇区 {i}: 中心角 {i * 360f / wheelSlots.Length:F1}°，位置 {wheelSlots[i].anchoredPosition}");
    }

    IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(0.5f);
        
        InitializeTools();
        
        yield return new WaitForSeconds(1f);
        
        InitializeTools();
    }
    
    private float lastScreenSize = 0f;
    
    public void UpdateWheelSize()
    {
        if (wheelUI == null) return;
        var rootCanvas = GetComponentInParent<Canvas>();
        float scale = rootCanvas != null ? Mathf.Max(0.01f, rootCanvas.scaleFactor) : 1f;
        Rect safe = Screen.safeArea;
        float screenSize = Mathf.Min(safe.width, safe.height) / scale;
        if (Mathf.Abs(screenSize - lastScreenSize) < 0.1f && safe == lastWheelSafeArea) return;
        float size = Mathf.Min(screenSize * 0.86f, 920f);
        var rect = (RectTransform)wheelUI.transform;
        rect.sizeDelta = Vector2.one * size;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, safe.center, null, out var center);
        rect.anchoredPosition = center;
        wheelDim.sizeDelta = new Vector2(Screen.width, Screen.height) / scale;
        wheelDim.anchoredPosition = -center;
        selectionRadius = size * 0.12f;
        UpdateSlotPositions(size, scale);
        UpdateSeparators(size);
        if (wheelRim != null) wheelRim.Configure(0f, 360f, size * 0.5f - 1.5f / scale, size * 0.5f, 0.6f / scale);
        if (deadZone != null) deadZone.Configure(0f, 360f, 0f, selectionRadius, 1f / scale);
        lastScreenSize = screenSize;
        lastWheelSafeArea = safe;
    }

    void SetupWheelAppearance()
    {
        ResetSlotColors();
    }

    void UpdateSeparators(float size)
    {
        float count = sectorGraphics.Length;
        float radius = size * 0.5f;
        float length = radius - selectionRadius;
        for (int i = 0; i < slotSeparators.Length; i++)
        {
            float angle = (i + 0.5f) * 360f / count;
            var rect = slotSeparators[i].rectTransform;
            rect.anchoredPosition = UISystem.ToolWheelLayout.Direction(angle) * (selectionRadius + length * 0.5f);
            rect.sizeDelta = new Vector2(separatorWidth, length);
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle);
            slotSeparators[i].raycastTarget = false;
        }
    }

    void UpdateSlotPositions(float size, float scale)
    {
        int count = wheelSlots.Length;
        float span = count > 0 ? 360f / count : 360f;
        float contentRadius = size * (count > 4 ? 0.22f : count == 4 ? 0.33f : 0.29f);
        float iconSize = size * (count > 4 ? 0.12f : count == 4 ? 0.17f : 0.19f);
        int fontSize = Mathf.CeilToInt(Mathf.Max(32f, 18f / scale));
        float labelWidth = count > 4 ? size * 0.28f : Mathf.Max(size * 0.39f, fontSize * 8.4f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * span;
            sectorGraphics[i].Configure(angle - span * 0.5f, span, selectionRadius, size * 0.5f, 1f / scale);
            var content = wheelSlots[i];
            content.anchoredPosition = UISystem.ToolWheelLayout.Direction(angle) * contentRadius;
            content.sizeDelta = Vector2.one * iconSize;
            slotImages[i].rectTransform.sizeDelta = Vector2.one * iconSize;
            slotImages[i].rectTransform.anchoredPosition = count > 4 ? Vector2.zero : Vector2.up * 16f;
            var label = slotTexts[i];
            label.fontSize = fontSize;
            label.resizeTextForBestFit = false;
            var direction = UISystem.ToolWheelLayout.Direction(angle);
            if (count > 4)
            {
                // 道具较多时，图标在内圈、名称在外圈；左右扇区用较窄的两行名称。
                bool side = Mathf.Abs(direction.y) < 0.38f;
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.pivot = Vector2.one * 0.5f;
                label.rectTransform.sizeDelta = new Vector2(size * (side ? 0.19f : 0.24f), fontSize * 3f);
                label.rectTransform.anchoredPosition = direction * size * (side ? 0.39f : 0.37f) - content.anchoredPosition;
            }
            else if (count == 4)
            {
                CenterSectorContent(i, size, iconSize, scale);
            }
            else
            {
                label.alignment = TextAnchor.UpperCenter;
                label.rectTransform.sizeDelta = new Vector2(labelWidth, fontSize * 3f);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.rectTransform.anchoredPosition = new Vector2(-direction.x * size * 0.035f, -iconSize * 0.5f - 1f);
            }
            var marker = equippedMarkers[i].rectTransform;
            marker.sizeDelta = Vector2.one * (7f / scale);
            marker.anchoredPosition = count > 4 ? -direction * (iconSize * 0.6f + 10f) : new Vector2(0f, iconSize * 0.5f + 28f);
            if (count == 4)
                marker.anchoredPosition = slotImages[i].rectTransform.anchoredPosition + Vector2.up * (iconSize * 0.5f + 8f / scale);
        }
    }

    void CenterSectorContent(int index, float size, float iconSize, float scale)
    {
        var label = slotTexts[index];
        var originalStyle = label.fontStyle;
        label.alignment = TextAnchor.MiddleCenter;
        label.alignByGeometry = true;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.rectTransform.pivot = Vector2.one * 0.5f;
        label.fontStyle = FontStyle.Normal;
        float fullWidth = label.preferredWidth;
        label.fontStyle = FontStyle.Bold;
        fullWidth = Mathf.Max(fullWidth, label.preferredWidth);
        float maxWidth = size * (index % 2 == 1 ? 0.28f : 0.42f);
        float width = fullWidth > maxWidth && !label.text.Contains(" ")
            ? Mathf.Min(maxWidth, fullWidth * 0.5f + label.fontSize * 0.4f) : maxWidth;
        label.rectTransform.sizeDelta = new Vector2(width, label.fontSize * 3f);
        float height = label.preferredHeight;
        label.fontStyle = FontStyle.Normal;
        height = Mathf.Max(height, label.preferredHeight);
        label.fontStyle = originalStyle;
        label.rectTransform.sizeDelta = new Vector2(width, height + 1f);

        // 以图标和实际文字的整体高度居中；长名称换行，悬停加粗也保留相同布局。
        float gap = 6f / scale;
        slotImages[index].rectTransform.anchoredPosition = Vector2.up * ((height + gap) * 0.5f);
        label.rectTransform.anchoredPosition = Vector2.down * ((iconSize + gap) * 0.5f);
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
        // The opening gesture may still be held or released in this input frame.
        // Its start time predates OpenWheel regardless of script execution order.
        if (touch.startTime.ReadValue() <= wheelOpenedInputTime) return;

        if (touch.press.isPressed)
        {
            int slotIndex = GetWheelSlotAtScreenPoint(touch.position.ReadValue());
            mobileWheelCandidateSlot = slotIndex >= 0 && slotIndex < availableTools.Count ? slotIndex : -1;
            SetSelectedSlot(mobileWheelCandidateSlot);
        }

        var phase = touch.phase.ReadValue();
        // A very short tap can begin and end in one input update before ButtonControl
        // has observed its first press. The touch's start time still proves ownership.
        if (touch.press.wasReleasedThisFrame || phase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            mobileWheelCandidateSlot = phase == UnityEngine.InputSystem.TouchPhase.Ended
                ? GetWheelSlotAtScreenPoint(touch.position.ReadValue()) : -1;
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
    
    public bool IsWheelOpen => isWheelOpen;

    void OpenWheel()
    {
        if (VehicleController.Active != null) return;
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
            canvas.sortingOrder = 31000; // 在普通 HUD 之上，确认框和操作说明仍使用32767层级
            // Debug.Log($"[InventoryUISystem] Canvas设置 - RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}");
        }

        activeWheel = this;
        CollectionTool.SuppressSelectionInput();
        wheelOpenedInputTime = InputState.currentTime;
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
        
        CollectionTool.SuppressSelectionInput();
        isWheelOpen = false;
        wheelUI.SetActive(false);

        // Tab 轮盘关闭音效
        GeoModel.AudioSystem.AudioManager.Instance.PlayUI(GeoModel.AudioSystem.AudioKeys.UI.TabClose);

        bool mobilePointer = MobileInputManager.IsRuntimeMobileDevice();
        Cursor.lockState = mobilePointer ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mobilePointer;

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
        if (Touchscreen.current.primaryTouch.startTime.ReadValue() <= wheelOpenedInputTime) return;

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
        return GetWheelSlotByAngle(screenPoint);
    }

    int GetWheelSlotByAngle(Vector2 screenPoint)
    {
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)wheelUI.transform,
            screenPoint, uiCamera, out Vector2 direction)) return -1;
        return UISystem.ToolWheelLayout.SectorAtAngle(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg,
            availableTools.Count, direction.magnitude, selectionRadius);
    }

    void UpdateSelection()
    {
        // 获取输入位置（支持鼠标和触屏）
        if (isMobileMode && (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.isPressed ||
            Touchscreen.current.primaryTouch.startTime.ReadValue() <= wheelOpenedInputTime)) return;
        Vector2 inputPosition = GetInputPosition();

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

        sectorGraphics[selectedSlot].color = selectedSlotBackgroundColor;

        if (selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
        {
            slotImages[selectedSlot].color = selectedColor;
            slotImages[selectedSlot].transform.localScale = Vector3.one * 1.05f;
        }

        if (selectedSlot < slotTexts.Length && slotTexts[selectedSlot] != null)
        {
            slotTexts[selectedSlot].color = selectedColor;
            slotTexts[selectedSlot].fontStyle = FontStyle.Bold;
        }
    }
    
    private void LateUpdate()
    {
        string recommended = UISystem.CollectionGuidanceHUD.RecommendedToolId;
        Color hintColor = Color.Lerp(slotBackgroundColor, UISystem.GameUI.Accent,
            0.35f + 0.12f * Mathf.Sin(Time.unscaledTime * 3f));
        var equipped = wheelToolManager != null ? wheelToolManager.GetCurrentTool() : null;
        for (int i = 0; i < sectorGraphics.Length; i++)
        {
            equippedMarkers[i].gameObject.SetActive(availableTools[i] == equipped || (equipped == null && availableTools[i].toolID == "0"));
            if (i == selectedSlot) continue;
            sectorGraphics[i].color = availableTools[i].toolID == recommended ? hintColor : slotBackgroundColor;
        }
        for (int i = 0; i < mobileToolButtons.Count && i < availableTools.Count; i++)
        {
            if (mobileToolButtons[i] == null) continue;
            var outline = mobileToolButtons[i].GetComponent<Outline>();
            if (outline != null)
                outline.enabled = availableTools[i] != null && availableTools[i].toolID == recommended;
        }
    }

    void ResetSlotColors()
    {
        foreach (var sector in sectorGraphics)
            if (sector != null) sector.color = slotBackgroundColor;

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
                slotTexts[i].color = normalColor;
                slotTexts[i].fontStyle = FontStyle.Normal;
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
                if (availableTools[slotIndex] is EmptyHandTool)
                {
                    CollectionTool.SuppressSelectionInput();
                    toolManager.UnequipCurrentTool();
                    return;
                }
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
                if (availableTools[slotIndex] is EmptyHandTool)
                {
                    CollectionTool.SuppressSelectionInput();
                    toolManager.UnequipCurrentTool();
                    return;
                }
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
    
    // 教学使用顺序；其余工具仍按原有 ID 规则排在后面。
    private static readonly string[] TeachingToolOrder = { "0", "999", "1002", "1001" };

    public static bool IsToolVisibleInWheel(string id) => id != "1000" && id != "1100" && id != "1101";

    public static int CompareToolIds(string a, string b)
    {
        int indexA = System.Array.IndexOf(TeachingToolOrder, a);
        int indexB = System.Array.IndexOf(TeachingToolOrder, b);
        if (indexA < 0) indexA = TeachingToolOrder.Length;
        if (indexB < 0) indexB = TeachingToolOrder.Length;
        if (indexA != indexB) return indexA.CompareTo(indexB);
        if (int.TryParse(a, out int idA) && int.TryParse(b, out int idB)) return idA.CompareTo(idB);
        return string.Compare(a, b);
    }

    private void SortTools()
    {
        availableTools.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return CompareToolIds(a.toolID, b.toolID);
        });
    }

    void InitializeTools()
    {
        // 严格使用 ToolManager 的可用工具列表，避免未解锁工具出现在轮盘
        availableTools.Clear();

        var toolManager = FindFirstObjectByType<ToolManager>(FindObjectsInactive.Include);
        wheelToolManager = toolManager;
        if (toolManager != null && toolManager.availableTools != null)
        {
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && IsToolVisibleInWheel(tool.toolID))
                {
                    availableTools.Add(tool);
                }
            }
        }
        if (toolManager != null)
        {
            var emptyHand = toolManager.GetComponent<EmptyHandTool>();
            if (emptyHand == null) emptyHand = toolManager.gameObject.AddComponent<EmptyHandTool>();
            if (!availableTools.Contains(emptyHand)) availableTools.Add(emptyHand);
        }
        // 不再从场景中扫描所有 CollectionTool，防止未解锁工具被显示

        SortTools();

        UpdateWheelDisplay();
    }
    
    public void AddTool(CollectionTool tool)
    {
        // 旧存档保留解锁数据，轮盘始终隐藏简易钻和载具。
        if (tool == null || !IsToolVisibleInWheel(tool.toolID)) return;
        if (!availableTools.Contains(tool))
        {
            availableTools.Add(tool);
            
            SortTools();

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
        if (wheelUI == null) return;
        if (wheelSlots.Length != availableTools.Count) RebuildSectors();
        for (int i = 0; i < availableTools.Count; i++)
        {
            slotImages[i].sprite = ToolIconResolver.GetIcon(availableTools[i]);
            slotTexts[i].text = GetLocalizedToolName(availableTools[i]);
            var localized = slotTexts[i].GetComponent<LocalizedText>();
            if (localized == null) localized = slotTexts[i].gameObject.AddComponent<LocalizedText>();
            localized.TextKey = GetToolNameKey(availableTools[i]);
        }
        if (wheelSlots.Length == 4)
            UpdateSlotPositions(((RectTransform)wheelUI.transform).rect.width, canvas.scaleFactor);
        ResetSlotColors();
    }

    void HandleWheelLanguageChanged()
    {
        int previousSlot = selectedSlot;
        UpdateWheelDisplay();
        selectedSlot = -1;
        if (isWheelOpen && previousSlot >= 0 && previousSlot < availableTools.Count)
            SetSelectedSlot(previousSlot);
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
                case "0":
                    return "tool.empty_hand.name";
                case "999":
                    return "tool.scene_switcher.name";
                case "1000":
                    return "tool.drill.simple.short";
                case "1001":
                    return "tool.drill_tower.short";
                case "1002":
                    return "tool.hammer.name";
                case "1100":
                    return "tool.drone.name";
                case "1101":
                    return "tool.drill_car.name";
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
                return "tool.drill.simple.short";
            case "钻塔工具":
            case "Drill Tower":
                return "tool.drill_tower.short";
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
                       (mobileInputManager != null && (mobileInputManager.IsMobileDevice() || mobileInputManager.desktopTestMode));
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
        
        var recommendationOutline = buttonObj.AddComponent<Outline>();
        recommendationOutline.effectColor = UISystem.GameUI.Accent;
        recommendationOutline.effectDistance = new Vector2(3f, -3f);
        recommendationOutline.enabled = false;
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
                canvas.sortingOrder = 31000;
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
        if (wheelLocalization != null) wheelLocalization.OnLanguageChanged -= HandleWheelLanguageChanged;
        if (circleSprite != null)
        {
            if (Application.isPlaying)
            {
                Destroy(circleSprite.texture);
                Destroy(circleSprite);
            }
            else
            {
                DestroyImmediate(circleSprite.texture);
                DestroyImmediate(circleSprite);
            }
        }
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
