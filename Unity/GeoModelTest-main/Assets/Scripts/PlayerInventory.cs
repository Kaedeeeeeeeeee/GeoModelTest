using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("库存设置")]
    public int maxSamples = 20;
    public KeyCode inventoryKey = KeyCode.I;
    
    [Header("UI设置")]
    public bool showInventoryOnScreen = true;
    public Vector2 inventoryUIPosition = new Vector2(10, 10);
    
    private List<GeologicalSampleData> collectedSamples;
    private bool isInventoryOpen = false;
    
    void Start()
    {
        collectedSamples = new List<GeologicalSampleData>();
        Debug.Log("玩家库存系统初始化完成");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }
    }
    
    public bool AddSample(GeologicalSampleData sampleData)
    {
        if (collectedSamples.Count >= maxSamples)
        {
            Debug.LogWarning("库存已满，无法添加更多样本！");
            return false;
        }
        
        collectedSamples.Add(sampleData);
        Debug.Log($"样本已添加到库存：{sampleData.sampleID} (总数: {collectedSamples.Count})");
        
        return true;
    }
    
    public void RemoveSample(GeologicalSampleData sampleData)
    {
        if (collectedSamples.Remove(sampleData))
        {
            Debug.Log($"样本已从库存移除：{sampleData.sampleID}");
        }
    }
    
    public GeologicalSampleData[] GetAllSamples()
    {
        return collectedSamples.ToArray();
    }
    
    public int GetSampleCount()
    {
        return collectedSamples.Count;
    }
    
    public bool IsFull()
    {
        return collectedSamples.Count >= maxSamples;
    }
    
    void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        Debug.Log($"库存界面 {(isInventoryOpen ? "打开" : "关闭")}");
        
        if (isInventoryOpen)
        {
            ShowInventorySummary();
        }
    }
    
    void ShowInventorySummary()
    {
        Debug.Log("=== 库存摘要 ===");
        Debug.Log($"样本总数: {collectedSamples.Count}/{maxSamples}");
        
        Dictionary<LayerType, int> layerCounts = new Dictionary<LayerType, int>();
        
        foreach (var sample in collectedSamples)
        {
            foreach (var layerStat in sample.layerStats)
            {
                if (!layerCounts.ContainsKey(layerStat.layerType))
                {
                    layerCounts[layerStat.layerType] = 0;
                }
                layerCounts[layerStat.layerType]++;
            }
        }
        
        Debug.Log("地层类型统计:");
        foreach (var kvp in layerCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} 个样本");
        }
    }
    
    void OnGUI()
    {
        if (!showInventoryOnScreen) return;
        
        // 显示库存状态
        GUI.Label(new Rect(inventoryUIPosition.x, inventoryUIPosition.y, 200, 30), 
                 $"库存: {collectedSamples.Count}/{maxSamples} 样本");
        
        GUI.Label(new Rect(inventoryUIPosition.x, inventoryUIPosition.y + 25, 200, 30), 
                 $"按 {inventoryKey} 查看详情");
        
        // 如果库存界面打开，显示详细信息
        if (isInventoryOpen)
        {
            DrawInventoryWindow();
        }
    }
    
    void DrawInventoryWindow()
    {
        float windowWidth = 400;
        float windowHeight = 300;
        float windowX = Screen.width - windowWidth - 10;
        float windowY = 10;
        
        Rect windowRect = new Rect(windowX, windowY, windowWidth, windowHeight);
        GUI.Window(0, windowRect, InventoryWindowFunction, "地质样本库存");
    }
    
    void InventoryWindowFunction(int windowID)
    {
        float yPos = 30;
        float lineHeight = 20;
        
        GUI.Label(new Rect(10, yPos, 380, lineHeight), $"样本总数: {collectedSamples.Count}/{maxSamples}");
        yPos += lineHeight + 5;
        
        if (collectedSamples.Count == 0)
        {
            GUI.Label(new Rect(10, yPos, 380, lineHeight), "暂无样本");
        }
        else
        {
            // 滚动视图显示样本列表
            for (int i = 0; i < collectedSamples.Count && yPos < 250; i++)
            {
                GeologicalSampleData sample = collectedSamples[i];
                string sampleInfo = $"{i + 1}. ID: {sample.sampleID.Substring(0, 8)}... " +
                                   $"深度: {sample.drillingDepth:F1}m " +
                                   $"地层: {sample.layerStats.Length}种";
                
                GUI.Label(new Rect(10, yPos, 350, lineHeight), sampleInfo);
                
                if (GUI.Button(new Rect(360, yPos, 30, lineHeight), "X"))
                {
                    RemoveSample(sample);
                    break;
                }
                
                yPos += lineHeight;
            }
        }
        
        // 关闭按钮
        if (GUI.Button(new Rect(330, 270, 60, 25), "关闭"))
        {
            isInventoryOpen = false;
        }
    }
}