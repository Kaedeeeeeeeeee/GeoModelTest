using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 玩家持久化数据管理器 - 在场景切换时保存和恢复玩家状态
/// </summary>
public class PlayerPersistentData : MonoBehaviour
{
    [Header("数据持久化")]
    public bool enableDataPersistence = true;
    
    // 玩家数据
    private Vector3 playerPosition;
    private Quaternion playerRotation;
    private string currentEquippedTool;
    
    // 场景特定数据
    private Dictionary<string, SceneData> sceneDataMap = new Dictionary<string, SceneData>();
    
    /// <summary>
    /// 保存当前场景数据
    /// </summary>
    public void SaveCurrentSceneData(string sceneName)
    {
        if (!enableDataPersistence) return;
        
        Debug.Log($"保存场景数据: {sceneName}");
        
        SceneData sceneData = new SceneData();
        
        // 保存玩家位置和旋转
        SavePlayerTransform(sceneData);
        
        // 保存当前装备的工具
        SaveEquippedTool(sceneData);
        
        // 不保存样本数据 - 场景切换后样本自然消失
        
        // 保存到字典
        sceneDataMap[sceneName] = sceneData;
        
        Debug.Log($"场景数据保存完成: {sceneName}");
    }
    
    /// <summary>
    /// 恢复场景数据
    /// </summary>
    public void RestoreSceneData(string sceneName)
    {
        if (!enableDataPersistence) return;
        
        Debug.Log($"恢复场景数据: {sceneName}");
        
        if (sceneDataMap.TryGetValue(sceneName, out SceneData sceneData))
        {
            // 恢复玩家位置和旋转
            RestorePlayerTransform(sceneData);
            
            // 恢复装备的工具
            RestoreEquippedTool(sceneData);
            
            // 不恢复样本数据 - 每个场景重新开始
            
            Debug.Log($"场景数据恢复完成: {sceneName}");
        }
        else
        {
            Debug.Log($"场景 {sceneName} 没有保存的数据，使用默认状态");
            SetDefaultSceneState(sceneName);
        }
    }
    
