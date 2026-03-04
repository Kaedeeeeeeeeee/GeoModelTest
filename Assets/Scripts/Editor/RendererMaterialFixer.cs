using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量修复 Renderer 材质槽数量大于 submesh 数量的问题。
/// 只保留与 submesh 数量一致的前几个材质，避免 “has more materials than submeshes” 警告。
/// </summary>
public static class RendererMaterialFixer
{
    private const string ClassRoomPrefabPath = "Assets/Model/ClassRoom/ClassRoom.prefab";
    private static readonly string[] DefaultSearchFolders = new[]
    {
        "Assets/Model/ClassRoom",
        "Assets/school"
    };

    [MenuItem("Tools/Materials/修复材质槽与子网格数量")]
    public static void FixRendererMaterials()
    {
        var guids = new List<string>();

        // 尝试优先修复 ClassRoom 及 school 目录下的预制体
        foreach (string folder in DefaultSearchFolders)
        {
            guids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { folder }));
        }

        // 确保 ClassRoom 主预制体被包含
        string classRoomGuid = AssetDatabase.AssetPathToGUID(ClassRoomPrefabPath);
        if (!string.IsNullOrEmpty(classRoomGuid) && !guids.Contains(classRoomGuid))
        {
            guids.Add(classRoomGuid);
        }

        int modifiedRenderers = 0;
        int modifiedPrefabs = 0;

        foreach (string guid in guids.Distinct())
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            bool prefabChanged = false;

            foreach (Renderer renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
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
                if (subMeshCount == 0) continue;

                var mats = renderer.sharedMaterials;
                if (mats == null || mats.Length <= subMeshCount) continue;

                var trimmed = mats.Take(subMeshCount).ToArray();
                renderer.sharedMaterials = trimmed;
                prefabChanged = true;
                modifiedRenderers++;
            }

            if (prefabChanged)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                modifiedPrefabs++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log($"[RendererMaterialFixer] 完成修复。修改了 {modifiedPrefabs} 个预制体，{modifiedRenderers} 个 Renderer 的材质槽。");
    }
}
