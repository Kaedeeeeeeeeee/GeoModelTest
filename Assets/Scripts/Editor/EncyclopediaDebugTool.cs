using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Encyclopedia;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;

public class EncyclopediaDebugTool : EditorWindow
{
    [MenuItem("Tools/图鉴系统/UI组件调试工具")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaDebugTool>("图鉴UI调试");
    }

    private void OnGUI()
    {
        GUILayout.Label("图鉴UI组件调试工具", EditorStyles.boldLabel);

        if (GUILayout.Button("检查图鉴UI组件状态"))
        {
            CheckEncyclopediaUIComponents();
        }

        if (GUILayout.Button("查找图鉴UI GameObject"))
        {
            FindEncyclopediaUIGameObject();
        }

        if (GUILayout.Button("检查Entry Item Prefab"))
        {
            CheckEntryItemPrefab();
        }

        if (GUILayout.Button("强制刷新图鉴列表"))
        {
            ForceRefreshEncyclopediaList();
        }

        if (GUILayout.Button("激活图鉴面板"))
        {
            ActivateEncyclopediaPanel();
        }

        if (GUILayout.Button("创建测试条目UI"))
        {
            CreateTestEntryUI();
        }

        if (GUILayout.Button("创建EntryItem预制体"))
        {
            CreateEntryItemPrefab();
        }

        if (GUILayout.Button("修复EncyclopediaData单例"))
        {
            FixEncyclopediaDataSingleton();
        }

        if (GUILayout.Button("修复UI布局问题"))
        {
            FixUILayoutIssues();
        }

        if (GUILayout.Button("修复条目显示样式"))
        {
            FixEntryDisplayStyle();
        }

        if (GUILayout.Button("创建测试可见条目"))
        {
            CreateVisibleTestEntry();
        }

        if (GUILayout.Button("修复滚动视图显示"))
        {
            FixScrollViewDisplay();
        }

        if (GUILayout.Button("修复Content宽度问题"))
        {
            FixContentWidth();
        }

        if (GUILayout.Button("强制修复所有条目大小"))
        {
            ForceFixAllEntrySize();
        }

        if (GUILayout.Button("删除红色测试框"))
        {
            RemoveRedTestBlock();
        }

        if (GUILayout.Button("修复地层选择按钮"))
        {
            FixLayerSelectionButtons();
        }

        if (GUILayout.Button("深度检查地层标签系统"))
        {
            DeepInspectLayerTabSystem();
        }

        if (GUILayout.Button("强制重建地层标签"))
        {
            ForceRebuildLayerTabs();
        }

        if (GUILayout.Button("安全重建地层标签"))
        {
            SafeRebuildLayerTabs();
        }

        if (GUILayout.Button("修复地层标签显示问题"))
        {
            FixLayerTabDisplayIssues();
        }

        if (GUILayout.Button("防止运行时重复创建地层标签"))
        {
            PreventRuntimeDuplication();
        }
    }

    private void CheckEncyclopediaUIComponents()
    {
        // 查找所有的EncyclopediaUI组件，包括禁用的
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();

        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到任何EncyclopediaUI组件！");
            return;
        }

        Debug.Log($"=== 找到 {allEncyclopediaUIs.Length} 个EncyclopediaUI组件 ===");

