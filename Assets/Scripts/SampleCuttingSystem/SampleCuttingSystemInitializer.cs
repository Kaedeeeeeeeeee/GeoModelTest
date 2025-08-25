using UnityEngine;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本切割系统初始化器
    /// 负责系统的初始化和与其他游戏系统的集成
    /// </summary>
    public class SampleCuttingSystemInitializer : MonoBehaviour
    {
        [Header("自动初始化设置")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool integrateWithGameInitializer = true;
        [SerializeField] private bool enableDebugOutput = true;
        
        [Header("系统prefab引用")]
        [SerializeField] private GameObject cuttingStationPrefab;        // 切割台预制体
        [SerializeField] private Transform laboratorySpawnPoint;         // 实验室生成位置
        [SerializeField] private Canvas laboratoryUICanvas;             // 实验室UI画布
        
        // 系统组件
        private SampleCuttingSystemManager systemManager;
        private GameObject cuttingStationInstance;
        
        void Start()
        {
            if (initializeOnStart)
            {
                InitializeSystem();
            }
        }
        
        /// <summary>
        /// 初始化切割系统
        /// </summary>
        public void InitializeSystem()
        {
            try
            {
                LogDebug("开始初始化样本切割系统...");
                
                // 1. 确保在实验室场景中
                if (!IsInLaboratoryScene())
                {
                    LogDebug("当前不在实验室场景，跳过切割系统初始化");
                    return;
                }
                
                // 2. 创建或设置切割台
                SetupCuttingStation();
                
                // 3. 初始化系统管理器
                SetupSystemManager();
                
                // 4. 集成到游戏初始化器
                if (integrateWithGameInitializer)
                {
                    IntegrateWithGameSystems();
                }
                
                LogDebug("样本切割系统初始化完成！");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"样本切割系统初始化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否在实验室场景
        /// </summary>
        private bool IsInLaboratoryScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return currentScene.Contains("Laboratory") || currentScene.Contains("实验室");
        }
        
        /// <summary>
        /// 设置切割台
        /// </summary>
        private void SetupCuttingStation()
        {
            // 方法1: 通过标签查找
            try 
            {
                cuttingStationInstance = GameObject.FindGameObjectWithTag("CuttingStation");
            }
            catch (System.Exception)
            {
                // 标签不存在，使用名称查找
                cuttingStationInstance = GameObject.Find("SampleCuttingStation");
            }
            
            // 方法2: 通过名称查找
            if (cuttingStationInstance == null)
            {
                cuttingStationInstance = GameObject.Find("SampleCuttingStation");
            }
            
            // 方法3: 通过组件查找
            if (cuttingStationInstance == null)
            {
                var manager = FindFirstObjectByType<SampleCuttingSystemManager>();
                if (manager != null)
                {
                    cuttingStationInstance = manager.gameObject;
                }
            }
            
            if (cuttingStationInstance == null && cuttingStationPrefab != null)
            {
                // 使用预制体创建切割台
                Vector3 spawnPosition = laboratorySpawnPoint != null ? 
                    laboratorySpawnPoint.position : new Vector3(0.693000019f, 0.588999987f, 14.5710001f);
                    
                cuttingStationInstance = Instantiate(cuttingStationPrefab, spawnPosition, Quaternion.identity);
                cuttingStationInstance.name = "SampleCuttingStation";
                
                LogDebug($"从预制体创建切割台实例: {spawnPosition}");
            }
            else if (cuttingStationInstance == null)
            {
                // 运行时创建基础切割台对象（备用方案）
                cuttingStationInstance = new GameObject("SampleCuttingStation");
                
                // 设置到实验台位置
                Vector3 defaultPosition = new Vector3(0.693000019f, 0.588999987f, 14.5710001f);
                if (laboratorySpawnPoint != null)
                {
                    cuttingStationInstance.transform.position = laboratorySpawnPoint.position;
                }
                else
                {
                    cuttingStationInstance.transform.position = defaultPosition;
                }
                
                LogDebug($"运行时创建基础切割台对象: {cuttingStationInstance.transform.position}");
            }
            
            // 确保切割台有正确的标签
            if (cuttingStationInstance != null)
            {
                try
                {
                    cuttingStationInstance.tag = "CuttingStation";
                }
                catch (System.Exception)
                {
                    // 标签不存在时跳过设置
                    LogDebug("CuttingStation标签不存在，跳过标签设置");
                }
            }
        }
        
        /// <summary>
        /// 设置系统管理器
        /// </summary>
        private void SetupSystemManager()
        {
            if (cuttingStationInstance == null)
            {
                Debug.LogError("切割台实例为空，无法设置系统管理器");
                return;
            }
            
            // 获取或添加系统管理器
            systemManager = cuttingStationInstance.GetComponent<SampleCuttingSystemManager>();
            if (systemManager == null)
            {
                systemManager = cuttingStationInstance.AddComponent<SampleCuttingSystemManager>();
                LogDebug("添加系统管理器组件");
            }
            
            // 确保所有必要组件存在
            EnsureRequiredComponents();
            
            // 初始化系统管理器
            systemManager.InitializeSystem();
        }
        
        /// <summary>
        /// 确保必要组件存在
        /// </summary>
        private void EnsureRequiredComponents()
        {
            if (cuttingStationInstance == null) return;
            
            // 添加必要的组件
            var components = new System.Type[]
            {
                typeof(CuttingStationInteraction),
                typeof(SampleCuttingGame),
                typeof(SampleLayerAnalyzer),
                typeof(LayerDatabaseMapper),
                typeof(CuttingStationUI),
                typeof(SingleLayerSampleGenerator),
                typeof(WarehouseIntegration)
            };
            
            foreach (var componentType in components)
            {
                if (cuttingStationInstance.GetComponent(componentType) == null)
                {
                    cuttingStationInstance.AddComponent(componentType);
                    LogDebug($"添加组件: {componentType.Name}");
                }
            }
            
            // 确保有AudioSource组件
            if (cuttingStationInstance.GetComponent<AudioSource>() == null)
            {
                cuttingStationInstance.AddComponent<AudioSource>();
                LogDebug("添加AudioSource组件");
            }
        }
        
        /// <summary>
        /// 与游戏系统集成
        /// </summary>
        private void IntegrateWithGameSystems()
        {
            // 1. 集成到GameInitializer
            IntegrateWithGameInitializer();
            
            // 2. 集成到场景系统
            IntegrateWithSceneSystem();
            
            // 3. 集成到本地化系统
            IntegrateWithLocalizationSystem();
            
            LogDebug("完成与游戏系统的集成");
        }
        
        /// <summary>
        /// 集成到GameInitializer
        /// </summary>
        private void IntegrateWithGameInitializer()
        {
            var gameInitializer = FindFirstObjectByType<GameInitializer>();
            if (gameInitializer != null)
            {
                LogDebug("找到GameInitializer，已集成切割系统");
                // 这里可以添加具体的集成逻辑
            }
        }
        
        /// <summary>
        /// 集成到场景系统
        /// </summary>
        private void IntegrateWithSceneSystem()
        {
            var sceneManager = FindFirstObjectByType<GameSceneManager>();
            if (sceneManager != null)
            {
                LogDebug("找到场景管理器，切割系统已注册");
                // 这里可以注册场景切换事件
            }
        }
        
        /// <summary>
        /// 集成到本地化系统
        /// </summary>
        private void IntegrateWithLocalizationSystem()
        {
            var localizationManager = FindFirstObjectByType<LocalizationManager>();
            if (localizationManager != null)
            {
                LogDebug("找到本地化管理器，切割系统UI已本地化");
                // 这里可以添加本地化文本的注册
            }
        }
        
        /// <summary>
        /// 创建UI界面
        /// </summary>
        private void SetupUI()
        {
            if (laboratoryUICanvas == null)
            {
                // 查找实验室UI画布
                laboratoryUICanvas = FindUICanvas();
            }
            
            if (laboratoryUICanvas != null)
            {
                // 在这里可以创建切割台的UI界面
                LogDebug("切割系统UI已设置到实验室画布");
            }
        }
        
        /// <summary>
        /// 查找UI画布
        /// </summary>
        private Canvas FindUICanvas()
        {
            // 首先查找名为Laboratory的Canvas
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.name.Contains("Laboratory") || canvas.name.Contains("实验室"))
                {
                    return canvas;
                }
            }
            
            // 如果没找到，返回第一个Canvas
            return canvases.Length > 0 ? canvases[0] : null;
        }
        
        /// <summary>
        /// 获取系统状态
        /// </summary>
        public bool IsSystemInitialized()
        {
            return systemManager != null && 
                   cuttingStationInstance != null && 
                   systemManager.CheckSystemHealth();
        }
        
        /// <summary>
        /// 获取系统管理器
        /// </summary>
        public SampleCuttingSystemManager GetSystemManager()
        {
            return systemManager;
        }
        
        /// <summary>
        /// 获取切割台实例
        /// </summary>
        public GameObject GetCuttingStationInstance()
        {
            return cuttingStationInstance;
        }
        
        /// <summary>
        /// 重新初始化系统
        /// </summary>
        public void ReinitializeSystem()
        {
            LogDebug("重新初始化切割系统...");
            
            // 清理现有实例
            if (cuttingStationInstance != null)
            {
                DestroyImmediate(cuttingStationInstance);
                cuttingStationInstance = null;
                systemManager = null;
            }
            
            // 重新初始化
            InitializeSystem();
        }
        
        /// <summary>
        /// 调试日志输出
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugOutput)
            {
                Debug.Log($"[SampleCuttingSystem] {message}");
            }
        }
        
        /// <summary>
        /// 验证系统完整性
        /// </summary>
        public bool ValidateSystemIntegrity()
        {
            bool isValid = true;
            
            if (cuttingStationInstance == null)
            {
                Debug.LogError("切割台实例缺失");
                isValid = false;
            }
            
            if (systemManager == null)
            {
                Debug.LogError("系统管理器缺失");
                isValid = false;
            }
            
            if (systemManager != null && !systemManager.CheckSystemHealth())
            {
                Debug.LogError("系统健康检查失败");
                isValid = false;
            }
            
            if (isValid)
            {
                LogDebug("系统完整性验证通过");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Editor方法：手动初始化
        /// </summary>
        [ContextMenu("手动初始化系统")]
        private void ManualInitializeSystem()
        {
            InitializeSystem();
        }
        
        [ContextMenu("验证系统完整性")]
        private void EditorValidateSystem()
        {
            ValidateSystemIntegrity();
        }
        
        [ContextMenu("重新初始化")]
        private void EditorReinitializeSystem()
        {
            ReinitializeSystem();
        }
        
        [ContextMenu("显示系统信息")]
        private void ShowSystemInfo()
        {
            Debug.Log("=== 样本切割系统信息 ===");
            Debug.Log($"系统已初始化: {IsSystemInitialized()}");
            Debug.Log($"切割台实例: {cuttingStationInstance?.name ?? "无"}");
            Debug.Log($"系统管理器: {systemManager?.name ?? "无"}");
            Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"在实验室场景: {IsInLaboratoryScene()}");
            
            if (systemManager != null)
            {
                var stats = systemManager.GetStatistics();
                Debug.Log($"切割统计: 总会话={stats.totalCuttingSessions}, 成功率={stats.successRate:P1}");
            }
        }
    }
}