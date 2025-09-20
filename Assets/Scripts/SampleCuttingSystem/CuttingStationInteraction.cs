using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SampleCuttingSystem
{
    /// <summary>
    /// 切割台交互检测组件
    /// 处理玩家接近检测和切割界面打开
    /// </summary>
    public class CuttingStationInteraction : MonoBehaviour
    {
        [Header("交互设置")]
        [SerializeField] private float interactionRange = 3f;        // 交互范围
        [SerializeField] private LayerMask playerLayer = -1;        // 玩家层级
        [SerializeField] private KeyCode interactionKey = KeyCode.F; // 交互按键

        [Header("UI提示")]
        [SerializeField] private GameObject interactionPrompt;      // 交互提示UI
        [SerializeField] private Text promptText;                   // 提示文字
        [SerializeField] private Canvas promptCanvas;               // 提示Canvas

        [Header("切割界面")]
        [SerializeField] private GameObject cuttingInterfacePrefab; // 切割界面预制体
        [SerializeField] private Transform interfaceParent;         // 界面父对象

        // 状态变量
        private bool playerInRange = false;
        private GameObject nearbyPlayer;
        private GameObject currentCuttingInterface;
        private MobileInputManager mobileInputManager; // 移动端输入管理器
        private bool wasFKeyPressedLastFrame = false; // 上一帧F键状态
        private SampleCuttingSystemManager cuttingSystemManager;

        void Start()
        {
            SetupInteractionPrompt();
            SetupComponents();

            // 获取移动端输入管理器
            mobileInputManager = MobileInputManager.Instance;
            if (mobileInputManager == null)
            {
                mobileInputManager = FindObjectOfType<MobileInputManager>();
            }
        }

        void Update()
        {
            CheckPlayerInteraction();
            HandleInput();
        }

        /// <summary>
        /// 检测F键输入 - 支持键盘和移动端虚拟按钮
        /// </summary>
        bool IsFKeyPressed()
        {
            // 键盘F键检测 - 支持新旧输入系统
            bool keyboardFPressed = false;

            // 优先使用旧输入系统（更兼容）
            keyboardFPressed = Input.GetKeyDown(interactionKey);

            // 如果旧输入系统无效，尝试新输入系统
            if (!keyboardFPressed && Keyboard.current != null)
            {
                keyboardFPressed = Keyboard.current.fKey.wasPressedThisFrame;
            }

            // 移动端F键检测
            bool mobileFPressed = false;
            if (mobileInputManager != null)
            {
                bool currentFKeyState = mobileInputManager.IsSecondaryInteracting;
                mobileFPressed = currentFKeyState && !wasFKeyPressedLastFrame;
                wasFKeyPressedLastFrame = currentFKeyState;
            }

            // 添加调试输出
            if (keyboardFPressed || mobileFPressed)
            {
                Debug.Log($"F键被按下! 键盘: {keyboardFPressed}, 移动端: {mobileFPressed}");
            }

            return keyboardFPressed || mobileFPressed;
        }

        /// <summary>
        /// 设置组件引用
        /// </summary>
        private void SetupComponents()
        {
            cuttingSystemManager = GetComponent<SampleCuttingSystemManager>();
            if (cuttingSystemManager == null)
            {
                Debug.LogWarning("切割系统管理器未找到");
            }

            Debug.Log("设置界面父对象...");
            // 设置界面父对象
            if (interfaceParent == null)
            {
                // 查找或创建UI Canvas
                Canvas uiCanvas = FindUICanvas();
                if (uiCanvas != null)
                {
                    interfaceParent = uiCanvas.transform;
                    Debug.Log($"界面父对象设置为: {interfaceParent.name}");
                }
                else
                {
                    Debug.LogError("无法找到或创建UI Canvas！");
                }
            }
        }

        /// <summary>
        /// 查找或创建UI Canvas
        /// </summary>
        private Canvas FindUICanvas()
        {
            // 首先尝试找现有的Canvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Debug.Log($"找到现有Canvas: {canvas.name}");
                    return canvas;
                }
            }

            // 如果没有找到，创建新的
            Debug.Log("未找到合适的Canvas，创建新的");
            GameObject canvasObj = new GameObject("CuttingUICanvas");
            Canvas newCanvas = canvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 50; // 低于移动端UI的100

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            return newCanvas;
        }

        /// <summary>
        /// 设置交互提示
        /// </summary>
        private void SetupInteractionPrompt()
        {
            if (interactionPrompt == null)
            {
                CreateInteractionPrompt();
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        /// <summary>
        /// 创建交互提示UI
        /// </summary>
        private void CreateInteractionPrompt()
        {
            Debug.Log("创建交互提示UI...");

            // 创建提示Canvas - 使用屏幕空间覆盖
            GameObject promptCanvasObj = new GameObject("InteractionPromptCanvas");
            promptCanvas = promptCanvasObj.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.sortingOrder = 500; // 确保在其他UI之上

            // 添加必要组件
            promptCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            promptCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform canvasRect = promptCanvasObj.GetComponent<RectTransform>();

            // 创建提示背景 - 显示在屏幕底部中央
            GameObject promptBg = new GameObject("PromptBackground");
            promptBg.transform.SetParent(promptCanvasObj.transform, false);

            Image bgImage = promptBg.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);

            RectTransform bgRect = promptBg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0f); // 底部中央
            bgRect.anchorMax = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0, 100); // 距离底部100像素
            bgRect.sizeDelta = new Vector2(300, 80); // 固定大小

            // 创建提示文字
            GameObject promptTextObj = new GameObject("PromptText");
            promptTextObj.transform.SetParent(promptBg.transform, false);

            promptText = promptTextObj.AddComponent<Text>();
            promptText.text = "[F] 使用切割台";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 20;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.fontStyle = FontStyle.Bold;

            RectTransform textRect = promptTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            interactionPrompt = promptCanvasObj;

            Debug.Log("交互提示UI创建完成");
        }

        /// <summary>
        /// 检查玩家交互
        /// </summary>
        private void CheckPlayerInteraction()
        {
            // 检测范围内的玩家
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);

            bool foundPlayer = false;
            foreach (Collider col in nearbyColliders)
            {
                if (col.CompareTag("Player") || col.GetComponent<FirstPersonController>() != null)
                {
                    nearbyPlayer = col.gameObject;
                    foundPlayer = true;
                    break;
                }
            }

            // 更新交互状态
            if (foundPlayer && !playerInRange)
            {
                // 玩家进入范围
                playerInRange = true;
                ShowInteractionPrompt(true);
                Debug.Log($"玩家进入切割台交互范围 - 玩家: {nearbyPlayer.name}");
            }
            else if (!foundPlayer && playerInRange)
            {
                // 玩家离开范围
                playerInRange = false;
                nearbyPlayer = null;
                ShowInteractionPrompt(false);
                Debug.Log("玩家离开切割台交互范围");
            }

            // 调试输出已禁用
        }

        /// <summary>
        /// 显示/隐藏交互提示
        /// </summary>
        private void ShowInteractionPrompt(bool show)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
                if (show)
                {
                    Debug.Log("显示F键交互提示");
                }
                else
                {
                    Debug.Log("隐藏F键交互提示");
                }
            }
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            if (playerInRange && IsFKeyPressed())
            {
                Debug.Log("玩家在范围内并按下F键，打开切割界面");
                OpenCuttingInterface();
            }

            // 添加ESC键快速关闭功能 - 支持新旧输入系统
            bool escPressed = Input.GetKeyDown(KeyCode.Escape);
            if (!escPressed && Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }

            if (currentCuttingInterface != null && escPressed)
            {
                CloseCuttingInterface();
            }
        }

        /// <summary>
        /// 打开切割界面
        /// </summary>
        public void OpenCuttingInterface()
        {
            // 强制清理状态，防止遗留对象阻止界面打开
            if (currentCuttingInterface != null)
            {
                Debug.LogWarning("检测到遗留的切割界面引用，进行清理");

                // 检查对象是否真的存在
                if (currentCuttingInterface == null) // Unity的null检查（对象已被销毁）
                {
                    Debug.Log("界面对象已被销毁，清理引用");
                    currentCuttingInterface = null;
                }
                else if (currentCuttingInterface.activeInHierarchy == false)
                {
                    Debug.Log("界面对象已被禁用，清理引用");
                    currentCuttingInterface = null;
                }
                else
                {
                    Debug.LogWarning("切割界面已经打开且激活，跳过");
                    return;
                }
            }

            Debug.Log("=== 开始打开切割界面 ===");

            // 重新验证Canvas状态（可能被其他系统删除）
            Debug.Log("重新检查Canvas状态...");
            if (interfaceParent == null)
            {
                Debug.LogWarning("界面父对象为空，重新设置Canvas");
                Canvas uiCanvas = FindUICanvas();
                if (uiCanvas != null)
                {
                    interfaceParent = uiCanvas.transform;
                    Debug.Log($"重新设置界面父对象: {interfaceParent.name}");
                }
                else
                {
                    Debug.LogError("无法找到或创建UI Canvas！");
                    return;
                }
            }
            else
            {
                Debug.Log($"当前界面父对象: {interfaceParent.name}");

                // 验证Canvas还存在且活跃
                if (interfaceParent.gameObject == null || !interfaceParent.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("Canvas已被删除或禁用，重新创建");
                    Canvas newCanvas = FindUICanvas();
                    interfaceParent = newCanvas.transform;
                }
            }

            // 调试信息
            var warehouseUI = FindFirstObjectByType<WarehouseUI>();
            Debug.Log($"找到WarehouseUI: {warehouseUI != null}");
            if (warehouseUI != null)
            {
                Debug.Log($"WarehousePanel存在: {warehouseUI.warehousePanel != null}");
                if (warehouseUI.warehousePanel != null)
                {
                    Debug.Log($"WarehousePanel激活状态: {warehouseUI.warehousePanel.activeInHierarchy}");
                }
            }

            Debug.Log("开始创建切割界面...");
            // 创建切割界面
            CreateCuttingInterface();

            // 检查界面是否创建成功
            if (currentCuttingInterface != null)
            {
                Debug.Log($"✅ 切割界面创建成功: {currentCuttingInterface.name}");
                Debug.Log($"界面激活状态: {currentCuttingInterface.activeInHierarchy}");

                // 详细验证UI状态
                VerifyUICreation();
            }
            else
            {
                Debug.LogError("❌ 切割界面创建失败！");
            }

            // 暂停游戏或禁用玩家控制
            SetPlayerControlEnabled(false);

            Debug.Log("=== 切割界面打开流程完成 ===");
        }

        /// <summary>
        /// 创建切割界面
        /// </summary>
        private void CreateCuttingInterface()
        {
            // 如果有预制体，使用预制体
            if (cuttingInterfacePrefab != null)
            {
                currentCuttingInterface = Instantiate(cuttingInterfacePrefab, interfaceParent);
            }
            else
            {
                // 创建基础切割界面
                currentCuttingInterface = CreateBasicCuttingInterface();
            }

            // 设置界面组件
            SetupCuttingInterface();
        }


        /// <summary>
        /// 创建基础切割界面
        /// </summary>
        private GameObject CreateBasicCuttingInterface()
        {
            Debug.Log("创建基础切割界面");

            // 首先尝试集成现有的仓库UI
            var warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null)
            {
                Debug.Log("找到WarehouseUI，尝试集成");
                try
                {
                    return CreateIntegratedWarehouseInterface(warehouseUI);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"集成仓库UI失败: {e.Message}");
                }
            }

            Debug.Log("创建独立的切割界面");
            // 如果没找到仓库UI或集成失败，创建独立界面
            return CreateSimpleCuttingInterface();
        }

        /// <summary>
        /// 设置界面组件
        /// </summary>
        private void SetupCuttingInterface()
        {
            if (currentCuttingInterface != null)
            {
                // 隐藏交互提示
                ShowInteractionPrompt(false);

                // 设置鼠标模式
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Debug.Log("切割界面设置完成");
            }
        }

        /// <summary>
        /// 验证UI创建状态
        /// </summary>
        private void VerifyUICreation()
        {
            if (currentCuttingInterface == null)
            {
                Debug.LogError("❌ 切割界面创建验证失败：界面对象为空");
                return;
            }

            Debug.Log("🔍 开始验证UI创建状态...");

            // 检查界面激活状态
            bool isActive = currentCuttingInterface.activeInHierarchy;
            Debug.Log($"界面激活状态: {isActive}");

            // 检查RectTransform
            RectTransform rectTransform = currentCuttingInterface.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"RectTransform: 锚点=({rectTransform.anchorMin}, {rectTransform.anchorMax}), 尺寸={rectTransform.sizeDelta}");
            }
            else
            {
                Debug.LogWarning("⚠️ 界面缺少RectTransform组件");
            }

            // 检查Canvas组件
            Canvas canvas = currentCuttingInterface.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"Canvas: 渲染模式={canvas.renderMode}, 排序顺序={canvas.sortingOrder}");
            }
            else
            {
                // 检查父级是否有Canvas
                Canvas parentCanvas = currentCuttingInterface.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"父级Canvas: {parentCanvas.name}, 渲染模式={parentCanvas.renderMode}");
                }
                else
                {
                    Debug.LogWarning("⚠️ 界面及其父级都缺少Canvas组件");
                }
            }

            // 检查子组件
            int childCount = currentCuttingInterface.transform.childCount;
            Debug.Log($"子组件数量: {childCount}");

            for (int i = 0; i < childCount && i < 5; i++) // 最多显示前5个
            {
                Transform child = currentCuttingInterface.transform.GetChild(i);
                Debug.Log($"  子组件 {i}: {child.name}, 激活={child.gameObject.activeInHierarchy}");
            }

            Debug.Log("✅ UI创建状态验证完成");
        }

        /// <summary>
        /// 设置玩家控制状态
        /// </summary>
        private void SetPlayerControlEnabled(bool enabled)
        {
            Debug.Log($"设置玩家控制状态: {enabled}");

            // 检测是否为移动端环境
            bool isMobileEnvironment = IsMobileEnvironment();
            Debug.Log($"移动端环境检测: {isMobileEnvironment}");

            // 查找第一人称控制器
            FirstPersonController fpsController = FindFirstObjectByType<FirstPersonController>();
            if (fpsController != null)
            {
                // 使用反射设置enableMouseLook字段
                var enableMouseField = fpsController.GetType().GetField("enableMouseLook",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (enableMouseField != null)
                {
                    enableMouseField.SetValue(fpsController, enabled);
                    Debug.Log($"FirstPersonController鼠标控制: {enabled}");
                }

                // 在移动端，不要完全禁用FirstPersonController，以免影响移动端输入
                if (isMobileEnvironment)
                {
                    Debug.Log("移动端环境，保持FirstPersonController启用状态");
                }
                else
                {
                    // 设置组件启用状态
                    fpsController.enabled = enabled;
                    Debug.Log($"FirstPersonController启用状态: {enabled}");
                }
            }
            else
            {
                Debug.LogWarning("未找到FirstPersonController组件");
            }

            // 设置鼠标状态 - 在移动端不修改鼠标状态
            if (isMobileEnvironment)
            {
                Debug.Log("移动端环境，跳过鼠标状态设置");
            }
            else
            {
                if (enabled)
                {
                    // 恢复游戏控制
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Debug.Log("恢复鼠标锁定状态");
                }
                else
                {
                    // 界面控制模式
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Debug.Log("设置鼠标为UI模式");
                }
            }

            // 不要暂停游戏时间，保持移动端UI正常工作
            // Time.timeScale = enabled ? 1f : 0f;
            Debug.Log("保持游戏时间正常运行，确保移动端UI可用");
        }

        /// <summary>
        /// 检测是否为移动端环境
        /// </summary>
        private bool IsMobileEnvironment()
        {
            Debug.Log("=== 开始移动端环境检测 ===");

            // 1. 直接检查移动端UI特征组件
            bool hasMobileUI = false;

            // 查找虚拟摇杆组件
            var joysticks = FindObjectsOfType<Component>().Where(c =>
                c.GetType().Name.ToLower().Contains("joystick") ||
                c.name.ToLower().Contains("joystick") ||
                c.name.ToLower().Contains("mobile")
            ).ToArray();

            if (joysticks.Length > 0)
            {
                hasMobileUI = true;
                Debug.Log($"检测到 {joysticks.Length} 个移动端UI组件");
                foreach (var joy in joysticks)
                {
                    Debug.Log($"  - {joy.name} ({joy.GetType().Name})");
                }
            }

            // 查找Canvas中是否有移动端特征的UI
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var mobileObjects = canvas.GetComponentsInChildren<Transform>()
                    .Where(t => t.name.ToLower().Contains("mobile") ||
                               t.name.ToLower().Contains("joystick") ||
                               t.name.ToLower().Contains("touch"))
                    .ToArray();

                if (mobileObjects.Length > 0)
                {
                    hasMobileUI = true;
                    Debug.Log($"在Canvas {canvas.name} 中发现移动端UI:");
                    foreach (var obj in mobileObjects)
                    {
                        Debug.Log($"  - {obj.name}");
                    }
                }
            }

            // 2. 如果发现了移动端UI，直接返回true
            if (hasMobileUI)
            {
                Debug.Log("=== 检测到移动端UI，判定为移动端环境 ===");
                return true;
            }

            // 3. 检查运行时平台
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Debug.Log("通过RuntimePlatform检测到移动端");
                return true;
            }

            // 4. 检查是否有触摸屏
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                Debug.Log("检测到触摸屏设备");
                return true;
            }

            // 5. 在编辑器中，如果没有找到移动端UI，默认判定为桌面环境
            Debug.Log("=== 未检测到移动端特征，判定为桌面环境 ===");
            return false;
        }

        /// <summary>
        /// 尝试激活现有的切割系统
        /// </summary>
        private bool TryActivateExistingCuttingSystem()
        {
            Debug.Log("开始激活现有切割系统...");

            // 查找切割系统管理器
            SampleCuttingSystemManager manager = FindObjectOfType<SampleCuttingSystemManager>();
            if (manager == null)
            {
                Debug.Log("未找到SampleCuttingSystemManager，尝试创建...");

                // 创建切割系统管理器
                GameObject managerObj = new GameObject("SampleCuttingSystemManager");
                manager = managerObj.AddComponent<SampleCuttingSystemManager>();
                Debug.Log($"✅ 创建了新的SampleCuttingSystemManager: {manager.name}");
            }
            else
            {
                Debug.Log($"✅ 找到现有的SampleCuttingSystemManager: {manager.name}");
            }

            // 查找切割UI
            CuttingStationUI cuttingUI = FindObjectOfType<CuttingStationUI>();
            if (cuttingUI == null)
            {
                Debug.Log("未找到CuttingStationUI，在管理器上添加组件...");
                cuttingUI = manager.gameObject.AddComponent<CuttingStationUI>();
                Debug.Log("✅ 添加了CuttingStationUI组件");
            }
            else
            {
                Debug.Log($"✅ 找到现有的CuttingStationUI: {cuttingUI.name}");
            }

            // 激活组件
            Debug.Log($"激活前状态 - Manager: {manager.gameObject.activeInHierarchy}, UI: {cuttingUI.gameObject.activeInHierarchy}");

            manager.gameObject.SetActive(true);
            cuttingUI.gameObject.SetActive(true);

            Debug.Log($"激活后状态 - Manager: {manager.gameObject.activeInHierarchy}, UI: {cuttingUI.gameObject.activeInHierarchy}");

            // 设置当前界面引用（用于关闭）
            currentCuttingInterface = manager.gameObject;
            Debug.Log($"设置currentCuttingInterface = {currentCuttingInterface.name}");

            // 隐藏交互提示
            ShowInteractionPrompt(false);

            // 设置鼠标模式
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("✅ 鼠标状态已设置为可见和自由移动");

            Debug.Log("🎉 切割系统激活完成！");
            return true;
        }

        /// <summary>
        /// 关闭切割界面
        /// </summary>
        public void CloseCuttingInterface()
        {
            Debug.Log("=== 开始关闭切割界面 ===");

            // 恢复仓库UI状态
            RestoreWarehouseUI();

            if (currentCuttingInterface != null)
            {
                Debug.Log("关闭切割界面");

                // 如果是集成的仓库界面，只隐藏，不销毁
                var warehouseUI = FindFirstObjectByType<WarehouseUI>();
                if (warehouseUI != null && currentCuttingInterface == warehouseUI.warehousePanel)
                {
                    Debug.Log("隐藏集成的仓库界面");
                    warehouseUI.warehousePanel.SetActive(false);
                }
                else
                {
                    Debug.Log("销毁独立的切割界面");
                    Destroy(currentCuttingInterface);
                }
                currentCuttingInterface = null;
            }

            // 恢复玩家控制
            SetPlayerControlEnabled(true);

            // 如果玩家还在范围内，重新显示提示
            if (playerInRange)
            {
                ShowInteractionPrompt(true);
            }

            Debug.Log("=== 切割界面关闭完成 ===");
        }

        /// <summary>
        /// 创建简单的切割界面（来自v1.11版本）
        /// </summary>
        private GameObject CreateSimpleCuttingInterface()
        {
            Debug.Log($"创建界面，父对象: {interfaceParent?.name ?? "null"}");

            GameObject interfaceObj = new GameObject("CuttingInterface");
            interfaceObj.transform.SetParent(interfaceParent, false);

            // 设置全屏背景
            RectTransform rectTransform = interfaceObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Debug.Log("设置背景");
            // 添加显眼的背景
            Image background = interfaceObj.AddComponent<Image>();
            background.color = new Color(0.0f, 0.5f, 0.8f, 0.95f); // 蓝色半透明背景，更显眼
            background.raycastTarget = true; // 确保能接收射线检测

            // 立即激活界面
            interfaceObj.SetActive(true);
            Debug.Log($"界面已激活: {interfaceObj.activeInHierarchy}");

            // 添加标题
            Debug.Log("创建标题");
            CreateSimpleTitle(interfaceObj);

            // 创建提示信息
            Debug.Log("创建提示信息");
            CreateSimpleInstruction(interfaceObj);

            // 创建关闭按钮
            Debug.Log("创建关闭按钮");
            CreateSimpleCloseButton(interfaceObj);

            // 创建测试图像
            CreateTestVisual(interfaceObj);

            Debug.Log($"简单界面创建完成: {interfaceObj.name}");

            // 再次验证状态
            Debug.Log($"最终状态检查 - 激活: {interfaceObj.activeInHierarchy}, 启用: {interfaceObj.activeSelf}");

            return interfaceObj;
        }

        /// <summary>
        /// 创建简单标题
        /// </summary>
        private void CreateSimpleTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "样本切割系统";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 创建简单说明
        /// </summary>
        private void CreateSimpleInstruction(GameObject parent)
        {
            GameObject instructionObj = new GameObject("Instruction");
            instructionObj.transform.SetParent(parent.transform, false);

            RectTransform instructionRect = instructionObj.AddComponent<RectTransform>();
            instructionRect.anchorMin = new Vector2(0.1f, 0.3f);
            instructionRect.anchorMax = new Vector2(0.9f, 0.7f);
            instructionRect.offsetMin = Vector2.zero;
            instructionRect.offsetMax = Vector2.zero;

            Text instructionText = instructionObj.AddComponent<Text>();
            instructionText.text = "欢迎使用样本切割系统！\n\n此系统可以对多层地质样本进行精确切割，\n将复合样本分解为单独的地层样本。\n\n请将需要切割的样本拖拽到此界面中开始操作。";
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 24;
            instructionText.color = Color.white;
            instructionText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 创建集成仓库系统的界面（来自v1.11版本）
        /// </summary>
        private GameObject CreateIntegratedWarehouseInterface(WarehouseUI warehouseUI)
        {
            Debug.Log("创建集成仓库界面...");

            // 激活现有的仓库UI
            if (warehouseUI.warehousePanel != null)
            {
                Debug.Log("激活仓库面板");
                warehouseUI.warehousePanel.SetActive(true);

                // 确保所有父级对象都激活
                Transform parent = warehouseUI.warehousePanel.transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"激活父级对象: {parent.name}");
                        parent.gameObject.SetActive(true);
                    }
                    parent = parent.parent;
                }

                // 修改仓库UI布局为左侧显示
                Debug.Log("修改仓库UI布局");
                ModifyWarehouseUILayout(warehouseUI);

                // 在右侧添加切割区域
                Debug.Log("添加切割区域到仓库");
                AddCuttingAreaToWarehouse(warehouseUI);

                // 设置UI状态
                Debug.Log("设置仓库UI为切割模式");
                SetWarehouseUIForCutting(warehouseUI);

                // 强制刷新仓库内容显示
                Debug.Log("刷新仓库内容显示");
                RefreshWarehouseContent(warehouseUI);

                // 为仓库样本添加拖拽功能
                Debug.Log("为仓库样本添加拖拽功能");
                WarehouseSampleEnhancer.EnhanceWarehouseSamples(warehouseUI.rightPanel);

                Debug.Log($"集成仓库界面完成，返回面板: {warehouseUI.warehousePanel.name}");
                return warehouseUI.warehousePanel;
            }
            else
            {
                Debug.LogWarning("仓库面板为空，创建基础界面");
                return CreateSimpleCuttingInterface();
            }
        }

        /// <summary>
        /// 修改仓库UI布局
        /// </summary>
        private void ModifyWarehouseUILayout(WarehouseUI warehouseUI)
        {
            if (warehouseUI.warehousePanel != null)
            {
                // 获取仓库面板的RectTransform
                RectTransform warehouseRect = warehouseUI.warehousePanel.GetComponent<RectTransform>();
                if (warehouseRect != null)
                {
                    // 设置为全屏
                    warehouseRect.anchorMin = Vector2.zero;
                    warehouseRect.anchorMax = Vector2.one;
                    warehouseRect.offsetMin = Vector2.zero;
                    warehouseRect.offsetMax = Vector2.zero;
                }

                // 调整面板布局：右侧面板（仓库）显示在左半边，左侧面板（背包）隐藏
                if (warehouseUI.rightPanel != null)
                {
                    Debug.Log("调整仓库面板到左侧");
                    RectTransform rightRect = warehouseUI.rightPanel.GetComponent<RectTransform>();
                    if (rightRect != null)
                    {
                        // 将仓库面板调整到左半边显示
                        rightRect.anchorMin = new Vector2(0f, 0f);
                        rightRect.anchorMax = new Vector2(0.5f, 1f);
                        rightRect.offsetMin = new Vector2(10f, 10f);
                        rightRect.offsetMax = new Vector2(-5f, -10f);
                    }
                }

                if (warehouseUI.leftPanel != null)
                {
                    Debug.Log("隐藏背包面板");
                    // 隐藏背包面板，因为我们要显示仓库
                    warehouseUI.leftPanel.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 在仓库UI上添加切割区域
        /// </summary>
        private void AddCuttingAreaToWarehouse(WarehouseUI warehouseUI)
        {
            // 创建右侧切割面板
            GameObject cuttingArea = new GameObject("CuttingArea");
            cuttingArea.transform.SetParent(warehouseUI.warehousePanel.transform, false);

            RectTransform cuttingRect = cuttingArea.AddComponent<RectTransform>();
            cuttingRect.anchorMin = new Vector2(0.5f, 0f);
            cuttingRect.anchorMax = new Vector2(1f, 1f);
            cuttingRect.offsetMin = new Vector2(5f, 10f);
            cuttingRect.offsetMax = new Vector2(-10f, -10f);

            // 添加背景
            Image cuttingBg = cuttingArea.AddComponent<Image>();
            cuttingBg.color = new Color(0.2f, 0.3f, 0.4f, 0.9f); // 深蓝灰色背景

            // 添加标题
            CreateCuttingAreaTitle(cuttingArea);

            // 添加拖拽区域
            CreateCuttingDropZone(cuttingArea);

            // 添加关闭按钮
            CreateCuttingCloseButton(cuttingArea);
        }

        /// <summary>
        /// 创建切割区域标题
        /// </summary>
        private void CreateCuttingAreaTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("CuttingTitle");
            titleObj.transform.SetParent(parent.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.9f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(10f, 0f);
            titleRect.offsetMax = new Vector2(-10f, 0f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "样本切割台";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 创建切割拖拽区域
        /// </summary>
        private void CreateCuttingDropZone(GameObject parent)
        {
            GameObject dropZone = new GameObject("CuttingDropZone");
            dropZone.transform.SetParent(parent.transform, false);

            RectTransform dropRect = dropZone.AddComponent<RectTransform>();
            dropRect.anchorMin = new Vector2(0.1f, 0.2f);
            dropRect.anchorMax = new Vector2(0.9f, 0.8f);
            dropRect.offsetMin = Vector2.zero;
            dropRect.offsetMax = Vector2.zero;

            // 添加拖拽区域背景
            Image dropBg = dropZone.AddComponent<Image>();
            dropBg.color = new Color(0.1f, 0.2f, 0.3f, 0.8f); // 深色背景

            // 添加边框效果
            Outline outline = dropZone.AddComponent<Outline>();
            outline.effectColor = Color.cyan;
            outline.effectDistance = new Vector2(2f, 2f);

            // ✅ 关键修复：添加SampleDropZone组件
            SampleDropZone dropZoneComponent = dropZone.AddComponent<SampleDropZone>();
            Debug.Log("✅ 添加了SampleDropZone组件");

            // ✅ 关键修复：添加SampleCuttingGame组件
            SampleCuttingGame cuttingGame = dropZone.AddComponent<SampleCuttingGame>();
            Debug.Log("✅ 添加了SampleCuttingGame组件");

            // 创建3D预览区域
            Create3DPreviewArea(dropZone);

            // 添加提示文字（overlay在3D预览区域上方）
            GameObject hintText = new GameObject("DropHint");
            hintText.transform.SetParent(dropZone.transform, false);

            RectTransform hintRect = hintText.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0.9f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.offsetMin = new Vector2(10f, 0f);
            hintRect.offsetMax = new Vector2(-10f, -5f);

            // 添加半透明背景
            Image hintBg = hintText.AddComponent<Image>();
            hintBg.color = new Color(0f, 0f, 0f, 0.7f); // 半透明黑色背景

            // 创建独立的文字对象
            GameObject textObj = new GameObject("HintText");
            textObj.transform.SetParent(hintText.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text hint = textObj.AddComponent<Text>();
            hint.text = "拖拽样本到此处开始切割 • 支持多层地质样本";
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 14;
            hint.color = Color.cyan;
            hint.alignment = TextAnchor.MiddleCenter;

            Debug.Log("🎯 完整的切割投放区域创建完成，包含所有必要组件");
        }

        /// <summary>
        /// 创建3D预览区域
        /// </summary>
        private void Create3DPreviewArea(GameObject dropZone)
        {
            Debug.Log("创建3D样本预览区域");

            // 创建3D预览区域容器
            GameObject previewArea = new GameObject("SamplePreviewArea");
            previewArea.transform.SetParent(dropZone.transform, false);

            RectTransform previewRect = previewArea.AddComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;

            // 添加RawImage组件用于显示3D渲染内容
            RawImage rawImage = previewArea.AddComponent<RawImage>();
            rawImage.color = Color.white;

            // 添加边框效果
            Outline previewOutline = previewArea.AddComponent<Outline>();
            previewOutline.effectColor = Color.yellow;
            previewOutline.effectDistance = new Vector2(1f, 1f);

            // 暂时禁用GameObject，防止Awake执行
            bool wasActive = dropZone.activeInHierarchy;
            dropZone.SetActive(false);

            // 添加Sample3DModelViewer组件
            Sample3DModelViewer viewer = dropZone.AddComponent<Sample3DModelViewer>();

            // 设置RawImage引用
            var viewerType = viewer.GetType();
            var rawImageField = viewerType.GetField("rawImage",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (rawImageField != null)
            {
                rawImageField.SetValue(viewer, rawImage);
                Debug.Log("✅ 设置了Sample3DModelViewer的rawImage引用");
            }

            // 重新激活GameObject，让Awake正常执行
            dropZone.SetActive(wasActive);

            // 设置渲染参数
            var textureWidthField = viewerType.GetField("textureWidth",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (textureWidthField != null)
            {
                textureWidthField.SetValue(viewer, 512);
            }

            var textureHeightField = viewerType.GetField("textureHeight",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (textureHeightField != null)
            {
                textureHeightField.SetValue(viewer, 512);
            }

            Debug.Log("✅ 3D样本预览区域创建完成，包含RawImage和Sample3DModelViewer组件");
        }

        /// <summary>
        /// 创建切割关闭按钮
        /// </summary>
        private void CreateCuttingCloseButton(GameObject parent)
        {
            GameObject closeBtn = new GameObject("CuttingCloseButton");
            closeBtn.transform.SetParent(parent.transform, false);

            RectTransform btnRect = closeBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.8f, 0.05f);
            btnRect.anchorMax = new Vector2(0.95f, 0.15f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image btnBg = closeBtn.AddComponent<Image>();
            btnBg.color = Color.red;

            Button button = closeBtn.AddComponent<Button>();
            button.onClick.AddListener(() => {
                Debug.Log("点击切割界面关闭按钮");
                CloseCuttingInterface();
            });

            // 添加按钮文字
            GameObject btnText = new GameObject("CloseText");
            btnText.transform.SetParent(closeBtn.transform, false);

            RectTransform textRect = btnText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = btnText.AddComponent<Text>();
            text.text = "关闭";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 创建简单关闭按钮
        /// </summary>
        private void CreateSimpleCloseButton(GameObject parent)
        {
            GameObject closeButtonObj = new GameObject("CloseButton");
            closeButtonObj.transform.SetParent(parent.transform, false);

            RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.9f, 0.9f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-50, -50);
            closeRect.sizeDelta = new Vector2(80, 80);

            Image closeImage = closeButtonObj.AddComponent<Image>();
            closeImage.color = Color.red;

            Button closeButton = closeButtonObj.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => {
                Debug.Log("点击关闭按钮");
                CloseCuttingInterface();
            });

            // 添加X文字
            GameObject xTextObj = new GameObject("XText");
            xTextObj.transform.SetParent(closeButtonObj.transform, false);

            RectTransform xRect = xTextObj.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            Text xText = xTextObj.AddComponent<Text>();
            xText.text = "✕";
            xText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xText.fontSize = 36;
            xText.color = Color.white;
            xText.alignment = TextAnchor.MiddleCenter;
            xText.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 设置仓库UI为切割模式
        /// </summary>
        private void SetWarehouseUIForCutting(WarehouseUI warehouseUI)
        {
            // 禁用多选等不需要的功能
            if (warehouseUI.multiSelectButton != null)
                warehouseUI.multiSelectButton.gameObject.SetActive(false);
            if (warehouseUI.batchTransferButton != null)
                warehouseUI.batchTransferButton.gameObject.SetActive(false);
            if (warehouseUI.batchDiscardButton != null)
                warehouseUI.batchDiscardButton.gameObject.SetActive(false);

            // 禁用原有的关闭按钮
            if (warehouseUI.closeButton != null)
                warehouseUI.closeButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// 刷新仓库内容显示
        /// </summary>
        private void RefreshWarehouseContent(WarehouseUI warehouseUI)
        {
            if (warehouseUI == null)
            {
                Debug.LogError("WarehouseUI为空，无法刷新内容");
                return;
            }

            try
            {
                Debug.Log("开始刷新仓库内容显示");

                // 方法1: 调用InitializeWarehouseUI重新初始化
                var initMethod = warehouseUI.GetType().GetMethod("InitializeWarehouseUI",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (initMethod != null)
                {
                    Debug.Log("调用InitializeWarehouseUI重新初始化");
                    initMethod.Invoke(warehouseUI, null);
                }

                // 方法2: 调用SetupUIComponents重新设置组件
                var setupMethod = warehouseUI.GetType().GetMethod("SetupUIComponents",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (setupMethod != null)
                {
                    Debug.Log("调用SetupUIComponents重新设置组件");
                    setupMethod.Invoke(warehouseUI, null);
                }

                // 方法3: 尝试重新打开仓库界面来刷新内容
                Debug.Log("使用重新打开方式刷新仓库界面");
                warehouseUI.CloseWarehouseInterface();

                // 等待一帧再重新打开
                StartCoroutine(ReopenWarehouseAfterDelay(warehouseUI));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"刷新仓库内容失败: {e.Message}");
            }
        }

        /// <summary>
        /// 延迟重新打开仓库界面
        /// </summary>
        private System.Collections.IEnumerator ReopenWarehouseAfterDelay(WarehouseUI warehouseUI)
        {
            yield return new WaitForEndOfFrame();

            if (warehouseUI != null)
            {
                Debug.Log("延迟重新打开仓库界面");
                warehouseUI.OpenWarehouseInterface();

                // 重新应用切割模式的布局调整
                yield return new WaitForEndOfFrame();
                ModifyWarehouseUILayout(warehouseUI);

                // 重新为样本添加拖拽功能
                yield return new WaitForEndOfFrame();
                WarehouseSampleEnhancer.EnhanceWarehouseSamples(warehouseUI.rightPanel);
            }
        }

        /// <summary>
        /// 创建测试可视化元素
        /// </summary>
        private void CreateTestVisual(GameObject parent)
        {
            // 创建一个明显的测试矩形
            GameObject testVisual = new GameObject("TestVisual");
            testVisual.transform.SetParent(parent.transform, false);

            RectTransform testRect = testVisual.AddComponent<RectTransform>();
            testRect.anchorMin = new Vector2(0.3f, 0.3f);
            testRect.anchorMax = new Vector2(0.7f, 0.7f);
            testRect.offsetMin = Vector2.zero;
            testRect.offsetMax = Vector2.zero;

            Image testImage = testVisual.AddComponent<Image>();
            testImage.color = Color.red; // 红色，非常明显
            testImage.raycastTarget = false;

            testVisual.SetActive(true);
            Debug.Log("创建红色测试矩形");
        }

        /// <summary>
        /// 恢复仓库UI状态
        /// </summary>
        private void RestoreWarehouseUI()
        {
            var warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null)
            {
                Debug.Log("恢复仓库UI状态");

                // 恢复原有的按钮状态
                if (warehouseUI.multiSelectButton != null)
                    warehouseUI.multiSelectButton.gameObject.SetActive(true);
                if (warehouseUI.batchTransferButton != null)
                    warehouseUI.batchTransferButton.gameObject.SetActive(true);
                if (warehouseUI.batchDiscardButton != null)
                    warehouseUI.batchDiscardButton.gameObject.SetActive(true);
                if (warehouseUI.closeButton != null)
                    warehouseUI.closeButton.gameObject.SetActive(true);

                // 恢复仓库面板（右侧面板）到原始位置
                if (warehouseUI.rightPanel != null)
                {
                    RectTransform rightRect = warehouseUI.rightPanel.GetComponent<RectTransform>();
                    if (rightRect != null)
                    {
                        // 恢复仓库面板到右侧
                        rightRect.anchorMin = new Vector2(0.37f, 0.15f);
                        rightRect.anchorMax = new Vector2(0.98f, 0.95f);
                        rightRect.offsetMin = Vector2.zero;
                        rightRect.offsetMax = Vector2.zero;
                        Debug.Log("恢复仓库面板到右侧位置");
                    }
                }

                // 恢复背包面板（左侧面板）
                if (warehouseUI.leftPanel != null)
                {
                    warehouseUI.leftPanel.gameObject.SetActive(true);
                    RectTransform leftRect = warehouseUI.leftPanel.GetComponent<RectTransform>();
                    if (leftRect != null)
                    {
                        // 恢复背包面板到左侧原始位置
                        leftRect.anchorMin = new Vector2(0.02f, 0.15f);
                        leftRect.anchorMax = new Vector2(0.35f, 0.95f);
                        leftRect.offsetMin = Vector2.zero;
                        leftRect.offsetMax = Vector2.zero;
                        Debug.Log("恢复背包面板到左侧位置");
                    }
                }

                // 移除添加的切割区域
                Transform cuttingArea = warehouseUI.warehousePanel.transform.Find("CuttingArea");
                if (cuttingArea != null)
                {
                    Destroy(cuttingArea.gameObject);
                    Debug.Log("移除切割区域组件");
                }
            }
        }

        /// <summary>
        /// 在编辑器中绘制交互范围
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}