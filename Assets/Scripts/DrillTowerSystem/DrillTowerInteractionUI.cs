using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 钻塔交互UI提示系统
/// </summary>
public class DrillTowerInteractionUI : MonoBehaviour
{
    [Header("UI设置")]
    public Canvas uiCanvas;
    public GameObject interactionPrompt;
    public Text promptText;
    public float promptDistance = 3f;
    
    [Header("提示文本")]
    public string basePromptText = "按 F 键进行钻探";
    public string drillingText = "钻探中...";
    public string maxDepthText = "已达最大钻探深度";
    
    private DrillTower currentTower;
    private Camera playerCamera;
    private bool isShowingPrompt = false;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }

        CreateInteractionUI();

        // 监听语言切换事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedTexts;
        }
    }
    
    void Update()
    {
        UpdateInteractionPrompt();
    }
    
    /// <summary>
    /// 创建交互UI
    /// </summary>
    void CreateInteractionUI()
    {
        // 如果没有Canvas，创建一个
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("DrillTowerInteractionCanvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100; // 确保在最前面
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 创建交互提示
        if (interactionPrompt == null)
        {
            interactionPrompt = new GameObject("InteractionPrompt");
            interactionPrompt.transform.SetParent(uiCanvas.transform);
            
            RectTransform rectTransform = interactionPrompt.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400, 120); // 增加宽度和高度以适应更多文本
            
            // 添加背景
            Image background = interactionPrompt.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.7f);
            
            // 创建文本
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(interactionPrompt.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            promptText = textObj.AddComponent<Text>();
            promptText.text = basePromptText;
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 24;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;
            
            // 初始隐藏
            interactionPrompt.SetActive(false);
        }
        
        Debug.Log("钻塔交互UI创建完成");
    }
    
    /// <summary>
    /// 更新交互提示
    /// </summary>
    void UpdateInteractionPrompt()
    {
        if (playerCamera == null) return;
        
        // 查找最近的钻塔
        DrillTower nearestTower = FindNearestTower();
        
        if (nearestTower != null && nearestTower != currentTower)
        {
            currentTower = nearestTower;
        }
        
        if (currentTower != null)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, currentTower.transform.position);
            
            if (distance <= promptDistance)
            {
                ShowInteractionPrompt(currentTower);
            }
            else
            {
                HideInteractionPrompt();
            }
        }
        else
        {
            HideInteractionPrompt();
        }
    }
    
    /// <summary>
    /// 查找最近的钻塔
    /// </summary>
    DrillTower FindNearestTower()
    {
        DrillTower[] allTowers = FindObjectsOfType<DrillTower>();
        DrillTower nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (DrillTower tower in allTowers)
        {
            float distance = Vector3.Distance(playerCamera.transform.position, tower.transform.position);
            if (distance < minDistance && distance <= promptDistance)
            {
                minDistance = distance;
                nearest = tower;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// 显示交互提示
    /// </summary>
    void ShowInteractionPrompt(DrillTower tower)
    {
        // 检查UI对象是否仍然有效
        if (interactionPrompt == null)
        {
            Debug.LogWarning("[DrillTowerInteractionUI] interactionPrompt已被销毁，重新初始化UI");
            CreateInteractionUI();
            return;
        }

        if (!isShowingPrompt)
        {
            interactionPrompt.SetActive(true);
            isShowingPrompt = true;
        }

        // 更新提示文本
        UpdatePromptText(tower);
    }
    
    /// <summary>
    /// 隐藏交互提示
    /// </summary>
    void HideInteractionPrompt()
    {
        if (isShowingPrompt)
        {
            // 检查UI对象是否仍然有效
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            isShowingPrompt = false;
            currentTower = null;
        }
    }
    
    /// <summary>
    /// 更新提示文本内容
    /// </summary>
    void UpdatePromptText(DrillTower tower)
    {
        if (promptText == null) return;

        if (tower.isDrilling)
        {
            promptText.text = LocalizationManager.Instance?.GetText("drill_tower.drilling") ?? "钻探中...";
            promptText.color = Color.yellow;
        }
        else if (tower.currentDrillCount >= 5) // 假设最大5次钻探
        {
            string maxDepthText = LocalizationManager.Instance?.GetText("drill_tower.max_depth") ?? "已达最大钻探深度";
            string recallText = LocalizationManager.Instance?.GetText("drill_tower.recall_prompt") ?? "按 G 键收回钻塔";
            promptText.text = $"{maxDepthText}\n{recallText}";
            promptText.color = Color.red;
        }
        else
        {
            int nextDrillNumber = tower.currentDrillCount + 1;
            float startDepth = tower.currentDrillCount * 2f;
            float endDepth = startDepth + 2f;

            string drillPrompt = LocalizationManager.Instance?.GetText("drill_tower.drill_prompt", nextDrillNumber, startDepth, endDepth) ?? $"按 F 键进行第{nextDrillNumber}次钻探\n({startDepth:F0}m-{endDepth:F0}m)";
            string recallText = LocalizationManager.Instance?.GetText("drill_tower.recall_prompt") ?? "按 G 键收回钻塔";
            promptText.text = $"{drillPrompt}\n{recallText}";
            promptText.color = Color.white;
        }
    }

    /// <summary>
    /// 更新本地化文本（语言切换时调用）
    /// </summary>
    void UpdateLocalizedTexts()
    {
        // 如果当前正在显示提示，则重新更新文本
        if (currentTower != null && isShowingPrompt)
        {
            UpdatePromptText(currentTower);
        }
    }

    void OnDestroy()
    {
        // 移除语言切换事件监听器
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
        }
    }
}