    /// <summary>
    /// 保存玩家位置和旋转
    /// </summary>
    void SavePlayerTransform(SceneData sceneData)
    {
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            sceneData.playerPosition = player.transform.position;
            sceneData.playerRotation = player.transform.rotation;
            Debug.Log($"保存玩家位置: {sceneData.playerPosition}");
        }
    }
    
    /// <summary>
    /// 恢复玩家位置和旋转
    /// </summary>
    void RestorePlayerTransform(SceneData sceneData)
    {
        StartCoroutine(RestorePlayerTransformCoroutine(sceneData));
    }
    
    IEnumerator RestorePlayerTransformCoroutine(SceneData sceneData)
    {
        // 等待玩家对象初始化
        yield return new WaitForSeconds(0.1f);
        
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            player.transform.position = sceneData.playerPosition;
            player.transform.rotation = sceneData.playerRotation;
            Debug.Log($"恢复玩家位置: {sceneData.playerPosition}");
            
            // 确保玩家对象和摄像机都激活
            player.gameObject.SetActive(true);
            
            // 查找并激活摄像机
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                Debug.Log("玩家摄像机已激活");
            }
            else
            {
                Debug.LogWarning("未找到玩家摄像机");
            }
        }
        else
        {
            Debug.LogWarning("未找到玩家对象，尝试设置主摄像机");
            EnsureCameraExists(sceneData.playerPosition, sceneData.playerRotation);
        }
    }
    
    /// <summary>
    /// 保存当前装备的工具
    /// </summary>
    void SaveEquippedTool(SceneData sceneData)
    {
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            CollectionTool currentTool = toolManager.GetCurrentTool();
            if (currentTool != null)
            {
                sceneData.equippedToolID = currentTool.toolID;
                Debug.Log($"保存装备工具: {sceneData.equippedToolID}");
            }
        }
    }
    
    /// <summary>
    /// 恢复装备的工具
    /// </summary>
    void RestoreEquippedTool(SceneData sceneData)
    {
        if (string.IsNullOrEmpty(sceneData.equippedToolID)) return;
        
        StartCoroutine(RestoreEquippedToolCoroutine(sceneData));
    }
    
    IEnumerator RestoreEquippedToolCoroutine(SceneData sceneData)
    {
        // 等待工具管理器初始化
        yield return new WaitForSeconds(0.2f);
        
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null && toolManager.availableTools != null)
        {
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && tool.toolID == sceneData.equippedToolID)
                {
                    toolManager.EquipTool(tool);
                    Debug.Log($"恢复装备工具: {sceneData.equippedToolID}");
                    break;
                }
            }
        }
    }
    
    
    /// <summary>
    /// 设置默认场景状态
    /// </summary>
    void SetDefaultSceneState(string sceneName)
    {
        // 设置默认玩家位置
        Vector3 defaultPosition = Vector3.zero;
        Quaternion defaultRotation = Quaternion.identity;
        
        // 根据场景设置不同的默认位置
        switch (sceneName)
        {
            case "MainScene":
                defaultPosition = new Vector3(0, 1, 0);
                break;
            case "Laboratory Scene":
                defaultPosition = new Vector3(0, 1, 0);
                break;
        }
        
        StartCoroutine(SetDefaultPlayerPosition(defaultPosition, defaultRotation));
    }
    
    IEnumerator SetDefaultPlayerPosition(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(0.1f);
        
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            player.transform.position = position;
            player.transform.rotation = rotation;
            Debug.Log($"设置默认玩家位置: {position}");
            
            // 确保玩家对象和摄像机都激活
            player.gameObject.SetActive(true);
            
            // 查找并激活摄像机
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                Debug.Log("玩家摄像机已激活");
            }
            else
            {
                Debug.LogWarning("未找到玩家摄像机");
            }
        }
        else
        {
            Debug.LogWarning($"未找到玩家对象，尝试查找主摄像机");
            EnsureCameraExists(position, rotation);
        }
    }
    
    /// <summary>
    /// 确保场景中有可用的摄像机
    /// </summary>
    void EnsureCameraExists(Vector3 position, Quaternion rotation)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // 查找任何摄像机
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
                mainCamera.tag = "MainCamera";
                Debug.Log($"找到摄像机并设为主摄像机: {mainCamera.name}");
            }
        }
        
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.transform.position = position + Vector3.up * 1.6f; // 摄像机高度
            mainCamera.transform.rotation = rotation;
            Debug.Log($"摄像机位置已设置: {mainCamera.transform.position}");
        }
        else
        {
            Debug.LogError("场景中没有找到任何摄像机！");
            CreateEmergencyCamera(position, rotation);
        }
    }
    
    /// <summary>
    /// 创建紧急摄像机
    /// </summary>
    void CreateEmergencyCamera(Vector3 position, Quaternion rotation)
    {
        GameObject cameraObj = new GameObject("Emergency Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        
        cameraObj.transform.position = position + Vector3.up * 1.6f;
        cameraObj.transform.rotation = rotation;
        
        // 添加音频监听器
        cameraObj.AddComponent<AudioListener>();
        
        Debug.Log("创建紧急摄像机完成");
    }
    
    /// <summary>
    /// 清除场景数据
    /// </summary>
    public void ClearSceneData(string sceneName)
    {
        if (sceneDataMap.ContainsKey(sceneName))
        {
            sceneDataMap.Remove(sceneName);
            Debug.Log($"清除场景数据: {sceneName}");
        }
    }
    
    /// <summary>
    /// 清除所有场景数据
    /// </summary>
    public void ClearAllSceneData()
    {
        sceneDataMap.Clear();
        Debug.Log("清除所有场景数据");
    }
}

/// <summary>
/// 场景数据结构 - 简化版，只保存玩家状态
/// </summary>
[System.Serializable]
public class SceneData
{
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string equippedToolID;
    
    public SceneData()
    {
        playerPosition = Vector3.zero;
        playerRotation = Quaternion.identity;
        equippedToolID = "";
    }
}