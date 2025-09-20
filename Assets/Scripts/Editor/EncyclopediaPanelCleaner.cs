using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 图鉴面板清理工具
/// 用于删除多余的图鉴面板并清理现有面板中的IconImage组件
/// </summary>
public class EncyclopediaPanelCleaner : EditorWindow
{
    [MenuItem("Tools/图鉴系统/清理多余面板")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaPanelCleaner>("图鉴面板清理工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("图鉴面板清理工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("扫描当前场景中的图鉴面板", GUILayout.Height(30)))
        {
            ScanEncyclopediaPanels();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("删除多余的图鉴面板（保留一个）", GUILayout.Height(30)))
        {
            CleanupDuplicatePanels();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("清理现有面板中的IconImage组件", GUILayout.Height(30)))
        {
            CleanupIconImages();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("执行完整清理（推荐）", GUILayout.Height(40)))
        {
            ScanEncyclopediaPanels();
            CleanupDuplicatePanels();
            CleanupIconImages();
            Debug.Log("=== 完整清理完成 ===");
        }
    }

    private void ScanEncyclopediaPanels()
    {
        var allPanels = FindObjectsOfType<GameObject>();
        var encyclopediaPanels = new List<GameObject>();

        foreach (var obj in allPanels)
        {
            if (obj.name == "EncyclopediaPanel")
            {
                encyclopediaPanels.Add(obj);
            }
        }

        Debug.Log($"=== 扫描结果 ===");
        Debug.Log($"找到 {encyclopediaPanels.Count} 个EncyclopediaPanel:");

        for (int i = 0; i < encyclopediaPanels.Count; i++)
        {
            var panel = encyclopediaPanels[i];
            var parentName = panel.transform.parent ? panel.transform.parent.name : "无父级";
            var hasEncyclopediaUI = panel.GetComponent<Encyclopedia.EncyclopediaUI>() != null;
            Debug.Log($"  {i + 1}. {panel.name} (父级: {parentName}, 有EncyclopediaUI: {hasEncyclopediaUI})");
        }
    }

    private void CleanupDuplicatePanels()
    {
        var allPanels = FindObjectsOfType<GameObject>();
        var encyclopediaPanels = new List<GameObject>();

        foreach (var obj in allPanels)
        {
            if (obj.name == "EncyclopediaPanel")
            {
                encyclopediaPanels.Add(obj);
            }
        }

        if (encyclopediaPanels.Count <= 1)
        {
            Debug.Log("只有1个或没有EncyclopediaPanel，无需清理");
            return;
        }

        // 优先保留有EncyclopediaUI组件的面板
        GameObject keepPanel = null;
        var toDelete = new List<GameObject>();

        foreach (var panel in encyclopediaPanels)
        {
            if (panel.GetComponent<Encyclopedia.EncyclopediaUI>() != null && keepPanel == null)
            {
                keepPanel = panel;
            }
            else
            {
                toDelete.Add(panel);
            }
        }

        // 如果没有找到有EncyclopediaUI的面板，保留第一个
        if (keepPanel == null && encyclopediaPanels.Count > 0)
        {
            keepPanel = encyclopediaPanels[0];
            toDelete.Remove(keepPanel);
        }

        Debug.Log($"=== 清理多余面板 ===");
        Debug.Log($"保留面板: {keepPanel?.name} (父级: {keepPanel?.transform.parent?.name})");
        Debug.Log($"将删除 {toDelete.Count} 个多余面板:");

        foreach (var panel in toDelete)
        {
            Debug.Log($"  删除: {panel.name} (父级: {panel.transform.parent?.name})");
            DestroyImmediate(panel);
        }

        Debug.Log("多余面板清理完成！");
    }

    private void CleanupIconImages()
    {
        var allPanels = FindObjectsOfType<GameObject>();
        var encyclopediaPanels = new List<GameObject>();

        foreach (var obj in allPanels)
        {
            if (obj.name == "EncyclopediaPanel")
            {
                encyclopediaPanels.Add(obj);
            }
        }

        Debug.Log($"=== 清理IconImage组件 ===");

        int totalRemoved = 0;

        foreach (var panel in encyclopediaPanels)
        {
            var iconImages = panel.GetComponentsInChildren<Image>(true);
            var removedInThisPanel = 0;

            foreach (var image in iconImages)
            {
                if (image.gameObject.name == "IconImage")
                {
                    Debug.Log($"  删除IconImage: {GetGameObjectPath(image.gameObject)}");
                    DestroyImmediate(image.gameObject);
                    removedInThisPanel++;
                    totalRemoved++;
                }
            }

            if (removedInThisPanel > 0)
            {
                Debug.Log($"面板 {panel.name} 中删除了 {removedInThisPanel} 个IconImage组件");
            }
        }

        Debug.Log($"总共删除了 {totalRemoved} 个IconImage组件");

        // 同时调整NameText的位置
        AdjustNameTextPositions(encyclopediaPanels);
    }

    private void AdjustNameTextPositions(List<GameObject> panels)
    {
        Debug.Log("=== 调整NameText位置 ===");

        int adjustedCount = 0;

        foreach (var panel in panels)
        {
            var nameTexts = panel.GetComponentsInChildren<Text>(true);

            foreach (var text in nameTexts)
            {
                if (text.gameObject.name == "NameText")
                {
                    var rectTransform = text.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // 检查当前offsetMin.x是否是70（原来为图标留的空间）
                        if (Mathf.Approximately(rectTransform.offsetMin.x, 70f))
                        {
                            rectTransform.offsetMin = new Vector2(15f, rectTransform.offsetMin.y);
                            Debug.Log($"  调整NameText位置: {GetGameObjectPath(text.gameObject)} (70 -> 15)");
                            adjustedCount++;
                        }
                    }
                }
            }
        }

        Debug.Log($"调整了 {adjustedCount} 个NameText的位置");
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform t = obj.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}