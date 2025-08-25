using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        private SampleCuttingSystemManager cuttingSystemManager;
        
        void Start()
        {
            SetupInteractionPrompt();
            SetupComponents();
        }
        
        void Update()
        {
            CheckPlayerInteraction();
            HandleInput();
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
            else
            {
                Debug.Log($"界面父对象已存在: {interfaceParent.name}");
            }
        }
        
        /// <summary>
        /// 设置交互提示UI
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
            // 创建屏幕空间Canvas
            GameObject canvasObj = new GameObject("CuttingStationPromptCanvas");
            promptCanvas = canvasObj.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 创建交互提示面板
            GameObject promptObj = new GameObject("InteractionPrompt");
            promptObj.transform.SetParent(canvasObj.transform);
            
            RectTransform rectTransform = promptObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(150, 40); // 宽高都变成原来的一半
            
            // 添加背景
            Image background = promptObj.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.8f);
            
            // 创建文本
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(promptObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            promptText = textObj.AddComponent<Text>();
            promptText.text = "[F] 使用样本切割台";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 14; // 调整字体大小适应更小的UI框
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;
            
            // 添加本地化组件
            if (FindFirstObjectByType<LocalizationManager>() != null)
            {
                LocalizedText localizedText = textObj.AddComponent<LocalizedText>();
                localizedText.SetTextKey("cutting_station.interaction.prompt");
            }
            
            interactionPrompt = promptObj;
        }
        
        /// <summary>
        /// 检查玩家交互
        /// </summary>
        private void CheckPlayerInteraction()
        {
            bool foundPlayer = false;
            
            // 多种方式查找玩家
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, playerLayer);
            foreach (var collider in colliders)
            {
                if (IsPlayerObject(collider.gameObject))
                {
                    nearbyPlayer = collider.gameObject;
                    foundPlayer = true;
                    break;
                }
            }
            
            // 备用查找方式
            if (!foundPlayer)
            {
                FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
                if (player != null)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance <= interactionRange)
                    {
                        nearbyPlayer = player.gameObject;
                        foundPlayer = true;
                    }
                }
            }
            
            if (foundPlayer && !playerInRange)
            {
                OnPlayerEnter();
            }
            else if (!foundPlayer && playerInRange)
            {
                OnPlayerExit();
            }
        }
        
        /// <summary>
        /// 判断是否为玩家对象
        /// </summary>
        private bool IsPlayerObject(GameObject obj)
        {
            if (obj.CompareTag("Player")) return true;
            
            string objName = obj.name.ToLower();
            if (objName.Contains("lily") || objName.Contains("player") || objName.Contains("firstperson"))
                return true;
                
            if (obj.GetComponent<FirstPersonController>() != null) return true;
            
            // 检查父对象
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<FirstPersonController>() != null) return true;
                parent = parent.parent;
            }
            
            return false;
        }
        
        /// <summary>
        /// 玩家进入交互范围
        /// </summary>
        private void OnPlayerEnter()
        {
            playerInRange = true;
            ShowInteractionPrompt();
            Debug.Log("玩家进入切割台交互范围");
        }
        
        /// <summary>
        /// 玩家离开交互范围
        /// </summary>
        private void OnPlayerExit()
        {
            playerInRange = false;
            HideInteractionPrompt();
            Debug.Log("玩家离开切割台交互范围");
        }
        
        /// <summary>
        /// 显示交互提示
        /// </summary>
        private void ShowInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
        
        /// <summary>
        /// 隐藏交互提示
        /// </summary>
        private void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
        
        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenCuttingInterface();
            }
            
            // 添加ESC键快速关闭功能
            if (currentCuttingInterface != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("ESC键按下，关闭切割界面");
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
        /// 关闭切割界面
        /// </summary>
        public void CloseCuttingInterface()
        {
            // 恢复仓库UI状态
            RestoreWarehouseUI();
            
            if (currentCuttingInterface != null)
            {
                // 如果是集成的仓库界面，只隐藏，不销毁
                var warehouseUI = FindFirstObjectByType<WarehouseUI>();
                if (warehouseUI != null && currentCuttingInterface == warehouseUI.warehousePanel)
                {
                    warehouseUI.warehousePanel.SetActive(false);
                }
                else
                {
                    Destroy(currentCuttingInterface);
                }
                currentCuttingInterface = null;
            }
            
            // 恢复玩家控制
            SetPlayerControlEnabled(true);
            
            Debug.Log("关闭切割界面");
        }
        
        /// <summary>
        /// 恢复仓库UI状态
        /// </summary>
        private void RestoreWarehouseUI()
        {
            var warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null)
            {
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
                    }
                }
                
                // 移除添加的切割区域
                Transform cuttingArea = warehouseUI.warehousePanel.transform.Find("CuttingArea");
                if (cuttingArea != null)
                {
                    Destroy(cuttingArea.gameObject);
                }
            }
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
        /// 创建简单的切割界面
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
        /// 创建简单标题
        /// </summary>
        private void CreateSimpleTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // 添加背景使标题更明显
            Image titleBg = titleObj.AddComponent<Image>();
            titleBg.color = new Color(0f, 0f, 0f, 0.8f); // 黑色半透明背景
            
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "样本切割系统测试界面";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48; // 更大的字体
            titleText.color = Color.yellow; // 黄色，更显眼
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.raycastTarget = false;
            
            titleObj.SetActive(true);
            Debug.Log($"标题创建完成，文本: {titleText.text}");
        }
        
        /// <summary>
        /// 创建简单说明
        /// </summary>
        private void CreateSimpleInstruction(GameObject parent)
        {
            GameObject instructionObj = new GameObject("Instruction");
            instructionObj.transform.SetParent(parent.transform, false);
            
            RectTransform instructionRect = instructionObj.AddComponent<RectTransform>();
            instructionRect.anchorMin = new Vector2(0.1f, 0.2f);
            instructionRect.anchorMax = new Vector2(0.9f, 0.7f);
            instructionRect.offsetMin = Vector2.zero;
            instructionRect.offsetMax = Vector2.zero;
            
            // 添加背景使文本更清晰
            Image instructionBg = instructionObj.AddComponent<Image>();
            instructionBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // 深灰色背景
            
            Text instructionText = instructionObj.AddComponent<Text>();
            instructionText.text = "✅ 切割系统界面测试成功！\n\n如果您能看到这个蓝色界面和红色矩形，\n说明UI创建和显示系统正常工作。\n\n🔧 这是一个增强的测试界面，\n用于验证所有UI组件的功能。\n\n⬇️ 点击下方红色按钮关闭界面";
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 28; // 更大字体
            instructionText.color = Color.white;
            instructionText.alignment = TextAnchor.MiddleCenter;
            instructionText.raycastTarget = false;
            
            instructionObj.SetActive(true);
            Debug.Log($"增强说明创建完成，内容长度: {instructionText.text.Length}");
        }
        
        /// <summary>
        /// 创建简单关闭按钮
        /// </summary>
        private void CreateSimpleCloseButton(GameObject parent)
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(parent.transform, false);
            
            RectTransform btnRect = closeBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.35f, 0.05f);  // 稍微更大的按钮
            btnRect.anchorMax = new Vector2(0.65f, 0.18f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            Image btnBg = closeBtn.AddComponent<Image>();
            btnBg.color = new Color(1f, 0.2f, 0.2f, 1f);  // 更鲜艳的红色，完全不透明
            
            Button button = closeBtn.AddComponent<Button>();
            button.onClick.AddListener(CloseCuttingInterface);
            
            // 添加按钮文字
            GameObject btnText = new GameObject("Text");
            btnText.transform.SetParent(closeBtn.transform, false);
            
            RectTransform textRect = btnText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = btnText.AddComponent<Text>();
            text.text = "🚪 关闭测试界面";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;  // 文本不拦截点击
            
            // 确保按钮和文本都激活
            btnText.SetActive(true);
            closeBtn.SetActive(true);
            
            Debug.Log("增强关闭按钮创建完成");
        }
        
        /// <summary>
        /// 创建集成仓库系统的界面
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
                return CreateBasicCuttingInterface();
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
            cuttingBg.color = new Color(0f, 0f, 0f, 0.8f); // 黑色背景
            
            // 添加切割游戏组件（作为主控制器）
            SampleCuttingGame cuttingGame = cuttingArea.AddComponent<SampleCuttingGame>();
            Debug.Log("添加SampleCuttingGame组件到切割区域");
            
            // 添加投放区域组件
            SampleDropZone dropZone = cuttingArea.AddComponent<SampleDropZone>();
            Debug.Log("添加SampleDropZone组件到切割区域");
            
            // 添加标题
            CreateCuttingAreaTitle(cuttingArea);
            
            // 添加3D模型显示区域
            Create3DModelViewArea(cuttingArea);
            
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
            dropBg.color = new Color(0f, 0f, 0f, 0.4f); // 半透明黑色
            
            // 添加虚线边框效果
            Outline outline = dropZone.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, 2f);
            
            // 添加提示文字
            GameObject hintText = new GameObject("DropHint");
            hintText.transform.SetParent(dropZone.transform, false);
            
            RectTransform hintRect = hintText.AddComponent<RectTransform>();
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(20f, 20f);
            hintRect.offsetMax = new Vector2(-20f, -20f);
            
            Text hint = hintText.AddComponent<Text>();
            hint.text = "将多层地质样本\n从左侧拖拽到此处\n\n开始样本切割操作\n\n支持的样本类型：\n• 多层钻探样本\n• 地质钻芯样本";
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 18;
            hint.color = Color.yellow;
            hint.alignment = TextAnchor.MiddleCenter;
            
            // 添加拖拽检测组件
            AddDropHandler(dropZone);
        }
        
        /// <summary>
        /// 添加拖拽检测处理
        /// </summary>
        private void AddDropHandler(GameObject dropZone)
        {
            // 添加仓库集成组件
            var warehouseIntegration = GetComponent<WarehouseIntegration>();
            if (warehouseIntegration == null)
            {
                warehouseIntegration = gameObject.AddComponent<WarehouseIntegration>();
            }
            
            // 设置拖拽区域
            var dropZoneRect = dropZone.GetComponent<RectTransform>();
            if (dropZoneRect != null && warehouseIntegration != null)
            {
                // 使用反射设置dropZone字段
                var dropZoneField = typeof(WarehouseIntegration).GetField("dropZone", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dropZoneField != null)
                {
                    dropZoneField.SetValue(warehouseIntegration, dropZoneRect);
                }
            }
        }
        
        /// <summary>
        /// 创建切割区域关闭按钮
        /// </summary>
        private void CreateCuttingCloseButton(GameObject parent)
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(parent.transform, false);
            
            RectTransform btnRect = closeBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.85f, 0.85f);
            btnRect.anchorMax = new Vector2(0.98f, 0.98f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            Image btnBg = closeBtn.AddComponent<Image>();
            btnBg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            
            Button button = closeBtn.AddComponent<Button>();
            button.onClick.AddListener(CloseCuttingInterface);
            
            // 添加按钮文字
            GameObject btnText = new GameObject("Text");
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
        /// 创建主切割面板
        /// </summary>
        private void CreateMainCuttingPanel(GameObject parent)
        {
            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(parent.transform, false);
            
            RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.1f, 0.1f);
            mainRect.anchorMax = new Vector2(0.9f, 0.9f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;
            
            Image panelBg = mainPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // 创建左侧仓库区域
            CreateWarehousePanel(mainPanel);
            
            // 创建右侧切割区域
            CreateCuttingPanel(mainPanel);
            
            // 创建顶部标题
            CreateTitleBar(mainPanel);
            
            // 创建关闭按钮
            CreateCloseButton(mainPanel);
        }
        
        /// <summary>
        /// 创建仓库面板
        /// </summary>
        private void CreateWarehousePanel(GameObject parent)
        {
            GameObject warehousePanel = new GameObject("WarehousePanel");
            warehousePanel.transform.SetParent(parent.transform, false);
            
            RectTransform warehouseRect = warehousePanel.AddComponent<RectTransform>();
            warehouseRect.anchorMin = new Vector2(0f, 0f);
            warehouseRect.anchorMax = new Vector2(0.45f, 0.85f);
            warehouseRect.offsetMin = new Vector2(20f, 20f);
            warehouseRect.offsetMax = new Vector2(-10f, -10f);
            
            Image warehouseBg = warehousePanel.AddComponent<Image>();
            warehouseBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // 标题
            GameObject warehouseTitle = new GameObject("Title");
            warehouseTitle.transform.SetParent(warehousePanel.transform, false);
            
            RectTransform titleRect = warehouseTitle.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.9f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            Text titleText = warehouseTitle.AddComponent<Text>();
            titleText.text = "样本仓库";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            // 这里需要集成实际的仓库UI
            // TODO: 集成WarehouseManager的UI组件
        }
        
        /// <summary>
        /// 创建切割面板
        /// </summary>
        private void CreateCuttingPanel(GameObject parent)
        {
            GameObject cuttingPanel = new GameObject("CuttingPanel");
            cuttingPanel.transform.SetParent(parent.transform, false);
            
            RectTransform cuttingRect = cuttingPanel.AddComponent<RectTransform>();
            cuttingRect.anchorMin = new Vector2(0.55f, 0f);
            cuttingRect.anchorMax = new Vector2(1f, 0.85f);
            cuttingRect.offsetMin = new Vector2(10f, 20f);
            cuttingRect.offsetMax = new Vector2(-20f, -10f);
            
            Image cuttingBg = cuttingPanel.AddComponent<Image>();
            cuttingBg.color = new Color(0.0f, 0.3f, 0.0f, 0.6f);
            
            // 标题
            GameObject cuttingTitle = new GameObject("Title");
            cuttingTitle.transform.SetParent(cuttingPanel.transform, false);
            
            RectTransform titleRect = cuttingTitle.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.9f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            Text titleText = cuttingTitle.AddComponent<Text>();
            titleText.text = "样本切割区";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            // 拖拽提示
            GameObject dropHint = new GameObject("DropHint");
            dropHint.transform.SetParent(cuttingPanel.transform, false);
            
            RectTransform hintRect = dropHint.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.1f, 0.3f);
            hintRect.anchorMax = new Vector2(0.9f, 0.7f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            
            Text hintText = dropHint.AddComponent<Text>();
            hintText.text = "将多层地质样本\n从左侧拖拽到此处\n开始切割操作";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.fontSize = 16;
            hintText.color = Color.yellow;
            hintText.alignment = TextAnchor.MiddleCenter;
        }
        
        /// <summary>
        /// 创建标题栏
        /// </summary>
        private void CreateTitleBar(GameObject parent)
        {
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(parent.transform, false);
            
            RectTransform titleRect = titleBar.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0f, 0.5f, 0.8f, 0.8f);
            
            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(titleBar.transform, false);
            
            RectTransform textRect = titleText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = titleText.AddComponent<Text>();
            text.text = "样本切割台 - 选择要切割的地质样本";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }
        
        /// <summary>
        /// 创建关闭按钮
        /// </summary>
        private void CreateCloseButton(GameObject parent)
        {
            GameObject closeButton = new GameObject("CloseButton");
            closeButton.transform.SetParent(parent.transform, false);
            
            RectTransform buttonRect = closeButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.9f, 0.95f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.offsetMin = new Vector2(-50f, -15f);
            buttonRect.offsetMax = new Vector2(-10f, -5f);
            
            Image buttonBg = closeButton.AddComponent<Image>();
            buttonBg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            
            Button button = closeButton.AddComponent<Button>();
            button.onClick.AddListener(CloseCuttingInterface);
            
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(closeButton.transform, false);
            
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = buttonText.AddComponent<Text>();
            text.text = "×";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }
        
        /// <summary>
        /// 设置切割界面
        /// </summary>
        private void SetupCuttingInterface()
        {
            // 这里可以添加界面设置逻辑
            // 例如绑定事件、设置数据等
            
            if (currentCuttingInterface != null)
            {
                // 获取WarehouseIntegration组件并设置
                var warehouseIntegration = GetComponent<WarehouseIntegration>();
                if (warehouseIntegration != null)
                {
                    // 连接仓库系统和切割界面
                    // TODO: 实现具体的连接逻辑
                }
            }
        }
        
        /// <summary>
        /// 查找UI Canvas
        /// </summary>
        private Canvas FindUICanvas()
        {
            Debug.Log("开始查找UI Canvas...");
            
            // 查找主UI Canvas
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"找到 {canvases.Length} 个Canvas");
            
            Canvas bestCanvas = null;
            int highestSortingOrder = -1;
            
            foreach (var canvas in canvases)
            {
                Debug.Log($"Canvas: {canvas.name}, RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}, Active: {canvas.gameObject.activeInHierarchy}");
                
                // 优先选择ScreenSpaceOverlay模式且活跃的Canvas
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && 
                    canvas.gameObject.activeInHierarchy)
                {
                    if (canvas.sortingOrder > highestSortingOrder)
                    {
                        bestCanvas = canvas;
                        highestSortingOrder = canvas.sortingOrder;
                    }
                }
            }
            
            if (bestCanvas != null)
            {
                Debug.Log($"选择最佳Canvas: {bestCanvas.name}, SortingOrder: {bestCanvas.sortingOrder}");
                
                // 验证Canvas设置
                VerifyCanvasSettings(bestCanvas);
                return bestCanvas;
            }
            
            // 如果没找到合适的，创建一个新的
            Debug.Log("创建新的UI Canvas");
            return CreateNewUICanvas();
        }
        
        /// <summary>
        /// 创建新的UI Canvas
        /// </summary>
        private Canvas CreateNewUICanvas()
        {
            GameObject canvasObj = new GameObject("CuttingStationMainUICanvas");
            
            // 确保Canvas在场景根部，不被任何其他对象遮挡
            canvasObj.transform.SetParent(null);
            
            // 尝试添加标记防止被清理系统删除
            try
            {
                canvasObj.tag = "UICanvas"; // 使用特殊标签
            }
            catch (System.Exception)
            {
                // 标签不存在时使用默认标签
                canvasObj.tag = "Untagged";
                Debug.Log("UICanvas标签不存在，使用默认标签");
            }
            
            Canvas newCanvas = canvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 9999; // 设置极高优先级
            newCanvas.pixelPerfect = false;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            
            // 添加一个标识组件防止被清理
            var protector = canvasObj.AddComponent<CanvasProtector>();
            
            // 确保Canvas立即激活
            canvasObj.SetActive(true);
            
            // 标记为不销毁
            DontDestroyOnLoad(canvasObj);
            
            Debug.Log($"创建受保护的Canvas: {newCanvas.name}, SortingOrder: {newCanvas.sortingOrder}");
            
            // 验证新创建的Canvas
            VerifyCanvasSettings(newCanvas);
            
            return newCanvas;
        }
        
        /// <summary>
        /// 验证Canvas设置
        /// </summary>
        private void VerifyCanvasSettings(Canvas canvas)
        {
            Debug.Log("=== Canvas设置验证 ===");
            Debug.Log($"Canvas名称: {canvas.name}");
            Debug.Log($"活跃状态: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"渲染模式: {canvas.renderMode}");
            Debug.Log($"排序顺序: {canvas.sortingOrder}");
            Debug.Log($"像素完美: {canvas.pixelPerfect}");
            Debug.Log($"世界坐标: {canvas.transform.position}");
            Debug.Log($"缩放: {canvas.transform.localScale}");
            
            // 检查组件
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"CanvasScaler模式: {scaler.uiScaleMode}");
                Debug.Log($"参考分辨率: {scaler.referenceResolution}");
            }
            else
            {
                Debug.LogWarning("Canvas缺少CanvasScaler组件");
            }
            
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning("Canvas缺少GraphicRaycaster组件");
            }
            
            Debug.Log("=== 验证完成 ===");
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
        /// 验证UI创建状态
        /// </summary>
        private void VerifyUICreation()
        {
            if (currentCuttingInterface == null)
            {
                Debug.LogError("当前切割界面为空！");
                return;
            }
            
            Debug.Log("=== UI创建状态验证 ===");
            Debug.Log($"界面名称: {currentCuttingInterface.name}");
            Debug.Log($"界面位置: {currentCuttingInterface.transform.position}");
            Debug.Log($"界面缩放: {currentCuttingInterface.transform.localScale}");
            Debug.Log($"界面激活: {currentCuttingInterface.activeInHierarchy}");
            Debug.Log($"界面启用: {currentCuttingInterface.activeSelf}");
            
            // 检查父对象
            if (currentCuttingInterface.transform.parent != null)
            {
                Debug.Log($"父对象: {currentCuttingInterface.transform.parent.name}");
                Debug.Log($"父对象激活: {currentCuttingInterface.transform.parent.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.Log("界面在根级别（无父对象）");
            }
            
            // 检查RectTransform
            var rectTransform = currentCuttingInterface.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"RectTransform存在");
                Debug.Log($"  AnchorMin: {rectTransform.anchorMin}");
                Debug.Log($"  AnchorMax: {rectTransform.anchorMax}");
                Debug.Log($"  OffsetMin: {rectTransform.offsetMin}");
                Debug.Log($"  OffsetMax: {rectTransform.offsetMax}");
                Debug.Log($"  SizeDelta: {rectTransform.sizeDelta}");
                Debug.Log($"  AnchoredPosition: {rectTransform.anchoredPosition}");
                
                // 计算实际屏幕尺寸
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                Debug.Log($"屏幕尺寸: {screenSize}");
                
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                Debug.Log($"世界角点: [{corners[0]}, {corners[1]}, {corners[2]}, {corners[3]}]");
            }
            else
            {
                Debug.LogWarning("界面缺少RectTransform组件");
            }
            
            // 检查Image组件
            var image = currentCuttingInterface.GetComponent<Image>();
            if (image != null)
            {
                Debug.Log($"Image组件存在，颜色: {image.color}");
                Debug.Log($"Image启用: {image.enabled}");
                Debug.Log($"材质: {image.material?.name ?? "null"}");
            }
            else
            {
                Debug.LogWarning("界面缺少Image组件");
            }
            
            // 检查子对象
            int childCount = currentCuttingInterface.transform.childCount;
            Debug.Log($"子对象数量: {childCount}");
            for (int i = 0; i < childCount && i < 5; i++) // 最多显示5个
            {
                Transform child = currentCuttingInterface.transform.GetChild(i);
                Debug.Log($"  子对象{i}: {child.name}, 激活: {child.gameObject.activeInHierarchy}");
            }
            
            Debug.Log("=== UI验证完成 ===");
            
            // 强制激活界面及其所有父级
            if (currentCuttingInterface != null)
            {
                Debug.Log("强制激活切割界面层级");
                
                // 激活界面本身
                currentCuttingInterface.SetActive(true);
                
                // 向上激活所有父级
                Transform parent = currentCuttingInterface.transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"强制激活父级: {parent.name}");
                        parent.gameObject.SetActive(true);
                    }
                    parent = parent.parent;
                }
                
                Debug.Log($"最终激活检查 - 界面: {currentCuttingInterface.activeInHierarchy}");
            }
            
            // 强制刷新Canvas
            if (interfaceParent != null)
            {
                Canvas parentCanvas = interfaceParent.GetComponent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log("强制刷新Canvas");
                    parentCanvas.enabled = false;
                    parentCanvas.enabled = true;
                }
            }
        }
        
        /// <summary>
        /// 设置玩家控制状态
        /// </summary>
        private void SetPlayerControlEnabled(bool enabled)
        {
            if (nearbyPlayer != null)
            {
                var playerController = nearbyPlayer.GetComponent<FirstPersonController>();
                if (playerController != null)
                {
                    playerController.enableMouseLook = enabled;
                    // 这里可以添加更多控制禁用逻辑
                }
            }
            
            // 设置鼠标光标
            if (enabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        /// <summary>
        /// 在Scene视图中绘制交互范围
        /// </summary>
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
        
        [ContextMenu("测试打开切割界面")]
        private void TestOpenInterface()
        {
            OpenCuttingInterface();
        }
        
        [ContextMenu("测试关闭切割界面")]
        private void TestCloseInterface()
        {
            CloseCuttingInterface();
        }
        
        [ContextMenu("强制重置界面状态")]
        private void ForceResetInterfaceState()
        {
            Debug.Log("=== 强制重置界面状态 ===");
            
            if (currentCuttingInterface != null)
            {
                Debug.Log($"发现界面引用: {currentCuttingInterface.name}");
                Debug.Log($"界面激活状态: {currentCuttingInterface.activeInHierarchy}");
                
                try
                {
                    Destroy(currentCuttingInterface);
                    Debug.Log("已销毁界面对象");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"销毁界面时出错: {e.Message}");
                }
            }
            
            currentCuttingInterface = null;
            Debug.Log("界面状态已重置，可以再次打开界面");
        }
        
        [ContextMenu("检查界面状态")]
        private void CheckInterfaceState()
        {
            Debug.Log("=== 界面状态检查 ===");
            Debug.Log($"currentCuttingInterface: {(currentCuttingInterface != null ? currentCuttingInterface.name : "null")}");
            Debug.Log($"playerInRange: {playerInRange}");
            Debug.Log($"interactionPrompt: {(interactionPrompt != null ? interactionPrompt.name : "null")}");
            
            if (currentCuttingInterface != null)
            {
                Debug.Log($"界面激活状态: {currentCuttingInterface.activeInHierarchy}");
                Debug.Log($"界面位置: {currentCuttingInterface.transform.position}");
            }
        }
        
        /// <summary>
        /// 创建3D模型显示区域
        /// </summary>
        private void Create3DModelViewArea(GameObject parent)
        {
            GameObject modelArea = new GameObject("ModelViewArea");
            modelArea.transform.SetParent(parent.transform, false);
            
            RectTransform modelRect = modelArea.AddComponent<RectTransform>();
            // 覆盖整个右侧UI背景区域
            modelRect.anchorMin = Vector2.zero;        // (0, 0) - 左下角
            modelRect.anchorMax = Vector2.one;         // (1, 1) - 右上角
            modelRect.offsetMin = Vector2.zero;
            modelRect.offsetMax = Vector2.zero;
            
            Debug.Log("3D模型显示区域已扩展到全屏幕大小");
            
            // 移除背景和边框，让3D模型直接覆盖在原UI背景上
            // 这样可以保持原UI的视觉效果，同时显示3D模型
            
            // 创建RenderTexture显示区域
            GameObject renderDisplay = new GameObject("RenderDisplay");
            renderDisplay.transform.SetParent(modelArea.transform, false);
            
            RectTransform renderRect = renderDisplay.AddComponent<RectTransform>();
            renderRect.anchorMin = Vector2.zero;
            renderRect.anchorMax = Vector2.one;
            renderRect.offsetMin = Vector2.zero;    // 移除边距，完全填充
            renderRect.offsetMax = Vector2.zero;    // 移除边距，完全填充
            
            // 添加RawImage来显示RenderTexture
            RawImage rawImage = renderDisplay.AddComponent<RawImage>();
            rawImage.color = Color.white; // 确保RawImage可见
            rawImage.raycastTarget = false; // 重要：不阻挡鼠标事件，让底层UI可以交互
            
            // 添加3D模型显示控制器组件
            Sample3DModelViewer modelViewer = modelArea.AddComponent<Sample3DModelViewer>();
            modelViewer.rawImage = rawImage;
            
            // 添加交互控制器组件
            Sample3DModelViewerController controller = modelArea.AddComponent<Sample3DModelViewerController>();
            
            // 设置UI层级：3D模型显示区域应该在背景之上，但在交互控件之下
            EnsureProperUILayering(modelArea, parent);
            
            Debug.Log($"RawImage创建完成: RectTransform={renderRect.rect}, Parent={renderDisplay.transform.parent.name}");
            Debug.Log("3D模型交互控制器已添加");
            
            // 添加提示文字
            CreateModelViewPrompt(modelArea);
            
            Debug.Log("全屏3D模型显示区域创建完成");
        }
        
        /// <summary>
        /// 确保UI层级正确：3D模型在背景之上，交互控件之下
        /// </summary>
        private void EnsureProperUILayering(GameObject modelArea, GameObject parent)
        {
            // 将3D模型显示区域设置为较低的层级索引，让其他UI元素显示在上面
            int totalChildren = parent.transform.childCount;
            int modelLayerIndex = Mathf.Max(1, totalChildren / 3); // 设置在较低位置，但不是最底层
            
            modelArea.transform.SetSiblingIndex(modelLayerIndex);
            
            Debug.Log($"3D模型显示区域层级设置为: {modelLayerIndex}/{totalChildren}");
            
            // 确保重要的交互元素在更高层级
            EnsureInteractiveElementsOnTop(parent);
        }
        
        /// <summary>
        /// 确保交互元素显示在顶层
        /// </summary>
        private void EnsureInteractiveElementsOnTop(GameObject parent)
        {
            Transform parentTransform = parent.transform;
            
            // 查找并提升重要交互元素的层级
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                GameObject childObj = child.gameObject;
                
                // 检查是否包含重要的交互组件
                if (ShouldBeOnTop(childObj))
                {
                    child.SetAsLastSibling(); // 移动到最顶层
                    Debug.Log($"将交互元素移至顶层: {childObj.name}");
                }
            }
        }
        
        /// <summary>
        /// 判断UI元素是否应该显示在顶层
        /// </summary>
        private bool ShouldBeOnTop(GameObject obj)
        {
            // 检查是否包含交互组件
            if (obj.GetComponent<Button>() != null) return true;
            if (obj.GetComponent<Slider>() != null) return true;
            if (obj.GetComponent<Toggle>() != null) return true;
            if (obj.GetComponent<Dropdown>() != null) return true;
            if (obj.GetComponent<InputField>() != null) return true;
            
            // 检查特定名称
            string objName = obj.name.ToLower();
            if (objName.Contains("button")) return true;
            if (objName.Contains("close")) return true;
            if (objName.Contains("progress")) return true;
            if (objName.Contains("title")) return true;
            if (objName.Contains("control")) return true;
            if (objName.Contains("切割")) return true;
            
            // 检查子对象是否包含交互组件
            return obj.GetComponentInChildren<Button>() != null ||
                   obj.GetComponentInChildren<Slider>() != null ||
                   obj.GetComponentInChildren<Toggle>() != null;
        }
        
        /// <summary>
        /// 创建模型显示提示文字
        /// </summary>
        private void CreateModelViewPrompt(GameObject parent)
        {
            GameObject promptObj = new GameObject("ModelPrompt");
            promptObj.transform.SetParent(parent.transform, false);
            
            RectTransform promptRect = promptObj.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0f, 0f);
            promptRect.anchorMax = new Vector2(1f, 1f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
            
            // 确保提示文字在RawImage后面
            promptObj.transform.SetSiblingIndex(0);
            
            Text promptText = promptObj.AddComponent<Text>();
            promptText.text = "拖入样本查看3D模型";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 16;
            promptText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            promptText.alignment = TextAnchor.MiddleCenter;
            
            // 关键：当有RenderTexture内容时，提示应该不可见
            promptText.raycastTarget = false; // 不阻挡鼠标事件
            
            // 样本放入后会隐藏此提示
            promptObj.name = "DefaultPrompt";
            
            Debug.Log("默认提示文字创建完成，层级设置为最底层");
        }
    }
}