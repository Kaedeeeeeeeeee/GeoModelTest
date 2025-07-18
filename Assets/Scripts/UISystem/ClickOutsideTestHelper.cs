using UnityEngine;

/// <summary>
/// 点击外部关闭功能测试助手
/// </summary>
public class ClickOutsideTestHelper : MonoBehaviour
{
    /// <summary>
    /// 测试点击外部关闭功能
    /// </summary>
    [ContextMenu("测试点击外部关闭功能")]
    public void TestClickOutsideToClose()
    {
        Debug.Log("=== 点击外部关闭功能测试 ===");
        
        // 查找InventoryUI系统
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("未找到InventoryUI系统！");
            return;
        }
        
        Debug.Log("找到InventoryUI系统");
        
        // 获取样本背包系统
        SampleInventory sampleInventory = SampleInventory.Instance;
        if (sampleInventory == null)
        {
            Debug.LogError("未找到SampleInventory系统！");
            return;
        }
        
        var samples = sampleInventory.GetInventorySamples();
        if (samples.Count == 0)
        {
            Debug.LogError("背包中没有样本可供测试！");
            return;
        }
        
        var testSample = samples[0];
        Debug.Log($"测试样本: {testSample.displayName}");
        
        // 使用反射调用ShowSampleDetail方法
        var method = inventoryUI.GetType().GetMethod("ShowSampleDetail", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            try
            {
                method.Invoke(inventoryUI, new object[] { testSample });
                Debug.Log("✅ 成功显示样本详情");
                Debug.Log("现在可以尝试点击详情面板外部的半透明背景区域来关闭面板");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"调用ShowSampleDetail失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("未找到ShowSampleDetail方法！");
        }
    }
    
    /// <summary>
    /// 检查背景覆盖层状态
    /// </summary>
    [ContextMenu("检查背景覆盖层状态")]
    public void CheckOverlayStatus()
    {
        Debug.Log("=== 检查背景覆盖层状态 ===");
        
        // 查找背景覆盖层
        GameObject overlay = GameObject.Find("DetailBackgroundOverlay");
        if (overlay != null)
        {
            Debug.Log($"找到背景覆盖层: {overlay.name}");
            Debug.Log($"覆盖层是否激活: {overlay.activeInHierarchy}");
            
            // 检查Button组件
            var button = overlay.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                Debug.Log($"覆盖层Button组件存在，监听器数量: {button.onClick.GetPersistentEventCount()}");
            }
            else
            {
                Debug.LogWarning("覆盖层没有Button组件！");
            }
        }
        else
        {
            Debug.LogWarning("未找到背景覆盖层！");
        }
        
        // 检查详情面板
        GameObject detailPanel = GameObject.Find("DetailPanel");
        if (detailPanel != null)
        {
            Debug.Log($"找到详情面板: {detailPanel.name}");
            Debug.Log($"详情面板是否激活: {detailPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("未找到详情面板！");
        }
    }
}