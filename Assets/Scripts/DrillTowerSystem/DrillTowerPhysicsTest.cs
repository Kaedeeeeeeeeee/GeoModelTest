using UnityEngine;

/// <summary>
/// 钻塔物理效果测试器 - 验证钻塔是否正确落地
/// </summary>
public class DrillTowerPhysicsTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool enablePhysicsTest = true;
    public float testInterval = 3f; // 每3秒检测一次
    
    void Start()
    {
        if (enablePhysicsTest)
        {
            InvokeRepeating(nameof(TestTowerPhysics), 2f, testInterval);
            Debug.Log("🧪 钻塔物理测试器已启动");
        }
    }
    
    /// <summary>
    /// 测试钻塔物理效果
    /// </summary>
    void TestTowerPhysics()
    {
        // 查找所有钻塔
        DrillTower[] towers = FindObjectsOfType<DrillTower>();
        
        if (towers.Length == 0)
        {
            // 通过名称查找钻塔对象
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("DrillTower") && obj.activeInHierarchy)
                {
                    TestSingleTower(obj);
                }
            }
        }
        else
        {
            foreach (DrillTower tower in towers)
            {
                TestSingleTower(tower.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 测试单个钻塔的物理状态
    /// </summary>
    void TestSingleTower(GameObject towerObj)
    {
        Debug.Log($"🧪 测试钻塔物理状态: {towerObj.name}");
        
        // 检查位置
        Vector3 position = towerObj.transform.position;
        Debug.Log($"   📍 当前位置: {position}");
        
        // 检查是否在地面附近
        if (position.y > 10f)
        {
            Debug.LogWarning($"   ⚠️ 钻塔位置过高: {position.y:F2}m，可能在空中飘浮");
        }
        else if (position.y < -2f)
        {
            Debug.LogWarning($"   ⚠️ 钻塔位置过低: {position.y:F2}m，可能掉落到地下");
        }
        else
        {
            Debug.Log($"   ✅ 钻塔高度正常: {position.y:F2}m");
        }
        
        // 检查Rigidbody组件
        Rigidbody rb = towerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"   🔧 Rigidbody状态:");
            Debug.Log($"      质量: {rb.mass}kg");
            Debug.Log($"      重力: {rb.useGravity}");
            Debug.Log($"      运动学: {rb.isKinematic}");
            Debug.Log($"      速度: {rb.linearVelocity.magnitude:F3}m/s");
            Debug.Log($"      冻结旋转: {rb.freezeRotation}");
            
            // 检查是否静止
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                Debug.LogWarning($"   ⚠️ 钻塔仍在移动，速度: {rb.linearVelocity.magnitude:F3}m/s");
            }
            else
            {
                Debug.Log($"   ✅ 钻塔已稳定静止");
            }
        }
        else
        {
            Debug.LogWarning($"   ❌ 钻塔缺少Rigidbody组件！");
        }
        
        // 检查碰撞器
        Collider[] colliders = towerObj.GetComponents<Collider>();
        Debug.Log($"   🔧 碰撞器数量: {colliders.Length}");
        
        foreach (Collider col in colliders)
        {
            Debug.Log($"      - {col.GetType().Name}: 触发器={col.isTrigger}, 启用={col.enabled}");
        }
        
        // 检查地面接触
        CheckGroundContact(towerObj);
    }
    
    /// <summary>
    /// 检查钻塔是否与地面接触
    /// </summary>
    void CheckGroundContact(GameObject towerObj)
    {
        Vector3 towerPos = towerObj.transform.position;
        Vector3 rayStart = towerPos + Vector3.up * 0.5f; // 从钻塔稍上方开始
        
        LayerMask groundLayers = 1; // Default layer
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 2f, groundLayers))
        {
            float distanceToGround = hit.distance - 0.5f; // 减去起始偏移
            Debug.Log($"   🌍 地面接触检测:");
            Debug.Log($"      距离地面: {distanceToGround:F3}m");
            Debug.Log($"      地面对象: {hit.collider.name}");
            
            if (distanceToGround < 0.1f)
            {
                Debug.Log($"   ✅ 钻塔正确接触地面");
            }
            else if (distanceToGround > 1f)
            {
                Debug.LogWarning($"   ⚠️ 钻塔离地面过远: {distanceToGround:F3}m");
            }
        }
        else
        {
            Debug.LogWarning($"   ❌ 钻塔下方未检测到地面");
        }
    }
    
    /// <summary>
    /// 手动修复钻塔物理问题
    /// </summary>
    [ContextMenu("修复所有钻塔物理问题")]
    public void FixAllTowerPhysics()
    {
        Debug.Log("🔧 开始修复所有钻塔物理问题...");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int fixedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("DrillTower") && obj.activeInHierarchy)
            {
                if (FixSingleTowerPhysics(obj))
                {
                    fixedCount++;
                }
            }
        }
        
        Debug.Log($"✅ 修复完成，共修复 {fixedCount} 个钻塔");
    }
    
    /// <summary>
    /// 修复单个钻塔的物理问题
    /// </summary>
    bool FixSingleTowerPhysics(GameObject towerObj)
    {
        bool needsFix = false;
        Debug.Log($"🔧 修复钻塔物理问题: {towerObj.name}");
        
        // 检查位置是否异常
        Vector3 pos = towerObj.transform.position;
        if (pos.y > 10f || pos.y < -2f)
        {
            // 尝试将钻塔放置到合理位置
            Vector3 fixedPos = new Vector3(pos.x, 5f, pos.z); // 临时高度，让它掉落
            towerObj.transform.position = fixedPos;
            needsFix = true;
            Debug.Log($"   修复了异常位置: {pos} → {fixedPos}");
        }
        
        // 确保Rigidbody设置正确
        Rigidbody rb = towerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.useGravity)
            {
                rb.useGravity = true;
                needsFix = true;
                Debug.Log($"   启用了重力");
            }
            
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                needsFix = true;
                Debug.Log($"   禁用了运动学模式");
            }
            
            if (!rb.freezeRotation)
            {
                rb.freezeRotation = true;
                needsFix = true;
                Debug.Log($"   冻结了旋转");
            }
        }
        
        return needsFix;
    }
}