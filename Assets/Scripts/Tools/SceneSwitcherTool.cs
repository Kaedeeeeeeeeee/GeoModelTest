using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 场景切换器工具 - 实现场景间切换功能
/// 工具ID: 999，点击鼠标左键显示场景选择UI
/// </summary>
public class SceneSwitcherTool : CollectionTool
{
    [Header("场景切换器设置")]
    public GameObject switcherPrefab; // 切换器预制体
    public float useRange = 5f; // 使用范围
    public float useCooldown = 1f; // 使用冷却时间
    
    [Header("音效")]
    public AudioClip switcherActivateSound; // 激活音效
    public AudioClip sceneChangeSound; // 场景切换音效
    
    // 切换器对象引用
    private GameObject equippedSwitcher;
    private Transform playerHand; // 玩家手部位置
    private AudioSource audioSource;
    
    // 场景管理器引用
    private GameSceneManager sceneManager;
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasFKeyPressedLastFrame = false; // 上一帧F键状态
    
    protected override void Start()
    {
        base.Start();
        
        // 配置工具基础属性
        toolID = "999";
        toolName = GetLocalizedToolName();
        useRange = this.useRange;
        useCooldown = this.useCooldown;
        
        // 订阅语言切换事件，实时更新工具名称
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateToolName;
        }
        
        // 初始化音效组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 查找玩家手部位置
        FindPlayerHandPosition();
        
