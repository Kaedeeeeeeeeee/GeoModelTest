using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一次性为 ClassRoom/School 相关对象补齐默认材质（1.mat），避免运行时材质丢失。
/// 仅在编辑器运行，不会影响打包或运行时性能。
/// </summary>
public static class ClassRoomMaterialApplier
{
    private const string FallbackMaterialPath = "Assets/school/material/Materials/1.mat";
    private static readonly string[] PrefabSearchFolders = {
        "Assets/Model/ClassRoom",
        "Assets/school/Prefabs",
        "Assets/school/props"
    };

    [MenuItem("Tools/Materials/为ClassRoom补齐默认材质(1.mat)")]
    public static void ApplyDefaultMaterial()
    {
        Material fallbackMat = AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
        if (fallbackMat == null)
        {
            Debug.LogError($"[ClassRoomMaterialApplier] 未找到默认材质: {FallbackMaterialPath}");
            return;
        }

        int fixedPrefabRenderers = 0;
        int fixedPrefabs = 0;

        // 修复 prefab 资产
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabSearchFolders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            bool changed = FixRenderers(prefabRoot, fallbackMat, ref fixedPrefabRenderers);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                fixedPrefabs++;
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // 修复当前打开的场景对象（如正在编辑的 MainScene）
        int fixedSceneRenderers = 0;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root.name != "ClassRoom" && !root.name.Contains("school")) continue;
                if (FixRenderers(root, fallbackMat, ref fixedSceneRenderers))
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }
        }

        Debug.Log($"[ClassRoomMaterialApplier] 修复完成。Prefab 渲染器修复 {fixedPrefabRenderers} 个（{fixedPrefabs} 个 prefab 变更），场景内修复 {fixedSceneRenderers} 个。");
    }

    /// <summary>
    /// 修复一个对象下所有 Renderer 的材质：数量对齐 submesh，null 用默认材质补齐。
    /// </summary>
    private static bool FixRenderers(GameObject root, Material fallbackMat, ref int fixedCount)
    {
        bool changed = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            if (renderer is MeshRenderer meshRenderer)
            {
                mesh = meshRenderer.GetComponent<MeshFilter>()?.sharedMesh;
            }
            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }

            int subMeshCount = mesh != null ? mesh.subMeshCount : 0;
            var mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                int targetCount = Mathf.Max(subMeshCount, 1);
                renderer.sharedMaterials = CreateFilledArray(targetCount, fallbackMat);
                fixedCount++;
                changed = true;
                continue;
            }

            bool rendererChanged = false;
            int desiredCount = subMeshCount > 0 ? subMeshCount : mats.Length;

            if (mats.Length != desiredCount)
            {
                System.Array.Resize(ref mats, desiredCount);
                rendererChanged = true;
            }

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                {
                    mats[i] = fallbackMat;
                    rendererChanged = true;
                }
            }

            if (rendererChanged)
            {
                renderer.sharedMaterials = mats;
                fixedCount++;
                changed = true;
            }
        }

        return changed;
    }

    private static Material[] CreateFilledArray(int count, Material mat)
    {
        var arr = new Material[count];
        for (int i = 0; i < count; i++) arr[i] = mat;
        return arr;
    }
}
