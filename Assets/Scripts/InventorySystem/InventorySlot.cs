using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 背包槽位组件 - 管理单个样本槽位的显示和交互
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    public Image slotBackground;
    public Image sampleIcon;
    public Text sampleNameText;
    public Text quantityText;
    
    [Header("视觉设置")]
    public Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    public Color filledSlotColor = new Color(0.5f, 0.5f, 0.5f, 0.9f);
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);
    
    // 事件系统
    public System.Action<InventorySlot, SampleItem> OnSlotClicked;
    
    // 私有成员
    private SampleItem currentSample;
    private int slotIndex;
    private bool isSelected = false;
    
    void Start()
    {
        SetupSlotComponents();
    }
    
    /// <summary>
    /// 设置槽位
    /// </summary>
    public void SetupSlot(int index)
    {
        slotIndex = index;
        SetupSlotComponents();
        SetSample(null); // 初始化为空槽位
    }
    
    /// <summary>
    /// 设置槽位组件
    /// </summary>
    void SetupSlotComponents()
    {
        // 创建槽位背景
        if (slotBackground == null)
        {
            slotBackground = gameObject.AddComponent<Image>();
        }
        slotBackground.color = emptySlotColor;
        
        // 创建样本图标
        if (sampleIcon == null)
        {
            GameObject iconObj = new GameObject("SampleIcon");
            iconObj.transform.SetParent(transform);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(120, 120);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0, 20);
            
            sampleIcon = iconObj.AddComponent<Image>();
            sampleIcon.color = Color.white;
            sampleIcon.gameObject.SetActive(false);
        }
        
        // 创建样本名称文本
        if (sampleNameText == null)
        {
            GameObject nameObj = new GameObject("SampleName");
            nameObj.transform.SetParent(transform);
            
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(150, 40);
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0, 10);
            
            sampleNameText = nameObj.AddComponent<Text>();
            sampleNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sampleNameText.fontSize = 20;
            sampleNameText.color = Color.white;
            sampleNameText.alignment = TextAnchor.MiddleCenter;
            sampleNameText.gameObject.SetActive(false);
        }
        
        // 创建数量文本（目前不使用，但保留用于未来扩展）
        if (quantityText == null)
        {
            GameObject quantityObj = new GameObject("Quantity");
            quantityObj.transform.SetParent(transform);
            
            RectTransform quantityRect = quantityObj.AddComponent<RectTransform>();
            quantityRect.sizeDelta = new Vector2(40, 40);
            quantityRect.anchorMin = new Vector2(1f, 1f);
            quantityRect.anchorMax = new Vector2(1f, 1f);
            quantityRect.pivot = new Vector2(1f, 1f);
            quantityRect.anchoredPosition = new Vector2(-10, -10);
            
            quantityText = quantityObj.AddComponent<Text>();
            quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantityText.fontSize = 24;
            quantityText.color = Color.yellow;
            quantityText.alignment = TextAnchor.MiddleCenter;
            quantityText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 设置样本数据
    /// </summary>
    public void SetSample(SampleItem sample)
    {
        currentSample = sample;
        
        if (sample != null)
        {
            ShowSample(sample);
        }
        else
        {
            ShowEmptySlot();
        }
        
        UpdateVisualState();
    }
    
    /// <summary>
    /// 显示样本
    /// </summary>
    void ShowSample(SampleItem sample)
    {
        // 显示样本图标
        if (sampleIcon != null)
        {
            sampleIcon.gameObject.SetActive(true);
            
            if (sample.Icon != null)
            {
                sampleIcon.sprite = sample.Icon;
                sampleIcon.color = Color.white;
            }
            else
            {
                // 使用默认图标或颜色表示
                sampleIcon.sprite = null;
                sampleIcon.color = GetSampleColor(sample);
            }
        }
        
        // 显示样本名称
        if (sampleNameText != null)
        {
            sampleNameText.gameObject.SetActive(true);
            sampleNameText.text = GetShortName(sample.displayName);
        }
        
        // 更新背景颜色
        if (slotBackground != null)
        {
            slotBackground.color = filledSlotColor;
        }
    }
    
    /// <summary>
    /// 显示空槽位
    /// </summary>
    void ShowEmptySlot()
    {
        // 隐藏样本相关UI
        if (sampleIcon != null)
        {
            sampleIcon.gameObject.SetActive(false);
        }
        
        if (sampleNameText != null)
        {
            sampleNameText.gameObject.SetActive(false);
        }
        
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }
        
        // 更新背景颜色
        if (slotBackground != null)
        {
            slotBackground.color = emptySlotColor;
        }
    }
    
    /// <summary>
    /// 获取样本颜色（当没有图标时使用）
    /// </summary>
    Color GetSampleColor(SampleItem sample)
    {
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
            _ => Color.gray
        };
    }
    
    /// <summary>
    /// 获取短名称（用于显示）
    /// </summary>
    string GetShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        
        // 如果名称过长，截取并添加省略号
        if (fullName.Length > 8)
        {
            return fullName.Substring(0, 6) + "..";
        }
        
        return fullName;
    }
    
    /// <summary>
    /// 更新视觉状态
    /// </summary>
    void UpdateVisualState()
    {
        if (slotBackground != null)
        {
            if (isSelected)
            {
                slotBackground.color = selectedSlotColor;
            }
            else if (currentSample != null)
            {
                slotBackground.color = filledSlotColor;
            }
            else
            {
                slotBackground.color = emptySlotColor;
            }
        }
    }
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 获取当前样本
    /// </summary>
    public SampleItem GetCurrentSample()
    {
        return currentSample;
    }
    
    /// <summary>
    /// 获取槽位索引
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
    
    /// <summary>
    /// 检查槽位是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return currentSample == null;
    }
    
    /// <summary>
    /// 处理点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 左键点击
            OnSlotClicked?.Invoke(this, currentSample);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 右键点击 - 快速操作菜单
            ShowQuickActionMenu();
        }
    }
    
    /// <summary>
    /// 显示快速操作菜单
    /// </summary>
    void ShowQuickActionMenu()
    {
        if (currentSample == null) return;
        
        // TODO: 实现右键快速菜单
        Debug.Log($"右键点击样本: {currentSample.displayName}");
    }
    
    /// <summary>
    /// 获取槽位信息（用于调试）
    /// </summary>
    public string GetSlotInfo()
    {
        string info = $"槽位 {slotIndex}: ";
        
        if (currentSample != null)
        {
            info += $"{currentSample.displayName} (ID: {currentSample.sampleID})";
        }
        else
        {
            info += "空";
        }
        
        return info;
    }
    
    /// <summary>
    /// 在Inspector中显示槽位信息
    /// </summary>
    [ContextMenu("显示槽位信息")]
    void ShowSlotInfo()
    {
        Debug.Log(GetSlotInfo());
    }
}