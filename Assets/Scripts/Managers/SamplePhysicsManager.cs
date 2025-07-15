using UnityEngine;

public class SamplePhysicsManager : MonoBehaviour
{
    [Header("Physics Settings")]
    public float mass = 1f;
    public float drag = 0.5f;
    public float angularDrag = 0.5f;
    public bool useGravity = true;
    
    private Rigidbody rb;
    private Collider mainCollider;
    private Collider triggerCollider;
    
    void Start()
    {
        SetupPhysics();
    }
    
    void SetupPhysics()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = useGravity;
        
        Collider[] colliders = GetComponents<Collider>();
        
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
            }
            else
            {
                mainCollider = col;
            }
        }
        
        if (mainCollider == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = transform.localScale.y * 2;
            capsule.radius = transform.localScale.x / 2;
            mainCollider = capsule;
        }
        
        
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            return;
        
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            rb.isKinematic = true;
            
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GeologicalSample sample = GetComponent<GeologicalSample>();
            if (sample != null)
            {
                
            }
        }
    }
}