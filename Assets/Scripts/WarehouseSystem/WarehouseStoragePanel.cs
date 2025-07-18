using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 仓库存储面板 - 管理仓库界面中的仓库物品显示，支持翻页
/// </summary>
public class WarehouseStoragePanel : MonoBehaviour
{
    [Header("网格设置")]
    public int gridColumns = 10; // 每行10个物品
    public int gridRows = 5;     // 5行，每页50个格子
    public float cellSize = 70f;
    public float spacing = 8f;
    
    [Header("翻页设置")]
    public int itemsPerPage = 50;
    public int currentPage = 0;
    
    [Header("UI组件")]
    public Transform itemGridParent;
    public GameObject itemSlotPrefab;
    public ScrollRect scrollRect;
    
    [Header("翻页控制")]
    public Button prevPageButton;
    public Button nextPageButton;
    public Text pageInfoText;
    public Transform pageControlsParent;
    
    [Header("物品显示")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color filledSlotColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.2f, 0.8f);
    
    // 私有成员
    private List<WarehouseItemSlot> itemSlots = new List<WarehouseItemSlot>();
    private List<SampleItem> currentPageItems = new List<SampleItem>();
    private List<SampleItem> allWarehouseItems = new List<SampleItem>();
    private WarehouseStorage warehouseStorage;
    private MultiSelectSystem multiSelectSystem;
    private int totalPages = 0;
    
    void Start()
    {
        InitializeStoragePanel();
        SetupGridLayout();
        CreateItemSlots();
        CreatePageControls();
        
        // 获取系统引用
        warehouseStorage = WarehouseManager.Instance?.Storage;
        
        // 尝试获取多选系统引用
        SetupMultiSelectSystem();
        
        // 订阅事件
        if (multiSelectSystem != null)
        {
            multiSelectSystem.OnSelectionChanged += OnSelectionChanged;
        }
        
        if (warehouseStorage != null)
        {
            warehouseStorage.OnStorageChanged += OnStorageChanged;
        }
    }
    
    /// <summary>
    /// 初始化仓库面板
    /// </summary>
    void InitializeStoragePanel()
    {
        // 创建网格父对象
        if (itemGridParent == null)
        {
            GameObject gridParent = new GameObject("StorageGrid");
            gridParent.transform.SetParent(transform);
            
            RectTransform gridRect = gridParent.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(10, 60); // 留出底部空间给翻页控制
            gridRect.offsetMax = new Vector2(-10, -10);
            
            itemGridParent = gridParent.transform;
        }
        
        Debug.Log("仓库面板初始化完成");
    }
    
    /// <summary>
    /// 设置多选系统引用
    /// </summary>
    void SetupMultiSelectSystem()
    {
        if (multiSelectSystem == null)
        {
            multiSelectSystem = FindFirstObjectByType<MultiSelectSystem>();
            Debug.Log($"[WarehouseStoragePanel] 尝试获取多选系统: {multiSelectSystem != null}");
        }
        
        // 如果仍然没有找到，可能是因为多选系统还没有创建，稍后再试
        if (multiSelectSystem == null)
        {
            Debug.LogWarning("[WarehouseStoragePanel] 多选系统未找到，将在刷新显示时再次尝试");
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
                slot.SetSlotType(WarehouseItemSlot.SlotType.WarehouseSlot);
                slot.OnSlotClicked += OnSlotClicked;
            }
        }
        
