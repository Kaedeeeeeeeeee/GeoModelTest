using UnityEngine;

[System.Serializable]
public class GeologyLayer : MonoBehaviour
{
    [Header("地层基本信息")]
    public string layerName = "未命名地层";
    public LayerType layerType = LayerType.Sedimentary;
    public string geologicalAge = "未知年代";
    public string formation = "未知组";
    
    [Header("地层物理属性")]
    public Material layerMaterial;
    public Color layerColor = Color.gray;
    public float density = 2.5f;
    public float hardness = 5.0f;
    
    [Header("地层几何属性")]
    public float averageThickness = 1.0f;
    public Vector3 strikeDirection = Vector3.forward;
    public float dipAngle = 0f; // 倾斜角度
    
    [Header("教学信息")]
    [TextArea(3, 5)]
    public string description = "地层描述信息";
    public string formationEnvironment = "沉积环境";
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Bounds layerBounds;
    
    void Start()
    {
        InitializeLayer();
    }
    
    void InitializeLayer()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
        if (meshFilter != null && meshFilter.mesh != null)
        {
            layerBounds = meshFilter.mesh.bounds;
            layerBounds.center = transform.TransformPoint(layerBounds.center);
            layerBounds.size = Vector3.Scale(layerBounds.size, transform.lossyScale);
        }
        
        // 尝试设置标签，如果不存在则跳过
        try
        {
            if (gameObject.tag != "GeologyLayer")
            {
                gameObject.tag = "GeologyLayer";
            }
        }
        catch (UnityException)
        {
        }
        
    }
    
    public Vector3 GetNormalAtPoint(Vector3 worldPoint)
    {
        if (meshCollider != null)
        {
            try
            {
                // 对于凸形碰撞体，使用ClosestPoint
                if (meshCollider.convex)
                {
                    Vector3 closestPoint = meshCollider.ClosestPoint(worldPoint);
                    Vector3 normal = (worldPoint - closestPoint).normalized;
                    
                    if (normal == Vector3.zero)
                    {
                        normal = GetGeneralNormal();
                    }
                    
                    return normal;
                }
            }
            catch (System.Exception)
            {
                // ClosestPoint失败，使用一般法向量
            }
        }
        
        // 对于非凸网格或失败情况，使用计算的一般法向量
        return GetGeneralNormal();
    }
    
    public Vector3 GetGeneralNormal()
    {
        // 根据走向和倾角计算地层法向量
        Vector3 strike = strikeDirection.normalized;
        Vector3 dip = Vector3.Cross(Vector3.up, strike);
        
        // 应用倾斜角度
        float dipRadians = dipAngle * Mathf.Deg2Rad;
        Vector3 normal = Vector3.up * Mathf.Cos(dipRadians) + dip * Mathf.Sin(dipRadians);
        
        return transform.TransformDirection(normal);
    }
    
    public float GetThicknessAtPoint(Vector3 worldPoint)
    {
        // 这里可以实现基于点的厚度计算
        // 目前返回平均厚度，后续可以根据实际地层几何进行改进
        return averageThickness;
    }
    
    public bool ContainsPoint(Vector3 worldPoint)
    {
        if (meshCollider != null)
        {
            Vector3 closestPoint = meshCollider.ClosestPoint(worldPoint);
            return Vector3.Distance(worldPoint, closestPoint) < 0.01f;
        }
        
        return layerBounds.Contains(worldPoint);
    }
    
    public LayerInfo CreateLayerInfo(Vector2[] boundaryShape, float areaPercentage, Vector3 samplePoint)
    {
        return new LayerInfo
        {
            layer = this,
            areaPercentage = areaPercentage,
            boundaryShape = boundaryShape,
            normalDirection = GetNormalAtPoint(samplePoint),
            thickness = GetThicknessAtPoint(samplePoint),
            color = layerColor,
            material = layerMaterial
        };
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = layerColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Gizmos.DrawWireMesh(meshFilter.mesh);
        }
        
        // 绘制地层法向量
        Gizmos.color = Color.red;
        Vector3 center = transform.position;
        Vector3 normal = GetGeneralNormal();
        Gizmos.DrawRay(center, normal * 2f);
        
        // 绘制走向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, transform.TransformDirection(strikeDirection) * 2f);
    }
}

public enum LayerType
{
    Sedimentary,    // 沉积岩
    Igneous,        // 火成岩
    Metamorphic,    // 变质岩
    Soil,           // 土壤层
    Alluvium,       // 冲积层
    Bedrock         // 基岩
}