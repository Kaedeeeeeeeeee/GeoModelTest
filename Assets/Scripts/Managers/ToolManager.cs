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