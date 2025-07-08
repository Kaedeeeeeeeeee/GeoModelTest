using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class CollectionSystemSetupTool : EditorWindow
{
    [MenuItem("Tools/Collection System Setup")]
    public static void ShowWindow()
    {
        GetWindow<CollectionSystemSetupTool>("采集系统配置");
    }
    
    void OnGUI()
    {
        GUILayout.Label("采集道具系统一键配置工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("配置采集系统", GUILayout.Height(40)))
        {
            SetupCollectionSystem();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("此工具将配置完整的采集道具系统，包括UI圆盘、钻探工具和地层挖掘功能", MessageType.Info);
        
        EditorGUILayout.Space();
        GUILayout.Label("配置内容：", EditorStyles.boldLabel);
        GUILayout.Label("• 创建道具选择UI圆盘");
        GUILayout.Label("• 为Lily添加工具管理器");
        GUILayout.Label("• 创建钻探工具");
        GUILayout.Label("• 为Terrier配置地层挖掘系统");
        GUILayout.Label("• 配置UI输入控制");
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("清理采集系统组件", GUILayout.Height(30)))
        {
            CleanupCollectionSystem();
        }
    }
    
    void SetupCollectionSystem()
    {
        GameObject lily = GameObject.Find("Lily");
        GameObject terrier = GameObject.Find("Terrier");
        
        if (lily == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到名为'Lily'的游戏对象！", "确定");
            return;
        }
        
        if (terrier == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到名为'Terrier'的游戏对象！", "确定");
            return;
        }
        
        SetupUISystem();
        SetupToolManager(lily);
        SetupBoringTool(lily);
        SetupTerrainHoleSystem(terrier);
        SetupDebugger(lily);
        
        EditorUtility.DisplayDialog("完成", "采集系统配置完成！\\n\\n按住Tab键打开道具选择圆盘\\n按T键测试系统\\n按R键刷新工具列表", "确定");
    }
    
    void SetupUISystem()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        GameObject canvasObj;
        
        if (existingCanvas == null)
        {
            canvasObj = new GameObject("Collection UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = existingCanvas.gameObject;
        }
        
        InventoryUISystem inventoryUI = canvasObj.GetComponent<InventoryUISystem>();
        if (inventoryUI == null)
        {
            inventoryUI = canvasObj.AddComponent<InventoryUISystem>();
        }
        
        SetupWheelUI(canvasObj, inventoryUI);
        
        Debug.Log("UI系统配置完成！");
    }
    
    void SetupWheelUI(GameObject canvas, InventoryUISystem inventoryUI)
    {
        Transform existingWheel = canvas.transform.Find("ToolWheel");
        if (existingWheel != null)
        {
            DestroyImmediate(existingWheel.gameObject);
        }
        
        GameObject wheelObj = new GameObject("ToolWheel");
        wheelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform wheelRect = wheelObj.AddComponent<RectTransform>();
        wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
        wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        float initialSize = 600f;
        wheelRect.sizeDelta = new Vector2(initialSize, initialSize);
        
        Image wheelBG = wheelObj.AddComponent<Image>();
        wheelBG.color = new Color(0, 0, 0, 0.5f);
        wheelBG.sprite = CreateCircleSprite();
        
        for (int i = 0; i < 8; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(wheelObj.transform, false);
            
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            float slotSize = initialSize * 0.12f;
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            
            float angle = i * 45f * Mathf.Deg2Rad;
            float radius = initialSize * 0.3f;
            Vector2 slotPos = new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
            slotRect.anchoredPosition = slotPos;
            
            Image slotImage = slotObj.AddComponent<Image>();
            slotImage.color = Color.white;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(slotSize * 1.8f, slotSize * 0.5f);
            textRect.anchoredPosition = new Vector2(0, -slotSize * 0.8f);
            
            Text slotText = textObj.AddComponent<Text>();
            slotText.text = $"工具{i + 1}";
            slotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotText.fontSize = Mathf.RoundToInt(slotSize * 0.25f);
            slotText.alignment = TextAnchor.MiddleCenter;
            slotText.color = Color.white;
            
            if (inventoryUI.wheelSlots.Length > i)
            {
                inventoryUI.wheelSlots[i] = slotRect;
                inventoryUI.slotImages[i] = slotImage;
                inventoryUI.slotTexts[i] = slotText;
            }
        }
        
        inventoryUI.wheelUI = wheelObj;
        inventoryUI.wheelCenter = wheelRect;
        
        wheelObj.SetActive(false);
    }
    
    Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];
        
        Vector2 center = new Vector2(64, 64);
        float radius = 60f;
        
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 0.3f : 0f;
                pixels[y * 128 + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }
    
    void SetupToolManager(GameObject lily)
    {
        ToolManager toolManager = lily.GetComponent<ToolManager>();
        if (toolManager == null)
        {
            toolManager = lily.AddComponent<ToolManager>();
        }
        
        Transform toolHolder = lily.transform.Find("ToolHolder");
        if (toolHolder == null)
        {
            GameObject toolHolderObj = new GameObject("ToolHolder");
            toolHolderObj.transform.SetParent(lily.transform);
            toolHolderObj.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
            toolManager.toolHolder = toolHolderObj.transform;
        }
        
        Debug.Log("工具管理器配置完成！");
    }
    
    void SetupBoringTool(GameObject lily)
    {
        Transform toolHolder = lily.transform.Find("ToolHolder");
        if (toolHolder == null)
        {
            GameObject toolHolderObj = new GameObject("ToolHolder");
            toolHolderObj.transform.SetParent(lily.transform);
            toolHolderObj.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
            toolHolder = toolHolderObj.transform;
        }
        
        Transform existingTool = toolHolder.Find("BoringTool");
        if (existingTool != null)
        {
            DestroyImmediate(existingTool.gameObject);
        }
        
        GameObject boringToolObj = new GameObject("BoringTool");
        boringToolObj.transform.SetParent(toolHolder);
        boringToolObj.transform.localPosition = Vector3.zero;
        
        BoringTool boringTool = boringToolObj.AddComponent<BoringTool>();
        
        GameObject toolModel = CreateBoringToolModel();
        toolModel.transform.SetParent(boringToolObj.transform);
        toolModel.transform.localPosition = Vector3.zero;
        boringTool.toolModel = toolModel;
        
        Sprite toolIcon = CreateToolIcon();
        boringTool.toolIcon = toolIcon;
        boringTool.toolName = "钻探工具";
        
        Debug.Log($"创建钻探工具完成 - 名称: {boringTool.toolName}, 图标: {(toolIcon != null ? "已设置" : "未设置")}");
        
        ToolManager toolManager = lily.GetComponent<ToolManager>();
        if (toolManager != null)
        {
            System.Collections.Generic.List<CollectionTool> tools = new System.Collections.Generic.List<CollectionTool>();
            if (toolManager.availableTools != null)
            {
                tools.AddRange(toolManager.availableTools);
            }
            
            if (!tools.Contains(boringTool))
            {
                tools.Add(boringTool);
                toolManager.availableTools = tools.ToArray();
            }
        }
        
        Debug.Log("钻探工具创建完成！");
    }
    
    GameObject CreateBoringToolModel()
    {
        GameObject model = new GameObject("BoringToolModel");
        
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.SetParent(model.transform);
        handle.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
        handle.transform.localPosition = new Vector3(0, 0, 0);
        
        MeshRenderer handleRenderer = handle.GetComponent<MeshRenderer>();
        Material handleMaterial = new Material(Shader.Find("Standard"));
        handleMaterial.color = new Color(0.4f, 0.2f, 0.1f);
        handleRenderer.material = handleMaterial;
        
        GameObject drill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drill.transform.SetParent(model.transform);
        drill.transform.localScale = new Vector3(0.02f, 0.3f, 0.02f);
        drill.transform.localPosition = new Vector3(0, -0.8f, 0);
        
        MeshRenderer drillRenderer = drill.GetComponent<MeshRenderer>();
        Material drillMaterial = new Material(Shader.Find("Standard"));
        drillMaterial.color = Color.gray;
        drillMaterial.SetFloat("_Metallic", 0.8f);
        drillRenderer.material = drillMaterial;
        
        model.SetActive(false);
        return model;
    }
    
    Sprite CreateToolIcon()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                bool isDrill = (x >= 30 && x <= 34 && y >= 10 && y <= 50);
                bool isHandle = (x >= 28 && x <= 36 && y >= 45 && y <= 55);
                
                if (isDrill)
                    pixels[y * 64 + x] = Color.gray;
                else if (isHandle)
                    pixels[y * 64 + x] = new Color(0.4f, 0.2f, 0.1f, 1);
                else
                    pixels[y * 64 + x] = Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    
    void SetupTerrainHoleSystem(GameObject terrier)
    {
        TerrainHoleSystem[] existingSystems = terrier.GetComponentsInChildren<TerrainHoleSystem>();
        if (existingSystems.Length == 0)
        {
            MeshRenderer[] renderers = terrier.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                TerrainHoleSystem holeSystem = renderer.gameObject.GetComponent<TerrainHoleSystem>();
                if (holeSystem == null)
                {
                    renderer.gameObject.AddComponent<TerrainHoleSystem>();
                }
            }
        }
        
        Debug.Log("地层挖掘系统配置完成！");
    }
    
    void SetupDebugger(GameObject lily)
    {
        CollectionSystemDebugger debugger = lily.GetComponent<CollectionSystemDebugger>();
        if (debugger == null)
        {
            lily.AddComponent<CollectionSystemDebugger>();
            Debug.Log("添加了采集系统调试器 - 按T键测试系统");
        }
    }
    
    void CleanupCollectionSystem()
    {
        InventoryUISystem[] inventoryUIs = FindObjectsOfType<InventoryUISystem>();
        foreach (var ui in inventoryUIs)
        {
            if (ui.wheelUI != null)
            {
                DestroyImmediate(ui.wheelUI);
            }
            DestroyImmediate(ui);
        }
        
        ToolManager[] toolManagers = FindObjectsOfType<ToolManager>();
        foreach (var manager in toolManagers)
        {
            DestroyImmediate(manager);
        }
        
        BoringTool[] boringTools = FindObjectsOfType<BoringTool>();
        foreach (var tool in boringTools)
        {
            DestroyImmediate(tool.gameObject);
        }
        
        TerrainHoleSystem[] holeSystems = FindObjectsOfType<TerrainHoleSystem>();
        foreach (var system in holeSystems)
        {
            system.RemoveAllHoles();
            DestroyImmediate(system);
        }
        
        EditorUtility.DisplayDialog("完成", "采集系统组件已清理", "确定");
    }
}