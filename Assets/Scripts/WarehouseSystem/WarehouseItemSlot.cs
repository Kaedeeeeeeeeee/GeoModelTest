using UnityEngine;
using UnityEngine.UI;
using SampleCuttingSystem;

/// <summary>
/// 仓库物品槽位 - 用于显示背包和仓库中的物品
/// </summary>
public class WarehouseItemSlot : MonoBehaviour
{
    public enum SlotType
    {
        InventorySlot,   // 背包槽位
        WarehouseSlot    // 仓库槽位
    }
    
    [Header("槽位设置")]
    public SlotType slotType = SlotType.InventorySlot;
    
    [Header("UI组件")]
    public Image slotBackground;
    public Image itemIcon;
    public Text itemText;
    public Button slotButton;
    
    [Header("视觉反馈")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    
    // 私有成员
    private SampleItem currentItem;
    private bool hasItem = false;
    private bool isSelected = false;
    private GameObject selectionMark; // 选中标记
    private GameObject disabledMark; // 禁用标记（红叉）
    
    // 事件
    public System.Action<WarehouseItemSlot> OnSlotClicked;
    public System.Action<WarehouseItemSlot> OnSlotHover;
    public System.Action<WarehouseItemSlot> OnSlotExit;
    
    void Start()
    {
        // 确保按钮事件正确绑定
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
        }
    }
    
    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Initialize(Image background, Image icon, Text text, Button button)
    {
        slotBackground = background;
        itemIcon = icon;
        itemText = text;
        slotButton = button;
        
        // 绑定按钮事件
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
        }
        
        // 创建选中标记
        CreateSelectionMark();

        // 初始化拖拽功能
        InitializeDragHandler();