        // 获取场景管理器
        sceneManager = GameSceneManager.Instance;

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }

        Debug.Log("场景切换器工具初始化完成");
    }
    
    /// <summary>
    /// 获取本地化的工具名称
    /// </summary>
    private string GetLocalizedToolName()
    {
        return LocalizationManager.Instance?.GetText("tool.scene_switcher.name") ?? "场景切换器";
    }
    
    /// <summary>
    /// 更新工具名称（语言切换时调用）
    /// </summary>
    private void UpdateToolName()
    {
        toolName = GetLocalizedToolName();
        Debug.Log($"场景切换器名称已更新: {toolName}");
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (equippedSwitcher != null)
        {
            DestroyImmediate(equippedSwitcher);
        }
        
        // 取消订阅语言切换事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateToolName;
        }
    }
    
    /// <summary>
    /// 查找玩家手部位置
    /// </summary>
    void FindPlayerHandPosition()
    {
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            playerHand = player.transform;
            Debug.Log("找到玩家模型，场景切换器将作为玩家的子对象");
        }
        else
        {
            Debug.LogWarning("未找到玩家模型，场景切换器可能无法正确显示");
        }
    }
    
    protected override void HandleInput()
    {
        if (!canUse) return;

        // 处理鼠标左键点击（桌面端）
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseSceneSwitcher();
            return;
        }

        // 处理移动端触摸输入
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                UseSceneSwitcher();
                return;
            }
        }

        // 处理F键输入（移动端虚拟按钮）
        if (IsFKeyPressed())
        {
            UseSceneSwitcher();
        }
    }

    /// <summary>
    /// 检测F键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsFKeyPressed()
    {
        // 键盘F键检测
        bool keyboardFPressed = UnityEngine.InputSystem.Keyboard.current != null &&
                                UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;

        // 移动端F键检测
        bool mobileFPressed = false;
        if (mobileInputManager != null)
        {
            bool currentFKeyState = mobileInputManager.IsSecondaryInteracting;
            mobileFPressed = currentFKeyState && !wasFKeyPressedLastFrame;
            wasFKeyPressedLastFrame = currentFKeyState;
        }

        return keyboardFPressed || mobileFPressed;
    }
    
    /// <summary>
    /// 使用场景切换器
    /// </summary>
    void UseSceneSwitcher()
    {
        if (!canUse)
        {
            Debug.Log("场景切换器在冷却中，无法使用");
            return;
        }

        Debug.Log("场景切换器被激活！");

        // 播放激活音效
        if (switcherActivateSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switcherActivateSound);
        }

        // 播放切换器动画
        PlaySwitcherAnimation();

        // 显示场景选择UI
        if (sceneManager != null)
        {
            Debug.Log("调用场景管理器显示UI");
            sceneManager.ShowSceneSelectionUI();
        }
        else
        {
            Debug.LogError("场景管理器未找到！");
            // 尝试重新获取场景管理器
            sceneManager = GameSceneManager.Instance;
            if (sceneManager != null)
            {
                Debug.Log("重新获取场景管理器成功，显示UI");
                sceneManager.ShowSceneSelectionUI();
            }
            else
            {
                Debug.LogError("仍然无法找到场景管理器！");
            }
        }

        // 设置冷却时间
        lastUseTime = Time.time;
        canUse = false;

        Debug.Log("场景切换器已激活");
    }
    
    /// <summary>
    /// 播放切换器动画
    /// </summary>
    void PlaySwitcherAnimation()
    {
        if (equippedSwitcher != null)
        {
            StartCoroutine(SwitcherActivateAnimation());
        }
    }
    
    /// <summary>
    /// 切换器激活动画协程
    /// </summary>
    IEnumerator SwitcherActivateAnimation()
    {
        if (equippedSwitcher == null) yield break;
        
        Transform switcherTransform = equippedSwitcher.transform;
        Vector3 startPos = switcherTransform.localPosition;
        Vector3 startRot = switcherTransform.localEulerAngles;
        Vector3 startScale = switcherTransform.localScale;
        
        // 动画参数
        float animationDuration = 0.8f;
        float elapsed = 0f;
        
        // 激活动画：轻微上举并发光
        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            
            // 上举动画
            float liftAmount = Mathf.Sin(t * Mathf.PI) * 0.1f;
            Vector3 liftPos = startPos + Vector3.up * liftAmount;
            
            // 旋转动画
            float rotationAmount = Mathf.Sin(t * Mathf.PI * 2) * 10f;
            Vector3 rotationRot = startRot + Vector3.up * rotationAmount;
            
            // 缩放动画
            float scaleAmount = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            Vector3 scaleScale = startScale * scaleAmount;
            
            switcherTransform.localPosition = liftPos;
            switcherTransform.localEulerAngles = rotationRot;
            switcherTransform.localScale = scaleScale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保回到初始状态
        switcherTransform.localPosition = startPos;
        switcherTransform.localEulerAngles = startRot;
        switcherTransform.localScale = startScale;
        
        Debug.Log("场景切换器动画完成");
    }
    
    protected override bool CanUseOnTarget(RaycastHit hit)
    {
        // 场景切换器不需要目标，直接返回true
        return true;
    }
    
    protected override void UseTool(RaycastHit hit)
    {
        // 场景切换器的使用逻辑在HandleInput中处理
        // 这个方法保持为空，符合CollectionTool接口
    }
    
    public override void Equip()
    {
        base.Equip();
        
        // 显示切换器在手中
        ShowSwitcherInHand();
        
        Debug.Log("装备场景切换器");
    }
    
    public override void Unequip()
    {
        base.Unequip();
        
        // 隐藏切换器
        HideSwitcherInHand();
        
        Debug.Log("卸下场景切换器");
    }
    
    /// <summary>
    /// 在手中显示切换器
    /// </summary>
    void ShowSwitcherInHand()
    {
        if (switcherPrefab != null && playerHand != null)
        {
            // 创建切换器实例
            equippedSwitcher = Instantiate(switcherPrefab);
            equippedSwitcher.name = "EquippedSceneSwitcher";
            
            // 设置切换器位置和父级
            equippedSwitcher.transform.SetParent(playerHand);
            
            // 调整切换器位置和旋转（使用用户指定的数值）
            equippedSwitcher.transform.localPosition = new Vector3(0.137f, 0.408f, 0.263f);
            equippedSwitcher.transform.localRotation = Quaternion.Euler(-40.028f, -15.516f, 56.415f);
            equippedSwitcher.transform.localScale = Vector3.one * 15f; // 适当缩放
            
            // 移除碰撞器，避免干扰
            Collider[] colliders = equippedSwitcher.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            
            Debug.Log("场景切换器已装备到手中");
        }
        else
        {
            Debug.LogWarning("无法显示场景切换器：缺少预制体或手部位置");
        }
    }
    
    /// <summary>
    /// 隐藏手中的切换器
    /// </summary>
    void HideSwitcherInHand()
    {
        if (equippedSwitcher != null)
        {
            DestroyImmediate(equippedSwitcher);
            equippedSwitcher = null;
            Debug.Log("场景切换器已从手中移除");
        }
    }
    
    /// <summary>
    /// 显示消息给玩家
    /// </summary>
    void ShowMessage(string message)
    {
        Debug.Log($"[场景切换器] {message}");
        // TODO: 集成UI消息系统
    }
    
}