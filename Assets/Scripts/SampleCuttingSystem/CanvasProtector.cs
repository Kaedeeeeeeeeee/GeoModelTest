using UnityEngine;

namespace SampleCuttingSystem
{
    /// <summary>
    /// Canvas保护组件
    /// 防止Canvas被清理系统误删除
    /// </summary>
    public class CanvasProtector : MonoBehaviour
    {
        [Header("Canvas保护设置")]
        [SerializeField] private bool protectFromSceneCleanup = true;
        [SerializeField] private bool protectFromDestroy = true;
        
        private Canvas protectedCanvas;
        
        void Awake()
        {
            protectedCanvas = GetComponent<Canvas>();
            
            if (protectedCanvas != null)
            {
                Debug.Log($"[CanvasProtector] 保护Canvas: {protectedCanvas.name}");
                
                // 设置为不销毁
                if (protectFromDestroy)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                // 添加特殊标记
                gameObject.name = "[Protected]" + gameObject.name;
            }
        }
        
        void OnDestroy()
        {
            if (protectedCanvas != null)
            {
                Debug.LogWarning($"[CanvasProtector] 受保护的Canvas正在被销毁: {protectedCanvas.name}");
            }
        }
        
        /// <summary>
        /// 检查Canvas是否受保护
        /// </summary>
        public bool IsProtected()
        {
            return protectFromSceneCleanup;
        }
        
        /// <summary>
        /// 获取受保护的Canvas
        /// </summary>
        public Canvas GetProtectedCanvas()
        {
            return protectedCanvas;
        }
    }
}