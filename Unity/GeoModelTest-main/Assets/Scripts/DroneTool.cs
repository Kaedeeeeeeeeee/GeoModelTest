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
            Debug.LogWarning("无人机预制体未分配，请在Inspector中设置prefabToPlace字段");
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
            Debug.Log("无人机放置位置太低");
            return false;
        }
        
        if (position.y > maxPlacementHeight)
        {
            Debug.Log("无人机放置位置太高");
            return false;
        }
        
        Collider[] overlapping = Physics.OverlapSphere(position, 2f);
        foreach (var col in overlapping)
        {
            if (col.name.Contains("Drone") && col.GetComponent<DroneController>() != null)
            {
                Debug.Log("此位置已有无人机");
                return false;
            }
        }
        
        Debug.Log("无人机可以放置在此位置");
        return true;
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        // 预制体应该已经包含所有必要组件，无需运行时添加
        Debug.Log($"无人机已成功放置在: {placedObject.transform.position}");
        
        // 检查组件是否存在（用于调试）
        bool hasCollider = placedObject.GetComponent<Collider>() != null;
        bool hasRigidbody = placedObject.GetComponent<Rigidbody>() != null;
        bool hasController = placedObject.GetComponent<DroneController>() != null;
        
        Debug.Log($"无人机组件检查 - Collider: {hasCollider}, Rigidbody: {hasRigidbody}, Controller: {hasController}");
    }
    
    protected override GameObject GetTemplateObject()
    {
        if (prefabToPlace != null)
        {
            Debug.Log($"使用prefabToPlace: {prefabToPlace.name}");
            return prefabToPlace;
        }
        
        GameObject droneInScene = GameObject.Find("Drone");
        if (droneInScene != null)
        {
            Debug.Log($"使用场景中的Drone对象作为模板: {droneInScene.name}, 类型: {droneInScene.GetType()}");
            return droneInScene;
        }
        
        Debug.LogWarning("未找到Drone对象");
        return null;
    }
    
    protected override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("选择了无人机工具 - 点击左键进入放置模式");
    }
}