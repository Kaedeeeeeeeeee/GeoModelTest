using UnityEngine;

public class GeologicalSample : MonoBehaviour
{
    [Header("Sample Properties")]
    public string sampleName = "地层样本";
    public string sampleType = "未知";
    public Vector3 extractionPosition;
    public Quaternion extractionRotation;
    public Material originalMaterial;
    
    [Header("Sample Visual")]
    public float sampleRadius = 0.1f;
    public float sampleHeight = 1f;
    
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    
    void Start()
    {
        SetupSampleVisual();
    }
    
    void SetupSampleVisual()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        CreateCylinderMesh();
        
        if (originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }
    
    void CreateCylinderMesh()
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cylinderMesh = cylinder.GetComponent<MeshFilter>().mesh;
        
        meshFilter.mesh = cylinderMesh;
        
        transform.localScale = new Vector3(sampleRadius * 2, sampleHeight / 2, sampleRadius * 2);
        
        DestroyImmediate(cylinder);
    }
    
    public void Initialize(Vector3 position, Quaternion rotation, Material material, string type)
    {
        extractionPosition = position;
        extractionRotation = rotation;
        originalMaterial = material;
        sampleType = type;
        sampleName = $"{type} 样本";
        
        transform.position = position;
        transform.rotation = rotation;
        
        SetupSampleVisual();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
        }
    }
}