        foreach (var encyclopediaUI in allEncyclopediaUIs)
        {
            CheckSingleEncyclopediaUI(encyclopediaUI);
        }
    }

    private void CheckSingleEncyclopediaUI(EncyclopediaUI encyclopediaUI)
    {

        Debug.Log($"=== 图鉴UI组件检查: {encyclopediaUI.gameObject.name} ===");
        Debug.Log($"GameObject路径: {GetGameObjectPath(encyclopediaUI.gameObject)}");
        Debug.Log($"对象是否激活: {encyclopediaUI.gameObject.activeSelf}");
        Debug.Log($"组件是否启用: {encyclopediaUI.enabled}");

        // 通过反射检查私有字段
        var fields = typeof(EncyclopediaUI).GetFields(System.Reflection.BindingFlags.NonPublic |
                                                      System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject) ||
                field.FieldType == typeof(Transform) ||
                field.FieldType.IsSubclassOf(typeof(Component)))
            {
                var value = field.GetValue(encyclopediaUI);
                Debug.Log($"{field.Name}: {(value != null ? ((UnityEngine.Object)value).name : "NULL")}");
            }
        }
    }

    private void FindEncyclopediaUIGameObject()
    {
        var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        var encyclopediaObjects = allGameObjects.Where(go =>
            go.name.Contains("Encyclopedia") || go.name.Contains("图鉴")).ToArray();

        Debug.Log($"=== 找到 {encyclopediaObjects.Length} 个图鉴相关GameObject ===");
        foreach (var go in encyclopediaObjects)
        {
            Debug.Log($"名称: {go.name}, 路径: {GetGameObjectPath(go)}, 激活: {go.activeSelf}");

            // 检查子组件
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    Debug.Log($"  - 组件: {comp.GetType().Name}");
                }
            }
        }
    }

    private void CheckEntryItemPrefab()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 通过反射获取entryItemPrefab
        var prefabField = typeof(EncyclopediaUI).GetField("entryItemPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (prefabField != null)
        {
            var prefab = prefabField.GetValue(encyclopediaUI) as GameObject;

            if (prefab == null)
            {
                Debug.LogError("entryItemPrefab为null！请在Inspector中配置。");
                return;
            }

            Debug.Log("=== Entry Item Prefab 检查 ===");
            Debug.Log($"Prefab名称: {prefab.name}");

            // 检查预制体的子组件
            CheckPrefabChildren(prefab.transform, "");
        }
    }

    private void CheckPrefabChildren(Transform parent, string indent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            Debug.Log($"{indent}{child.name} - 组件: {string.Join(", ", child.GetComponents<Component>().Select(c => c.GetType().Name))}");

            if (child.childCount > 0)
            {
                CheckPrefabChildren(child, indent + "  ");
            }
        }
    }

    private void ForceRefreshEncyclopediaList()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 通过反射调用私有方法
        var refreshMethod = typeof(EncyclopediaUI).GetMethod("RefreshEntryList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (refreshMethod != null)
        {
            Debug.Log("强制调用RefreshEntryList...");
            refreshMethod.Invoke(encyclopediaUI, null);
        }
    }

    private void CreateTestEntryUI()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 获取容器
        var containerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (containerField != null)
        {
            var container = containerField.GetValue(encyclopediaUI) as Transform;

            if (container == null)
            {
                Debug.LogError("entryListContainer为null！");
                return;
            }

            // 创建简单的测试UI
            var testGO = new GameObject("TestEntry");
            testGO.transform.SetParent(container);
            testGO.transform.localScale = Vector3.one;

            // 添加Text组件显示测试文本
            var text = testGO.AddComponent<Text>();
            text.text = "测试条目 - 青葉山層-砂岩-石英";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.fontSize = 14;

            // 添加RectTransform设置
            var rectTransform = testGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(0, -30);
            rectTransform.sizeDelta = new Vector2(0, 30);

            Debug.Log("已创建测试条目UI");
        }
    }

    private string GetGameObjectPath(GameObject go)
    {
        var path = go.name;
        var parent = go.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void ActivateEncyclopediaPanel()
    {
        // 查找所有的EncyclopediaUI组件，包括禁用的
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();

        foreach (var encyclopediaUI in allEncyclopediaUIs)
        {
            if (encyclopediaUI.gameObject.name == "EncyclopediaPanel")
            {
                Debug.Log($"激活图鉴面板: {GetGameObjectPath(encyclopediaUI.gameObject)}");
                encyclopediaUI.gameObject.SetActive(true);

                // 强制刷新
                var refreshMethod = typeof(EncyclopediaUI).GetMethod("RefreshEntryList",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (refreshMethod != null)
                {
                    Debug.Log("调用RefreshEntryList...");
                    refreshMethod.Invoke(encyclopediaUI, null);
                }
                return;
            }
        }

        Debug.LogError("未找到EncyclopediaPanel！");
    }

    private void CreateEntryItemPrefab()
    {
        // 创建EntryItem预制体
        var entryItemGO = new GameObject("EntryItemPrefab");

        // 添加RectTransform
        var rectTransform = entryItemGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 60);

        // 添加背景Image
        var image = entryItemGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 添加Button组件
        var button = entryItemGO.AddComponent<Button>();

        // 创建NameText子对象
        var nameTextGO = new GameObject("NameText");
        nameTextGO.transform.SetParent(entryItemGO.transform);
        var nameText = nameTextGO.AddComponent<Text>();
        nameText.text = "Sample Entry Name";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;

        var nameTextRect = nameTextGO.GetComponent<RectTransform>();
        nameTextRect.anchorMin = new Vector2(0, 0);
        nameTextRect.anchorMax = new Vector2(1, 1);
        nameTextRect.offsetMin = new Vector2(10, 5);
        nameTextRect.offsetMax = new Vector2(-10, -5);

        // 创建IconImage子对象
        var iconImageGO = new GameObject("IconImage");
        iconImageGO.transform.SetParent(entryItemGO.transform);
        var iconImage = iconImageGO.AddComponent<Image>();
        iconImage.color = Color.white;

        var iconImageRect = iconImageGO.GetComponent<RectTransform>();
        iconImageRect.anchorMin = new Vector2(0, 0.5f);
        iconImageRect.anchorMax = new Vector2(0, 0.5f);
        iconImageRect.anchoredPosition = new Vector2(30, 0);
        iconImageRect.sizeDelta = new Vector2(40, 40);

        // 创建RarityText子对象
        var rarityTextGO = new GameObject("RarityText");
        rarityTextGO.transform.SetParent(entryItemGO.transform);
        var rarityText = rarityTextGO.AddComponent<Text>();
        rarityText.text = "常见";
        rarityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rarityText.fontSize = 12;
        rarityText.color = Color.white;
        rarityText.alignment = TextAnchor.MiddleRight;

        var rarityTextRect = rarityTextGO.GetComponent<RectTransform>();
        rarityTextRect.anchorMin = new Vector2(0.7f, 0);
        rarityTextRect.anchorMax = new Vector2(1, 1);
        rarityTextRect.offsetMin = new Vector2(0, 5);
        rarityTextRect.offsetMax = new Vector2(-10, -5);

        // 创建StatusImage子对象
        var statusImageGO = new GameObject("StatusImage");
        statusImageGO.transform.SetParent(entryItemGO.transform);
        var statusImage = statusImageGO.AddComponent<Image>();
        statusImage.color = Color.green;

        var statusImageRect = statusImageGO.GetComponent<RectTransform>();
        statusImageRect.anchorMin = new Vector2(1, 0.5f);
        statusImageRect.anchorMax = new Vector2(1, 0.5f);
        statusImageRect.anchoredPosition = new Vector2(-15, 0);
        statusImageRect.sizeDelta = new Vector2(10, 10);

        Debug.Log("EntryItem预制体已创建！");

        // 自动分配给EncyclopediaUI组件
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        foreach (var encyclopediaUI in allEncyclopediaUIs)
        {
            if (encyclopediaUI.gameObject.name == "EncyclopediaPanel")
            {
                // 使用反射设置entryItemPrefab字段
                var prefabField = typeof(EncyclopediaUI).GetField("entryItemPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (prefabField != null)
                {
                    prefabField.SetValue(encyclopediaUI, entryItemGO);
                    Debug.Log("EntryItemPrefab已分配给EncyclopediaUI组件！");
                }
                break;
            }
        }
    }

    private void FixEncyclopediaDataSingleton()
    {
        var encyclopediaData = FindObjectOfType<EncyclopediaData>();
        if (encyclopediaData == null)
        {
            Debug.LogError("未找到EncyclopediaData组件！");
            return;
        }

        Debug.Log($"找到EncyclopediaData组件: {encyclopediaData.name}");

        // 获取Instance属性的backing field (<Instance>k__BackingField)
        var instanceBackingField = typeof(EncyclopediaData).GetField("<Instance>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (instanceBackingField == null)
        {
            // 如果找不到backing field，尝试找字段名变体
            var fields = typeof(EncyclopediaData).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            foreach (var field in fields)
            {
                Debug.Log($"找到静态字段: {field.Name}, 类型: {field.FieldType.Name}");
                if (field.FieldType == typeof(EncyclopediaData))
                {
                    instanceBackingField = field;
                    break;
                }
            }
        }

        if (instanceBackingField != null)
        {
            instanceBackingField.SetValue(null, encyclopediaData);
            Debug.Log("已设置EncyclopediaData.Instance backing field");
        }
        else
        {
            Debug.LogWarning("无法找到Instance backing field");
        }

        // 手动调用LoadEncyclopediaData方法
        var loadMethod = typeof(EncyclopediaData).GetMethod("LoadEncyclopediaData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (loadMethod != null)
        {
            try
            {
                loadMethod.Invoke(encyclopediaData, null);
                Debug.Log("已调用LoadEncyclopediaData()");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"调用LoadEncyclopediaData时出现异常: {e.Message}");
            }
        }

        // 检查单例是否修复
        if (EncyclopediaData.Instance != null)
        {
            Debug.Log($"EncyclopediaData单例修复成功！组件: {EncyclopediaData.Instance.name}");

            // 检查数据是否加载
            if (EncyclopediaData.Instance.AllEntries != null)
            {
                Debug.Log($"数据条目数量: {EncyclopediaData.Instance.AllEntries.Count}");
                Debug.Log($"数据加载状态: {EncyclopediaData.Instance.IsDataLoaded}");
            }
            else
            {
                Debug.LogWarning("AllEntries为null");
            }
        }
        else
        {
            Debug.LogError("EncyclopediaData单例仍然为null！");
        }
    }

    private void FixUILayoutIssues()
    {
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取entryListContainer（Content）
        var containerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (containerField == null)
        {
            Debug.LogError("未找到entryListContainer字段！");
            return;
        }

        var container = containerField.GetValue(encyclopediaUI) as Transform;
        if (container == null)
        {
            Debug.LogError("entryListContainer为null！");
            return;
        }

        Debug.Log($"找到容器: {container.name}，子对象数量: {container.childCount}");

        // 确保容器有VerticalLayoutGroup
        var layoutGroup = container.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.Log("添加VerticalLayoutGroup到容器");
            layoutGroup = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
        }

        // 确保容器有ContentSizeFitter
        var sizeFitter = container.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            Debug.Log("添加ContentSizeFitter到容器");
            sizeFitter = container.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        // 修复所有子条目的RectTransform
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var rectTransform = child.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                // 设置为拉伸宽度，固定高度
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.sizeDelta = new Vector2(0, 60); // 宽度0（拉伸），高度60
                rectTransform.anchoredPosition = new Vector2(0, -i * 65f); // 垂直排列

                Debug.Log($"修复条目 {i}: {child.name}，位置: {rectTransform.anchoredPosition}");
            }

            // 确保条目可见
            child.gameObject.SetActive(true);
        }

        // 强制刷新布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());

        Debug.Log($"UI布局修复完成！容器子对象: {container.childCount}");
    }

    private void FixEntryDisplayStyle()
    {
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取entryListContainer（Content）
        var containerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var container = containerField?.GetValue(encyclopediaUI) as Transform;
        if (container == null)
        {
            Debug.LogError("entryListContainer为null！");
            return;
        }

        Debug.Log($"开始修复 {container.childCount} 个条目的显示样式...");

        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var rectTransform = child.GetComponent<RectTransform>();

            // 修复条目大小
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(0, 80); // 增加高度到80
            }

            // 修复背景颜色
            var image = child.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.3f, 0.3f, 0.3f, 1f); // 更明显的灰色背景
            }

            // 修复文本颜色和大小
            var nameText = child.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.color = Color.white; // 白色文字
                nameText.fontSize = 18; // 更大字体
                nameText.fontStyle = FontStyle.Bold; // 粗体
                nameText.alignment = TextAnchor.MiddleLeft; // 左对齐

                Debug.Log($"条目 {i}: {nameText.text}");
            }
            else
            {
                Debug.LogWarning($"条目 {i} 未找到NameText组件");
            }

            // 修复稀有度文字
            var rarityText = child.Find("RarityText")?.GetComponent<Text>();
            if (rarityText != null)
            {
                rarityText.color = Color.yellow;
                rarityText.fontSize = 14;
                rarityText.fontStyle = FontStyle.Bold;
            }

            // 修复状态指示器
            var statusImage = child.Find("StatusImage")?.GetComponent<Image>();
            if (statusImage != null)
            {
                statusImage.color = Color.red; // 红色表示未发现
            }
        }

        // 强制刷新布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());

        Debug.Log("条目显示样式修复完成！");
    }

    private void CreateVisibleTestEntry()
    {
        // 找到图鉴面板
        var encyclopediaPanel = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == "EncyclopediaPanel");

        if (encyclopediaPanel == null)
        {
            Debug.LogError("未找到EncyclopediaPanel GameObject！");
            return;
        }

        Debug.Log($"找到EncyclopediaPanel: {encyclopediaPanel.name}");

        // 直接在EncyclopediaPanel下创建一个大的测试UI
        var testEntry = new GameObject("大测试条目");
        testEntry.transform.SetParent(encyclopediaPanel.transform);

        // 设置RectTransform - 放在界面中央
        var rectTransform = testEntry.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.3f, 0.3f);
        rectTransform.anchorMax = new Vector2(0.7f, 0.7f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        // 添加明显的背景
        var image = testEntry.AddComponent<Image>();
        image.color = Color.red; // 红色背景，非常显眼

        // 创建子对象来放置文字
        var textObject = new GameObject("TestText");
        textObject.transform.SetParent(testEntry.transform);

        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // 添加大号文字
        var text = textObject.AddComponent<Text>();
        text.text = "✅ 图鉴面板工作正常！\n测试条目显示成功\n\n现在需要修复滚动视图\n让真实条目显示出来";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;

        // 确保在最上层显示
        testEntry.transform.SetAsLastSibling();

        Debug.Log("已创建大测试条目，应该在图鉴界面中央显示红色背景");
    }

    private void FixScrollViewDisplay()
    {
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取ScrollRect组件
        var scrollRectField = typeof(EncyclopediaUI).GetField("entryScrollRect",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var scrollRect = scrollRectField?.GetValue(encyclopediaUI) as ScrollRect;
        if (scrollRect == null)
        {
            Debug.LogError("未找到ScrollRect组件！");
            return;
        }

        Debug.Log($"找到ScrollRect: {scrollRect.name}");

        // 重置滚动位置到顶部
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;

        // 确保ScrollRect设置正确
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 10f;

        // 获取Content区域
        var content = scrollRect.content;
        if (content != null)
        {
            Debug.Log($"Content大小: {content.sizeDelta}, 位置: {content.anchoredPosition}");

            // 强制重新计算Content大小
            var contentSizeFitter = content.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                contentSizeFitter.SetLayoutVertical();
            }

            var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutVertical();
            }

            // 强制刷新布局
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Debug.Log($"刷新后Content大小: {content.sizeDelta}, 位置: {content.anchoredPosition}");
        }

        // 获取Viewport
        var viewport = scrollRect.viewport;
        if (viewport != null)
        {
            Debug.Log($"Viewport大小: {viewport.rect.size}");

            // 确保Viewport有Mask组件
            var mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = viewport.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                Debug.Log("添加了Mask组件到Viewport");
            }
        }

        Debug.Log("滚动视图修复完成！尝试滚动查看条目");
    }

    private void FixContentWidth()
    {
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取容器
        var containerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var container = containerField?.GetValue(encyclopediaUI) as Transform;
        if (container == null)
        {
            Debug.LogError("entryListContainer为null！");
            return;
        }

        var contentRect = container.GetComponent<RectTransform>();
        if (contentRect == null)
        {
            Debug.LogError("Content没有RectTransform组件！");
            return;
        }

        Debug.Log($"修复前Content: 大小={contentRect.sizeDelta}, 锚点Min={contentRect.anchorMin}, 锚点Max={contentRect.anchorMax}");

        // 修复Content的RectTransform设置
        contentRect.anchorMin = new Vector2(0, 1);  // 左上角锚点
        contentRect.anchorMax = new Vector2(1, 1);  // 右上角锚点
        contentRect.pivot = new Vector2(0.5f, 1f);  // 顶部中心为轴点

        // 设置正确的大小 - 宽度拉伸，高度根据内容自动计算
        contentRect.sizeDelta = new Vector2(0, 77 * 85f); // 宽度0（拉伸），高度=条目数×条目高度
        contentRect.anchoredPosition = new Vector2(0, 0);

        Debug.Log($"修复后Content: 大小={contentRect.sizeDelta}, 锚点Min={contentRect.anchorMin}, 锚点Max={contentRect.anchorMax}");

        // 修复所有子条目的宽度
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var childRect = child.GetComponent<RectTransform>();

            if (childRect != null)
            {
                // 设置条目为拉伸宽度
                childRect.anchorMin = new Vector2(0, 1);
                childRect.anchorMax = new Vector2(1, 1);
                childRect.sizeDelta = new Vector2(0, 80); // 宽度0（拉伸），高度80
                childRect.anchoredPosition = new Vector2(0, -i * 85f); // 垂直排列，间距85

                Debug.Log($"修复条目 {i}: 位置={childRect.anchoredPosition}, 大小={childRect.sizeDelta}");
            }
        }

        // 移除可能干扰的ContentSizeFitter
        var sizeFitter = contentRect.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // 强制刷新布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Debug.Log($"Content宽度修复完成！最终大小: {contentRect.sizeDelta}");
    }

    private void ForceFixAllEntrySize()
    {
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取容器
        var containerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var container = containerField?.GetValue(encyclopediaUI) as Transform;
        if (container == null)
        {
            Debug.LogError("entryListContainer为null！");
            return;
        }

        Debug.Log($"强制修复 {container.childCount} 个条目的大小...");

        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            var childRect = child.GetComponent<RectTransform>();

            if (childRect != null)
            {
                // 完全重设RectTransform
                childRect.anchorMin = new Vector2(0, 0);
                childRect.anchorMax = new Vector2(1, 0);
                childRect.pivot = new Vector2(0.5f, 0.5f);

                // 设置固定大小和位置
                childRect.sizeDelta = new Vector2(0, 80); // 宽度拉伸，高度80
                childRect.anchoredPosition = new Vector2(0, i * -85f); // 从顶部开始向下排列

                // 直接设置rect属性（更强制的方法）
                childRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, 1850);
                childRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, i * 85f, 80);

                Debug.Log($"强制修复条目 {i}: 最终rect={childRect.rect}");
            }

            // 确保条目激活
            child.gameObject.SetActive(true);
        }

        // 移除所有Layout组件，用手动布局
        var layoutGroup = container.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            DestroyImmediate(layoutGroup);
            Debug.Log("移除了LayoutGroup组件，使用手动布局");
        }

        var contentSizeFitter = container.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            DestroyImmediate(contentSizeFitter);
            Debug.Log("移除了ContentSizeFitter组件");
        }

        Debug.Log("强制修复完成！所有条目应该可见");
    }

    private void RemoveRedTestBlock()
    {
        Debug.Log("开始寻找并删除红色测试框...");

        // 查找所有名为 "大测试条目" 的GameObject
        var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        var testEntries = allGameObjects.Where(go => go.name == "大测试条目").ToArray();

        if (testEntries.Length == 0)
        {
            Debug.LogWarning("未找到名为 '大测试条目' 的红色测试框");
            return;
        }

        foreach (var testEntry in testEntries)
        {
            Debug.Log($"找到红色测试框: {GetGameObjectPath(testEntry)}");

            // 检查是否确实是红色的Image组件
            var image = testEntry.GetComponent<Image>();
            if (image != null && image.color == Color.red)
            {
                Debug.Log($"确认删除红色测试框: {testEntry.name}");
                DestroyImmediate(testEntry);
            }
            else
            {
                Debug.LogWarning($"GameObject '{testEntry.name}' 存在但不是红色背景，跳过删除");
            }
        }

        Debug.Log("红色测试框删除完成！");
    }

    private void FixLayerSelectionButtons()
    {
        Debug.Log("开始修复地层选择按钮显示...");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 通过反射检查和修复layerTabContainer
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (layerTabContainerField != null)
        {
            var layerTabContainer = layerTabContainerField.GetValue(encyclopediaUI) as Transform;

            if (layerTabContainer != null)
            {
                Debug.Log($"找到layerTabContainer: {GetGameObjectPath(layerTabContainer.gameObject)}");
                Debug.Log($"Container激活状态: {layerTabContainer.gameObject.activeSelf}");

                // 强制激活地层标签容器
                if (!layerTabContainer.gameObject.activeSelf)
                {
                    layerTabContainer.gameObject.SetActive(true);
                    Debug.Log("已激活layerTabContainer");
                }

                // 检查和激活其所有父对象
                Transform parent = layerTabContainer.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        parent.gameObject.SetActive(true);
                        Debug.Log($"激活父级对象: {parent.name}");
                    }
                    parent = parent.parent;
                }
            }
            else
            {
                Debug.LogWarning("layerTabContainer为null");
            }
        }

        if (layerTabsField != null)
        {
            var layerTabs = layerTabsField.GetValue(encyclopediaUI) as System.Collections.IList;

            if (layerTabs != null && layerTabs.Count > 0)
            {
                Debug.Log($"找到layerTabs列表，数量: {layerTabs.Count}");

                for (int i = 0; i < layerTabs.Count; i++)
                {
                    var tab = layerTabs[i] as Button;
                    if (tab != null)
                    {
                        Debug.Log($"地层标签 {i}: {tab.name}, 激活状态: {tab.gameObject.activeSelf}");

                        // 强制激活地层标签
                        if (!tab.gameObject.activeSelf)
                        {
                            tab.gameObject.SetActive(true);
                            Debug.Log($"    已激活地层标签: {tab.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"地层标签 {i} 为null");
                    }
                }
            }
            else
            {
                Debug.LogWarning("layerTabs列表为null或空，尝试重新初始化...");

                // 尝试调用SetupLayerTabs方法重新创建地层标签
                var setupLayerTabsMethod = typeof(EncyclopediaUI).GetMethod("SetupLayerTabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (setupLayerTabsMethod != null)
                {
                    try
                    {
                        Debug.Log("尝试调用SetupLayerTabs方法...");
                        setupLayerTabsMethod.Invoke(encyclopediaUI, null);
                        Debug.Log("SetupLayerTabs方法调用成功");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"调用SetupLayerTabs时出现异常: {e.Message}");
                    }
                }
            }
        }

        // 递归搜索所有可能的地层选择UI元素
        var encyclopediaPanel = encyclopediaUI.gameObject;
        var layerSelectionObjects = new List<GameObject>();
        SearchForLayerObjects(encyclopediaPanel.transform, layerSelectionObjects);

        if (layerSelectionObjects.Count > 0)
        {
            Debug.Log($"找到 {layerSelectionObjects.Count} 个可能的地层选择UI对象:");

            foreach (var obj in layerSelectionObjects)
            {
                Debug.Log($"  - {GetGameObjectPath(obj)}, 激活: {obj.activeSelf}");

                // 尝试激活这些对象
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    Debug.Log($"    已激活: {obj.name}");
                }
            }
        }

        Debug.Log("地层选择按钮修复完成！");
    }

    private void SearchForLayerObjects(Transform parent, List<GameObject> results)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var childName = child.name.ToLower();

            // 检查是否包含地层相关的关键词
            if (childName.Contains("layer") ||
                childName.Contains("地层") ||
                childName.Contains("tab") ||
                childName.Contains("button") && (childName.Contains("layer") || childName.Contains("地层")))
            {
                results.Add(child.gameObject);
            }

            // 递归搜索子对象
            if (child.childCount > 0)
            {
                SearchForLayerObjects(child, results);
            }
        }
    }

    private void DeepInspectLayerTabSystem()
    {
        Debug.Log("=== 深度检查地层标签系统 ===");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 检查EncyclopediaData
        Debug.Log("--- 检查EncyclopediaData ---");
        if (EncyclopediaData.Instance == null)
        {
            Debug.LogError("EncyclopediaData.Instance为null！");
            return;
        }

        Debug.Log($"数据加载状态: {EncyclopediaData.Instance.IsDataLoaded}");
        Debug.Log($"地层名称数量: {EncyclopediaData.Instance.LayerNames?.Count ?? 0}");

        if (EncyclopediaData.Instance.LayerNames != null)
        {
            for (int i = 0; i < EncyclopediaData.Instance.LayerNames.Count; i++)
            {
                Debug.Log($"地层 {i}: {EncyclopediaData.Instance.LayerNames[i]}");
            }
        }

        // 检查layerTabContainer
        Debug.Log("--- 检查layerTabContainer ---");
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabContainer = layerTabContainerField?.GetValue(encyclopediaUI) as Transform;
        if (layerTabContainer != null)
        {
            Debug.Log($"layerTabContainer: {GetGameObjectPath(layerTabContainer.gameObject)}");
            Debug.Log($"激活状态: {layerTabContainer.gameObject.activeSelf}");
            Debug.Log($"子对象数量: {layerTabContainer.childCount}");

            for (int i = 0; i < layerTabContainer.childCount; i++)
            {
                var child = layerTabContainer.GetChild(i);
                Debug.Log($"  子对象 {i}: {child.name}, 激活: {child.gameObject.activeSelf}");

                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    var text = child.GetComponentInChildren<Text>();
                    Debug.Log($"    按钮文本: {(text != null ? text.text : "无文本")}");
                }
            }
        }
        else
        {
            Debug.LogError("layerTabContainer为null！");
        }

        // 检查layerTabPrefab
        Debug.Log("--- 检查layerTabPrefab ---");
        var layerTabPrefabField = typeof(EncyclopediaUI).GetField("layerTabPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabPrefab = layerTabPrefabField?.GetValue(encyclopediaUI) as Button;
        if (layerTabPrefab != null)
        {
            Debug.Log($"layerTabPrefab: {layerTabPrefab.name}");
        }
        else
        {
            Debug.LogError("layerTabPrefab为null！这是地层标签无法创建的主要原因！");
        }

        // 检查layerTabs列表
        Debug.Log("--- 检查layerTabs列表 ---");
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabs = layerTabsField?.GetValue(encyclopediaUI) as System.Collections.IList;
        if (layerTabs != null)
        {
            Debug.Log($"layerTabs列表数量: {layerTabs.Count}");
        }
        else
        {
            Debug.LogWarning("layerTabs列表为null");
        }

        Debug.Log("=== 深度检查完成 ===");
    }

    private void ForceRebuildLayerTabs()
    {
        Debug.Log("=== 强制重建地层标签 ===");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 检查数据
        if (EncyclopediaData.Instance == null || !EncyclopediaData.Instance.IsDataLoaded)
        {
            Debug.LogError("EncyclopediaData未加载，无法创建地层标签");
            return;
        }

        // 获取必要的字段
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabPrefabField = typeof(EncyclopediaUI).GetField("layerTabPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabContainer = layerTabContainerField?.GetValue(encyclopediaUI) as Transform;
        var layerTabPrefab = layerTabPrefabField?.GetValue(encyclopediaUI) as Button;
        var layerTabs = layerTabsField?.GetValue(encyclopediaUI) as System.Collections.IList;

        if (layerTabContainer == null)
        {
            Debug.LogError("layerTabContainer为null！");
            return;
        }

        if (layerTabPrefab == null)
        {
            Debug.LogError("layerTabPrefab为null！需要在Inspector中设置预制体！");
            return;
        }

        // 清除现有标签
        Debug.Log("清除现有地层标签...");
        if (layerTabs != null)
        {
            for (int i = layerTabs.Count - 1; i >= 0; i--)
            {
                var tab = layerTabs[i] as Button;
                if (tab != null)
                {
                    DestroyImmediate(tab.gameObject);
                }
            }
            layerTabs.Clear();
        }

        // 清除容器中的所有子对象
        for (int i = layerTabContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(layerTabContainer.GetChild(i).gameObject);
        }

        // 重新创建地层标签
        Debug.Log("重新创建地层标签...");
        var newLayerTabs = new List<Button>();

        foreach (string layerName in EncyclopediaData.Instance.LayerNames)
        {
            Debug.Log($"创建地层标签: {layerName}");

            var tabButton = Instantiate(layerTabPrefab, layerTabContainer);
            tabButton.name = $"LayerTab_{layerName}";

            var textComponent = tabButton.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = layerName;
                Debug.Log($"  设置标签文本: {layerName}");
            }
            else
            {
                Debug.LogWarning($"  标签 {layerName} 没有Text组件！");
            }

            // 添加点击事件（使用反射调用私有方法）
            string layer = layerName; // 捕获局部变量
            tabButton.onClick.RemoveAllListeners();
            tabButton.onClick.AddListener(() => {
                var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (onLayerTabClickedMethod != null)
                {
                    onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { layer });
                }
            });

            newLayerTabs.Add(tabButton);
        }

        // 更新layerTabs字段
        if (layerTabsField != null)
        {
            layerTabsField.SetValue(encyclopediaUI, newLayerTabs);
        }

        Debug.Log($"地层标签重建完成！创建了 {newLayerTabs.Count} 个标签");

        // 激活第一个标签
        if (newLayerTabs.Count > 0)
        {
            var firstLayer = EncyclopediaData.Instance.LayerNames[0];
            Debug.Log($"选择第一个地层: {firstLayer}");

            var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (onLayerTabClickedMethod != null)
            {
                onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { firstLayer });
            }
        }

        Debug.Log("=== 强制重建完成 ===");
    }

    private void SafeRebuildLayerTabs()
    {
        Debug.Log("=== 安全重建地层标签 ===");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 检查数据
        if (EncyclopediaData.Instance == null || !EncyclopediaData.Instance.IsDataLoaded)
        {
            Debug.LogError("EncyclopediaData未加载，无法创建地层标签");
            return;
        }

        // 获取必要的字段
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabPrefabField = typeof(EncyclopediaUI).GetField("layerTabPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabContainer = layerTabContainerField?.GetValue(encyclopediaUI) as Transform;
        var layerTabPrefab = layerTabPrefabField?.GetValue(encyclopediaUI) as Button;
        var layerTabs = layerTabsField?.GetValue(encyclopediaUI) as System.Collections.IList;

        if (layerTabContainer == null)
        {
            Debug.LogError("layerTabContainer为null！");
            return;
        }

        // 检查prefab是否存在且有效
        if (layerTabPrefab == null)
        {
            Debug.LogError("layerTabPrefab为null！需要重新创建预制体...");

            // 尝试从容器中找到现有的预制体或创建新的
            if (layerTabContainer.childCount > 0)
            {
                var existingChild = layerTabContainer.GetChild(0);
                var existingButton = existingChild.GetComponent<Button>();

                if (existingButton != null)
                {
                    Debug.Log("使用容器中现有的按钮作为模板");
                    layerTabPrefab = existingButton;
                    layerTabPrefabField?.SetValue(encyclopediaUI, layerTabPrefab);
                }
                else
                {
                    Debug.LogError("容器中的对象不是Button，无法创建地层标签");
                    return;
                }
            }
            else
            {
                Debug.Log("创建简单的地层标签预制体...");
                layerTabPrefab = CreateSimpleLayerTabPrefab(layerTabContainer);
                layerTabPrefabField?.SetValue(encyclopediaUI, layerTabPrefab);
            }
        }

        // 验证prefab有效性
        if (layerTabPrefab == null)
        {
            Debug.LogError("无法获取有效的layerTabPrefab！");
            return;
        }

        // 安全清除现有标签（不销毁预制体本身）
        Debug.Log("安全清除现有地层标签...");
        if (layerTabs != null)
        {
            for (int i = layerTabs.Count - 1; i >= 0; i--)
            {
                var tab = layerTabs[i] as Button;
                if (tab != null && tab != layerTabPrefab) // 不要销毁预制体本身
                {
                    DestroyImmediate(tab.gameObject);
                }
            }
            layerTabs.Clear();
        }

        // 清除容器中除预制体外的其他子对象
        for (int i = layerTabContainer.childCount - 1; i >= 0; i--)
        {
            var child = layerTabContainer.GetChild(i);
            if (child != layerTabPrefab.transform) // 保留预制体
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 重新创建地层标签
        Debug.Log("重新创建地层标签...");
        var newLayerTabs = new List<Button>();

        // 首先将现有的预制体作为第一个标签使用
        if (layerTabPrefab.transform.parent == layerTabContainer)
        {
            var firstLayerName = EncyclopediaData.Instance.LayerNames[0];
            var textComponent = layerTabPrefab.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = firstLayerName;
            }
            layerTabPrefab.name = $"LayerTab_{firstLayerName}";

            // 设置点击事件
            layerTabPrefab.onClick.RemoveAllListeners();
            layerTabPrefab.onClick.AddListener(() => {
                var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (onLayerTabClickedMethod != null)
                {
                    onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { firstLayerName });
                }
            });

            newLayerTabs.Add(layerTabPrefab);
            Debug.Log($"设置第一个地层标签: {firstLayerName}");
        }

        // 创建剩余的地层标签
        for (int i = 1; i < EncyclopediaData.Instance.LayerNames.Count; i++)
        {
            string layerName = EncyclopediaData.Instance.LayerNames[i];
            Debug.Log($"创建地层标签: {layerName}");

            var tabButton = Instantiate(layerTabPrefab, layerTabContainer);
            tabButton.name = $"LayerTab_{layerName}";

            var textComponent = tabButton.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = layerName;
                Debug.Log($"  设置标签文本: {layerName}");
            }

            // 添加点击事件
            string layer = layerName; // 捕获局部变量
            tabButton.onClick.RemoveAllListeners();
            tabButton.onClick.AddListener(() => {
                var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (onLayerTabClickedMethod != null)
                {
                    onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { layer });
                }
            });

            newLayerTabs.Add(tabButton);
        }

        // 更新layerTabs字段
        if (layerTabsField != null)
        {
            layerTabsField.SetValue(encyclopediaUI, newLayerTabs);
        }

        Debug.Log($"安全重建完成！创建了 {newLayerTabs.Count} 个地层标签");

        // 激活第一个标签
        if (newLayerTabs.Count > 0)
        {
            var firstLayer = EncyclopediaData.Instance.LayerNames[0];
            Debug.Log($"选择第一个地层: {firstLayer}");

            var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (onLayerTabClickedMethod != null)
            {
                onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { firstLayer });
            }
        }

        Debug.Log("=== 安全重建完成 ===");
    }

    private Button CreateSimpleLayerTabPrefab(Transform parent)
    {
        Debug.Log("创建简单地层标签预制体...");

        var tabGO = new GameObject("LayerTabPrefab");
        tabGO.transform.SetParent(parent);

        var rectTransform = tabGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 40);

        var image = tabGO.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        var button = tabGO.AddComponent<Button>();

        // 创建文本子对象
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(tabGO.transform);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.text = "地层名称";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        Debug.Log("简单地层标签预制体创建完成");
        return button;
    }

    private void FixLayerTabDisplayIssues()
    {
        Debug.Log("=== 修复地层标签显示问题 ===");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取地层标签容器
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabContainer = layerTabContainerField?.GetValue(encyclopediaUI) as Transform;
        if (layerTabContainer == null)
        {
            Debug.LogError("layerTabContainer为null！");
            return;
        }

        Debug.Log($"地层容器路径: {GetGameObjectPath(layerTabContainer.gameObject)}");
        Debug.Log($"容器子对象数量: {layerTabContainer.childCount}");

        // 第1步：清除重复的地层标签
        Debug.Log("--- 清除重复的地层标签 ---");
        var uniqueNames = new HashSet<string>();
        var toRemove = new List<Transform>();

        for (int i = 0; i < layerTabContainer.childCount; i++)
        {
            var child = layerTabContainer.GetChild(i);
            var text = child.GetComponentInChildren<Text>();

            if (text != null)
            {
                string layerName = text.text;
                Debug.Log($"检查子对象 {i}: {child.name}, 文本: {layerName}");

                if (uniqueNames.Contains(layerName))
                {
                    Debug.Log($"  发现重复标签: {layerName}, 标记删除");
                    toRemove.Add(child);
                }
                else
                {
                    uniqueNames.Add(layerName);
                    Debug.Log($"  保留标签: {layerName}");
                }
            }
        }

        // 删除重复的标签
        foreach (var child in toRemove)
        {
            Debug.Log($"删除重复标签: {child.name}");
            DestroyImmediate(child.gameObject);
        }

        // 第2步：检查是否有"地层名称"标题
        Debug.Log("--- 检查标题设置 ---");
        bool hasTitle = false;
        for (int i = 0; i < layerTabContainer.childCount; i++)
        {
            var child = layerTabContainer.GetChild(i);
            var text = child.GetComponentInChildren<Text>();

            if (text != null && text.text == "地层名称")
            {
                hasTitle = true;
                Debug.Log("找到标题标签: 地层名称");
                break;
            }
        }

        // 如果没有标题，创建一个
        if (!hasTitle)
        {
            Debug.Log("创建标题标签...");
            CreateLayerTitleTab(layerTabContainer);
        }

        // 第3步：修复UI交互问题 - 检查Canvas设置
        Debug.Log("--- 修复UI交互问题 ---");
        var canvas = layerTabContainer.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"Canvas RenderMode: {canvas.renderMode}");
            Debug.Log($"Canvas SortingOrder: {canvas.sortingOrder}");

            // 确保Canvas设置正确
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("修复Canvas renderMode为ScreenSpaceOverlay");
            }

            // 检查GraphicRaycaster
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("添加GraphicRaycaster组件");
            }
        }

        // 第4步：确保所有地层标签都有正确的Button组件和事件
        Debug.Log("--- 修复按钮事件 ---");
        for (int i = 0; i < layerTabContainer.childCount; i++)
        {
            var child = layerTabContainer.GetChild(i);
            var button = child.GetComponent<Button>();
            var text = child.GetComponentInChildren<Text>();

            if (button != null && text != null && text.text != "地层名称")
            {
                string layerName = text.text;
                Debug.Log($"修复按钮事件: {layerName}");

                // 清除现有事件并重新添加
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log($"点击地层标签: {layerName}");

                    var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (onLayerTabClickedMethod != null)
                    {
                        onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { layerName });
                    }
                });

                // 确保按钮可交互
                button.interactable = true;
            }
        }

        Debug.Log($"=== 修复完成！剩余地层标签数量: {layerTabContainer.childCount} ===");
    }

    private void CreateLayerTitleTab(Transform parent)
    {
        var titleGO = new GameObject("LayerTitle");
        titleGO.transform.SetParent(parent);
        titleGO.transform.SetAsFirstSibling(); // 放在最前面

        var rectTransform = titleGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 35);

        var image = titleGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.6f, 1f); // 蓝色标题背景

        // 创建标题文本
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(titleGO.transform);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.text = "地层名称";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        Debug.Log("创建了地层标题");
    }

    private void PreventRuntimeDuplication()
    {
        Debug.Log("=== 防止运行时重复创建地层标签 ===");

        // 找到图鉴面板
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取layerTabs字段并预填充
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (layerTabsField == null || layerTabContainerField == null)
        {
            Debug.LogError("无法找到必要的字段！");
            return;
        }

        var layerTabContainer = layerTabContainerField.GetValue(encyclopediaUI) as Transform;
        if (layerTabContainer == null)
        {
            Debug.LogError("layerTabContainer为null！");
            return;
        }

        // 收集现有的地层标签按钮（排除标题）
        var existingTabs = new List<Button>();
        for (int i = 0; i < layerTabContainer.childCount; i++)
        {
            var child = layerTabContainer.GetChild(i);
            var button = child.GetComponent<Button>();
            var text = child.GetComponentInChildren<Text>();

            if (button != null && text != null && text.text != "地层名称")
            {
                existingTabs.Add(button);
                Debug.Log($"发现现有地层标签: {text.text}");
            }
        }

        if (existingTabs.Count > 0)
        {
            // 设置layerTabs字段，这样EncyclopediaUI就不会重新创建
            layerTabsField.SetValue(encyclopediaUI, existingTabs);
            Debug.Log($"已设置layerTabs字段，包含 {existingTabs.Count} 个标签，防止运行时重复创建");

            // 另外，我们需要修改EncyclopediaUI，让它检查现有标签
            Debug.Log("提示：如果重复问题仍然存在，需要修改EncyclopediaUI的SetupLayerTabs方法");
        }
        else
        {
            Debug.LogWarning("未找到现有的地层标签，可能需要先创建");
        }

        Debug.Log("=== 防止重复创建完成 ===");
    }

    [MenuItem("Tools/图鉴系统/测试地层标签防重复")]
    public static void TestLayerTabPrevention()
    {
        Debug.Log("=== 测试地层标签防重复功能 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取字段
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabs = layerTabsField.GetValue(encyclopediaUI) as List<Button>;
        var layerTabContainer = layerTabContainerField.GetValue(encyclopediaUI) as Transform;

        Debug.Log($"当前layerTabs数量: {layerTabs?.Count ?? 0}");
        Debug.Log($"layerTabContainer子物体数量: {layerTabContainer?.childCount ?? 0}");
        Debug.Log($"应用程序运行状态: {Application.isPlaying}");

        if (layerTabContainer != null)
        {
            Debug.Log("layerTabContainer子物体详情:");
            for (int i = 0; i < layerTabContainer.childCount; i++)
            {
                var child = layerTabContainer.GetChild(i);
                var text = child.GetComponentInChildren<Text>();
                Debug.Log($"  [{i}] {child.name} - Text: {text?.text ?? "无文本"}");
            }
        }

        Debug.Log("=== 测试完成 ===");
    }

    [MenuItem("Tools/图鉴系统/修复按钮点击事件")]
    public static void FixButtonClickEvents()
    {
        Debug.Log("=== 修复图鉴按钮点击事件 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取字段
        var layerTabsField = typeof(EncyclopediaUI).GetField("layerTabs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layerTabContainerField = typeof(EncyclopediaUI).GetField("layerTabContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var layerTabs = layerTabsField.GetValue(encyclopediaUI) as List<Button>;
        var layerTabContainer = layerTabContainerField.GetValue(encyclopediaUI) as Transform;

        Debug.Log($"修复前 - layerTabs数量: {layerTabs?.Count ?? 0}");

        // 如果layerTabs为空，收集现有按钮
        if (layerTabs.Count == 0 && layerTabContainer.childCount > 0)
        {
            Debug.Log("开始收集现有地层标签按钮...");

            // 获取OnLayerTabClicked方法
            var onLayerTabClickedMethod = typeof(EncyclopediaUI).GetMethod("OnLayerTabClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (onLayerTabClickedMethod == null)
            {
                Debug.LogError("无法找到OnLayerTabClicked方法！");
                return;
            }

            for (int i = 0; i < layerTabContainer.childCount; i++)
            {
                var child = layerTabContainer.GetChild(i);
                var button = child.GetComponent<Button>();
                var text = child.GetComponentInChildren<Text>();

                if (button != null && text != null && text.text != "地层名称")
                {
                    layerTabs.Add(button);
                    string layerName = text.text;

                    // 清除现有监听器
                    button.onClick.RemoveAllListeners();

                    // 添加新的点击事件
                    button.onClick.AddListener(() => {
                        Debug.Log($"地层标签被点击: {layerName}");
                        onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { layerName });
                    });

                    Debug.Log($"重新绑定地层标签点击事件: {layerName}");
                }
            }

            // 更新layerTabs字段
            layerTabsField.SetValue(encyclopediaUI, layerTabs);
            Debug.Log($"修复后 - layerTabs数量: {layerTabs.Count}");

            // 强制触发第一个地层的选择
            if (layerTabs.Count > 0)
            {
                var firstLayerName = layerTabs[0].GetComponentInChildren<Text>().text;
                Debug.Log($"默认选择第一个地层: {firstLayerName}");
                onLayerTabClickedMethod.Invoke(encyclopediaUI, new object[] { firstLayerName });
            }
        }
        else
        {
            Debug.Log("layerTabs已存在，检查点击事件...");

            // 检查现有按钮的点击事件
            for (int i = 0; i < layerTabs.Count; i++)
            {
                var button = layerTabs[i];
                if (button != null)
                {
                    var persistentCalls = button.onClick.GetPersistentEventCount();
                    Debug.Log($"按钮 {i} 的持久事件数量: {persistentCalls}");
                }
            }
        }

        Debug.Log("=== 按钮点击事件修复完成 ===");
    }

    [MenuItem("Tools/图鉴系统/完整修复图鉴系统")]
    public static void CompleteFixEncyclopediaSystem()
    {
        Debug.Log("=== 开始完整修复图鉴系统 ===");

        // 1. 修复单例问题
        Debug.Log("步骤1: 修复单例问题");
        RepairSingletons();

        // 2. 修复按钮点击事件
        Debug.Log("步骤2: 修复按钮点击事件");
        FixButtonClickEvents();

        // 3. 强制刷新显示
        Debug.Log("步骤3: 强制刷新显示");
        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                // 强制调用RefreshEntryList
                var refreshMethod = typeof(EncyclopediaUI).GetMethod("RefreshEntryList",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(ui, null);
                    Debug.Log("强制刷新条目列表完成");
                }
                break;
            }
        }

        Debug.Log("=== 图鉴系统完整修复完成！请测试地层标签和条目点击功能 ===");
    }

    [MenuItem("Tools/图鉴系统/检查条目列表容器问题")]
    public static void DiagnoseEntryListContainer()
    {
        Debug.Log("=== 检查条目列表容器问题 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取字段
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryItemsField = typeof(EncyclopediaUI).GetField("entryItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var entryListContainer = entryListContainerField.GetValue(encyclopediaUI) as Transform;
        var entryItems = entryItemsField.GetValue(encyclopediaUI) as List<GameObject>;

        Debug.Log($"entryListContainer: {entryListContainer?.name ?? "null"}");
        Debug.Log($"entryListContainer子物体数量: {entryListContainer?.childCount ?? 0}");
        Debug.Log($"entryItems列表数量: {entryItems?.Count ?? 0}");

        if (entryListContainer != null)
        {
            Debug.Log($"entryListContainer路径: {GetGameObjectPathStatic(entryListContainer.gameObject)}");
            Debug.Log($"entryListContainer是否激活: {entryListContainer.gameObject.activeInHierarchy}");

            // 检查子物体
            for (int i = 0; i < entryListContainer.childCount; i++)
            {
                var child = entryListContainer.GetChild(i);
                Debug.Log($"  子物体[{i}]: {child.name} - 激活: {child.gameObject.activeInHierarchy}");
            }

            // 检查RectTransform
            var rectTransform = entryListContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"entryListContainer RectTransform:");
                Debug.Log($"  位置: {rectTransform.anchoredPosition}");
                Debug.Log($"  大小: {rectTransform.sizeDelta}");
                Debug.Log($"  锚点: {rectTransform.anchorMin} - {rectTransform.anchorMax}");
            }

            // 检查LayoutGroup
            var layoutGroup = entryListContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                Debug.Log($"找到LayoutGroup: {layoutGroup.GetType().Name}");
            }

            // 检查ContentSizeFitter
            var contentSizeFitter = entryListContainer.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                Debug.Log($"找到ContentSizeFitter: horizontal={contentSizeFitter.horizontalFit}, vertical={contentSizeFitter.verticalFit}");
            }
        }

        Debug.Log("=== 检查完成 ===");
    }

    [MenuItem("Tools/图鉴系统/修复Content容器宽度问题")]
    public static void FixContentContainerWidth()
    {
        Debug.Log("=== 修复Content容器宽度问题 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        var rectTransform = entryListContainer.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("entryListContainer没有RectTransform组件！");
            return;
        }

        Debug.Log($"修复前 - 位置: {rectTransform.anchoredPosition}, 大小: {rectTransform.sizeDelta}");

        // 修复宽度问题
        // 方法1: 设置合适的宽度
        var newSizeDelta = rectTransform.sizeDelta;
        newSizeDelta.x = 800f; // 设置合适的宽度
        rectTransform.sizeDelta = newSizeDelta;

        Debug.Log($"修复后 - 位置: {rectTransform.anchoredPosition}, 大小: {rectTransform.sizeDelta}");

        // 方法2: 使用SetInsetAndSizeFromParentEdge强制设置尺寸
        if (rectTransform.parent != null)
        {
            var parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                Debug.Log($"父容器大小: {parentRect.rect.size}");

                // 强制设置为父容器的宽度
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, parentRect.rect.width);
                Debug.Log($"强制设置宽度后 - 大小: {rectTransform.sizeDelta}");
            }
        }

        // 方法3: 移除可能导致宽度为0的组件
        var contentSizeFitter = entryListContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            Debug.Log($"发现ContentSizeFitter: horizontal={contentSizeFitter.horizontalFit}");
            if (contentSizeFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                Debug.Log("已设置ContentSizeFitter.horizontalFit为Unconstrained");
            }
        }

        // 强制更新布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Debug.Log($"最终结果 - 位置: {rectTransform.anchoredPosition}, 大小: {rectTransform.sizeDelta}");
        Debug.Log("=== Content容器宽度修复完成 ===");
    }

    [MenuItem("Tools/图鉴系统/修复条目重叠问题")]
    public static void FixEntryOverlapIssue()
    {
        Debug.Log("=== 修复条目重叠问题 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"检查Content容器: {entryListContainer.name}");
        Debug.Log($"子物体数量: {entryListContainer.childCount}");

        // 检查现有布局组件
        var existingVerticalLayout = entryListContainer.GetComponent<VerticalLayoutGroup>();
        var existingHorizontalLayout = entryListContainer.GetComponent<HorizontalLayoutGroup>();
        var existingGridLayout = entryListContainer.GetComponent<GridLayoutGroup>();
        var existingContentSizeFitter = entryListContainer.GetComponent<ContentSizeFitter>();

        Debug.Log($"现有组件 - VerticalLayout: {existingVerticalLayout != null}, HorizontalLayout: {existingHorizontalLayout != null}, GridLayout: {existingGridLayout != null}, ContentSizeFitter: {existingContentSizeFitter != null}");

        // 移除冲突的布局组件
        if (existingHorizontalLayout != null)
        {
            UnityEngine.Object.DestroyImmediate(existingHorizontalLayout);
            Debug.Log("移除了HorizontalLayoutGroup");
        }
        if (existingGridLayout != null)
        {
            UnityEngine.Object.DestroyImmediate(existingGridLayout);
            Debug.Log("移除了GridLayoutGroup");
        }

        // 添加或配置VerticalLayoutGroup
        if (existingVerticalLayout == null)
        {
            var verticalLayout = entryListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            Debug.Log("添加了VerticalLayoutGroup");
        }
        else
        {
            Debug.Log("VerticalLayoutGroup已存在");
        }

        var verticalLayoutGroup = entryListContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            // 配置布局设置
            verticalLayoutGroup.spacing = 5f; // 条目间距
            verticalLayoutGroup.padding = new RectOffset(10, 10, 10, 10); // 内边距
            verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childScaleWidth = false;
            verticalLayoutGroup.childScaleHeight = false;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            Debug.Log("配置VerticalLayoutGroup完成");
        }

        // 配置或添加ContentSizeFitter
        if (existingContentSizeFitter == null)
        {
            entryListContainer.gameObject.AddComponent<ContentSizeFitter>();
            Debug.Log("添加了ContentSizeFitter");
        }

        var finalContentSizeFitter = entryListContainer.GetComponent<ContentSizeFitter>();
        if (finalContentSizeFitter != null)
        {
            finalContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            finalContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Debug.Log("配置ContentSizeFitter完成");
        }

        // 强制重建布局
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(entryListContainer.GetComponent<RectTransform>());

        var rectTransform = entryListContainer.GetComponent<RectTransform>();
        Debug.Log($"修复后 - 位置: {rectTransform.anchoredPosition}, 大小: {rectTransform.sizeDelta}");

        Debug.Log("=== 条目重叠问题修复完成 ===");
    }

    [MenuItem("Tools/图鉴系统/检查条目点击事件")]
    public static void DiagnoseEntryClickEvents()
    {
        Debug.Log("=== 检查条目点击事件 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"检查条目点击事件 - 容器: {entryListContainer.name}");
        Debug.Log($"子物体数量: {entryListContainer.childCount}");

        // 检查前几个条目的按钮组件和事件
        int checkCount = Math.Min(5, entryListContainer.childCount);
        for (int i = 0; i < checkCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            Debug.Log($"条目[{i}] {child.name}:");

            if (button != null)
            {
                Debug.Log($"  - 有Button组件: 是");
                Debug.Log($"  - 按钮激活状态: {button.enabled}");
                Debug.Log($"  - 按钮可交互: {button.interactable}");
                Debug.Log($"  - 持久事件数量: {button.onClick.GetPersistentEventCount()}");

                // 检查按钮的子组件
                var nameText = child.Find("NameText");
                var iconImage = child.Find("IconImage");
                Debug.Log($"  - NameText存在: {nameText != null}");
                Debug.Log($"  - IconImage存在: {iconImage != null}");

                if (nameText != null)
                {
                    var text = nameText.GetComponent<Text>();
                    Debug.Log($"  - 文本内容: {text?.text ?? "无"}");
                }
            }
            else
            {
                Debug.Log($"  - 有Button组件: 否");
            }
        }

        // 检查detailPanel是否存在
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField.GetValue(encyclopediaUI) as GameObject;

        Debug.Log($"详情面板 - 存在: {detailPanel != null}");
        if (detailPanel != null)
        {
            Debug.Log($"详情面板 - 激活: {detailPanel.activeInHierarchy}");
            Debug.Log($"详情面板 - 路径: {GetGameObjectPathStatic(detailPanel)}");
        }

        Debug.Log("=== 条目点击事件检查完成 ===");
    }

    [MenuItem("Tools/图鉴系统/修复条目点击事件")]
    public static void FixEntryClickEvents()
    {
        if (Application.isPlaying)
        {
            FixEntryClickEventsRuntime();
        }
        else
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
        }
    }

    private static void FixEntryClickEventsRuntime()
    {
        Debug.Log("=== 修复条目点击事件 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        EncyclopediaUI encyclopediaUI = null;

        foreach (var ui in allEncyclopediaUIs)
        {
            if (ui.gameObject.name == "EncyclopediaPanel")
            {
                encyclopediaUI = ui;
                break;
            }
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaPanel！");
            return;
        }

        // 获取必要的字段和方法
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryItemsField = typeof(EncyclopediaUI).GetField("entryItems",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var onEntryItemClickedMethod = typeof(EncyclopediaUI).GetMethod("OnEntryItemClicked",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var entryListContainer = entryListContainerField.GetValue(encyclopediaUI) as Transform;
        var entryItems = entryItemsField.GetValue(encyclopediaUI) as List<GameObject>;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        if (onEntryItemClickedMethod == null)
        {
            Debug.LogError("未找到OnEntryItemClicked方法！");
            return;
        }

        Debug.Log($"开始修复 {entryListContainer.childCount} 个条目的点击事件");

        // 检查EncyclopediaData实例
        if (EncyclopediaData.Instance == null)
        {
            Debug.LogError("EncyclopediaData.Instance为null，需要先初始化！");

            // 查找并强制初始化EncyclopediaData
            var dataComponent = FindObjectOfType<EncyclopediaData>();
            if (dataComponent != null)
            {
                Debug.Log("找到EncyclopediaData组件，尝试强制初始化...");
                if (dataComponent.gameObject.scene.name != null)
                {
                    // 使用反射调用Awake方法
                    var awakeMethod = typeof(EncyclopediaData).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                    awakeMethod?.Invoke(dataComponent, null);

                    // 等待一帧后重试
                    EditorApplication.delayCall += () => {
                        if (EncyclopediaData.Instance != null)
                        {
                            Debug.Log("EncyclopediaData初始化成功，重新执行点击事件修复");
                            FixEntryClickEvents();
                        }
                        else
                        {
                            Debug.LogError("强制初始化EncyclopediaData失败");
                        }
                    };
                    return;
                }
            }
            else
            {
                Debug.LogError("场景中未找到EncyclopediaData组件！");
                return;
            }
        }

        int fixedCount = 0;
        var allEntries = EncyclopediaData.Instance.AllEntries.Values.ToList();

        // 遍历所有条目，重新绑定点击事件
        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                // 清除现有事件
                button.onClick.RemoveAllListeners();

                // 尝试根据文本内容找到对应的EncyclopediaEntry
                var nameText = child.Find("NameText");
                if (nameText != null)
                {
                    var textComponent = nameText.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        string entryName = textComponent.text;

                        // 查找匹配的Entry
                        EncyclopediaEntry matchedEntry = null;
                        foreach (var entry in allEntries)
                        {
                            if (entry.GetFormattedDisplayName() == entryName)
                            {
                                matchedEntry = entry;
                                break;
                            }
                        }

                        if (matchedEntry != null)
                        {
                            // 绑定点击事件
                            button.onClick.AddListener(() => {
                                Debug.Log($"条目被点击: {matchedEntry.GetFormattedDisplayName()}");
                                onEntryItemClickedMethod.Invoke(encyclopediaUI, new object[] { matchedEntry });
                            });

                            fixedCount++;
                            Debug.Log($"修复条目[{i}]: {entryName}");
                        }
                        else
                        {
                            Debug.LogWarning($"无法找到匹配的Entry: {entryName}");
                        }
                    }
                }
            }
        }

        // 更新entryItems列表
        if (entryItems != null)
        {
            entryItems.Clear();
            for (int i = 0; i < entryListContainer.childCount; i++)
            {
                entryItems.Add(entryListContainer.GetChild(i).gameObject);
            }
            Debug.Log($"更新entryItems列表: {entryItems.Count} 个条目");
        }

        Debug.Log($"=== 条目点击事件修复完成！成功修复 {fixedCount} 个条目 ===");
    }

    [MenuItem("Tools/图鉴系统/测试条目点击事件")]
    public static void TestEntryClickEvents()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 测试条目点击事件 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var encyclopediaUI = allEncyclopediaUIs[0];

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"找到 {entryListContainer.childCount} 个条目进行测试");

        int testCount = 0;
        for (int i = 0; i < entryListContainer.childCount && testCount < 3; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                testCount++;
                Debug.Log($"测试第 {testCount} 个按钮: {child.name}");

                // 检查是否有onClick事件
                int eventCount = button.onClick.GetPersistentEventCount();
                Debug.Log($"  持久事件数: {eventCount}");

                if (eventCount > 0)
                {
                    for (int j = 0; j < eventCount; j++)
                    {
                        Debug.Log($"  事件 {j}: {button.onClick.GetPersistentMethodName(j)}");
                    }
                }

                // 检查运行时事件
                var field = typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var calls = field.GetValue(button.onClick);
                    var countProperty = calls.GetType().GetProperty("Count");
                    if (countProperty != null)
                    {
                        int runtimeCount = (int)countProperty.GetValue(calls);
                        Debug.Log($"  运行时事件数: {runtimeCount}");
                    }
                }

                // 模拟点击测试
                try
                {
                    button.onClick.Invoke();
                    Debug.Log($"  点击测试: 成功调用onClick事件");
                }
                catch (Exception e)
                {
                    Debug.LogError($"  点击测试失败: {e.Message}");
                }
            }
        }

        Debug.Log("=== 条目点击测试完成 ===");
    }

    [MenuItem("Tools/图鉴系统/实时点击调试")]
    public static void EnableRealTimeClickDebugging()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 启用实时点击调试 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var encyclopediaUI = allEncyclopediaUIs[0];

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"为 {entryListContainer.childCount} 个条目添加实时调试");

        // 为所有条目添加调试组件
        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                // 移除现有的调试组件
                var existingDebugger = child.GetComponent<ClickDebugger>();
                if (existingDebugger != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingDebugger);
                }

                // 添加新的调试组件
                var debugger = child.gameObject.AddComponent<ClickDebugger>();
                debugger.entryIndex = i;
                debugger.entryName = child.name;
                debugger.button = button;

                Debug.Log($"为条目[{i}] {child.name} 添加了点击调试器");
            }
        }

        Debug.Log("=== 实时点击调试已启用 ===");
        Debug.Log("现在尝试点击条目，将会看到详细的调试信息");
    }

    [MenuItem("Tools/图鉴系统/检查UI层级阻塞")]
    public static void CheckUILayerBlocking()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 检查UI层级阻塞 ===");

        var allCanvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {allCanvases.Length} 个Canvas:");

        foreach (var canvas in allCanvases.OrderByDescending(c => c.sortingOrder))
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  - sortingOrder: {canvas.sortingOrder}");
            Debug.Log($"  - renderMode: {canvas.renderMode}");
            Debug.Log($"  - enabled: {canvas.enabled}");
            Debug.Log($"  - activeInHierarchy: {canvas.gameObject.activeInHierarchy}");

            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"  - GraphicRaycaster: enabled={raycaster.enabled}");
            }
            else
            {
                Debug.Log($"  - GraphicRaycaster: 不存在");
            }

            // 检查是否包含EncyclopediaPanel
            var encyclopediaPanel = canvas.GetComponentInChildren<EncyclopediaUI>();
            if (encyclopediaPanel != null)
            {
                Debug.Log($"  - 包含EncyclopediaUI: {encyclopediaPanel.name}");
            }
        }

        // 检查EventSystem
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"EventSystem存在: {eventSystem.name}");
            Debug.Log($"  - enabled: {eventSystem.enabled}");
            Debug.Log($"  - currentSelectedGameObject: {eventSystem.currentSelectedGameObject?.name ?? "null"}");
        }
        else
        {
            Debug.LogError("未找到EventSystem！这可能是点击事件不工作的原因！");
        }

        Debug.Log("=== UI层级检查完成 ===");
    }

    [MenuItem("Tools/图鉴系统/修复图鉴Canvas层级")]
    public static void FixEncyclopediaCanvasLayer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 修复图鉴Canvas层级 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var encyclopediaUI = allEncyclopediaUIs[0];
        var encyclopediaCanvas = encyclopediaUI.GetComponentInParent<Canvas>();

        if (encyclopediaCanvas == null)
        {
            Debug.LogError("EncyclopediaUI没有找到父级Canvas！");
            return;
        }

        Debug.Log($"找到图鉴Canvas: {encyclopediaCanvas.name}");
        Debug.Log($"当前sortingOrder: {encyclopediaCanvas.sortingOrder}");

        // 获取最高的Canvas层级
        var allCanvases = FindObjectsOfType<Canvas>();
        int maxSortingOrder = allCanvases.Max(c => c.sortingOrder);

        Debug.Log($"当前最高Canvas层级: {maxSortingOrder}");

        // 将图鉴Canvas设置为最高层级 + 1
        int newSortingOrder = maxSortingOrder + 1;
        encyclopediaCanvas.sortingOrder = newSortingOrder;

        Debug.Log($"图鉴Canvas新层级: {newSortingOrder}");

        // 确保Canvas设置正确
        encyclopediaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 检查GraphicRaycaster
        var raycaster = encyclopediaCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = encyclopediaCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("添加了缺失的GraphicRaycaster");
        }

        raycaster.enabled = true;
        Debug.Log($"GraphicRaycaster状态: enabled={raycaster.enabled}");

        Debug.Log("=== 图鉴Canvas层级修复完成 ===");
        Debug.Log("请重新尝试点击条目");
    }

    [MenuItem("Tools/图鉴系统/检查缺失点击事件的条目")]
    public static void CheckMissingClickEvents()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 检查缺失点击事件的条目 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var encyclopediaUI = allEncyclopediaUIs[0];

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"检查 {entryListContainer.childCount} 个条目的点击事件状态");

        int missingEventCount = 0;
        int validEventCount = 0;

        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                // 检查运行时事件
                var field = typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
                int runtimeCount = 0;
                if (field != null)
                {
                    var calls = field.GetValue(button.onClick);
                    var countProperty = calls.GetType().GetProperty("Count");
                    if (countProperty != null)
                    {
                        runtimeCount = (int)countProperty.GetValue(calls);
                    }
                }

                int persistentCount = button.onClick.GetPersistentEventCount();

                if (runtimeCount == 0 && persistentCount == 0)
                {
                    missingEventCount++;
                    Debug.LogWarning($"条目[{i}] {child.name} 缺失点击事件 - button.enabled:{button.enabled}, interactable:{button.interactable}");

                    // 检查Button是否被正确配置
                    var rect = button.GetComponent<RectTransform>();
                    Debug.Log($"  - 位置: {rect.anchoredPosition}, 大小: {rect.sizeDelta}");
                    Debug.Log($"  - 可见: {button.gameObject.activeInHierarchy}");
                }
                else
                {
                    validEventCount++;
                    Debug.Log($"条目[{i}] {child.name} 点击事件正常 - runtime:{runtimeCount}, persistent:{persistentCount}");
                }
            }
            else
            {
                Debug.LogWarning($"条目[{i}] {child.name} 没有Button组件！");
            }
        }

        Debug.Log($"=== 检查完成 ===");
        Debug.Log($"正常条目: {validEventCount}, 缺失事件条目: {missingEventCount}");

        if (missingEventCount > 0)
        {
            Debug.LogWarning($"发现 {missingEventCount} 个条目缺失点击事件，建议重新运行修复工具");
        }
    }

    [MenuItem("Tools/图鉴系统/添加自动修复组件(永久)")]
    public static void AddPermanentFixerComponent()
    {
        Debug.Log("=== 添加永久自动修复组件 ===");

        // 在编辑模式下查找图鉴相关的GameObject
        EncyclopediaUI encyclopediaUI = null;

        if (Application.isPlaying)
        {
            encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        }
        else
        {
            // 编辑模式下从场景中查找
            encyclopediaUI = Resources.FindObjectsOfTypeAll<EncyclopediaUI>()
                .FirstOrDefault(ui => ui.gameObject.scene.name != null);
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！请确保图鉴系统已正确设置。");
            return;
        }

        var targetGameObject = encyclopediaUI.gameObject;

        // 检查是否已存在修复器组件
        var existingFixer = targetGameObject.GetComponent<EncyclopediaCanvasFixer>();
        if (existingFixer != null)
        {
            Debug.Log($"GameObject '{targetGameObject.name}' 已经有EncyclopediaCanvasFixer组件");

            // 询问是否重新配置
            if (EditorApplication.isPlaying)
            {
                Debug.Log("重新配置现有的修复器组件");
                existingFixer.ApplyFixes();
            }
            return;
        }

        // 添加修复器组件
        var fixer = targetGameObject.AddComponent<EncyclopediaCanvasFixer>();

        Debug.Log($"已添加EncyclopediaCanvasFixer组件到: {targetGameObject.name}");
        Debug.Log("该组件将在每次游戏启动时自动修复Canvas层级和点击事件问题");

        // 如果在运行时，立即应用修复
        if (Application.isPlaying)
        {
            Debug.Log("立即应用修复...");
            fixer.ApplyFixes();
        }
        else
        {
            // 在编辑模式下标记场景为已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetGameObject.scene);
            Debug.Log("请保存场景以使修改永久生效");
        }

        Debug.Log("=== 永久修复组件添加完成 ===");
    }

    [MenuItem("Tools/图鉴系统/检查自动修复组件状态")]
    public static void CheckFixerComponentStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 检查自动修复组件状态 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var fixer = encyclopediaUI.GetComponent<EncyclopediaCanvasFixer>();
        if (fixer == null)
        {
            Debug.LogError("EncyclopediaUI上没有EncyclopediaCanvasFixer组件！");
            Debug.Log("建议停止游戏并重新添加永久修复组件");
            return;
        }

        Debug.Log($"找到EncyclopediaCanvasFixer组件在: {encyclopediaUI.name}");
        Debug.Log($"组件enabled状态: {fixer.enabled}");
        Debug.Log($"GameObject active状态: {fixer.gameObject.activeInHierarchy}");

        // 检查Canvas状态
        var canvas = encyclopediaUI.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  - sortingOrder: {canvas.sortingOrder}");
            Debug.Log($"  - enabled: {canvas.enabled}");
            Debug.Log($"  - activeInHierarchy: {canvas.gameObject.activeInHierarchy}");
        }

        // 手动触发修复
        Debug.Log("手动触发修复...");
        fixer.ApplyFixes();

        Debug.Log("=== 自动修复组件状态检查完成 ===");
    }

    [MenuItem("Tools/图鉴系统/强制重新初始化修复组件")]
    public static void ForceReinitializeFixer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 强制重新初始化修复组件 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 移除现有的修复组件
        var existingFixer = encyclopediaUI.GetComponent<EncyclopediaCanvasFixer>();
        if (existingFixer != null)
        {
            Debug.Log("移除现有的EncyclopediaCanvasFixer组件");
            UnityEngine.Object.DestroyImmediate(existingFixer);
        }

        // 添加新的修复组件
        var newFixer = encyclopediaUI.gameObject.AddComponent<EncyclopediaCanvasFixer>();
        Debug.Log("添加新的EncyclopediaCanvasFixer组件");

        // 立即应用修复
        Debug.Log("立即应用修复...");
        newFixer.ApplyFixes();

        Debug.Log("=== 强制重新初始化完成 ===");
    }

    [MenuItem("Tools/图鉴系统/切换显示模式")]
    public static void ToggleDisplayMode()
    {
        Debug.Log("=== 图鉴显示模式设置 ===");

        string message = @"当前图鉴系统处于测试模式，所有条目信息都会显示。

测试模式特点：
- 所有条目名称直接显示（不显示???）
- 所有描述信息直接显示
- 所有详细信息都可查看
- 3D模型和属性信息全部可见

这有助于测试图鉴系统的完整功能。

如需恢复正式版本（只显示已发现的条目），
请手动编辑 EncyclopediaEntry.cs 文件，
取消注释正式版本的代码块。";

        Debug.Log(message);

        if (EditorApplication.isPlaying)
        {
            Debug.Log("当前在运行模式，修改已生效");
        }
        else
        {
            Debug.Log("请运行游戏来查看效果");
        }
    }

    [MenuItem("Tools/图鉴系统/修复黑色图标方块")]
    public static void FixBlackIconBoxes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 修复黑色图标方块 ===");

        var allEncyclopediaUIs = Resources.FindObjectsOfTypeAll<EncyclopediaUI>();
        if (allEncyclopediaUIs.Length == 0)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var encyclopediaUI = allEncyclopediaUIs[0];

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"检查 {entryListContainer.childCount} 个条目的图标状态");

        int fixedCount = 0;
        int hiddenCount = 0;

        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var iconImage = child.transform.Find("IconImage")?.GetComponent<Image>();

            if (iconImage != null)
            {
                if (iconImage.sprite == null)
                {
                    // 没有图标时隐藏，而不是显示黑块
                    iconImage.gameObject.SetActive(false);
                    hiddenCount++;
                    Debug.Log($"隐藏条目[{i}] {child.name} 的图标（无图片）");
                }
                else
                {
                    // 有图标时确保显示
                    iconImage.gameObject.SetActive(true);
                    iconImage.color = Color.white;
                    fixedCount++;
                    Debug.Log($"修复条目[{i}] {child.name} 的图标显示");
                }
            }
        }

        Debug.Log($"=== 修复完成 ===");
        Debug.Log($"显示图标的条目: {fixedCount}");
        Debug.Log($"隐藏图标的条目: {hiddenCount}");
        Debug.Log("黑色方块问题已修复！");
    }

    [MenuItem("Tools/图鉴系统/更新自动修复组件")]
    public static void UpdateAutoFixerComponent()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 更新自动修复组件 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        var fixer = encyclopediaUI.GetComponent<EncyclopediaCanvasFixer>();
        if (fixer == null)
        {
            Debug.LogError("EncyclopediaUI上没有EncyclopediaCanvasFixer组件！");
            Debug.Log("请先添加永久修复组件");
            return;
        }

        Debug.Log($"找到现有修复组件: {fixer.GetType().Name}");

        // 移除旧组件
        UnityEngine.Object.DestroyImmediate(fixer);
        Debug.Log("移除旧的修复组件");

        // 添加新的修复组件
        var newFixer = encyclopediaUI.gameObject.AddComponent<EncyclopediaCanvasFixer>();
        Debug.Log("添加更新的修复组件");

        // 立即应用修复
        Debug.Log("立即应用所有修复...");
        newFixer.ApplyFixes();

        Debug.Log("=== 自动修复组件更新完成 ===");
        Debug.Log("现在重启游戏应该会自动修复黑色方块了！");
    }

    [MenuItem("Tools/图鉴系统/添加详情面板关闭按钮")]
    public static void AddDetailPanelCloseButton()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 添加详情面板关闭按钮 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("未找到detailPanel！");
            return;
        }

        Debug.Log($"找到详情面板: {detailPanel.name}");

        // 检查是否已存在关闭按钮
        var existingCloseButton = detailPanel.transform.Find("CloseButton");
        if (existingCloseButton != null)
        {
            Debug.Log("详情面板已有关闭按钮，移除旧的");
            UnityEngine.Object.DestroyImmediate(existingCloseButton.gameObject);
        }

        // 创建关闭按钮
        GameObject closeButtonGO = new GameObject("CloseButton");
        closeButtonGO.transform.SetParent(detailPanel.transform, false);

        // 设置RectTransform（右上角位置）
        RectTransform closeRect = closeButtonGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        closeRect.sizeDelta = new Vector2(40, 40);

        // 添加Image组件（背景）
        Image buttonImage = closeButtonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // 红色半透明背景

        // 添加Button组件
        Button closeButton = closeButtonGO.AddComponent<Button>();

        // 绑定关闭事件
        closeButton.onClick.AddListener(() => {
            Debug.Log("详情面板关闭按钮被点击");
            encyclopediaUI.CloseDetailPanel();
        });

        // 创建X文字
        GameObject closeTextGO = new GameObject("CloseText");
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);

        RectTransform textRect = closeTextGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text closeText = closeTextGO.AddComponent<Text>();
        closeText.text = "×";
        closeText.fontSize = 24;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Debug.Log("关闭按钮创建完成！");
        Debug.Log("现在打开详情面板应该能看到右上角的红色关闭按钮了");
    }

    [MenuItem("Tools/图鉴系统/为详情小窗口添加关闭按钮")]
    public static void AddDetailWindowCloseButton()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 为详情小窗口添加关闭按钮 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 使用反射获取detailPanel（详情小窗口）
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("未找到detailPanel（详情小窗口）！");
            return;
        }

        Debug.Log($"找到详情小窗口: {detailPanel.name}");

        // 移除主界面上的错误关闭按钮（如果存在）
        var mainCloseButton = detailPanel.transform.Find("CloseButton");
        if (mainCloseButton != null)
        {
            Debug.Log("移除之前错误添加的关闭按钮");
            UnityEngine.Object.DestroyImmediate(mainCloseButton.gameObject);
        }

        // 检查详情面板是否已存在正确的关闭按钮
        var existingDetailCloseButton = detailPanel.transform.Find("DetailCloseButton");
        if (existingDetailCloseButton != null)
        {
            Debug.Log("详情小窗口已有关闭按钮，移除旧的");
            UnityEngine.Object.DestroyImmediate(existingDetailCloseButton.gameObject);
        }

        // 创建详情小窗口专用的关闭按钮
        GameObject detailCloseButtonGO = new GameObject("DetailCloseButton");
        detailCloseButtonGO.transform.SetParent(detailPanel.transform, false);

        // 设置RectTransform（右上角位置，小一点）
        RectTransform closeRect = detailCloseButtonGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-15, -15); // 稍微往内一点
        closeRect.sizeDelta = new Vector2(30, 30); // 比主界面的小一些

        // 添加Image组件（背景）
        Image buttonImage = detailCloseButtonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.9f, 0.3f, 0.3f, 0.9f); // 稍微亮一点的红色

        // 添加Button组件
        Button detailCloseButton = detailCloseButtonGO.AddComponent<Button>();

        // 绑定关闭详情面板的事件（不是关闭整个图鉴）
        detailCloseButton.onClick.AddListener(() => {
            Debug.Log("详情小窗口关闭按钮被点击");
            encyclopediaUI.CloseDetailPanel(); // 只关闭详情面板
        });

        // 创建X文字
        GameObject closeTextGO = new GameObject("CloseText");
        closeTextGO.transform.SetParent(detailCloseButtonGO.transform, false);

        RectTransform textRect = closeTextGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text closeText = closeTextGO.AddComponent<Text>();
        closeText.text = "×";
        closeText.fontSize = 18; // 比主界面的小一点
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Debug.Log("详情小窗口关闭按钮创建完成！");
        Debug.Log("现在点击条目打开详情面板时，应该能看到详情面板右上角有红色关闭按钮");
        Debug.Log("该按钮只会关闭详情面板，不会关闭整个图鉴系统");
    }

    [MenuItem("Tools/图鉴系统/更新自动修复组件(包含关闭按钮)")]
    public static void UpdateAutoFixerWithCloseButton()
    {
        Debug.Log("=== 更新自动修复组件(包含关闭按钮) ===");

        // 首先在编辑模式下尝试查找
        EncyclopediaUI encyclopediaUI = null;

        if (Application.isPlaying)
        {
            encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        }
        else
        {
            // 编辑模式下从场景中查找
            encyclopediaUI = Resources.FindObjectsOfTypeAll<EncyclopediaUI>()
                .FirstOrDefault(ui => ui.gameObject.scene.name != null);
        }

        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！请确保图鉴系统已正确设置。");
            return;
        }

        var targetGameObject = encyclopediaUI.gameObject;

        // 移除旧的修复组件
        var existingFixer = targetGameObject.GetComponent<EncyclopediaCanvasFixer>();
        if (existingFixer != null)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(existingFixer);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(existingFixer);
            }
            Debug.Log("移除旧的EncyclopediaCanvasFixer组件");
        }

        // 添加新的修复组件（包含关闭按钮功能）
        var newFixer = targetGameObject.AddComponent<EncyclopediaCanvasFixer>();

        Debug.Log($"已添加增强版EncyclopediaCanvasFixer组件到: {targetGameObject.name}");
        Debug.Log("新版本包含以下自动修复功能：");
        Debug.Log("- Canvas层级修复");
        Debug.Log("- 点击事件修复");
        Debug.Log("- 黑色方块修复");
        Debug.Log("- 详情面板关闭按钮自动创建");

        // 如果在运行时，立即应用修复
        if (Application.isPlaying)
        {
            Debug.Log("立即应用所有修复...");
            newFixer.ApplyFixes();
        }
        else
        {
            // 在编辑模式下标记场景为已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetGameObject.scene);
            Debug.Log("请保存场景以使修改永久生效");
        }

        Debug.Log("=== 自动修复组件更新完成 ===");
        Debug.Log("现在重启游戏后，详情面板关闭按钮会自动创建！");
    }

    [MenuItem("Tools/图鉴系统/诊断黑色方块产生根源")]
    public static void DiagnoseBlackBoxOrigin()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 诊断黑色方块产生根源 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        Debug.Log($"检查 {entryListContainer.childCount} 个条目的图标状态");

        int blackBoxCount = 0;
        int hiddenIconCount = 0;
        int validIconCount = 0;

        for (int i = 0; i < Math.Min(10, entryListContainer.childCount); i++) // 只检查前10个
        {
            var child = entryListContainer.GetChild(i);
            var iconImage = child.transform.Find("IconImage")?.GetComponent<Image>();

            if (iconImage != null)
            {
                Debug.Log($"条目[{i}] {child.name}:");
                Debug.Log($"  - sprite: {iconImage.sprite?.name ?? "null"}");
                Debug.Log($"  - color: {iconImage.color}");
                Debug.Log($"  - activeInHierarchy: {iconImage.gameObject.activeInHierarchy}");

                if (iconImage.sprite == null && iconImage.color == Color.black)
                {
                    blackBoxCount++;
                    Debug.LogWarning($"  → 发现黑色方块！");
                }
                else if (!iconImage.gameObject.activeInHierarchy)
                {
                    hiddenIconCount++;
                    Debug.Log($"  → 图标已隐藏（正确）");
                }
                else if (iconImage.sprite != null)
                {
                    validIconCount++;
                    Debug.Log($"  → 有效图标");
                }
            }
        }

        Debug.Log($"=== 诊断结果 ===");
        Debug.Log($"黑色方块: {blackBoxCount}");
        Debug.Log($"隐藏图标: {hiddenIconCount}");
        Debug.Log($"有效图标: {validIconCount}");

        if (blackBoxCount > 0)
        {
            Debug.LogError("发现黑色方块！问题根源可能是：");
            Debug.LogError("1. EncyclopediaUI.cs中的图标设置逻辑没有正确修改");
            Debug.LogError("2. 条目创建时默认设置了黑色");
            Debug.LogError("3. 图标数据加载失败");
        }
        else
        {
            Debug.Log("未发现黑色方块，说明根源问题已解决！");
        }
    }

    [MenuItem("Tools/图鉴系统/调试3D模型显示问题")]
    public static void Debug3DModelDisplay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 调试3D模型显示问题 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 检查当前选中的条目
        var selectedEntryField = typeof(EncyclopediaUI).GetField("selectedEntry",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var selectedEntry = selectedEntryField?.GetValue(encyclopediaUI) as EncyclopediaEntry;

        if (selectedEntry == null)
        {
            Debug.LogWarning("当前没有选中的条目，请先点击一个条目");
            return;
        }

        Debug.Log($"当前选中条目: {selectedEntry.GetFormattedDisplayName()}");
        Debug.Log($"条目ID: {selectedEntry.id}");
        Debug.Log($"模型文件: {selectedEntry.modelFile ?? "null"}");
        Debug.Log($"3D模型对象: {selectedEntry.model3D?.name ?? "null"}");

        // 检查详情面板
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("未找到detailPanel！");
            return;
        }

        Debug.Log($"详情面板: {detailPanel.name}, 激活状态: {detailPanel.activeInHierarchy}");

        // 查找Model3DViewer组件
        var model3DViewer = detailPanel.GetComponentInChildren<Encyclopedia.Model3DViewer>();
        if (model3DViewer == null)
        {
            Debug.LogWarning("未找到Model3DViewer组件！");

            // 尝试在整个detailPanel层级中搜索
            var allViewers = Resources.FindObjectsOfTypeAll<Encyclopedia.Model3DViewer>();
            Debug.Log($"全局找到 {allViewers.Length} 个Model3DViewer组件");

            foreach (var viewer in allViewers)
            {
                Debug.Log($"  - Model3DViewer: {viewer.name}, 激活: {viewer.gameObject.activeInHierarchy}, 父级: {viewer.transform.parent?.name}");
            }
        }
        else
        {
            Debug.Log($"找到Model3DViewer: {model3DViewer.name}");
            Debug.Log($"  - 激活状态: {model3DViewer.gameObject.activeInHierarchy}");
            Debug.Log($"  - 位置: {model3DViewer.transform.position}");

            // 检查3D模型查看器的详细状态
            var viewerFields = typeof(Encyclopedia.Model3DViewer).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in viewerFields)
            {
                var value = field.GetValue(model3DViewer);
                Debug.Log($"  - {field.Name}: {value?.ToString() ?? "null"}");
            }
        }

        Debug.Log("=== 3D模型调试完成 ===");
    }

    [MenuItem("Tools/图鉴系统/紧急修复所有功能")]
    public static void EmergencyFixAllFunctions()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下运行此工具！");
            return;
        }

        Debug.Log("=== 紧急修复所有功能 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("未找到EncyclopediaUI组件！");
            return;
        }

        // 1. 重新修复Canvas层级
        Debug.Log("步骤1: 修复Canvas层级...");
        var canvas = encyclopediaUI.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 10001;
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = true;
            }
            Debug.Log("Canvas层级已修复");
        }

        // 2. 重新修复点击事件
        Debug.Log("步骤2: 修复点击事件...");
        FixClickEventsEmergency(encyclopediaUI);

        // 3. 重新修复黑色方块
        Debug.Log("步骤3: 修复黑色方块...");
        FixBlackIconBoxesEmergency(encyclopediaUI);

        // 4. 确保关闭按钮不干扰其他功能
        Debug.Log("步骤4: 优化关闭按钮...");
        OptimizeCloseButton(encyclopediaUI);

        Debug.Log("=== 紧急修复完成！请重新测试所有功能 ===");
    }

    private static void FixClickEventsEmergency(EncyclopediaUI encyclopediaUI)
    {
        // 获取entryListContainer
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogError("未找到entryListContainer！");
            return;
        }

        var onEntryItemClickedMethod = typeof(EncyclopediaUI).GetMethod("OnEntryItemClicked",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (onEntryItemClickedMethod == null)
        {
            Debug.LogError("未找到OnEntryItemClicked方法！");
            return;
        }

        // 确保EncyclopediaData已初始化
        if (EncyclopediaData.Instance == null)
        {
            var dataComponent = FindObjectOfType<EncyclopediaData>();
            if (dataComponent != null)
            {
                var awakeMethod = typeof(EncyclopediaData).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(dataComponent, null);
            }
        }

        if (EncyclopediaData.Instance?.AllEntries == null)
        {
            Debug.LogError("EncyclopediaData未正确初始化");
            return;
        }

        var allEntries = EncyclopediaData.Instance.AllEntries.Values;
        int fixedCount = 0;

        // 重新绑定所有条目的点击事件
        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                // 清除现有事件
                button.onClick.RemoveAllListeners();

                // 确保按钮可交互
                button.enabled = true;
                button.interactable = true;

                // 获取条目名称
                var nameTexts = child.GetComponentsInChildren<Text>();
                string entryName = "";
                foreach (var text in nameTexts)
                {
                    if (!string.IsNullOrEmpty(text.text) && text.text != "???")
                    {
                        entryName = text.text.Trim();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(entryName))
                {
                    // 查找匹配的Entry
                    EncyclopediaEntry matchedEntry = null;
                    foreach (var entry in allEntries)
                    {
                        if (entry.GetFormattedDisplayName() == entryName)
                        {
                            matchedEntry = entry;
                            break;
                        }
                    }

                    if (matchedEntry != null)
                    {
                        // 绑定点击事件
                        button.onClick.AddListener(() => {
                            Debug.Log($"紧急修复：条目被点击 - {matchedEntry.GetFormattedDisplayName()}");
                            onEntryItemClickedMethod.Invoke(encyclopediaUI, new object[] { matchedEntry });
                        });

                        fixedCount++;
                    }
                }
            }
        }

        Debug.Log($"紧急修复点击事件完成: {fixedCount} 个条目");
    }

    private static void FixBlackIconBoxesEmergency(EncyclopediaUI encyclopediaUI)
    {
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null) return;

        int hiddenCount = 0;
        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var iconImage = child.transform.Find("IconImage")?.GetComponent<Image>();

            if (iconImage != null && iconImage.sprite == null)
            {
                iconImage.gameObject.SetActive(false);
                hiddenCount++;
            }
        }

        Debug.Log($"紧急修复黑色方块完成: {hiddenCount} 个图标已隐藏");
    }

    private static void OptimizeCloseButton(EncyclopediaUI encyclopediaUI)
    {
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null) return;

        var closeButton = detailPanel.transform.Find("CloseButton");
        if (closeButton != null)
        {
            // 确保关闭按钮不会阻塞其他UI元素
            var buttonComponent = closeButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                // 设置按钮不阻塞射线
                var raycaster = closeButton.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                }

                Debug.Log("关闭按钮已优化");
            }
        }
    }

    // 调试组件类
    public class ClickDebugger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public int entryIndex;
        public string entryName;
        public Button button;

        private void Start()
        {
            Debug.Log($"[ClickDebugger] 条目[{entryIndex}] {entryName} 调试器已启动");

            // 检查Button状态
            if (button != null)
            {
                Debug.Log($"[ClickDebugger] Button状态 - enabled:{button.enabled}, interactable:{button.interactable}");
                Debug.Log($"[ClickDebugger] onClick事件数: persistent={button.onClick.GetPersistentEventCount()}");

                // 检查Graphic Raycaster
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    Debug.Log($"[ClickDebugger] Canvas GraphicRaycaster存在: {raycaster != null}");
                    if (raycaster != null)
                    {
                        Debug.Log($"[ClickDebugger] GraphicRaycaster enabled: {raycaster.enabled}");
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            Debug.Log($"[ClickDebugger] 鼠标按下 - 条目[{entryIndex}] {entryName}");
        }

        private void OnMouseUpAsButton()
        {
            Debug.Log($"[ClickDebugger] 鼠标点击 - 条目[{entryIndex}] {entryName}");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[ClickDebugger] 指针点击 - 条目[{entryIndex}] {entryName}");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"[ClickDebugger] 指针按下 - 条目[{entryIndex}] {entryName}");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"[ClickDebugger] 指针松开 - 条目[{entryIndex}] {entryName}");
        }
    }

    private static string GetGameObjectPathStatic(GameObject go)
    {
        var path = go.name;
        var parent = go.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }


    private static void RepairSingletons()
    {
        Debug.Log("=== 修复图鉴单例问题 ===");

        // 修复EncyclopediaData单例
        var encyclopediaDataComponents = Resources.FindObjectsOfTypeAll<EncyclopediaData>();
        foreach (var component in encyclopediaDataComponents)
        {
            if (component.gameObject.name.Contains("EncyclopediaData"))
            {
                Debug.Log($"发现EncyclopediaData组件: {component.gameObject.name}");

                // 直接设置Instance字段，避免调用Awake（因为DontDestroyOnLoad在编辑器中不可用）
                var instanceProperty = typeof(EncyclopediaData).GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (instanceProperty != null && instanceProperty.CanWrite)
                {
                    instanceProperty.SetValue(null, component);
                    Debug.Log("通过属性设置EncyclopediaData.Instance");
                }
                else
                {
                    // 如果是只读属性，尝试直接设置后备字段
                    var instanceField = typeof(EncyclopediaData).GetField("<Instance>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (instanceField == null)
                    {
                        instanceField = typeof(EncyclopediaData).GetField("instance",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    }

                    if (instanceField != null)
                    {
                        instanceField.SetValue(null, component);
                        Debug.Log("通过后备字段设置EncyclopediaData.Instance");
                    }
                    else
                    {
                        Debug.LogError("无法找到EncyclopediaData.Instance字段或属性");
                    }
                }

                // 手动调用LoadEncyclopediaData方法
                var loadMethod = typeof(EncyclopediaData).GetMethod("LoadEncyclopediaData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (loadMethod != null)
                {
                    try
                    {
                        loadMethod.Invoke(component, null);
                        Debug.Log("手动调用LoadEncyclopediaData()");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"LoadEncyclopediaData调用失败: {e.Message}");
                    }
                }
                break;
            }
        }

        // 修复CollectionManager单例
        var collectionManagerComponents = Resources.FindObjectsOfTypeAll<CollectionManager>();
        foreach (var component in collectionManagerComponents)
        {
            if (component.gameObject.name.Contains("CollectionManager"))
            {
                Debug.Log($"发现CollectionManager组件: {component.gameObject.name}");

                // 直接设置Instance字段
                var instanceProperty = typeof(CollectionManager).GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (instanceProperty != null && instanceProperty.CanWrite)
                {
                    instanceProperty.SetValue(null, component);
                    Debug.Log("通过属性设置CollectionManager.Instance");
                }
                else
                {
                    // 如果是只读属性，尝试直接设置后备字段
                    var instanceField = typeof(CollectionManager).GetField("<Instance>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (instanceField == null)
                    {
                        instanceField = typeof(CollectionManager).GetField("instance",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    }

                    if (instanceField != null)
                    {
                        instanceField.SetValue(null, component);
                        Debug.Log("通过后备字段设置CollectionManager.Instance");
                    }
                    else
                    {
                        Debug.LogError("无法找到CollectionManager.Instance字段或属性");
                    }
                }

                // 手动调用Initialize方法
                var initMethod = typeof(CollectionManager).GetMethod("Initialize",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (initMethod != null)
                {
                    try
                    {
                        initMethod.Invoke(component, null);
                        Debug.Log("手动调用CollectionManager.Initialize()");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"CollectionManager.Initialize调用失败: {e.Message}");
                    }
                }
                break;
            }
        }

        // 延迟验证单例是否正确设置（给反射时间生效）
        System.Threading.Thread.Sleep(100);

        if (EncyclopediaData.Instance != null)
        {
            Debug.Log($"✓ EncyclopediaData.Instance 已设置，数据加载状态: {EncyclopediaData.Instance.IsDataLoaded}");
            Debug.Log($"✓ 图鉴条目总数: {EncyclopediaData.Instance.AllEntries.Count}");
            Debug.Log($"✓ 地层名称数量: {EncyclopediaData.Instance.LayerNames.Count}");
        }
        else
        {
            Debug.LogError("✗ EncyclopediaData.Instance 仍为null");

            // 再次尝试通过不同方法设置
            var encyclopedia = encyclopediaDataComponents.FirstOrDefault(c => c.gameObject.name.Contains("EncyclopediaData"));
            if (encyclopedia != null)
            {
                // 通过反射强制设置静态字段
                var type = typeof(EncyclopediaData);
                var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                foreach (var field in fields)
                {
                    if (field.Name.ToLower().Contains("instance"))
                    {
                        field.SetValue(null, encyclopedia);
                        Debug.Log($"通过字段 {field.Name} 强制设置单例");
                        break;
                    }
                }
            }
        }

        if (CollectionManager.Instance != null)
        {
            Debug.Log("✓ CollectionManager.Instance 已设置");
        }
        else
        {
            Debug.LogError("✗ CollectionManager.Instance 仍为null");

            // 再次尝试通过不同方法设置
            var collection = collectionManagerComponents.FirstOrDefault(c => c.gameObject.name.Contains("CollectionManager"));
            if (collection != null)
            {
                // 通过反射强制设置静态字段
                var type = typeof(CollectionManager);
                var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                foreach (var field in fields)
                {
                    if (field.Name.ToLower().Contains("instance"))
                    {
                        field.SetValue(null, collection);
                        Debug.Log($"通过字段 {field.Name} 强制设置单例");
                        break;
                    }
                }
            }
        }

        Debug.Log("=== 单例修复完成 ===");
    }
}