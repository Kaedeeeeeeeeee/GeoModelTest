using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 仓库背包面板 - 管理仓库界面中的背包物品显示
/// </summary>
public class WarehouseInventoryPanel : MonoBehaviour
{
    [Header("网格设置")]
    public int gridColumns = 5; // 每行5个物品
    public int gridRows = 4;    // 4行，总共20个格子
    public float cellSize = 80f;
    public float spacing = 10f;
    
    [Header("UI组件")]
    public Transform itemGridParent;
    public GameObject itemSlotPrefab;
    public ScrollRect scrollRect;
    
    [Header("物品显示")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color filledSlotColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.2f, 0.8f);
    
    // 私有成员
    private List<WarehouseItemSlot> itemSlots = new List<WarehouseItemSlot>();
    private List<SampleItem> currentItems = new List<SampleItem>();
    private SampleInventory inventory;
    private MultiSelectSystem multiSelectSystem;
    
    void Start()
    {
        InitializeInventoryPanel();
        SetupGridLayout();
        CreateItemSlots();
        
        // 获取系统引用
        inventory = SampleInventory.Instance;
        
        // 尝试获取多选系统引用
        SetupMultiSelectSystem();
        
        // 订阅背包变化事件，实现实时同步
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshInventoryDisplay;
            Debug.Log("[WarehouseInventoryPanel] 已订阅背包变化事件，实现实时同步");
        }
        
