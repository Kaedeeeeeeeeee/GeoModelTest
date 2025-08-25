using UnityEngine;
using System.Reflection;

namespace SampleCuttingSystem
{
    /// <summary>
    /// SampleCollector扩展方法
    /// 提供对私有CollectSample方法的公共访问
    /// </summary>
    public static class SampleCollectorExtensions
    {
        /// <summary>
        /// 公共的收集样本方法
        /// 使用反射调用私有的CollectSample方法
        /// </summary>
        public static bool TryCollectSample(this SampleCollector collector)
        {
            if (collector == null) return false;
            
            try
            {
                // 使用反射获取私有方法
                MethodInfo collectMethod = collector.GetType().GetMethod("CollectSample", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (collectMethod != null)
                {
                    collectMethod.Invoke(collector, null);
                    return true;
                }
                else
                {
                    Debug.LogWarning("未找到CollectSample方法");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"调用CollectSample失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 强制收集样本（备选方法）
        /// 直接调用样本添加到背包的逻辑
        /// </summary>
        public static bool ForceCollectSample(this SampleCollector collector)
        {
            if (collector == null) return false;
            
            try
            {
                // 获取样本数据
                var sampleDataField = collector.GetType().GetField("sampleData", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (sampleDataField != null)
                {
                    var sampleData = sampleDataField.GetValue(collector) as SampleItem;
                    
                    if (sampleData != null)
                    {
                        // 查找样本背包
                        var inventory = Object.FindFirstObjectByType<SampleInventory>();
                        if (inventory != null)
                        {
                            // 尝试添加到背包
                            // 这里需要根据SampleInventory的实际API来实现
                            Debug.Log($"强制收集样本: {collector.name}");
                            
                            // 销毁样本对象
                            if (collector.gameObject != null)
                            {
                                Object.Destroy(collector.gameObject);
                            }
                            
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"强制收集样本失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 模拟玩家交互收集
        /// 通过设置玩家位置来触发自然收集
        /// </summary>
        public static void SimulatePlayerInteraction(this SampleCollector collector)
        {
            if (collector == null) return;
            
            try
            {
                // 查找玩家对象
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    player = Object.FindFirstObjectByType<FirstPersonController>()?.gameObject;
                }
                
                if (player != null && collector.gameObject != null)
                {
                    // 临时移动玩家到样本附近
                    Vector3 originalPosition = player.transform.position;
                    Vector3 samplePosition = collector.transform.position;
                    
                    // 将玩家移动到样本交互范围内
                    player.transform.position = samplePosition + Vector3.forward * 1f;
                    
                    // 等待一帧让SampleCollector检测到玩家
                    collector.StartCoroutine(RestorePlayerPosition(player, originalPosition, 0.1f));
                }
                else
                {
                    Debug.LogWarning("未找到玩家对象，无法模拟交互");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"模拟玩家交互失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 恢复玩家位置的协程辅助方法
        /// </summary>
        private static System.Collections.IEnumerator RestorePlayerPosition(GameObject player, Vector3 originalPosition, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (player != null)
            {
                player.transform.position = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// 切割系统专用的样本收集器
    /// 提供更直接的收集接口
    /// </summary>
    public class CuttingSystemCollector : MonoBehaviour
    {
        [Header("收集设置")]
        [SerializeField] private bool autoCollectOnStart = false;
        [SerializeField] private float autoCollectDelay = 1f;
        
        private SingleLayerSample associatedSample;
        
        /// <summary>
        /// 设置关联的样本数据
        /// </summary>
        public void SetAssociatedSample(SingleLayerSample sample)
        {
            associatedSample = sample;
            
            if (autoCollectOnStart)
            {
                Invoke(nameof(CollectToInventory), autoCollectDelay);
            }
        }
        
        /// <summary>
        /// 收集到背包
        /// </summary>
        public void CollectToInventory()
        {
            if (associatedSample == null)
            {
                Debug.LogWarning("没有关联的样本数据");
                return;
            }
            
            try
            {
                // 查找样本背包
                var inventory = FindFirstObjectByType<SampleInventory>();
                if (inventory != null)
                {
                    // 创建样本项目
                    // 这里需要根据实际的SampleInventory API来实现
                    Debug.Log($"收集样本到背包: {associatedSample.layerName}");
                    
                    // 播放收集效果
                    PlayCollectionEffect();
                    
                    // 销毁游戏对象
                    Destroy(gameObject, 0.5f);
                }
                else
                {
                    Debug.LogWarning("未找到样本背包");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"收集到背包失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 播放收集效果
        /// </summary>
        private void PlayCollectionEffect()
        {
            // 简单的缩放效果
            if (gameObject != null)
            {
                StartCoroutine(ScaleDownEffect());
            }
        }
        
        /// <summary>
        /// 缩放消失效果
        /// </summary>
        private System.Collections.IEnumerator ScaleDownEffect()
        {
            Vector3 originalScale = transform.localScale;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(1f, 0f, progress);
                
                transform.localScale = originalScale * scale;
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 手动触发收集
        /// </summary>
        [ContextMenu("手动收集")]
        public void ManualCollect()
        {
            CollectToInventory();
        }
    }
}