        // 初始化为空槽位
        ClearItem();
    }

    /// <summary>
    /// 初始化拖拽处理器
    /// </summary>
    void InitializeDragHandler()
    {
        // 检查是否已有SampleDragHandler组件
        var existingHandler = GetComponent<SampleDragHandler>();
        if (existingHandler == null)
        {
            // 添加SampleDragHandler组件
            var dragHandler = gameObject.AddComponent<SampleDragHandler>();
            Debug.Log($"[WarehouseItemSlot] 为槽位添加了SampleDragHandler组件");
        }
    }

    /// <summary>
    /// 设置槽位类型
    /// </summary>
    public void SetSlotType(SlotType type)
    {
        slotType = type;
    }
    
    /// <summary>
    /// 设置物品
    /// </summary>
    public void SetItem(SampleItem item)
    {
        // 检查对象是否仍然有效
        if (this == null) return;

        currentItem = item;
        hasItem = item != null;
        
        if (hasItem)
        {
            ShowSample(item);
        }
        else
        {
            ShowEmptySlot();
        }
        
        UpdateVisualState();
    }
    
    /// <summary>
    /// 显示样本（完全模仿背包系统）
    /// </summary>
    void ShowSample(SampleItem sample)
    {
        // 检查对象是否仍然有效
        if (this == null || sample == null) return;

        // 显示样本图标
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            
            if (sample.Icon != null)
            {
                // 使用样本的预生成图标
                itemIcon.sprite = sample.Icon;
                itemIcon.color = Color.white;
            }
            else
            {
                // 使用动态图标生成系统
                if (SampleIconGenerator.Instance != null)
                {
                    Sprite dynamicIcon = SampleIconGenerator.Instance.GenerateIconForSample(sample);
                    if (dynamicIcon != null)
                    {
                        itemIcon.sprite = dynamicIcon;
                        itemIcon.color = Color.white;
                    }
                    else
                    {
                        // 生成失败，使用颜色方案
                        itemIcon.sprite = CreateWhiteSquareSprite();
                        itemIcon.color = GetSampleColor(sample);
                    }
                }
                else
                {
                    // 没有图标生成器，使用颜色方案
                    itemIcon.sprite = CreateWhiteSquareSprite();
                    itemIcon.color = GetSampleColor(sample);
                }
            }
        }
        
        // 显示样本名称
        if (itemText != null)
        {
            itemText.gameObject.SetActive(true);
            string shortName = GetShortName(sample.displayName);
            itemText.text = shortName;
        }
        
        // 更新背景颜色
        if (slotBackground != null)
        {
            slotBackground.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); // filledSlotColor
        }
        
        // 检查样本是否可以切割（层数>1）
        UpdateCuttingEligibility(sample);

        // 更新拖拽处理器的样本数据
        UpdateDragHandlerData(sample);
    }
    
    /// <summary>
    /// 显示空槽位（完全模仿背包系统）
    /// </summary>
    void ShowEmptySlot()
    {
        // 隐藏样本相关UI
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
        }
        
        if (itemText != null)
        {
            itemText.gameObject.SetActive(false);
        }
        
        // 更新背景颜色
        if (slotBackground != null)
        {
            slotBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // emptySlotColor
        }
        
        currentItem = null;
        hasItem = false;
        isSelected = false;
        
        // 关键修复：更新选中标记显示
        UpdateSelectionVisual();
        UpdateVisualState();
        
        // 隐藏禁用标记
        HideDisabledMark();
    }
    
    /// <summary>
    /// 清空物品（保留用于兼容性）
    /// </summary>
    public void ClearItem()
    {
        ShowEmptySlot();
    }
    
    
    /// <summary>
    /// 创建白色方块sprite用于显示颜色
    /// </summary>
    Sprite CreateWhiteSquareSprite()
    {
        // 创建一个简单的1x1白色texture
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // 创建sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        return sprite;
    }
    
    /// <summary>
    /// 获取样本颜色（完全模仿背包系统）
    /// </summary>
    Color GetSampleColor(SampleItem sample)
    {
        if (sample == null) return Color.gray;
        
        // 基于样本的地质层颜色生成代表性颜色
        if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
        {
            // 使用第一层的颜色
            return sample.geologicalLayers[0].layerColor;
        }
        
        // 基于工具ID生成颜色
        return sample.sourceToolID switch
        {
            "1000" => new Color(0.8f, 0.6f, 0.4f), // 简易钻探 - 棕色
            "1001" => new Color(0.6f, 0.8f, 0.4f), // 钻塔 - 绿色
            "1002" => new Color(0.8f, 0.4f, 0.6f), // 地质锤 - 粉色
            _ => Color.gray
        };
    }
    
    
    /// <summary>
    /// 获取短名称（完全模仿背包系统）
    /// </summary>
    string GetShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        
        // 根据槽位类型调整文本长度
        int maxLength = slotType == SlotType.InventorySlot ? 8 : 6;
        
        // 如果名称过长，截取并添加省略号
        if (fullName.Length > maxLength)
        {
            return fullName.Substring(0, maxLength - 2) + "..";
        }
        
        return fullName;
    }
    
    
    /// <summary>
    /// 设置槽位颜色
    /// </summary>
    public void SetSlotColor(Color color)
    {
        if (slotBackground != null)
        {
            slotBackground.color = color;
        }
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 先更新视觉状态（背景颜色）
        UpdateVisualState();
        
        // 再更新选中标记
        UpdateSelectionVisual();
        
        // 强制刷新Canvas，确保视觉更新生效
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// 创建选中标记
    /// </summary>
    void CreateSelectionMark()
    {
        // 如果已存在，先销毁再重新创建
        if (selectionMark != null)
        {
            DestroyImmediate(selectionMark);
        }
        
        selectionMark = new GameObject("SelectionMark");
        selectionMark.transform.SetParent(transform, false);
        
        RectTransform markRect = selectionMark.AddComponent<RectTransform>();
        markRect.sizeDelta = new Vector2(30, 30); // 进一步增大尺寸
        markRect.anchorMin = new Vector2(1, 1);
        markRect.anchorMax = new Vector2(1, 1);
        markRect.pivot = new Vector2(1, 1);
        markRect.anchoredPosition = new Vector2(-5, -5); // 调整位置更靠近边角
        markRect.localScale = Vector3.one;
        
        Image markImage = selectionMark.AddComponent<Image>();
        markImage.color = new Color(0f, 1f, 0f, 1f); // 完全不透明的绿色
        markImage.sprite = CreateCheckmarkSprite();
        markImage.raycastTarget = false; // 防止阻挡点击
        markImage.type = Image.Type.Simple;
        markImage.preserveAspect = true;
        
        // 确保标记在最顶层
        selectionMark.transform.SetAsLastSibling();
        
        // 初始状态为非活跃
        selectionMark.SetActive(false);
    }
    
    /// <summary>
    /// 创建勾选标记Sprite
    /// </summary>
    Sprite CreateCheckmarkSprite()
    {
        // 创建一个更大更明显的勾选标记纹理
        Texture2D texture = new Texture2D(24, 24);
        
        // 填充背景为半透明的深绿色圆形
        for (int x = 0; x < 24; x++)
        {
            for (int y = 0; y < 24; y++)
            {
                float centerX = 12f;
                float centerY = 12f;
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                
                if (distance <= 11f) // 圆形背景
                {
                    texture.SetPixel(x, y, new Color(0f, 0.7f, 0f, 0.9f)); // 深绿色背景
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        // 添加白色勾选标记
        for (int x = 0; x < 24; x++)
        {
            for (int y = 0; y < 24; y++)
            {
                // 更粗的勾选标记
                if ((x >= 7 && x <= 9 && y >= 4 && y <= 12) || // 勾的短边
                    (x >= 9 && x <= 17 && y >= 10 && y <= 19)) // 勾的长边
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 更新选中标记显示
    /// </summary>
    void UpdateSelectionVisual()
    {
        // 如果标记不存在，先创建
        if (selectionMark == null)
        {
            CreateSelectionMark();
        }
        
        bool shouldShow = isSelected && hasItem;
        
        if (selectionMark != null)
        {
            // 先设置为非活跃，再设置为活跃（强制刷新）
            selectionMark.SetActive(false);
            
            if (shouldShow)
            {
                // 确保标记在最顶层
                selectionMark.transform.SetAsLastSibling();
                
                // 检查并确保 Image 组件存在且正确设置
                Image markImage = selectionMark.GetComponent<Image>();
                if (markImage != null)
                {
                    markImage.enabled = true;
                    markImage.color = new Color(0f, 1f, 0f, 1f); // 绿色，完全不透明
                }
                
                // 最后激活标记
                selectionMark.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 更新视觉状态（模仿背包系统）
    /// </summary>
    void UpdateVisualState()
    {
        if (slotBackground == null) return;
        
        if (isSelected)
        {
            slotBackground.color = new Color(0.8f, 0.8f, 0.2f, 0.9f); // selectedSlotColor
        }
        else if (hasItem)
        {
            slotBackground.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); // filledSlotColor
        }
        else
        {
            slotBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // emptySlotColor
        }
    }
    
    /// <summary>
    /// 槽位点击事件
    /// </summary>
    void OnSlotClick()
    {
        OnSlotClicked?.Invoke(this);
    }
    
    /// <summary>
    /// 检查是否有物品
    /// </summary>
    public bool HasItem()
    {
        return hasItem && currentItem != null;
    }
    
    /// <summary>
    /// 获取当前物品
    /// </summary>
    public SampleItem GetItem()
    {
        return currentItem;
    }
    
    /// <summary>
    /// 检查是否被选中
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// 获取槽位类型
    /// </summary>
    public SlotType GetSlotType()
    {
        return slotType;
    }
    
    /// <summary>
    /// 获取物品ID
    /// </summary>
    public string GetItemID()
    {
        return currentItem?.sampleID ?? "";
    }
    
    /// <summary>
    /// 获取物品名称
    /// </summary>
    public string GetItemName()
    {
        return currentItem?.displayName ?? "";
    }
    
    /// <summary>
    /// 获取工具ID
    /// </summary>
    public string GetToolID()
    {
        return currentItem?.sourceToolID ?? "";
    }
    
    /// <summary>
    /// 获取采集时间
    /// </summary>
    public System.DateTime GetCollectionTime()
    {
        return currentItem?.collectionTime ?? System.DateTime.MinValue;
    }
    
    /// <summary>
    /// 获取采集位置
    /// </summary>
    public Vector3 GetCollectionPosition()
    {
        return currentItem?.originalCollectionPosition ?? Vector3.zero;
    }
    
    /// <summary>
    /// 获取槽位详细信息
    /// </summary>
    public string GetSlotInfo()
    {
        if (!hasItem)
        {
            return $"[{slotType}] 空槽位";
        }
        
        return $"[{slotType}] {currentItem.displayName}\n" +
               $"ID: {currentItem.sampleID}\n" +
               $"工具: {currentItem.sourceToolID}\n" +
               $"时间: {currentItem.collectionTime:MM-dd HH:mm}\n" +
               $"位置: {currentItem.originalCollectionPosition}\n" +
               $"选中: {isSelected}";
    }
    
    /// <summary>
    /// 鼠标悬停效果
    /// </summary>
    public void OnPointerEnter()
    {
        if (hasItem && !isSelected)
        {
            SetSlotColor(hoverColor);
        }
        
        OnSlotHover?.Invoke(this);
    }
    
    /// <summary>
    /// 鼠标离开效果
    /// </summary>
    public void OnPointerExit()
    {
        if (hasItem && !isSelected)
        {
            UpdateVisualState();
        }
        
        OnSlotExit?.Invoke(this);
    }
    
    /// <summary>
    /// 在Inspector中显示槽位信息
    /// </summary>
    [ContextMenu("显示槽位信息")]
    void ShowSlotInfo()
    {
        Debug.Log(GetSlotInfo());
    }
    
    /// <summary>
    /// 验证槽位组件
    /// </summary>
    [ContextMenu("验证槽位组件")]
    void ValidateSlotComponents()
    {
        bool isValid = true;
        
        if (slotBackground == null)
        {
            Debug.LogError("槽位背景组件缺失");
            isValid = false;
        }
        
        if (itemIcon == null)
        {
            Debug.LogError("物品图标组件缺失");
            isValid = false;
        }
        
        if (itemText == null)
        {
            Debug.LogError("物品文本组件缺失");
            isValid = false;
        }
        
        if (slotButton == null)
        {
            Debug.LogError("槽位按钮组件缺失");
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log("槽位组件验证通过");
        }
    }
    
    /// <summary>
    /// 强制显示选中标记（测试用）
    /// </summary>
    [ContextMenu("强制显示选中标记")]
    void ForceShowSelectionMark()
    {
        if (selectionMark == null)
        {
            CreateSelectionMark();
        }
        
        isSelected = true;
        selectionMark.SetActive(true);
        Debug.Log($"[WarehouseItemSlot] 强制显示选中标记，活跃状态: {selectionMark.activeInHierarchy}");
        
        // 3秒后自动隐藏
        Invoke(nameof(HideSelectionMark), 3f);
    }
    
    void HideSelectionMark()
    {
        isSelected = false;
        if (selectionMark != null)
        {
            selectionMark.SetActive(false);
        }
        Debug.Log("[WarehouseItemSlot] 自动隐藏选中标记");
    }
    
    /// <summary>
    /// 更新样本的切割资格显示
    /// </summary>
    void UpdateCuttingEligibility(SampleItem sample)
    {
        if (sample == null || this == null) return;

        bool canBeCut = sample.layerCount > 1;

        if (canBeCut)
        {
            // 可以切割，隐藏禁用标记
            HideDisabledMark();
        }
        else
        {
            // 不可切割，显示红叉标记
            ShowDisabledMark();
        }

        // 更新拖拽组件的可用性 - 所有样本都应该可以拖拽
        // 即使是单层样本也可以拖拽到切割台进行分析
        UpdateDragComponent(true);
    }

    /// <summary>
    /// 更新拖拽处理器的样本数据
    /// </summary>
    void UpdateDragHandlerData(SampleItem sample)
    {
        if (this == null || sample == null) return;

        var dragHandler = GetComponent<SampleDragHandler>();
        if (dragHandler != null)
        {
            // 将SampleItem转换为SampleData
            var sampleData = new SampleData(
                sample.displayName,
                $"来源工具: {sample.sourceToolID}, 收集时间: {sample.collectionTime}",
                sample.layerCount
            );

            // 使用公开的SetSampleData方法
            dragHandler.SetSampleData(sampleData);
            Debug.Log($"[WarehouseItemSlot] 为拖拽处理器设置样本数据: {sample.displayName} (层数: {sample.layerCount})");
        }
    }

    /// <summary>
    /// 显示禁用标记（红叉）
    /// </summary>
    void ShowDisabledMark()
    {
        if (disabledMark == null)
        {
            CreateDisabledMark();
        }
        
        if (disabledMark != null)
        {
            disabledMark.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏禁用标记
    /// </summary>
    void HideDisabledMark()
    {
        if (disabledMark != null)
        {
            disabledMark.SetActive(false);
        }
    }
    
    /// <summary>
    /// 创建禁用标记（红叉图标）
    /// </summary>
    void CreateDisabledMark()
    {
        if (itemIcon == null) return;
        
        // 创建禁用标记对象
        disabledMark = new GameObject("DisabledMark");
        disabledMark.transform.SetParent(itemIcon.transform, false);
        
        // 添加Image组件
        Image markImage = disabledMark.AddComponent<Image>();
        
        // 创建红叉纹理
        Texture2D crossTexture = CreateRedCrossTexture();
        Sprite crossSprite = Sprite.Create(crossTexture, new Rect(0, 0, crossTexture.width, crossTexture.height), Vector2.one * 0.5f);
        
        markImage.sprite = crossSprite;
        markImage.color = Color.red;
        
        // 设置位置和大小（居中对齐）
        RectTransform markRect = disabledMark.GetComponent<RectTransform>();
        markRect.anchorMin = new Vector2(0.5f, 0.5f);
        markRect.anchorMax = new Vector2(0.5f, 0.5f);
        markRect.anchoredPosition = Vector2.zero;
        markRect.sizeDelta = new Vector2(32, 32); // 增大尺寸使其更清晰
        
        // 默认隐藏
        disabledMark.SetActive(false);
    }
    
    /// <summary>
    /// 创建红叉纹理
    /// </summary>
    Texture2D CreateRedCrossTexture()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        
        // 填充透明背景
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        
        // 绘制红叉
        int thickness = 3;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // 主对角线
                if (Mathf.Abs(x - y) < thickness)
                {
                    colors[y * size + x] = Color.red;
                }
                // 副对角线
                if (Mathf.Abs(x - (size - 1 - y)) < thickness)
                {
                    colors[y * size + x] = Color.red;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 更新拖拽组件的可用性
    /// </summary>
    void UpdateDragComponent(bool canBeCut)
    {
        // 检查对象是否仍然有效
        if (this == null) return;

        // 检查SampleDragHandler组件
        var dragHandler = GetComponent<SampleDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.SetDraggingEnabled(canBeCut);
            Debug.Log($"设置样本 {currentItem?.displayName} 的拖拽状态: {canBeCut}");
        }
    }
    
    /// <summary>
    /// 检查样本是否可以被切割
    /// </summary>
    public bool CanSampleBeCut()
    {
        return currentItem != null && currentItem.layerCount > 1;
    }
    
    void OnDestroy()
    {
        // 清理事件订阅
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnSlotClick);
        }
        
        // 清理禁用标记
        if (disabledMark != null)
        {
            DestroyImmediate(disabledMark);
        }
    }
}