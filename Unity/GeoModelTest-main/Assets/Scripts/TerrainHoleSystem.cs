using UnityEngine;
using System.Collections.Generic;

public class TerrainHoleSystem : MonoBehaviour
{
    [Header("Hole System")]
    public Material holeMaterial;
    public List<HoleData> holes = new List<HoleData>();
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh originalMesh;
    private Mesh modifiedMesh;
    
    [System.Serializable]
    public class HoleData
    {
        public Vector3 position;
        public float radius;
        public float depth;
        public Vector3 normal;
        
        public HoleData(Vector3 pos, float r, float d, Vector3 n)
        {
            position = pos;
            radius = r;
            depth = d;
            normal = n;
        }
    }
    
    void Start()
    {
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
        if (meshFilter != null && meshFilter.mesh != null)
        {
            originalMesh = meshFilter.mesh;
            modifiedMesh = Instantiate(originalMesh);
            meshFilter.mesh = modifiedMesh;
        }
    }
    
    public void CreateCylindricalHole(Vector3 worldPosition, float radius, float depth, Vector3 normal)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector3 localNormal = transform.InverseTransformDirection(normal);
        
        HoleData newHole = new HoleData(localPosition, radius, depth, localNormal);
        holes.Add(newHole);
        
        Debug.Log($"在位置 {localPosition} 创建了半径 {radius}，深度 {depth} 的圆柱形洞");
        
        CreateHoleVisual(newHole);
    }
    
    void CreateHoleVisual(HoleData hole)
    {
        GameObject holeObj = new GameObject($"Hole_{holes.Count}");
        holeObj.transform.SetParent(transform);
        
        Vector3 worldPos = transform.TransformPoint(hole.position);
        holeObj.transform.position = worldPos;
        
        Vector3 holeDirection = -hole.normal;
        if (holeDirection != Vector3.zero)
        {
            holeObj.transform.rotation = Quaternion.LookRotation(holeDirection);
        }
        
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(holeObj.transform);
        cylinder.transform.localPosition = Vector3.forward * (hole.depth / 2);
        cylinder.transform.localScale = new Vector3(hole.radius * 2, hole.depth / 2, hole.radius * 2);
        
        MeshRenderer cylinderRenderer = cylinder.GetComponent<MeshRenderer>();
        if (holeMaterial != null)
        {
            cylinderRenderer.material = holeMaterial;
        }
        else
        {
            Material darkMaterial = new Material(Shader.Find("Standard"));
            darkMaterial.color = Color.black;
            darkMaterial.SetFloat("_Smoothness", 0f);
            cylinderRenderer.material = darkMaterial;
        }
        
        MeshCollider cylinderCollider = cylinder.GetComponent<MeshCollider>();
        if (cylinderCollider != null)
        {
            cylinderCollider.convex = true;
            cylinderCollider.isTrigger = true;
        }
        
        cylinder.AddComponent<HoleMarker>();
    }
    
    public void RemoveAllHoles()
    {
        holes.Clear();
        
        HoleMarker[] holeMarkers = GetComponentsInChildren<HoleMarker>();
        for (int i = 0; i < holeMarkers.Length; i++)
        {
            if (holeMarkers[i] != null)
            {
                DestroyImmediate(holeMarkers[i].transform.parent.gameObject);
            }
        }
        
        if (meshFilter != null && originalMesh != null)
        {
            if (modifiedMesh != null)
            {
                DestroyImmediate(modifiedMesh);
            }
            modifiedMesh = Instantiate(originalMesh);
            meshFilter.mesh = modifiedMesh;
        }
    }
    
    public bool HasHoleAt(Vector3 worldPosition, float tolerance = 0.5f)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        
        foreach (var hole in holes)
        {
            float distance = Vector3.Distance(localPosition, hole.position);
            if (distance <= hole.radius + tolerance)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var hole in holes)
        {
            Vector3 worldPos = transform.TransformPoint(hole.position);
            Gizmos.DrawWireSphere(worldPos, hole.radius);
            
            Vector3 holeEnd = worldPos - transform.TransformDirection(hole.normal) * hole.depth;
            Gizmos.DrawLine(worldPos, holeEnd);
        }
    }
}

public class HoleMarker : MonoBehaviour
{
    // 用于标记洞穴对象的简单组件
}