using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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
    private int selectedSlot = -1;
    private Camera playerCamera;
    private FirstPersonController fpController;
    private Canvas canvas;
    
    private List<CollectionTool> availableTools = new List<CollectionTool>();
    
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
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
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
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            }
            
            text.fontSize = 12;
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
    }
    
    void Start()
    {
        playerCamera = Camera.main;
        fpController = FindFirstObjectByType<FirstPersonController>();
        canvas = GetComponent<Canvas>();
        
        // 检测现有UI或创建新UI
        if (!DetectExistingUI())
        {
            Debug.Log("未检测到现有UI，创建新的圆形UI");
            DestroyOldUI();
            CreateWheelUI();
        }
        else
        {
            Debug.Log("检测到现有UI，使用现有结构");
        }
        
        if (wheelUI != null)
        {
            wheelUI.SetActive(false);
            SetupWheelAppearance();
            UpdateWheelSize();
        }
        
        StartCoroutine(DelayedInitialize());
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
        
        // F2键：调试圆形布局
        if (UnityEngine.InputSystem.Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DebugCircularLayout();
        }
        
        // R键：刷新工具
        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
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
        
        float wheelSize = screenSize * 0.8f; // 80%屏幕大小
        
        RectTransform wheelRect = wheelUI.GetComponent<RectTransform>();
        if (wheelRect != null)
        {
            wheelRect.sizeDelta = new Vector2(wheelSize, wheelSize);
            // 确保轮盘居中
            wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
            wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRect.pivot = new Vector2(0.5f, 0.5f);
            wheelRect.anchoredPosition = Vector2.zero;
        }
        
        selectionRadius = wheelSize * 0.2f;
        
        UpdateSlotPositions(wheelSize);
        UpdateSeparators(wheelSize);
        
        lastScreenSize = screenSize;
        Debug.Log($"轮盘尺寸已更新为: {wheelSize}x{wheelSize} (屏幕大小: {screenSize})");
    }
    
    void SetupWheelAppearance()
    {
        if (wheelBackground != null)
        {
            wheelBackground.color = wheelBackgroundColor;
        }
        else
        {
            Image wheelImg = wheelUI.GetComponent<Image>();
            if (wheelImg != null)
            {
                wheelImg.color = wheelBackgroundColor;
            }
        }
        
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
        float slotSize = wheelSize * 0.08f;   // slot大小为轮盘的8%
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
                    slotTexts[i].fontSize = Mathf.RoundToInt(slotSize * 0.25f); // 字体大小为slot的25%
                    
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
        if (Keyboard.current.tabKey.isPressed && !isWheelOpen)
        {
            OpenWheel();
        }
        else if (!Keyboard.current.tabKey.isPressed && isWheelOpen)
        {
            CloseWheel();
        }
    }
    
    void OpenWheel()
    {
        isWheelOpen = true;
        wheelUI.SetActive(true);
        SetupWheelAppearance();
        UpdateWheelSize();
        Cursor.lockState = CursorLockMode.None;
        
        // 只禁用鼠标视角控制，保留键盘移动
        if (fpController != null)
        {
            fpController.enableMouseLook = false;
        }
        
        // 不暂停游戏，保持正常时间流逝
        Time.timeScale = 1.0f;
    }
    
    void CloseWheel()
    {
        
        
        if (selectedSlot >= 0 && selectedSlot < availableTools.Count)
        {
            SelectToolAndStartPreview(selectedSlot);
        }
        else if (selectedSlot >= 0)
        {
            
        }
        
        isWheelOpen = false;
        wheelUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        
        // 重新启用鼠标视角控制
        if (fpController != null)
        {
            fpController.enableMouseLook = true; // 恢复鼠标视角
        }
        
        selectedSlot = -1;
        ResetSlotColors();
    }
    
    void UpdateSelection()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = mousePosition - screenCenter;
        
        if (direction.magnitude > selectionRadius)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;
            
            int newSelectedSlot = Mathf.FloorToInt(angle / 45f);
            newSelectedSlot = Mathf.Clamp(newSelectedSlot, 0, 7);
            
            if (newSelectedSlot != selectedSlot)
            {
                ResetSlotColors();
                selectedSlot = newSelectedSlot;
                
                
                
                if (selectedSlot < availableTools.Count && selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
                {
                    slotImages[selectedSlot].color = selectedColor;
                    Transform slotTransform = slotImages[selectedSlot].transform;
                    slotTransform.localScale = Vector3.one * 1.2f;
                    
                }
                else if (selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
                {
                    slotImages[selectedSlot].color = selectedColor;
                    Transform slotTransform = slotImages[selectedSlot].transform;
                    slotTransform.localScale = Vector3.one * 1.2f;
                    
                }
            }
        }
        else
        {
            if (selectedSlot != -1)
            {
                ResetSlotColors();
                selectedSlot = -1;
            }
        }
    }
    
    void ResetSlotColors()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] != null)
            {
                slotImages[i].color = normalColor;
                slotImages[i].transform.localScale = Vector3.one;
            }
        }
    }
    
    void SelectTool(int slotIndex)
    {
        
        
        if (slotIndex < availableTools.Count && availableTools[slotIndex] != null)
        {
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
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
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
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
    
    void InitializeTools()
    {
        availableTools.Clear();
        
        CollectionTool[] tools = FindObjectsByType<CollectionTool>(FindObjectsSortMode.None);
        
        
        foreach (var tool in tools)
        {
            if (tool != null)
            {
                availableTools.Add(tool);
                
            }
        }
        
        var toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null && toolManager.availableTools != null)
        {
            
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && !availableTools.Contains(tool))
                {
                    availableTools.Add(tool);
                    
                }
            }
        }
        
        // 按toolID排序工具列表
        availableTools.Sort((a, b) => {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.Compare(a.toolID, b.toolID);
        });
        
        UpdateWheelDisplay();
    }
    
    public void AddTool(CollectionTool tool)
    {
        if (!availableTools.Contains(tool))
        {
            availableTools.Add(tool);
            UpdateWheelDisplay();
        }
    }
    
    public void RefreshTools()
    {
        InitializeTools();
    }
    
    void UpdateWheelDisplay()
    {
        
        
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (i < availableTools.Count && availableTools[i] != null)
            {
                
                
                if (slotImages[i] != null)
                {
                    slotImages[i].sprite = availableTools[i].toolIcon;
                    slotImages[i].gameObject.SetActive(true);
                    
                    if (availableTools[i].toolIcon == null)
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
                    slotTexts[i].text = availableTools[i].toolName;
                    slotTexts[i].gameObject.SetActive(true);
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
}