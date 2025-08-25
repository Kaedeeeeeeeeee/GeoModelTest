using UnityEngine;
using UnityEngine.SceneManagement;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 实验室切割系统初始化器
    /// 在实验室场景中自动设置切割台和相关组件
    /// </summary>
    public class LabCuttingSystemInitializer : MonoBehaviour
    {
        [Header("自动初始化设置")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool searchForExistingTable = true;
        [SerializeField] private string tableObjectName = "LabTable";
        
        [Header("切割台设置")]
        [SerializeField] private Vector3 cuttingStationOffset = new Vector3(0, 1.2f, 0);
        [SerializeField] private Vector3 cuttingStationScale = new Vector3(0.8f, 0.05f, 0.8f);
        
        private GameObject cuttingStation;
        private bool isInitialized = false;
        
        void Start()
        {
            if (autoInitializeOnStart)
            {
                // 延迟初始化，确保场景完全加载
                Invoke(nameof(InitializeCuttingSystem), 1f);
            }
        }
        
        /// <summary>
        /// 初始化切割系统
        /// </summary>
        [ContextMenu("初始化切割系统")]
        public void InitializeCuttingSystem()
        {
            if (isInitialized)
            {
                Debug.Log("切割系统已经初始化，跳过");
                return;
            }
            
            Debug.Log("开始初始化实验室切割系统...");
            
            // 查找实验台
            GameObject labTable = FindLabTable();
            
            if (labTable != null)
            {
                Debug.Log($"找到实验台: {labTable.name}");
                SetupCuttingStation(labTable);
            }
            else
            {
                Debug.LogWarning("未找到实验台，在默认位置创建切割台");
                CreateDefaultCuttingStation();
            }
            
            isInitialized = true;
            Debug.Log("实验室切割系统初始化完成！");
        }
        
        /// <summary>
        /// 查找实验台
        /// </summary>
        private GameObject FindLabTable()
        {
            if (searchForExistingTable)
            {
                // 优先查找指定名称的对象
                GameObject table = GameObject.Find(tableObjectName);
                if (table != null)
                {
                    return table;
                }
                
                // 查找包含"table"的对象
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.ToLower().Contains("table") || 
                        obj.name.ToLower().Contains("desk") ||
                        obj.name.ToLower().Contains("bench"))
                    {
                        Debug.Log($"找到可能的实验台: {obj.name}");
                        return obj;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 在实验台上设置切割台
        /// </summary>
        private void SetupCuttingStation(GameObject labTable)
        {
            // 计算切割台位置
            Vector3 tablePosition = labTable.transform.position;
            Vector3 stationPosition = tablePosition + cuttingStationOffset;
            
            // 创建切割台
            CreateCuttingStationAt(stationPosition);
            
            Debug.Log($"在实验台 {labTable.name} 上设置切割台，位置: {stationPosition}");
        }
        
        /// <summary>
        /// 创建默认位置的切割台
        /// </summary>
        private void CreateDefaultCuttingStation()
        {
            Vector3 defaultPosition = new Vector3(0, 1.5f, 0);
            CreateCuttingStationAt(defaultPosition);
            
            Debug.Log($"在默认位置创建切割台: {defaultPosition}");
        }
        
        /// <summary>
        /// 在指定位置创建切割台
        /// </summary>
        private void CreateCuttingStationAt(Vector3 position)
        {
            // 检查是否已存在切割台
            if (cuttingStation != null)
            {
                Debug.Log("切割台已存在，跳过创建");
                return;
            }
            
            // 创建切割台主体
            cuttingStation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cuttingStation.name = "LaboratoryCuttingStation";
            cuttingStation.transform.position = position;
            cuttingStation.transform.localScale = cuttingStationScale;
            
            // 设置材质
            var renderer = cuttingStation.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.9f, 1f, 0.8f); // 淡蓝色
            }
            
            // 添加切割站交互组件
            var interaction = cuttingStation.AddComponent<CuttingStationInteraction>();
            
            // 设置碰撞器为触发器
            var collider = cuttingStation.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            // 创建提示文本
            CreateInteractionPrompt(cuttingStation);
            
            Debug.Log("切割台创建完成");
        }
        
        /// <summary>
        /// 创建交互提示
        /// </summary>
        private void CreateInteractionPrompt(GameObject station)
        {
            // 创建提示文本对象
            GameObject promptObj = new GameObject("InteractionPrompt");
            promptObj.transform.SetParent(station.transform);
            promptObj.transform.localPosition = new Vector3(0, 1f, 0);
            
            // 添加3D文本
            TextMesh textMesh = promptObj.AddComponent<TextMesh>();
            textMesh.text = "按 F 键进入样本切割台";
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 20;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            // 让文本始终面向相机
            promptObj.AddComponent<FaceCamera>();
        }
        
        /// <summary>
        /// 让对象面向相机的简单脚本
        /// </summary>
        private class FaceCamera : MonoBehaviour
        {
            private Camera mainCamera;
            
            void Start()
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
            }
            
            void Update()
            {
                if (mainCamera != null)
                {
                    transform.LookAt(mainCamera.transform.position);
                    transform.Rotate(0, 180, 0); // 翻转文本
                }
            }
        }
        
        /// <summary>
        /// 清理切割系统
        /// </summary>
        [ContextMenu("清理切割系统")]
        public void CleanupCuttingSystem()
        {
            if (cuttingStation != null)
            {
                DestroyImmediate(cuttingStation);
                cuttingStation = null;
            }
            
            // 清理任何切割系统组件
            var cuttingGames = FindObjectsOfType<SampleCuttingGame>();
            foreach (var game in cuttingGames)
            {
                if (game != null && game.transform.parent == null)
                {
                    DestroyImmediate(game.gameObject);
                }
            }
            
            isInitialized = false;
            Debug.Log("切割系统已清理");
        }
        
        void OnValidate()
        {
            // 在编辑器中预览设置变化
            if (Application.isPlaying && isInitialized)
            {
                if (cuttingStation != null)
                {
                    // 更新切割台尺寸
                    cuttingStation.transform.localScale = cuttingStationScale;
                }
            }
        }
    }
}