        // 定时刷新作为备选方案（降低频率）
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RefreshInventoryPeriodically());
        }
    }
    
    /// <summary>
    /// 初始化背包面板
    /// </summary>
    void InitializeInventoryPanel()
    {
        // 创建网格父对象
        if (itemGridParent == null)
        {
            GameObject gridParent = new GameObject("InventoryGrid");
            gridParent.transform.SetParent(transform);
            
            RectTransform gridRect = gridParent.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(10, 10);
            gridRect.offsetMax = new Vector2(-10, -50); // 留出底部空间
            
            itemGridParent = gridParent.transform;
        }
        
        // 背包面板初始化完成
    }
    
    /// <summary>
    /// 设置多选系统引用
    /// </summary>
    void SetupMultiSelectSystem()
    {
        if (multiSelectSystem == null)
        {
            multiSelectSystem = FindFirstObjectByType<MultiSelectSystem>();
            
            // 如果找到了，订阅事件
            if (multiSelectSystem != null)
            {
                multiSelectSystem.OnSelectionChanged += OnSelectionChanged;
                Debug.Log($"[WarehouseInventoryPanel] 多选系统已连接");
            }
        }
    }
    
    /// <summary>
    /// 设置网格布局
    /// </summary>
    void SetupGridLayout()
    {
        GridLayoutGroup gridLayout = itemGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = itemGridParent.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        // 配置网格布局
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridColumns;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        
        // 添加Content Size Fitter
        ContentSizeFitter sizeFitter = itemGridParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = itemGridParent.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    
    /// <summary>
    /// 创建物品槽
    /// </summary>
    void CreateItemSlots()
    {
        int totalSlots = gridColumns * gridRows;
        
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = CreateItemSlot(i);
            WarehouseItemSlot slot = slotObj.GetComponent<WarehouseItemSlot>();
            if (slot != null)
            {
                itemSlots.Add(slot);
                slot.SetSlotType(WarehouseItemSlot.SlotType.InventorySlot);
                slot.OnSlotClicked += OnSlotClicked;
            }
        }
        
        // 创建了背包物品槽
    }
    
    /// <summary>
    /// 创建单个物品槽
    /// </summary>
    GameObject CreateItemSlot(int index)
    {
        GameObject slotObj = new GameObject($"InventorySlot_{index}");
        slotObj.transform.SetParent(itemGridParent);
        
        // 添加RectTransform
        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(cellSize, cellSize);
        
        // 添加背景图片（模仿背包系统的视觉样式）
        Image slotBg = slotObj.AddComponent<Image>();
        slotBg.color = emptySlotColor;
        
        // 不使用sprite，直接用颜色和边框显示槽位
        slotBg.sprite = null;
        slotBg.type = Image.Type.Simple;
        
        // 添加边框效果
        Outline outline = slotObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        outline.effectDistance = new Vector2(1, 1);
        
        // 添加Button组件用于点击
        Button slotButton = slotObj.AddComponent<Button>();
        slotButton.transition = Selectable.Transition.ColorTint;
        slotButton.targetGraphic = slotBg;
        slotButton.interactable = true;
        
        // 确保背景可以接收射线检测
        slotBg.raycastTarget = true;
        
        // 创建物品图标（完全模仿背包系统）
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slotObj.transform);
        
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 10);
        
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.type = Image.Type.Simple; // 确保可以正确显示颜色
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false; // 防止阻挡点击
        iconObj.SetActive(false); // 初始隐藏
        
        // 创建物品文本
        GameObject textObj = new GameObject("ItemText");
        textObj.transform.SetParent(slotObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.3f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text itemText = textObj.AddComponent<Text>();
        itemText.text = "";
        itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemText.fontSize = 12;
        itemText.color = Color.white;
        itemText.alignment = TextAnchor.MiddleCenter;
        itemText.verticalOverflow = VerticalWrapMode.Truncate;
        textObj.SetActive(false); // 模仿背包系统：初始隐藏
        
        // 添加WarehouseItemSlot组件
        WarehouseItemSlot itemSlot = slotObj.AddComponent<WarehouseItemSlot>();
        itemSlot.Initialize(slotBg, iconImage, itemText, slotButton);
        
        return slotObj;
    }
    
    /// <summary>
    /// 刷新背包显示
    /// </summary>
    public void RefreshInventoryDisplay()
    {
        // 检查组件是否已被销毁
        if (this == null || gameObject == null) return;

        if (inventory == null)
        {
            inventory = SampleInventory.Instance;
            
            // 如果刚刚获取到inventory实例，立即订阅事件
            if (inventory != null)
            {
                inventory.OnInventoryChanged += RefreshInventoryDisplay;
                Debug.Log("[WarehouseInventoryPanel] 延迟订阅背包变化事件成功");
            }
        }
        
        // 确保多选系统引用存在
        if (multiSelectSystem == null)
        {
            SetupMultiSelectSystem();
        }
        
        if (inventory != null)
        {
            // 直接使用真实的I键背包数据
            currentItems = inventory.GetInventorySamples();
        }
        else
        {
            currentItems.Clear();
            Debug.LogWarning("[WarehouseInventoryPanel] SampleInventory实例未找到，无法获取背包数据");
        }
        
        // 确保物品槽已创建
        if (itemSlots == null || itemSlots.Count == 0)
        {
            if (this != null && gameObject != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedRefresh());
            }
            return;
        }

        // 清理已被销毁的槽位引用
        CleanupDestroyedSlots();
        
        // 更新物品槽显示
        UpdateSlotDisplay();
    }

    /// <summary>
    /// 清理已被销毁的槽位引用
    /// </summary>
    void CleanupDestroyedSlots()
    {
        if (itemSlots == null) return;

        // 移除已被销毁的槽位
        for (int i = itemSlots.Count - 1; i >= 0; i--)
        {
            if (itemSlots[i] == null)
            {
                Debug.Log($"[WarehouseInventoryPanel] 移除已销毁的槽位 {i}");
                itemSlots.RemoveAt(i);
            }
        }

        // 如果所有槽位都被销毁，重新创建
        if (itemSlots.Count == 0)
        {
            Debug.Log("[WarehouseInventoryPanel] 所有槽位已被销毁，触发重新创建");
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedRefresh());
            }
        }
    }

    /// <summary>
    /// 更新槽位显示
    /// </summary>
    void UpdateSlotDisplay()
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            WarehouseItemSlot slot = itemSlots[i];

            // 检查槽位是否仍然有效
            if (slot == null)
            {
                Debug.LogWarning($"[WarehouseInventoryPanel] itemSlots[{i}] 已被销毁，跳过更新");
                continue;
            }

            if (i < currentItems.Count)
            {
                // 有物品的槽位
                SampleItem item = currentItems[i];
                slot.SetItem(item);
                slot.SetSlotColor(filledSlotColor);
                
                // 检查是否被选中（同时设置选中状态和背景颜色）
                // 额外检查：确保物品仍然在背包中（防止传输后残留选中状态）
                if (multiSelectSystem != null && multiSelectSystem.IsItemSelected(item) && item.currentLocation == SampleLocation.InInventory)
                {
                    slot.SetSelected(true);
                    slot.SetSlotColor(selectedSlotColor);
                }
                else
                {
                    slot.SetSelected(false);
                }
            }
            else
            {
                // 空槽位（强制清空选中状态）
                slot.ClearItem();
                slot.SetSelected(false); // 关键修复：清空选中状态
                slot.SetSlotColor(emptySlotColor);
            }
        }
    }
    
    /// <summary>
    /// 槽位点击事件
    /// </summary>
    void OnSlotClicked(WarehouseItemSlot slot)
    {
        if (slot.HasItem())
        {
            SampleItem item = slot.GetItem();
            
            Debug.Log($"[WarehouseInventoryPanel] 槽位点击: {item.displayName}");
            Debug.Log($"[WarehouseInventoryPanel] multiSelectSystem: {multiSelectSystem != null}");
            
            // 尝试重新获取多选系统（如果之前没有找到）
            if (multiSelectSystem == null)
            {
                SetupMultiSelectSystem();
            }
            
            // 检查是否在多选模式
            if (multiSelectSystem != null)
            {
                Debug.Log($"[WarehouseInventoryPanel] 多选系统状态: {multiSelectSystem.GetCurrentSelectionMode()}, 在多选模式: {multiSelectSystem.IsInMultiSelectMode()}");
                
                if (multiSelectSystem.IsInMultiSelectMode())
                {
                    Debug.Log($"[WarehouseInventoryPanel] 处于多选模式，执行选择操作");
                    // 多选模式：切换选择状态
                    bool success = multiSelectSystem.ToggleItemSelection(item, SampleLocation.InInventory);
                    Debug.Log($"[WarehouseInventoryPanel] 多选操作结果: {success}");
                }
                else
                {
                    Debug.Log($"[WarehouseInventoryPanel] 不在多选模式，检查是否应该自动进入多选模式");
                    
                    // 检查是否应该自动进入多选模式（用户友好性改进）
                    // 如果用户按住Ctrl键点击，则自动进入多选模式
                    bool ctrlPressed = UnityEngine.InputSystem.Keyboard.current != null && 
                                      (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed || 
                                       UnityEngine.InputSystem.Keyboard.current.rightCtrlKey.isPressed);
                    
                    if (ctrlPressed)
                    {
                        Debug.Log($"[WarehouseInventoryPanel] 检测到Ctrl键，自动进入多选模式");
                        multiSelectSystem.EnterMultiSelectMode();
                        
                        // 等待一帧再执行选择
                        StartCoroutine(DelayedToggleSelection(item));
                    }
                    else
                    {
                        Debug.Log($"[WarehouseInventoryPanel] 普通点击，显示物品详情");
                        // 普通模式：显示物品详情
                        ShowItemDetails(item);
                    }
                }
            }
            else
            {
                Debug.LogError($"[WarehouseInventoryPanel] 多选系统不存在！");
                // 备用选项：显示物品详情
                ShowItemDetails(item);
            }
        }
    }
    
    /// <summary>
    /// 显示物品详情（完全复用背包系统的逻辑）
    /// </summary>
    void ShowItemDetails(SampleItem item)
    {
        Debug.Log($"[WarehouseInventoryPanel] 显示物品详情: {item.displayName}");
        
        // 查找背包UI系统来复用详情显示功能
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            Debug.Log($"[WarehouseInventoryPanel] 找到InventoryUI系统");
            
            // 使用反射调用背包系统的ShowSampleDetail方法
            var method = inventoryUI.GetType().GetMethod("ShowSampleDetail", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                Debug.Log($"[WarehouseInventoryPanel] 找到ShowSampleDetail方法，准备调用");
                try
                {
                    method.Invoke(inventoryUI, new object[] { item });
                    Debug.Log("[WarehouseInventoryPanel] 成功使用背包系统显示样本详情");
                    
                    // 确保背包UI的Canvas层级高于仓库UI
                    Canvas inventoryCanvas = inventoryUI.GetComponent<Canvas>();
                    if (inventoryCanvas == null)
                    {
                        inventoryCanvas = inventoryUI.GetComponentInParent<Canvas>();
                    }
                    if (inventoryCanvas != null)
                    {
                        inventoryCanvas.sortingOrder = 250; // 确保在仓库UI之上
                        Debug.Log("[WarehouseInventoryPanel] 已提升背包UI Canvas层级");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WarehouseInventoryPanel] 调用ShowSampleDetail失败: {e.Message}");
                    FallbackShowDetails(item);
                }
            }
            else
            {
                Debug.LogWarning("[WarehouseInventoryPanel] 未找到背包系统的ShowSampleDetail方法");
                // 列出所有可用的方法
                var methods = inventoryUI.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Debug.Log($"[WarehouseInventoryPanel] 可用的私有方法: {string.Join(", ", methods.Select(m => m.Name))}");
                FallbackShowDetails(item);
            }
        }
        else
        {
            Debug.LogWarning("[WarehouseInventoryPanel] 未找到InventoryUI系统");
            FallbackShowDetails(item);
        }
    }
    
    /// <summary>
    /// 备用的详情显示方法
    /// </summary>
    void FallbackShowDetails(SampleItem item)
    {
        Debug.Log($"物品信息：\n" +
                  $"名称: {item.displayName}\n" +
                  $"ID: {item.sampleID}\n" +
                  $"工具: {item.sourceToolID}\n" +
                  $"采集时间: {item.collectionTime:yyyy-MM-dd HH:mm:ss}\n" +
                  $"位置: {item.originalCollectionPosition}");
    }
    
    /// <summary>
    /// 选择变化回调
    /// </summary>
    public void OnSelectionChanged(List<SampleItem> selectedItems)
    {
        // 更新选中状态的视觉反馈
        UpdateSlotDisplay();
    }
    
    /// <summary>
    /// 获取背包容量信息
    /// </summary>
    public (int current, int max) GetCapacityInfo()
    {
        if (inventory != null)
        {
            return inventory.GetCapacityInfo();
        }
        return (0, 20);
    }
    
    /// <summary>
    /// 获取当前显示的物品列表
    /// </summary>
    public List<SampleItem> GetCurrentItems()
    {
        return new List<SampleItem>(currentItems);
    }
    
    /// <summary>
    /// 按类型选择物品
    /// </summary>
    public int SelectItemsByType(string toolID)
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            Debug.LogWarning("多选系统未准备好");
            return 0;
        }
        
        return multiSelectSystem.SelectItemsByType(toolID, SampleLocation.InInventory);
    }
    
    /// <summary>
    /// 全选背包物品
    /// </summary>
    public int SelectAllItems()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            Debug.LogWarning("多选系统未准备好");
            return 0;
        }
        
        return multiSelectSystem.SelectAll(SampleLocation.InInventory);
    }
    
    /// <summary>
    /// 获取面板统计信息
    /// </summary>
    public string GetPanelStats()
    {
        var capacityInfo = GetCapacityInfo();
        var stats = $"=== 背包面板统计 ===\n";
        stats += $"当前物品: {capacityInfo.current}/{capacityInfo.max}\n";
        stats += $"使用率: {(float)capacityInfo.current / capacityInfo.max * 100:F1}%\n";
        stats += $"网格布局: {gridColumns}x{gridRows}\n";
        stats += $"总槽位: {itemSlots.Count}\n";
        
        if (multiSelectSystem != null)
        {
            int selectedCount = multiSelectSystem.GetSelectedItems().Count(item => 
                item.currentLocation == SampleLocation.InInventory);
            stats += $"选中物品: {selectedCount}\n";
        }
        
        return stats;
    }
    
    /// <summary>
    /// 延迟刷新（等待UI组件创建完成）
    /// </summary>
    IEnumerator DelayedRefresh()
    {
        yield return new WaitForEndOfFrame(); // 等待一帧
        
        // 再次检查物品槽是否已创建
        if (itemSlots != null && itemSlots.Count > 0)
        {
            UpdateSlotDisplay();
            Debug.Log($"[WarehouseInventoryPanel] 延迟刷新完成，显示 {currentItems.Count} 个物品");
        }
        else
        {
            Debug.LogError("[WarehouseInventoryPanel] 延迟刷新失败，物品槽仍未创建");
        }
    }
    
    /// <summary>
    /// 延迟切换选择状态
    /// </summary>
    IEnumerator DelayedToggleSelection(SampleItem item)
    {
        yield return new WaitForEndOfFrame();
        
        if (multiSelectSystem != null && multiSelectSystem.IsInMultiSelectMode())
        {
            Debug.Log($"[WarehouseInventoryPanel] 延迟执行选择操作: {item.displayName}");
            bool success = multiSelectSystem.ToggleItemSelection(item, SampleLocation.InInventory);
            Debug.Log($"[WarehouseInventoryPanel] 延迟选择结果: {success}");
        }
    }
    
    /// <summary>
    /// 定时刷新背包数据
    /// </summary>
    IEnumerator RefreshInventoryPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒刷新一次
            
            // 只在仓库界面打开时刷新，减少性能开销
            WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null && warehouseUI.IsWarehouseOpen())
            {
                RefreshInventoryDisplay();
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (multiSelectSystem != null)
        {
            multiSelectSystem.OnSelectionChanged -= OnSelectionChanged;
        }
        
        // 取消背包变化事件订阅
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshInventoryDisplay;
        }
        
        // 清理槽位事件
        foreach (var slot in itemSlots)
        {
            if (slot != null)
            {
                slot.OnSlotClicked -= OnSlotClicked;
            }
        }
    }
}