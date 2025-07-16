using UnityEngine;
using System.Collections.Generic;

public class ToolManager : MonoBehaviour
{
    [Header("Tool Management")]
    public Transform toolHolder;
    public CollectionTool[] availableTools;
    
    private CollectionTool currentTool;
    private int currentToolIndex = -1;
    
    void Start()
    {
        if (toolHolder == null)
        {
            toolHolder = transform;
        }
        
        InitializeTools();
    }
    
    void InitializeTools()
    {
        foreach (var tool in availableTools)
        {
            if (tool != null)
            {
                tool.Unequip();
            }
        }
        
        // 自动创建场景切换器工具
        CreateSceneSwitcherTool();
    }
    
    /// <summary>
    /// 创建场景切换器工具
    /// </summary>
    void CreateSceneSwitcherTool()
    {
        // 检查是否已经存在场景切换器工具
        SceneSwitcherTool existingTool = FindFirstObjectByType<SceneSwitcherTool>();
        if (existingTool != null)
        {
            Debug.Log("场景切换器工具已存在，确保添加到工具管理器");
            AddTool(existingTool);
            return;
        }
        
        // 创建场景切换器工具
        GameObject toolObject = new GameObject("SceneSwitcherTool");
        SceneSwitcherTool sceneSwitcher = toolObject.AddComponent<SceneSwitcherTool>();
        
        // 加载用户的SceneSwitcher预制体
        LoadSceneSwitcherPrefab(sceneSwitcher);
        
        // 添加到工具管理器
        AddTool(sceneSwitcher);
        
        // 确保GameSceneManager存在
        var sceneManager = GameSceneManager.Instance;
        
        Debug.Log("场景切换器工具已自动创建并添加到Tab UI");
    }
    
    /// <summary>
    /// 加载场景切换器预制体
    /// </summary>
    void LoadSceneSwitcherPrefab(SceneSwitcherTool tool)
    {
        // 尝试加载用户的SceneSwitcher预制体
        string prefabPath = "Assets/Model/SceneSwitcher/SceneSwitcher";
        GameObject sceneSwitcherPrefab = Resources.Load<GameObject>("Model/SceneSwitcher/SceneSwitcher");
        
        // 如果Resources.Load失败，尝试直接路径加载
        if (sceneSwitcherPrefab == null)
        {
#if UNITY_EDITOR
            sceneSwitcherPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/SceneSwitcher/SceneSwitcher.prefab");
#endif
        }
        
        if (sceneSwitcherPrefab != null)
        {
            // 使用用户的预制体
            tool.switcherPrefab = sceneSwitcherPrefab;
            Debug.Log("✅ 成功加载用户的SceneSwitcher预制体");
        }
        else
        {
            // 如果加载失败，创建备用简单模型
            Debug.LogWarning("❌ 无法加载SceneSwitcher预制体，创建备用模型");
            CreateFallbackSwitcherPrefab(tool);
        }
    }
    
    /// <summary>
    /// 创建备用场景切换器预制体
    /// </summary>
    void CreateFallbackSwitcherPrefab(SceneSwitcherTool tool)
    {
        // 创建简单的备用模型
        GameObject fallbackSwitcher = new GameObject("FallbackSceneSwitcher");
        
        // 添加简单的立方体
        GameObject visualModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualModel.name = "SwitcherModel";
        visualModel.transform.SetParent(fallbackSwitcher.transform);
        visualModel.transform.localPosition = Vector3.zero;
        visualModel.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        
        // 设置材质
        Renderer renderer = visualModel.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material switcherMaterial = new Material(Shader.Find("Standard"));
            switcherMaterial.color = new Color(0.9f, 0.7f, 0.1f); // 更亮的金色
            switcherMaterial.SetFloat("_Metallic", 0.9f);
            switcherMaterial.SetFloat("_Glossiness", 0.95f);
            renderer.material = switcherMaterial;
        }
        
        // 移除碰撞器
        Collider collider = visualModel.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 设置工具的预制体
        tool.switcherPrefab = fallbackSwitcher;
        
        Debug.Log("备用场景切换器预制体创建完成");
    }
    
    public void EquipTool(CollectionTool tool)
    {
        if (currentTool != null)
        {
            currentTool.Unequip();
        }
        
        currentTool = tool;
        
        if (currentTool != null)
        {
            currentTool.Equip();
            
            for (int i = 0; i < availableTools.Length; i++)
            {
                if (availableTools[i] == tool)
                {
                    currentToolIndex = i;
                    break;
                }
            }
        }
        else
        {
            currentToolIndex = -1;
        }
    }
    
    public void EquipTool(int index)
    {
        if (index >= 0 && index < availableTools.Length)
        {
            EquipTool(availableTools[index]);
        }
    }
    
    public void UnequipCurrentTool()
    {
        if (currentTool != null)
        {
            currentTool.Unequip();
            currentTool = null;
            currentToolIndex = -1;
        }
    }
    
    public CollectionTool GetCurrentTool()
    {
        return currentTool;
    }
    
    public bool HasTool(CollectionTool tool)
    {
        foreach (var availableTool in availableTools)
        {
            if (availableTool == tool)
                return true;
        }
        return false;
    }
    
    public void AddTool(CollectionTool newTool)
    {
        List<CollectionTool> toolsList = new List<CollectionTool>(availableTools);
        
        if (!toolsList.Contains(newTool))
        {
            toolsList.Add(newTool);
            availableTools = toolsList.ToArray();
            
            var inventoryUI = FindFirstObjectByType<InventoryUISystem>();
            if (inventoryUI != null)
            {
                inventoryUI.AddTool(newTool);
            }
        }
    }
}