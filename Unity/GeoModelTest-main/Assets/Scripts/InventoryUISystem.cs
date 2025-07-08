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
    
    void Start()
    {
        playerCamera = Camera.main;
        fpController = FindFirstObjectByType<FirstPersonController>();
        canvas = GetComponent<Canvas>();
        
        if (wheelUI != null)
        {
            wheelUI.SetActive(false);
            SetupWheelAppearance();
            UpdateWheelSize();
        }
        
        StartCoroutine(DelayedInitialize());
    }
    
    IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("延迟初始化工具系统");
        InitializeTools();
        
        yield return new WaitForSeconds(1f);
        Debug.Log("再次刷新工具系统");
        InitializeTools();
    }
    
    void UpdateWheelSize()
    {
        if (wheelUI == null) return;
        
        float screenSize = Mathf.Min(Screen.width, Screen.height);
        float wheelSize = screenSize * (wheelSizePercent / 100f);
        
        RectTransform wheelRect = wheelUI.GetComponent<RectTransform>();
        if (wheelRect != null)
        {
            wheelRect.sizeDelta = new Vector2(wheelSize, wheelSize);
        }
        
        selectionRadius = wheelSize * 0.2f;
        
        UpdateSlotPositions(wheelSize);
        UpdateSeparators(wheelSize);
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
            Debug.LogWarning("分隔线数组未设置！请在Unity编辑器中设置Slot Separators数组");
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
            Debug.Log("提示：要显示分隔线，请在Unity编辑器中为Slot Separators数组添加8个Image组件");
        }
    }
    
    void UpdateSlotPositions(float wheelSize)
    {
        float slotRadius = wheelSize * 0.35f;
        float slotSize = wheelSize * 0.15f;
        
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (wheelSlots[i] != null)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 slotPos = new Vector2(Mathf.Sin(angle) * slotRadius, Mathf.Cos(angle) * slotRadius);
                wheelSlots[i].anchoredPosition = slotPos;
                wheelSlots[i].sizeDelta = new Vector2(slotSize, slotSize);
                
                if (slotTexts[i] != null)
                {
                    RectTransform textRect = slotTexts[i].GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(slotSize * 1.5f, slotSize * 0.4f);
                    textRect.anchoredPosition = new Vector2(0, -slotSize * 0.8f);
                    slotTexts[i].fontSize = Mathf.RoundToInt(slotSize * 0.18f);
                    
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
    
    void Update()
    {
        HandleInput();
        
        if (isWheelOpen)
        {
            UpdateSelection();
        }
        
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("R键 - 手动刷新工具列表");
            InitializeTools();
        }
    }
    
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
            fpController.enableMouseLook = false; // 禁用鼠标视角
        }
        
        // 不暂停游戏，保持正常时间流逝
        Time.timeScale = 1.0f;
        
        Debug.Log($"道具圆盘已打开，可用工具数量: {availableTools.Count}");
        for (int i = 0; i < availableTools.Count; i++)
        {
            Debug.Log($"工具 {i}: {availableTools[i].toolName}");
        }
    }
    
    void CloseWheel()
    {
        Debug.Log($"关闭圆盘，选中槽位: {selectedSlot}，可用工具数: {availableTools.Count}");
        
        if (selectedSlot >= 0 && selectedSlot < availableTools.Count)
        {
            SelectToolAndStartPreview(selectedSlot);
        }
        else if (selectedSlot >= 0)
        {
            Debug.LogWarning($"选中的槽位 {selectedSlot} 超出可用工具范围 {availableTools.Count}");
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
                
                Debug.Log($"选择槽位: {selectedSlot}, 角度: {angle:F1}°, 鼠标距离中心: {direction.magnitude:F1}");
                
                if (selectedSlot < availableTools.Count && selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
                {
                    slotImages[selectedSlot].color = selectedColor;
                    Transform slotTransform = slotImages[selectedSlot].transform;
                    slotTransform.localScale = Vector3.one * 1.2f;
                    Debug.Log($"高亮显示工具: {availableTools[selectedSlot].toolName}");
                }
                else if (selectedSlot < slotImages.Length && slotImages[selectedSlot] != null)
                {
                    slotImages[selectedSlot].color = selectedColor;
                    Transform slotTransform = slotImages[selectedSlot].transform;
                    slotTransform.localScale = Vector3.one * 1.2f;
                    Debug.Log($"选中空槽位: {selectedSlot}");
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
        Debug.Log($"尝试选择工具，槽位: {slotIndex}, 可用工具数: {availableTools.Count}");
        
        if (slotIndex < availableTools.Count && availableTools[slotIndex] != null)
        {
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                toolManager.EquipTool(availableTools[slotIndex]);
                Debug.Log($"成功装备工具: {availableTools[slotIndex].toolName}");
            }
            else
            {
                Debug.LogError("未找到ToolManager组件！");
            }
        }
        else
        {
            Debug.LogWarning($"无法选择工具 - 槽位 {slotIndex} 无效或为空");
        }
    }

    void SelectToolAndStartPreview(int slotIndex)
    {
        Debug.Log($"选择工具并开始预览，槽位: {slotIndex}, 可用工具数: {availableTools.Count}");
        
        if (slotIndex < availableTools.Count && availableTools[slotIndex] != null)
        {
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                toolManager.EquipTool(availableTools[slotIndex]);
                Debug.Log($"成功装备工具: {availableTools[slotIndex].toolName}");
                
                // 检查是否是放置类工具，如果是则自动开始预览
                PlaceableTool placeableTool = availableTools[slotIndex] as PlaceableTool;
                if (placeableTool != null)
                {
                    placeableTool.EnterPlacementMode();
                    Debug.Log($"自动开始预览模式: {placeableTool.toolName}");
                }
            }
            else
            {
                Debug.LogError("未找到ToolManager组件！");
            }
        }
        else
        {
            Debug.LogWarning($"无法选择工具 - 槽位 {slotIndex} 无效或为空");
        }
    }
    
    void InitializeTools()
    {
        availableTools.Clear();
        
        CollectionTool[] tools = FindObjectsByType<CollectionTool>(FindObjectsSortMode.None);
        Debug.Log($"在场景中找到 {tools.Length} 个采集工具");
        
        foreach (var tool in tools)
        {
            if (tool != null)
            {
                availableTools.Add(tool);
                Debug.Log($"添加工具: {tool.toolName} ({tool.gameObject.name})");
            }
        }
        
        var toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null && toolManager.availableTools != null)
        {
            Debug.Log($"从ToolManager找到 {toolManager.availableTools.Length} 个工具");
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && !availableTools.Contains(tool))
                {
                    availableTools.Add(tool);
                    Debug.Log($"从ToolManager添加工具: {tool.toolName}");
                }
            }
        }
        
        Debug.Log($"总共初始化 {availableTools.Count} 个工具");
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
    
    void UpdateWheelDisplay()
    {
        Debug.Log($"更新圆盘显示 - 可用工具: {availableTools.Count}, 槽位数: {wheelSlots.Length}");
        
        for (int i = 0; i < wheelSlots.Length; i++)
        {
            if (i < availableTools.Count && availableTools[i] != null)
            {
                Debug.Log($"设置槽位 {i}: {availableTools[i].toolName}");
                
                if (slotImages[i] != null)
                {
                    slotImages[i].sprite = availableTools[i].toolIcon;
                    slotImages[i].gameObject.SetActive(true);
                    
                    if (availableTools[i].toolIcon == null)
                    {
                        Debug.LogWarning($"工具 {availableTools[i].toolName} 没有图标!");
                        slotImages[i].color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    }
                    else
                    {
                        slotImages[i].color = Color.white;
                    }
                }
                else
                {
                    Debug.LogError($"槽位 {i} 的Image组件为空!");
                }
                
                if (slotTexts[i] != null)
                {
                    slotTexts[i].text = availableTools[i].toolName;
                    slotTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogError($"槽位 {i} 的Text组件为空!");
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