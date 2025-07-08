using UnityEngine;

public class DrillCarTool : PlaceableTool
{
    [Header("Drill Car Settings")]
    public float slopeLimit = 30f;
    public float clearanceRadius = 3f;
    
    protected override void Start()
    {
        base.Start();
        
        toolName = "钻探车";
        useRange = 50f;
        useCooldown = 3f;
        placementOffset = 1.0f;
        
        if (prefabToPlace == null)
        {
            Debug.LogWarning("钻探车预制体未分配，请在Inspector中设置prefabToPlace字段");
        }
    }
    
    protected override Quaternion GetPlacementRotation(RaycastHit hit)
    {
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, hit.normal);
        if (forward == Vector3.zero)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, hit.normal);
        }
        
        return Quaternion.LookRotation(forward, hit.normal);
    }
    
    protected override bool CanPlaceAtPosition(Vector3 position)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        if (Physics.Raycast(ray, out RaycastHit hit, useRange, groundLayers))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > slopeLimit)
            {
                Debug.Log($"地面坡度太陡({slope:F1}°)，钻探车无法放置");
                return false;
            }
        }
        
        Collider[] overlapping = Physics.OverlapSphere(position, clearanceRadius);
        foreach (var col in overlapping)
        {
            if (col.name.Contains("Drill Car") && col.GetComponent<DrillCarController>() != null)
            {
                Debug.Log("此位置已有钻探车");
                return false;
            }
            
            if (col.name.Contains("Player") || col.name.Contains("FirstPerson"))
            {
                continue;
            }
            
            if (col.name.Contains("dem") || col.name.Contains("Terrain") || col.name.Contains("Ground"))
            {
                continue;
            }
            
            if (col.bounds.size.y > 2f && position.y + 1f < col.bounds.max.y)
            {
                Debug.Log($"此位置有高障碍物({col.name}, 高度:{col.bounds.size.y})，钻探车无法放置");
                return false;
            }
        }
        
        Debug.Log("钻探车可以放置在此位置");
        return true;
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        // 预制体应该已经包含所有必要组件，无需运行时添加
        Debug.Log($"钻探车已成功放置在: {placedObject.transform.position}");
        
        // 检查组件是否存在（用于调试）
        bool hasCollider = placedObject.GetComponent<Collider>() != null;
        bool hasRigidbody = placedObject.GetComponent<Rigidbody>() != null;
        bool hasController = placedObject.GetComponent<DrillCarController>() != null;
        
        Debug.Log($"钻探车组件检查 - Collider: {hasCollider}, Rigidbody: {hasRigidbody}, Controller: {hasController}");
    }
    
    protected override GameObject GetTemplateObject()
    {
        if (prefabToPlace != null)
        {
            Debug.Log($"使用prefabToPlace: {prefabToPlace.name}");
            return prefabToPlace;
        }
        
        GameObject drillCarInScene = GameObject.Find("Drill Car");
        if (drillCarInScene != null)
        {
            Debug.Log($"使用场景中的Drill Car对象作为模板: {drillCarInScene.name}, 类型: {drillCarInScene.GetType()}");
            return drillCarInScene;
        }
        
        Debug.LogWarning("未找到Drill Car对象");
        return null;
    }
    
    protected override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("选择了钻探车工具 - 点击左键进入放置模式");
    }
}