using UnityEngine;
using UnityEditor;

/// <summary>
/// 钻塔预制件创建器 - 将动态创建的钻塔保存为预制件文件
/// </summary>
public class DrillTowerPrefabCreator : EditorWindow
{
    [MenuItem("Tools/创建钻塔预制件")]
    public static void ShowWindow()
    {
        GetWindow<DrillTowerPrefabCreator>("钻塔预制件创建器");
    }

    void OnGUI()
    {
        GUILayout.Label("钻塔预制件创建器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("创建并保存钻塔预制件"))
        {
            CreateAndSaveDrillTowerPrefab();
        }

        GUILayout.Space(10);
        GUILayout.Label("这将创建一个钻塔预制件并保存到:", EditorStyles.helpBox);
        GUILayout.Label("Assets/Prefabs/DrillTowerPrefab.prefab", EditorStyles.miniLabel);
    }

    static void CreateAndSaveDrillTowerPrefab()
    {
        // 创建钻塔预制件（使用与DrillTowerSetup相同的逻辑）
        GameObject towerPrefab = CreateDrillTowerPrefabInternal();

        // 确保Prefabs文件夹存在
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 保存为预制件
        string prefabPath = $"{prefabFolder}/DrillTowerPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(towerPrefab, prefabPath);

        // 清理临时对象
        DestroyImmediate(towerPrefab);

        // 刷新AssetDatabase
        AssetDatabase.Refresh();

        Debug.Log($"✅ 钻塔预制件已保存到: {prefabPath}");
        EditorUtility.DisplayDialog("成功", $"钻塔预制件已保存到:\n{prefabPath}", "确定");

        // 在Project窗口中选中新创建的预制件
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    /// <summary>
    /// 创建钻塔预制件的内部方法（复制自DrillTowerSetup）
    /// </summary>
    static GameObject CreateDrillTowerPrefabInternal()
    {
        // 创建钻塔主体
        GameObject towerPrefab = new GameObject("DrillTowerPrefab");

        // 创建底座（扁平圆柱体）- 底部贴地，考虑到圆柱体的pivot在中心
        GameObject base_platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base_platform.name = "BasePlatform";
        base_platform.transform.SetParent(towerPrefab.transform);
        base_platform.transform.localPosition = new Vector3(0, 0.1f, 0); // 底座高度0.2f，所以中心在0.1f处
        base_platform.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);

        // 创建塔身（圆柱体）- 从底座向上
        GameObject towerBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        towerBody.name = "TowerBody";
        towerBody.transform.SetParent(towerPrefab.transform);
        towerBody.transform.localPosition = new Vector3(0, 1.4f, 0); // 底座顶部(0.2f) + 塔身高度一半(1.2f) = 1.4f
        towerBody.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        // 创建钻探臂（立方体）- 在塔身顶部
        GameObject drillArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drillArm.name = "DrillArm";
        drillArm.transform.SetParent(towerPrefab.transform);
        drillArm.transform.localPosition = new Vector3(0, 2.6f, 0.6f); // 塔身顶部(1.4f + 1.2f = 2.6f)
        drillArm.transform.localScale = new Vector3(0.3f, 0.3f, 1.2f);

        // 创建钻头（小圆柱体）- 悬挂在钻探臂下方
        GameObject drillBit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drillBit.name = "DrillBit";
        drillBit.transform.SetParent(towerPrefab.transform);
        drillBit.transform.localPosition = new Vector3(0, 1.0f, 0); // 稍微高于底座，调整到合适高度
        drillBit.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);

        // 配置材质 - 创建明显可见的默认材质
        Material finalMaterial = new Material(Shader.Find("Standard"));
        finalMaterial.color = new Color(0.8f, 0.3f, 0.1f, 1f); // 橙红色，确保可见
        finalMaterial.SetFloat("_Metallic", 0.2f);
        finalMaterial.SetFloat("_Glossiness", 0.6f);

        ApplyMaterialToChildren(towerPrefab, finalMaterial);

        // 确保所有渲染器都正常配置
        Renderer[] renderers = towerPrefab.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = finalMaterial;
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        // 添加物理组件，让钻塔有重力和碰撞
        Rigidbody towerRigidbody = towerPrefab.AddComponent<Rigidbody>();
        towerRigidbody.mass = 100f; // 重量适中，不会轻易被推动
        towerRigidbody.linearDamping = 5f; // 增加阻力，放置后快速稳定
        towerRigidbody.angularDamping = 10f; // 增加角阻力，防止旋转
        towerRigidbody.centerOfMass = new Vector3(0, 0.5f, 0); // 低重心，更稳定

        // 添加主碰撞器（用于物理碰撞）
        CapsuleCollider physicsCollider = towerPrefab.AddComponent<CapsuleCollider>();
        physicsCollider.radius = 0.6f;
        physicsCollider.height = 2.8f; // 稍微增加高度以覆盖整个钻塔
        physicsCollider.center = new Vector3(0, 1.4f, 0); // 从底座底部到钻塔顶部的中心
        physicsCollider.direction = 1; // Y轴方向

        // 添加交互碰撞器（用于F键交互检测）
        BoxCollider interactionCollider = towerPrefab.AddComponent<BoxCollider>();
        interactionCollider.isTrigger = true; // 设为触发器
        interactionCollider.size = new Vector3(2f, 3f, 2f); // 稍大，便于交互
        interactionCollider.center = new Vector3(0, 1.4f, 0); // 调整到钻塔中心

        // 冻结旋转，防止钻塔倾倒
        towerRigidbody.freezeRotation = true;

        return towerPrefab;
    }

    /// <summary>
    /// 为对象及其子对象应用材质
    /// </summary>
    static void ApplyMaterialToChildren(GameObject obj, Material material)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }
    }
}