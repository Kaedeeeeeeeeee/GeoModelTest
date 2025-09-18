using UnityEngine;
using System.Linq;
using System.Collections;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴系统初始化器
    /// 确保所有图鉴相关组件正确启动和连接
    /// </summary>
    public class EncyclopediaInitializer : MonoBehaviour
    {
        [Header("自动创建UI")]
        [SerializeField] private bool autoCreateUI = true;
        
        [Header("引用设置")]
        [SerializeField] private EncyclopediaData encyclopediaData;
        [SerializeField] private CollectionManager collectionManager;
        [SerializeField] private EncyclopediaUI encyclopediaUI;
        
        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = true;

        private void Awake()
        {
            if (showDebugInfo)
            {
                Debug.Log("[EncyclopediaInitializer] 图鉴系统初始化器启动...");
            }

            // 初始化系统组件
            InitializeSystem();
        }

        private void Start()
        {
            // 延迟检查组件状态和重新尝试UI创建
            Invoke(nameof(DelayedUISetup), 1f);
            Invoke(nameof(CheckSystemStatus), 3f);
        }
        
        /// <summary>
        /// 延迟UI设置
        /// </summary>
        private void DelayedUISetup()
        {
            // 如果UI仍然为空，再次尝试创建
            if (encyclopediaUI == null && autoCreateUI)
            {
                if (showDebugInfo)
                    Debug.Log("UI为空，重新尝试创建...");
                CreateEncyclopediaUI();
            }
        }

        /// <summary>
        /// 初始化图鉴系统
        /// </summary>
        private void InitializeSystem()
        {
            if (showDebugInfo)
                Debug.Log("[EncyclopediaInitializer] 开始初始化图鉴系统...");

            // 1. 确保数据管理器存在
            if (encyclopediaData == null)
            {
                encyclopediaData = FindObjectOfType<EncyclopediaData>();
                if (encyclopediaData == null)
                {
                    var dataGO = new GameObject("EncyclopediaData");
                    encyclopediaData = dataGO.AddComponent<EncyclopediaData>();
                    if (showDebugInfo)
                        Debug.Log("[EncyclopediaInitializer] 创建了EncyclopediaData组件");
                }
                else if (showDebugInfo)
                    Debug.Log("[EncyclopediaInitializer] 找到现有的EncyclopediaData组件");
            }

            // 2. 确保收集管理器存在
            if (collectionManager == null)
            {
                collectionManager = FindObjectOfType<CollectionManager>();
                if (collectionManager == null)
                {
                    var managerGO = new GameObject("CollectionManager");
                    collectionManager = managerGO.AddComponent<CollectionManager>();
                    if (showDebugInfo)
                        Debug.Log("[EncyclopediaInitializer] 创建了CollectionManager组件");
                }
                else if (showDebugInfo)
                    Debug.Log("[EncyclopediaInitializer] 找到现有的CollectionManager组件");
            }

            // 3. 自动创建UI（如果启用）
            if (autoCreateUI && encyclopediaUI == null)
            {
                if (showDebugInfo)
                    Debug.Log("[EncyclopediaInitializer] autoCreateUI=true，开始创建UI...");
                CreateEncyclopediaUI();
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[EncyclopediaInitializer] 跳过UI创建: autoCreateUI={autoCreateUI}, encyclopediaUI={encyclopediaUI}");
            }

        }

        /// <summary>
        /// 创建图鉴UI
        /// </summary>
        private void CreateEncyclopediaUI()
        {
            if (showDebugInfo)
                Debug.Log("开始创建图鉴UI...");

            // 查找现有的UI
            encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
            if (encyclopediaUI != null)
            {
                if (showDebugInfo)
                    Debug.Log("找到现有的EncyclopediaUI: " + encyclopediaUI.gameObject.name);
                
                // 确保现有UI是关闭状态
                if (encyclopediaUI.IsOpen())
                {
                    encyclopediaUI.CloseEncyclopedia();
                }
                return;
            }

            if (showDebugInfo)
                Debug.Log("没有找到现有的EncyclopediaUI，开始创建新的...");

            try
            {
                // 查找UI设置助手
                var uiSetup = FindObjectOfType<EncyclopediaUISetup>();
                if (uiSetup == null)
                {
                    if (showDebugInfo)
                        Debug.Log("创建EncyclopediaUISetup组件...");
                    var setupGO = new GameObject("EncyclopediaUISetup");
                    uiSetup = setupGO.AddComponent<EncyclopediaUISetup>();
                }

                if (showDebugInfo)
                    Debug.Log("调用CreateEncyclopediaUI...");

                // 创建UI结构
                uiSetup.CreateEncyclopediaUI();
                
                // 等待一帧后再查找UI控制器
                StartCoroutine(FindUIAfterCreation());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建图鉴UI时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 创建UI后查找组件
        /// </summary>
        private System.Collections.IEnumerator FindUIAfterCreation()
        {
            yield return null; // 等待一帧

            if (showDebugInfo)
                Debug.Log("[EncyclopediaInitializer] 开始查找EncyclopediaUI组件...");

            // 查找创建的UI控制器
            encyclopediaUI = FindObjectOfType<EncyclopediaUI>();

            if (showDebugInfo)
            {
                if (encyclopediaUI != null)
                {
                    Debug.Log($"[EncyclopediaInitializer] 成功找到图鉴UI: {encyclopediaUI.gameObject.name}");
                    Debug.Log($"[EncyclopediaInitializer] GameObject激活状态: {encyclopediaUI.gameObject.activeInHierarchy}");

                    // 确保新创建的UI是关闭状态
                    if (encyclopediaUI.IsOpen())
                    {
                        encyclopediaUI.CloseEncyclopedia();
                    }
                }
                else
                {
                    Debug.LogError("[EncyclopediaInitializer] 创建图鉴UI失败 - 未找到EncyclopediaUI组件");

                    // 搜索所有包含"Encyclopedia"的GameObject
                    var allGameObjects = FindObjectsOfType<GameObject>(true); // 包括非激活的
                    Debug.Log("[EncyclopediaInitializer] 搜索所有包含Encyclopedia的GameObject:");
                    foreach (var go in allGameObjects)
                    {
                        if (go.name.Contains("Encyclopedia"))
                        {
                            var ui = go.GetComponent<EncyclopediaUI>();
                            Debug.Log($"  - {go.name} (激活:{go.activeInHierarchy}) EncyclopediaUI:{ui != null}");
                        }
                    }

                    // 列出场景中所有可能相关的对象
                    var allObjects = FindObjectsOfType<MonoBehaviour>(true);
                    Debug.Log("[EncyclopediaInitializer] 场景中的所有Encyclopedia相关MonoBehaviour:");
                    foreach (var obj in allObjects)
                    {
                        if (obj.GetType().Namespace == "Encyclopedia")
                        {
                            Debug.Log($"  - {obj.GetType().Name} on {obj.gameObject.name} (激活:{obj.gameObject.activeInHierarchy})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查系统状态
        /// </summary>
        private void CheckSystemStatus()
        {
            if (!showDebugInfo) return;

            Debug.Log("=== 图鉴系统状态检查 ===");
            
            // 检查数据系统
            if (EncyclopediaData.Instance != null)
            {
                try
                {
                    var allEntries = EncyclopediaData.Instance.AllEntries;
                    if (allEntries != null)
                    {
                        Debug.Log($"✓ 数据系统: 已加载 {allEntries.Count} 个条目");
                        Debug.Log($"  - 矿物: {EncyclopediaData.Instance.TotalMinerals}");
                        Debug.Log($"  - 化石: {EncyclopediaData.Instance.TotalFossils}");
                    }
                    else
                    {
                        Debug.LogWarning("⚠ 数据系统: 实例存在但条目数据为空");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠ 数据系统访问错误: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("✗ 数据系统: 未初始化");
            }

            // 检查收集系统
            if (CollectionManager.Instance != null)
            {
                var stats = CollectionManager.Instance.CurrentStats;
                if (stats != null)
                {
                    Debug.Log($"✓ 收集系统: {stats.discoveredEntries}/{stats.totalEntries} ({stats.overallProgress:P1})");
                }
                else
                {
                    Debug.LogWarning("⚠ 收集系统: 实例存在但统计数据为空");
                }
            }
            else
            {
                Debug.LogWarning("✗ 收集系统: 未初始化");
            }

            // 检查UI系统
            if (encyclopediaUI != null)
            {
                Debug.Log("✓ UI系统: 已初始化");
            }
            else
            {
                Debug.LogWarning("✗ UI系统: 未初始化");
            }

            Debug.Log("=== 状态检查完成 ===");
        }

        /// <summary>
        /// 手动刷新系统
        /// </summary>
        [ContextMenu("刷新图鉴系统")]
        public void RefreshSystem()
        {
            InitializeSystem();
            CheckSystemStatus();
        }

        /// <summary>
        /// 测试发现条目
        /// </summary>
        [ContextMenu("测试发现条目")]
        public void TestDiscoverEntry()
        {
            if (CollectionManager.Instance != null && EncyclopediaData.Instance != null)
            {
                var firstEntry = EncyclopediaData.Instance.AllEntries.Values.FirstOrDefault();
                if (firstEntry != null)
                {
                    CollectionManager.Instance.DiscoverEntry(firstEntry.id);
                    Debug.Log($"测试发现了条目: {firstEntry.GetFormattedDisplayName()}");
                }
            }
        }

        /// <summary>
        /// 打开图鉴
        /// </summary>
        [ContextMenu("打开图鉴")]
        public void OpenEncyclopedia()
        {
            if (encyclopediaUI != null)
            {
                encyclopediaUI.OpenEncyclopedia();
            }
            else
            {
                Debug.LogWarning("图鉴UI未初始化，尝试重新创建...");
                CreateEncyclopediaUI();
            }
        }
        

        /// <summary>
        /// 强制重新创建UI
        /// </summary>
        [ContextMenu("强制重新创建UI")]
        public void ForceRecreateUI()
        {
            // 删除现有UI
            var existingUI = FindObjectOfType<EncyclopediaUI>();
            if (existingUI != null)
            {
                if (showDebugInfo)
                    Debug.Log("删除现有UI: " + existingUI.gameObject.name);
                DestroyImmediate(existingUI.gameObject);
            }

            // 删除现有Canvas（如果只有图鉴UI在使用）
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.name.Contains("Encyclopedia"))
                {
                    if (showDebugInfo)
                        Debug.Log("删除图鉴Canvas: " + canvas.gameObject.name);
                    DestroyImmediate(canvas.gameObject);
                }
            }

            encyclopediaUI = null;

            // 重新创建
            CreateEncyclopediaUI();
        }
    }
}