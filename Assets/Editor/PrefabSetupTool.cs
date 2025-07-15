using UnityEngine;
using UnityEditor;

public class PrefabSetupTool : EditorWindow
{
    [MenuItem("Tools/道具预制体设置工具")]
    public static void ShowWindow()
    {
        GetWindow<PrefabSetupTool>("道具预制体设置");
    }

    void OnGUI()
    {
        GUILayout.Label("道具预制体组件设置工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("为预制体添加运行时需要的组件", EditorStyles.helpBox);
        GUILayout.Space(10);

        if (GUILayout.Button("设置无人机预制体组件", GUILayout.Height(30)))
        {
            SetupDronePrefab();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("设置钻探车预制体组件", GUILayout.Height(30)))
        {
            SetupDrillCarPrefab();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("清理所有预制体组件", GUILayout.Height(30)))
        {
            CleanupAllPrefabs();
        }

        GUILayout.Space(10);
        GUILayout.Label("注意：此工具会直接修改预制体文件", EditorStyles.helpBox);
    }

    void SetupDronePrefab()
    {
        // 查找Drone预制体
        string[] guids = AssetDatabase.FindAssets("Drone t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogWarning("未找到Drone预制体");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (dronePrefab == null)
        {
            Debug.LogError("无法加载Drone预制体");
            return;
        }

        // 创建预制体实例进行修改
        GameObject instance = PrefabUtility.InstantiatePrefab(dronePrefab) as GameObject;

        // 添加胶囊碰撞器（更稳定）
        CapsuleCollider capsuleCol = instance.GetComponent<CapsuleCollider>();
        if (capsuleCol == null)
        {
            capsuleCol = instance.AddComponent<CapsuleCollider>();
        }
        capsuleCol.radius = 0.5f;
        capsuleCol.height = 1f;
        capsuleCol.direction = 1; // Y轴方向
        capsuleCol.isTrigger = false; // 实体碰撞器，可以与地面碰撞

        // 添加Rigidbody
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = instance.AddComponent<Rigidbody>();
        }
        rb.mass = 0.1f; // 很轻的质量，减少碰撞影响
        rb.linearDamping = 5f; // 高阻力，快速稳定
        rb.angularDamping = 10f; // 高角阻力
        rb.isKinematic = false; // 启用物理，让它能落地
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 限制翻滚

        // 添加控制器脚本
        DroneController controller = instance.GetComponent<DroneController>();
        if (controller == null)
        {
            controller = instance.AddComponent<DroneController>();
        }

        // 保存修改到预制体
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log($"已为Drone预制体添加组件：CapsuleCollider(实体), Rigidbody(物理), DroneController");
        AssetDatabase.Refresh();
    }

    void SetupDrillCarPrefab()
    {
        // 查找DrillCar预制体
        string[] guids = AssetDatabase.FindAssets("DrillCar t:Prefab");
        if (guids.Length == 0)
        {
            // 尝试其他可能的名称
            guids = AssetDatabase.FindAssets("Drill Car t:Prefab");
            if (guids.Length == 0)
            {
                Debug.LogWarning("未找到DrillCar或Drill Car预制体");
                return;
            }
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject drillCarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (drillCarPrefab == null)
        {
            Debug.LogError("无法加载DrillCar预制体");
            return;
        }

        // 创建预制体实例进行修改
        GameObject instance = PrefabUtility.InstantiatePrefab(drillCarPrefab) as GameObject;

        // 添加胶囊碰撞器（更稳定）
        CapsuleCollider capsuleCol = instance.GetComponent<CapsuleCollider>();
        if (capsuleCol == null)
        {
            capsuleCol = instance.AddComponent<CapsuleCollider>();
        }
        capsuleCol.radius = 0.6f;
        capsuleCol.height = 1.2f;
        capsuleCol.direction = 1; // Y轴方向
        capsuleCol.isTrigger = false; // 实体碰撞器，可以与地面碰撞

        // 添加Rigidbody
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = instance.AddComponent<Rigidbody>();
        }
        rb.mass = 1000f; // 较重质量，提高稳定性
        rb.linearDamping = 3f; // 适中阻力，便于控制
        rb.angularDamping = 10f; // 高角阻力，防止翻滚
        rb.centerOfMass = new Vector3(0, -1f, 0); // 很低的重心，防止翻倒
        rb.isKinematic = true; // 初始为运动学，驾驶时会改为物理模式
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 防止翻滚，只允许Y轴旋转

        // 添加控制器脚本
        DrillCarController controller = instance.GetComponent<DrillCarController>();
        if (controller == null)
        {
            controller = instance.AddComponent<DrillCarController>();
        }

        // 保存修改到预制体
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log($"已为DrillCar预制体添加组件：CapsuleCollider(实体), Rigidbody(物理), DrillCarController");
        AssetDatabase.Refresh();
    }

    void CleanupAllPrefabs()
    {
        CleanupDronePrefab();
        CleanupDrillCarPrefab();
    }

    void CleanupDronePrefab()
    {
        string[] guids = AssetDatabase.FindAssets("Drone t:Prefab");
        if (guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (dronePrefab == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(dronePrefab) as GameObject;

        // 移除组件
        DestroyImmediate(instance.GetComponent<CapsuleCollider>());
        DestroyImmediate(instance.GetComponent<Rigidbody>());
        DestroyImmediate(instance.GetComponent<DroneController>());

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log("已清理Drone预制体组件");
    }

    void CleanupDrillCarPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("DrillCar t:Prefab");
        if (guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("Drill Car t:Prefab");
            if (guids.Length == 0) return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject drillCarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (drillCarPrefab == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(drillCarPrefab) as GameObject;

        // 移除组件
        DestroyImmediate(instance.GetComponent<CapsuleCollider>());
        DestroyImmediate(instance.GetComponent<Rigidbody>());
        DestroyImmediate(instance.GetComponent<DrillCarController>());

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log("已清理DrillCar预制体组件");
    }
}