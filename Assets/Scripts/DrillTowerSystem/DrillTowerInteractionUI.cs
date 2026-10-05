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
    public string basePromptText = "［F］コアを採取する";
    public string drillingText = "コアを採取しています…";
    public string maxDepthText = "調査できる最大の深さに達しました";
    
    private UISystem.InteractionPrompt sharedPrompt;
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
        if (Core.GameInputState.IsModalOpen || StorySystem.StoryDirector.IsStoryPlaybackActive ||
            InventoryUISystem.IsAnyWheelOpen || QuestSystem.QuestManager.IsPickupFeedbackPending)
        {
            HideInteractionPrompt();
            return;
        }
        UpdateInteractionPrompt();
    }
    
    /// <summary>
    /// 创建交互UI
    /// </summary>
    void CreateInteractionUI()
    {
        if (sharedPrompt != null) return;
        sharedPrompt = UISystem.InteractionPrompt.Create("DrillTowerInteractionCanvas", "InteractionPrompt", 165, UISystem.MobileControlHint.Control.Secondary);
        uiCanvas = sharedPrompt.Canvas;
        interactionPrompt = sharedPrompt.Panel.gameObject;
        promptText = sharedPrompt.Action;
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
            sharedPrompt.SetVisible(true, 1);
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
                sharedPrompt.SetVisible(false);
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
        if (sharedPrompt == null) return;
        bool touch = UISystem.MobileControlHint.UsesTouchControls;
        string text;
        bool warning = false;
        if (tower.isDrilling)
        {
            text = UISystem.GameUI.L("drill_tower.drilling");
        }
        else if (tower.CanPutAway())
        {
            text = UISystem.GameUI.L("drill_tower.max_depth") + "\n" + UISystem.GameUI.L("drill_tower.put_away_action");
            warning = true;
        }
        else
        {
            float startDepth = tower.currentDrillCount * tower.toolReference.depthPerDrill;
            text = LocalizationManager.Resolve("drill_tower.drill_action", "{1:F0}～{2:F0}mのコアを採取する",
                tower.currentDrillCount + 1, startDepth, startDepth + tower.toolReference.depthPerDrill);
        }
        sharedPrompt.SetContent(text, "F", touch, warning, !tower.isDrilling);
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
        if (uiCanvas != null) Destroy(uiCanvas.gameObject);
        // 移除语言切换事件监听器
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
        }
    }
}