        Debug.Log($"创建了 {itemSlots.Count} 个仓库物品槽");
    }
    
    /// <summary>
    /// 创建单个物品槽
    /// </summary>
    GameObject CreateItemSlot(int index)
    {
        GameObject slotObj = new GameObject($"StorageSlot_{index}");
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
        
        // 创建物品图标（完全模仿背包系统）
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slotObj.transform);
        
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(50, 50);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 8);
        
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
        textRect.anchorMax = new Vector2(1, 0.25f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text itemText = textObj.AddComponent<Text>();
        itemText.text = "";
        itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemText.fontSize = 10;
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
    /// 创建翻页控制
    /// </summary>
    void CreatePageControls()
    {
        if (pageControlsParent == null)
        {
            GameObject controlsObj = new GameObject("PageControls");
            controlsObj.transform.SetParent(transform);
            
            RectTransform controlsRect = controlsObj.AddComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0, 0);
            controlsRect.anchorMax = new Vector2(1, 0);
            controlsRect.anchoredPosition = new Vector2(0, 30);
            controlsRect.sizeDelta = new Vector2(0, 50);
            
            pageControlsParent = controlsObj.transform;
        }
        
        // 创建上一页按钮
        if (prevPageButton == null)
        {
            prevPageButton = CreatePageButton("上一页", new Vector2(0.2f, 0.5f));
            prevPageButton.onClick.AddListener(PreviousPage);
        }
        
        // 创建下一页按钮
        if (nextPageButton == null)
        {
            nextPageButton = CreatePageButton("下一页", new Vector2(0.8f, 0.5f));
            nextPageButton.onClick.AddListener(NextPage);
        }
        
        // 创建页面信息文本
        if (pageInfoText == null)
        {
            GameObject textObj = new GameObject("PageInfo");
            textObj.transform.SetParent(pageControlsParent);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(200, 30);
            
            pageInfoText = textObj.AddComponent<Text>();
            pageInfoText.text = "第1页 / 共1页";
            pageInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pageInfoText.fontSize = 16;
            pageInfoText.color = Color.white;
            pageInfoText.alignment = TextAnchor.MiddleCenter;
        }
        
        UpdatePageControls();
    }
    
    /// <summary>
    /// 创建翻页按钮
    /// </summary>
    Button CreatePageButton(string text, Vector2 anchorPosition)
    {
        GameObject buttonObj = new GameObject($"Button_{text}");
        buttonObj.transform.SetParent(pageControlsParent);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorPosition;
        buttonRect.anchorMax = anchorPosition;
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(100, 40);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // 添加文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        return button;
    }
    
    /// <summary>
    /// 刷新仓库显示
    /// </summary>
    public void RefreshStorageDisplay()
    {
        if (warehouseStorage == null)
        {
            warehouseStorage = WarehouseManager.Instance?.Storage;
        }
        
        // 确保多选系统引用存在
        if (multiSelectSystem == null)
        {
            SetupMultiSelectSystem();
        }
        
        if (warehouseStorage != null)
        {
            allWarehouseItems = warehouseStorage.GetAllItems();
            totalPages = warehouseStorage.GetTotalPages();
            
            // 确保当前页面有效
            if (currentPage >= totalPages && totalPages > 0)
            {
                currentPage = totalPages - 1;
            }
            else if (currentPage < 0)
            {
                currentPage = 0;
            }
            
            // 获取当前页的物品
            currentPageItems = warehouseStorage.GetItemsForPage(currentPage);
        }
        else
        {
            allWarehouseItems.Clear();
            currentPageItems.Clear();
            totalPages = 0;
        }
        
        // 更新显示
        UpdateSlotDisplay();
        UpdatePageControls();
        
        Debug.Log($"仓库显示已刷新 - 当前页: {currentPage + 1}/{totalPages}，物品数: {currentPageItems.Count}");
    }
    
    /// <summary>
    /// 更新槽位显示
    /// </summary>
    void UpdateSlotDisplay()
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            WarehouseItemSlot slot = itemSlots[i];
            
            if (i < currentPageItems.Count)
            {
                // 有物品的槽位
                SampleItem item = currentPageItems[i];
                slot.SetItem(item);
                slot.SetSlotColor(filledSlotColor);
                
                // 检查是否被选中（同时设置选中状态和背景颜色）
                // 额外检查：确保物品仍然在仓库中（防止传输后残留选中状态）
                if (multiSelectSystem != null && multiSelectSystem.IsItemSelected(item) && item.currentLocation == SampleLocation.InWarehouse)
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
    /// 更新翻页控制
    /// </summary>
    void UpdatePageControls()
    {
        if (prevPageButton != null)
        {
            prevPageButton.interactable = currentPage > 0;
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPage < totalPages - 1;
        }
        
        if (pageInfoText != null)
        {
            if (totalPages > 0)
            {
                pageInfoText.text = $"第{currentPage + 1}页 / 共{totalPages}页";
            }
            else
            {
                pageInfoText.text = "第1页 / 共1页";
            }
        }
    }
    
    /// <summary>
    /// 上一页
    /// </summary>
    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshStorageDisplay();
            Debug.Log($"切换到上一页: {currentPage + 1}");
        }
    }
    
    /// <summary>
    /// 下一页
    /// </summary>
    public void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RefreshStorageDisplay();
            Debug.Log($"切换到下一页: {currentPage + 1}");
        }
    }
    
    /// <summary>
    /// 跳转到指定页
    /// </summary>
    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < totalPages)
        {
            currentPage = pageIndex;
            RefreshStorageDisplay();
            Debug.Log($"跳转到页面: {currentPage + 1}");
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
            
            Debug.Log($"[WarehouseStoragePanel] 槽位点击: {item.displayName}");
            Debug.Log($"[WarehouseStoragePanel] multiSelectSystem: {multiSelectSystem != null}");
            
            // 检查是否在多选模式
            if (multiSelectSystem != null && multiSelectSystem.IsInMultiSelectMode())
            {
                Debug.Log($"[WarehouseStoragePanel] 处于多选模式: {multiSelectSystem.GetCurrentSelectionMode()}");
                // 多选模式：切换选择状态
                multiSelectSystem.ToggleItemSelection(item, SampleLocation.InWarehouse);
            }
            else
            {
                Debug.Log($"[WarehouseStoragePanel] 处于普通模式，显示物品详情");
                // 普通模式：显示物品详情
                ShowItemDetails(item);
            }
        }
    }
    
    /// <summary>
    /// 显示物品详情（完全复用背包系统的逻辑）
    /// </summary>
    void ShowItemDetails(SampleItem item)
    {
        Debug.Log($"[WarehouseStoragePanel] 显示物品详情: {item.displayName}");
        
        // 查找背包UI系统来复用详情显示功能
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            Debug.Log($"[WarehouseStoragePanel] 找到InventoryUI系统");
            
            // 使用反射调用背包系统的ShowSampleDetail方法
            var method = inventoryUI.GetType().GetMethod("ShowSampleDetail", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                Debug.Log($"[WarehouseStoragePanel] 找到ShowSampleDetail方法，准备调用");
                try
                {
                    method.Invoke(inventoryUI, new object[] { item });
                    Debug.Log("[WarehouseStoragePanel] 成功使用背包系统显示样本详情");
                    
                    // 确保背包UI的Canvas层级高于仓库UI
                    Canvas inventoryCanvas = inventoryUI.GetComponent<Canvas>();
                    if (inventoryCanvas == null)
                    {
                        inventoryCanvas = inventoryUI.GetComponentInParent<Canvas>();
                    }
                    if (inventoryCanvas != null)
                    {
                        inventoryCanvas.sortingOrder = 250; // 确保在仓库UI之上
                        Debug.Log("[WarehouseStoragePanel] 已提升背包UI Canvas层级");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WarehouseStoragePanel] 调用ShowSampleDetail失败: {e.Message}");
                    FallbackShowDetails(item);
                }
            }
            else
            {
                Debug.LogWarning("[WarehouseStoragePanel] 未找到背包系统的ShowSampleDetail方法");
                // 列出所有可用的方法
                var methods = inventoryUI.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Debug.Log($"[WarehouseStoragePanel] 可用的私有方法: {string.Join(", ", methods.Select(m => m.Name))}");
                FallbackShowDetails(item);
            }
        }
        else
        {
            Debug.LogWarning("[WarehouseStoragePanel] 未找到InventoryUI系统");
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
    /// 仓库存储变化回调
    /// </summary>
    void OnStorageChanged()
    {
        // 刷新显示
        RefreshStorageDisplay();
    }
    
    /// <summary>
    /// 获取仓库容量信息
    /// </summary>
    public (int current, int max) GetCapacityInfo()
    {
        if (warehouseStorage != null)
        {
            return warehouseStorage.GetCapacityInfo();
        }
        return (0, 100);
    }
    
    /// <summary>
    /// 获取当前页面的物品列表
    /// </summary>
    public List<SampleItem> GetCurrentPageItems()
    {
        return new List<SampleItem>(currentPageItems);
    }
    
    /// <summary>
    /// 获取所有仓库物品
    /// </summary>
    public List<SampleItem> GetAllWarehouseItems()
    {
        return new List<SampleItem>(allWarehouseItems);
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
        
        return multiSelectSystem.SelectItemsByType(toolID, SampleLocation.InWarehouse);
    }
    
    /// <summary>
    /// 全选当前页面物品
    /// </summary>
    public int SelectCurrentPageItems()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            Debug.LogWarning("多选系统未准备好");
            return 0;
        }
        
        int selectedCount = 0;
        foreach (var item in currentPageItems)
        {
            if (multiSelectSystem.SelectItem(item, SampleLocation.InWarehouse))
            {
                selectedCount++;
            }
        }
        
        return selectedCount;
    }
    
    /// <summary>
    /// 全选所有仓库物品
    /// </summary>
    public int SelectAllWarehouseItems()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            Debug.LogWarning("多选系统未准备好");
            return 0;
        }
        
        return multiSelectSystem.SelectAll(SampleLocation.InWarehouse);
    }
    
    /// <summary>
    /// 获取面板统计信息
    /// </summary>
    public string GetPanelStats()
    {
        var capacityInfo = GetCapacityInfo();
        var stats = $"=== 仓库面板统计 ===\n";
        stats += $"总物品: {capacityInfo.current}/{capacityInfo.max}\n";
        stats += $"使用率: {(float)capacityInfo.current / capacityInfo.max * 100:F1}%\n";
        stats += $"当前页: {currentPage + 1}/{totalPages}\n";
        stats += $"当前页物品: {currentPageItems.Count}/{itemsPerPage}\n";
        stats += $"网格布局: {gridColumns}x{gridRows}\n";
        stats += $"总槽位: {itemSlots.Count}\n";
        
        if (multiSelectSystem != null)
        {
            int selectedCount = multiSelectSystem.GetSelectedItems().Count(item => 
                item.currentLocation == SampleLocation.InWarehouse);
            stats += $"选中物品: {selectedCount}\n";
        }
        
        return stats;
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (multiSelectSystem != null)
        {
            multiSelectSystem.OnSelectionChanged -= OnSelectionChanged;
        }
        
        if (warehouseStorage != null)
        {
            warehouseStorage.OnStorageChanged -= OnStorageChanged;
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