using UnityEngine;
using UnityEditor;
using Encyclopedia;
using System.Collections.Generic;

public class EncyclopediaRealTimeFixer : EditorWindow
{
    [MenuItem("Tools/图鉴系统/实时图鉴修复器")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaRealTimeFixer>("实时图鉴修复器");
    }

    private void OnGUI()
    {
        GUILayout.Label("=== 🔧 实时图鉴修复器 ===", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            GUILayout.Label("⚠️ 请先运行游戏", EditorStyles.helpBox);
            return;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("📊 检查所有条目状态", GUILayout.Height(30)))
        {
            CheckAllEntriesStatus();
        }

        if (GUILayout.Button("🔓 强制解锁所有条目", GUILayout.Height(30)))
        {
            UnlockAllEntries();
        }

        if (GUILayout.Button("🎲 为空模型创建测试立方体", GUILayout.Height(30)))
        {
            CreateTestModelsForEmptyEntries();
        }

        if (GUILayout.Button("🔧 修复并测试选中条目", GUILayout.Height(30)))
        {
            FixAndTestSelectedEntry();
        }
    }

    private void CheckAllEntriesStatus()
    {
        Debug.Log("=== 📊 检查所有条目状态 ===");

        var encyclopediaData = EncyclopediaData.Instance;
        if (encyclopediaData == null)
        {
            Debug.LogError("❌ EncyclopediaData.Instance为null");
            return;
        }

        var allMinerals = encyclopediaData.GetAllMinerals();
        var allFossils = encyclopediaData.GetAllFossils();
        var allEntries = new List<EncyclopediaEntry>();
        allEntries.AddRange(allMinerals);
        allEntries.AddRange(allFossils);
        Debug.Log($"总条目数: {allEntries.Count} (矿物: {allMinerals.Count}, 化石: {allFossils.Count})");

        int discoveredCount = 0;
        int withModelCount = 0;

        foreach (var entry in allEntries)
        {
            if (entry.isDiscovered) discoveredCount++;
            if (entry.model3D != null) withModelCount++;

            Debug.Log($"条目: {entry.displayName} | 发现: {entry.isDiscovered} | 3D模型: {(entry.model3D != null ? "有" : "无")}");
        }

        Debug.Log($"✅ 已发现: {discoveredCount}/{allEntries.Count}");
        Debug.Log($"🎮 有3D模型: {withModelCount}/{allEntries.Count}");
    }

    private void UnlockAllEntries()
    {
        Debug.Log("=== 🔓 强制解锁所有条目 ===");

        var encyclopediaData = EncyclopediaData.Instance;
        if (encyclopediaData == null)
        {
            Debug.LogError("❌ EncyclopediaData.Instance为null");
            return;
        }

        var allMinerals = encyclopediaData.GetAllMinerals();
        var allFossils = encyclopediaData.GetAllFossils();
        var allEntries = new List<EncyclopediaEntry>();
        allEntries.AddRange(allMinerals);
        allEntries.AddRange(allFossils);
        foreach (var entry in allEntries)
        {
            entry.isDiscovered = true;
        }

        Debug.Log($"✅ 已解锁所有 {allEntries.Count} 个条目");

        // 刷新图鉴UI
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI != null)
        {
            Debug.Log("🔄 刷新图鉴UI");
            // 通过反射调用RefreshEntryList
            var method = typeof(EncyclopediaUI).GetMethod("RefreshEntryList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(encyclopediaUI, null);
        }
    }

    private void CreateTestModelsForEmptyEntries()
    {
        Debug.Log("=== 🎲 为空模型创建测试立方体 ===");

        var encyclopediaData = EncyclopediaData.Instance;
        if (encyclopediaData == null)
        {
            Debug.LogError("❌ EncyclopediaData.Instance为null");
            return;
        }

        var allMinerals = encyclopediaData.GetAllMinerals();
        var allFossils = encyclopediaData.GetAllFossils();
        var allEntries = new List<EncyclopediaEntry>();
        allEntries.AddRange(allMinerals);
        allEntries.AddRange(allFossils);
        int createdCount = 0;

        foreach (var entry in allEntries)
        {
            if (entry.model3D == null)
            {
                // 创建一个简单的立方体作为测试模型
                GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testCube.name = $"TestModel_{entry.displayName}";

                // 随机颜色
                var renderer = testCube.GetComponent<Renderer>();
                renderer.material.color = new Color(
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f)
                );

                entry.model3D = testCube;
                createdCount++;

                Debug.Log($"创建测试模型: {entry.displayName}");
            }
        }

        Debug.Log($"✅ 为 {createdCount} 个条目创建了测试模型");
    }

    private void FixAndTestSelectedEntry()
    {
        Debug.Log("=== 🔧 修复并测试选中条目 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 未找到EncyclopediaUI");
            return;
        }

        // 获取第一个矿物条目进行测试
        var encyclopediaData = EncyclopediaData.Instance;
        if (encyclopediaData == null)
        {
            Debug.LogError("❌ EncyclopediaData.Instance为null");
            return;
        }

        var allMinerals = encyclopediaData.GetAllMinerals();
        var allFossils = encyclopediaData.GetAllFossils();
        var allEntries = new List<EncyclopediaEntry>();
        allEntries.AddRange(allMinerals);
        allEntries.AddRange(allFossils);
        if (allEntries.Count == 0)
        {
            Debug.LogError("❌ 没有找到任何条目");
            return;
        }

        var testEntry = allEntries[0];
        Debug.Log($"🎯 测试条目: {testEntry.displayName}");

        // 确保条目被发现
        testEntry.isDiscovered = true;

        // 如果没有3D模型，创建一个
        if (testEntry.model3D == null)
        {
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = $"DirectTestModel_{testEntry.displayName}";
            var renderer = testCube.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;
            testEntry.model3D = testCube;
            Debug.Log($"为 {testEntry.displayName} 创建了黄色测试立方体");
        }

        // 直接调用ShowEntryDetails
        var showDetailsMethod = typeof(EncyclopediaUI).GetMethod("ShowEntryDetails",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (showDetailsMethod != null)
        {
            Debug.Log($"🚀 直接调用ShowEntryDetails显示: {testEntry.displayName}");
            showDetailsMethod.Invoke(encyclopediaUI, new object[] { testEntry });
        }
        else
        {
            Debug.LogError("❌ 未找到ShowEntryDetails方法");
        }
    }
}