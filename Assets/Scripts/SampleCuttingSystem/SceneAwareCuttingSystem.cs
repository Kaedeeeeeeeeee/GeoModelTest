using UnityEngine;
using UnityEngine.SceneManagement;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 场景感知的切割系统
    /// 根据当前场景自动配置和初始化切割功能
    /// </summary>
    public class SceneAwareCuttingSystem : MonoBehaviour
    {
        [Header("场景配置")]
        [SerializeField] private string laboratorySceneName = "Laboratory Scene";
        [SerializeField] private string mainSceneName = "MainScene";
        
        [Header("系统状态")]
        [SerializeField] private bool isSystemActive = false;
        [SerializeField] private string currentScene = "";
        
        private LabCuttingSystemInitializer labInitializer;
        
        void Awake()
        {
            // 确保这个对象在场景切换时不被销毁
            DontDestroyOnLoad(gameObject);
            
            // 监听场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void Start()
        {
            // 检查当前场景
            CheckCurrentScene();
        }
        
        /// <summary>
        /// 场景加载完成时调用
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"场景加载: {scene.name}");
            CheckCurrentScene();
        }
        
        /// <summary>
        /// 检查当前场景并配置系统
        /// </summary>
        private void CheckCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            currentScene = activeScene.name;
            
            Debug.Log($"当前场景: {currentScene}");
            
            if (IsLaboratoryScene(currentScene))
            {
                Debug.Log("检测到实验室场景，初始化切割系统");
                InitializeLaboratorySystem();
            }
            else if (IsMainScene(currentScene))
            {
                Debug.Log("检测到主场景，切割系统待机");
                DeactivateSystem();
            }
            else
            {
                Debug.Log($"未知场景: {currentScene}，切割系统待机");
                DeactivateSystem();
            }
        }
        
        /// <summary>
        /// 检查是否为实验室场景
        /// </summary>
        private bool IsLaboratoryScene(string sceneName)
        {
            return sceneName.Contains("Laboratory") || 
                   sceneName.Contains("Lab") ||
                   sceneName.Equals(laboratorySceneName);
        }
        
        /// <summary>
        /// 检查是否为主场景
        /// </summary>
        private bool IsMainScene(string sceneName)
        {
            return sceneName.Contains("Main") ||
                   sceneName.Equals(mainSceneName);
        }
        
        /// <summary>
        /// 初始化实验室系统
        /// </summary>
        private void InitializeLaboratorySystem()
        {
            if (isSystemActive)
            {
                Debug.Log("系统已激活，跳过初始化");
                return;
            }
            
            // 延迟初始化，确保场景完全加载
            Invoke(nameof(SetupLaboratoryInitializer), 2f);
        }
        
        /// <summary>
        /// 设置实验室初始化器
        /// </summary>
        private void SetupLaboratoryInitializer()
        {
            // 查找现有的初始化器
            labInitializer = FindObjectOfType<LabCuttingSystemInitializer>();
            
            if (labInitializer == null)
            {
                // 创建新的初始化器
                GameObject initializerObj = new GameObject("LabCuttingSystemInitializer");
                labInitializer = initializerObj.AddComponent<LabCuttingSystemInitializer>();
                
                Debug.Log("创建了实验室切割系统初始化器");
            }
            else
            {
                Debug.Log("找到现有的初始化器");
            }
            
            // 启动初始化
            labInitializer.InitializeCuttingSystem();
            
            isSystemActive = true;
            Debug.Log("实验室切割系统已激活");
        }
        
        /// <summary>
        /// 停用系统
        /// </summary>
        private void DeactivateSystem()
        {
            if (labInitializer != null)
            {
                labInitializer.CleanupCuttingSystem();
                labInitializer = null;
            }
            
            isSystemActive = false;
            Debug.Log("切割系统已停用");
        }
        
        /// <summary>
        /// 手动切换到实验室场景
        /// </summary>
        [ContextMenu("切换到实验室场景")]
        public void SwitchToLaboratoryScene()
        {
            if (!IsLaboratoryScene(currentScene))
            {
                Debug.Log("切换到实验室场景...");
                SceneManager.LoadScene(laboratorySceneName);
            }
            else
            {
                Debug.Log("已经在实验室场景中");
            }
        }
        
        /// <summary>
        /// 手动切换到主场景
        /// </summary>
        [ContextMenu("切换到主场景")]
        public void SwitchToMainScene()
        {
            if (!IsMainScene(currentScene))
            {
                Debug.Log("切换到主场景...");
                SceneManager.LoadScene(mainSceneName);
            }
            else
            {
                Debug.Log("已经在主场景中");
            }
        }
        
        /// <summary>
        /// 强制重新初始化系统
        /// </summary>
        [ContextMenu("强制重新初始化")]
        public void ForceReinitialize()
        {
            Debug.Log("强制重新初始化切割系统");
            DeactivateSystem();
            
            if (IsLaboratoryScene(currentScene))
            {
                Invoke(nameof(InitializeLaboratorySystem), 1f);
            }
        }
        
        void OnDestroy()
        {
            // 清理事件监听
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        public string GetSystemStatus()
        {
            return $"场景: {currentScene}, 系统状态: {(isSystemActive ? "激活" : "待机")}";
        }
        
        void Update()
        {
            // 调试快捷键
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Debug.Log(GetSystemStatus());
            }
            
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ForceReinitialize();
            }
        }
    }
}