using UnityEngine;

public class DroneTool : PlaceableTool
{
    [Header("Drone Settings")]
    public float minPlacementHeight = 1f;
    public float maxPlacementHeight = 50f;
    
    protected override void Start()
    {
        base.Start();
        
        toolName = "无人机";
        useRange = 100f;
        useCooldown = 2f;
        placementOffset = 1.0f;
        
        if (prefabToPlace == null)
        {
            
        }
    }
    
    protected override Quaternion GetPlacementRotation(RaycastHit hit)
    {
        return Quaternion.identity;
    }
    
    protected override bool CanPlaceAtPosition(Vector3 position)
    {
        if (position.y < minPlacementHeight)
        {
            
            return false;
        }
        
        if (position.y > maxPlacementHeight)
        {
            
            return false;
        }
        
        Collider[] overlapping = Physics.OverlapSphere(position, 2f);
        foreach (var col in overlapping)
        {
            if (col.name.Contains("Drone") && col.GetComponent<DroneController>() != null)
            {
                
                return false;
            }
        }
        
        
        return true;
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        // 确保无人机有必要的组件
        SetupDroneComponents(placedObject);
        
        // 检查组件是否存在（用于调试）
        bool hasCollider = placedObject.GetComponent<Collider>() != null;
        bool hasRigidbody = placedObject.GetComponent<Rigidbody>() != null;
        bool hasController = placedObject.GetComponent<DroneController>() != null;
        
        Debug.Log($"无人机组件检查 - Collider: {hasCollider}, Rigidbody: {hasRigidbody}, Controller: {hasController}");
    }
    
    void SetupDroneComponents(GameObject drone)
    {
        // 确保有DroneController
        DroneController controller = drone.GetComponent<DroneController>();
        if (controller == null)
        {
            controller = drone.AddComponent<DroneController>();
            Debug.Log("已添加DroneController组件");
        }
        
        // 确保有Rigidbody
        Rigidbody rb = drone.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = drone.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
            Debug.Log("已添加Rigidbody组件");
        }
        
        // 确保有碰撞器
        Collider col = drone.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = drone.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            Debug.Log("已添加SphereCollider组件");
        }
    }
    
    protected override GameObject GetTemplateObject()
    {
        if (prefabToPlace != null)
        {
            return prefabToPlace;
        }
        
        // 尝试在场景中查找Drone
        GameObject droneInScene = GameObject.Find("Drone");
        if (droneInScene != null)
        {
            return droneInScene;
        }
        
        // 使用AssetDatabase查找预制体（仅在Editor中有效）
        #if UNITY_EDITOR
        string[] searchTerms = {"Drone", "drone"};
        foreach (string searchTerm in searchTerms)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(searchTerm + " t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject dronePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (dronePrefab != null)
                {
                    prefabToPlace = dronePrefab;
                    return dronePrefab;
                }
            }
        }
        #endif
        
        return null;
    }
    
    protected override void OnEquip()
    {
        base.OnEquip();
        
    }
}