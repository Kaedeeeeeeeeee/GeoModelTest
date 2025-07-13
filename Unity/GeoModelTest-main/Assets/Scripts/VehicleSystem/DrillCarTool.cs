using UnityEngine;

public class DrillCarTool : PlaceableTool
{
    [Header("Drill Car Settings")]
    public float minPlacementHeight = 0f;
    public float maxPlacementHeight = 50f;
    
    private GameObject placedDrillCar;
    
    protected override void Start()
    {
        base.Start();
        
        toolName = "钻探车";
        useRange = 100f;
        useCooldown = 2f;
        placementOffset = 1.0f;
        
        if (prefabToPlace == null)
        {
            // 将在GetTemplateObject中查找预制体
        }
    }
    
    protected override Quaternion GetPlacementRotation(RaycastHit hit)
    {
        return Quaternion.identity;
    }
    
    protected override bool CanUseOnTarget(RaycastHit hit)
    {
        if (hasPlacedObject) return false;
        
        // 检查是否有DrillCar重叠
        Collider[] overlapping = Physics.OverlapSphere(hit.point, 2f);
        foreach (var col in overlapping)
        {
            if (col.name.Contains("DrillCar") && col.GetComponent<DrillCarController>() != null)
            {
                return false;
            }
        }
        
        return true;
    }
    
    protected override void UseTool(RaycastHit hit)
    {
        // 这个方法在PlaceableTool中是空的，实际放置通过PlaceObject完成
    }
    
    void SetupDrillCarComponents(GameObject drillCar)
    {
        // 确保有DrillCarController
        DrillCarController controller = drillCar.GetComponent<DrillCarController>();
        if (controller == null)
        {
            controller = drillCar.AddComponent<DrillCarController>();
        }
        
        // 确保有Rigidbody
        Rigidbody rb = drillCar.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = drillCar.AddComponent<Rigidbody>();
            rb.mass = 1000f;
            rb.centerOfMass = new Vector3(0, -0.5f, 0);
            rb.linearDamping = 5f;  // 增加线性阻力，减少滑动
            rb.angularDamping = 15f; // 增加角度阻力，减少摇摆
            rb.isKinematic = true; // 初始为运动学模式
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // 确保有碰撞器
        Collider col = drillCar.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = drillCar.AddComponent<CapsuleCollider>();
            capsule.radius = 0.8f;
            capsule.height = 1.5f;
            capsule.direction = 1; // Y轴方向
            capsule.center = new Vector3(0, 0.75f, 0); // 调整中心点
        }
        
        // 设置图层（如果需要）
        if (drillCar.layer == 0) // Default layer
        {
            drillCar.layer = LayerMask.NameToLayer("Default");
        }
        
        // 保持默认标签，不设置Vehicle标签
    }
    
    
    
    protected override GameObject GetTemplateObject()
    {
        if (prefabToPlace != null)
        {
            return prefabToPlace;
        }
        
        // 尝试在场景中查找DrillCar
        GameObject drillCarInScene = GameObject.Find("DrillCar");
        if (drillCarInScene == null)
        {
            drillCarInScene = GameObject.Find("Drill Car");
        }
        if (drillCarInScene != null)
        {
            return drillCarInScene;
        }
        
        // 使用AssetDatabase查找预制体（仅在Editor中有效）
        #if UNITY_EDITOR
        string[] searchTerms = {"DrillCar", "Drill Car", "drill car", "drill"};
        foreach (string searchTerm in searchTerms)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(searchTerm + " t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject carPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (carPrefab != null)
                {
                    prefabToPlace = carPrefab;
                    return carPrefab;
                }
            }
        }
        #endif
        
        return null;
    }
    
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        // 确保钻探车有必要的组件
        SetupDrillCarComponents(placedObject);
        
        placedDrillCar = placedObject;
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
            if (col.name.Contains("DrillCar") && col.GetComponent<DrillCarController>() != null)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public GameObject GetPlacedDrillCar()
    {
        return placedDrillCar;
    }
    
}