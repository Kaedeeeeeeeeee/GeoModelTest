using UnityEngine;
using UnityEngine.InputSystem;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴系统调试助手
    /// 提供安全的调试和测试功能
    /// </summary>
    public class EncyclopediaDebugHelper : MonoBehaviour
    {
        [Header("调试选项")]
        [SerializeField] private bool enableDebugOutput = true;
        [SerializeField] private Key debugKey = Key.L;
        
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame)
            {
                PerformSafeSystemCheck();
            }
            
            // 测试O键
            if (Keyboard.current != null && Keyboard.current[Key.O].wasPressedThisFrame)
            {
                TestOKey();
            }
        }
        
        /// <summary>
        /// 安全的系统检查
        /// </summary>
        [ContextMenu("安全系统检查")]
        public void PerformSafeSystemCheck()
        {
            if (!enableDebugOutput) return;
            
            Debug.Log("=== 安全系统检查开始 ===");
            
            // 检查基础数据系统
            CheckDataSystem();
            
            // 检查收集系统
            CheckCollectionSystem();
            
            // 检查UI系统
            CheckUISystem();
            
            // 检查初始化器
            CheckInitializer();
            
            Debug.Log("=== 安全系统检查完成 ===");
        }
        
        private void CheckDataSystem()
        {
            try
            {
                if (EncyclopediaData.Instance == null)
                {
                    Debug.LogWarning("❌ EncyclopediaData.Instance 为空");
                    return;
                }
                
                Debug.Log("✅ EncyclopediaData.Instance 存在");
                
                if (EncyclopediaData.Instance.IsDataLoaded)
                {
                    Debug.Log($"✅ 数据已加载，条目数量: {EncyclopediaData.Instance.AllEntries?.Count ?? 0}");
                }
                else
                {
                    Debug.LogWarning("⚠️ 数据尚未加载完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 检查数据系统时出错: {e.Message}");
            }
        }
        
        private void CheckCollectionSystem()
        {
            try
            {
                if (CollectionManager.Instance == null)
                {
                    Debug.LogWarning("❌ CollectionManager.Instance 为空");
                    return;
                }
                
                Debug.Log("✅ CollectionManager.Instance 存在");
                
                var stats = CollectionManager.Instance.CurrentStats;
                if (stats == null)
                {
                    Debug.LogWarning("⚠️ CurrentStats 为空");
                }
                else
                {
                    Debug.Log($"✅ 统计数据: {stats.discoveredEntries}/{stats.totalEntries}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 检查收集系统时出错: {e.Message}");
            }
        }
        
        private void CheckUISystem()
        {
            try
            {
                var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
                if (encyclopediaUI == null)
                {
                    Debug.LogWarning("❌ 场景中未找到 EncyclopediaUI");
                }
                else
                {
                    Debug.Log($"✅ 找到 EncyclopediaUI: {encyclopediaUI.gameObject.name}");
                    Debug.Log($"   是否打开: {encyclopediaUI.IsOpen()}");
                }
                
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning("❌ 场景中未找到 Canvas");
                }
                else
                {
                    Debug.Log($"✅ 找到 Canvas: {canvas.gameObject.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 检查UI系统时出错: {e.Message}");
            }
        }
        
        private void CheckInitializer()
        {
            try
            {
                var initializer = FindObjectOfType<EncyclopediaInitializer>();
                if (initializer == null)
                {
                    Debug.LogWarning("❌ 场景中未找到 EncyclopediaInitializer");
                }
                else
                {
                    Debug.Log($"✅ 找到 EncyclopediaInitializer: {initializer.gameObject.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 检查初始化器时出错: {e.Message}");
            }
        }
        
        private void TestOKey()
        {
            Debug.Log("🔑 O键被按下!");
            
            var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
            if (encyclopediaUI != null)
            {
                Debug.Log($"   找到UI组件，当前状态: {(encyclopediaUI.IsOpen() ? "打开" : "关闭")}");
            }
            else
            {
                Debug.LogWarning("   未找到UI组件!");
            }
        }
        
        /// <summary>
        /// 强制创建基础系统
        /// </summary>
        [ContextMenu("强制创建基础系统")]
        public void ForceCreateBasicSystems()
        {
            // 创建数据系统
            if (EncyclopediaData.Instance == null)
            {
                var dataGO = new GameObject("EncyclopediaData");
                dataGO.AddComponent<EncyclopediaData>();
                Debug.Log("创建了 EncyclopediaData");
            }
            
            // 创建收集系统
            if (CollectionManager.Instance == null)
            {
                var collectionGO = new GameObject("CollectionManager");
                collectionGO.AddComponent<CollectionManager>();
                Debug.Log("创建了 CollectionManager");
            }
            
            Debug.Log("基础系统创建完成");
        }
        
        /// <summary>
        /// 简单的UI创建测试
        /// </summary>
        [ContextMenu("简单UI创建测试")]
        public void SimpleUITest()
        {
            // 创建一个简单的测试UI
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("TestCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("创建了测试Canvas");
            }
            
            var testPanel = new GameObject("TestPanel");
            testPanel.transform.SetParent(canvas.transform, false);
            var rectTransform = testPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var image = testPanel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 1, 0.5f);
            
            testPanel.SetActive(false);
            
            Debug.Log("创建了测试面板，按F2显示/隐藏");
        }
        
        private void Start()
        {
            if (enableDebugOutput)
            {
                Debug.Log($"图鉴调试助手已启动，按 {debugKey} 进行系统检查");
            }
        }
    }
}