using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlaceableTool : CollectionTool
{
    [Header("Placement Settings")]
    public GameObject prefabToPlace;
    public LayerMask groundLayers = 1;
    public float placementOffset = 0f;
    public Material previewMaterial;
    
    protected GameObject previewObject;
    protected bool isPlacementMode = false;
    protected bool hasPlacedObject = false; // 标记是否已经放置过对象
    
    protected override void Start()
    {
        base.Start();
    }
    
    protected virtual void CreatePreviewObject()
    {
        // 如果已经有预览对象，先清理掉
        if (previewObject != null) 
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
        
        GameObject templateObject = GetTemplateObject();
        if (templateObject != null)
        {
            try
            {
                previewObject = Instantiate(templateObject);
                previewObject.name = templateObject.name + "_Preview";
                
                SetupPreviewObject();
                previewObject.SetActive(false);
                
            }
            catch (System.InvalidCastException e)
            {
                previewObject = null;
            }
        }
    }
    
    protected virtual GameObject GetTemplateObject()
    {
        if (prefabToPlace != null)
        {
            return prefabToPlace;
        }
        
        return null;
    }
    
    protected virtual void SetupPreviewObject()
    {
        if (previewObject == null) return;
        
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        
        if (previewMaterial != null)
        {
            foreach (var renderer in renderers)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }
                renderer.materials = materials;
            }
        }
        else
        {
            foreach (var renderer in renderers)
            {
                Material[] newMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    Material originalMat = renderer.materials[i];
                    Material newMat = new Material(originalMat);
                    
                    Color color = newMat.color;
                    color.a = 0.5f;
                    newMat.color = color;
                    
                    if (newMat.HasProperty("_Color"))
                    {
                        newMat.SetColor("_Color", color);
                    }
                    
                    newMaterials[i] = newMat;
                }
                renderer.materials = newMaterials;
            }
        }
    }
    
    protected override void HandleInput()
    {
        if (isPlacementMode)
        {
            UpdatePreviewPosition();
            
            if (Mouse.current.leftButton.wasPressedThisFrame && canUse)
            {
                TryPlaceObject();
            }
            
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitPlacementMode();
            }
        }
        else
        {
            // 只有在未放置过对象时才允许通过鼠标左键进入放置模式
            if (Mouse.current.leftButton.wasPressedThisFrame && canUse && !hasPlacedObject)
            {
                EnterPlacementMode();
            }
        }
    }
    
    protected virtual void UpdatePreviewPosition()
    {
        if (previewObject == null) return;
        
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        Debug.DrawRay(ray.origin, ray.direction * useRange, Color.red, 0.1f);
        
        if (Physics.Raycast(ray, out RaycastHit hit, useRange, groundLayers))
        {
            Vector3 placementPosition = hit.point + Vector3.up * placementOffset;
            previewObject.transform.position = placementPosition;
            previewObject.transform.rotation = GetPlacementRotation(hit);
            previewObject.SetActive(true);
            
        }
        else
        {
            previewObject.SetActive(false);
            
            if (Physics.Raycast(ray, out RaycastHit anyHit, useRange))
            {
            }
            else
            {
            }
        }
    }
    
    protected virtual Quaternion GetPlacementRotation(RaycastHit hit)
    {
        return Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCamera.transform.forward, hit.normal), hit.normal);
    }
    
    public virtual void EnterPlacementMode()
    {
        // 如果已经放置过对象，则不允许再次进入放置模式
        if (hasPlacedObject)
        {
            return;
        }
        
        CreatePreviewObject();
        
        isPlacementMode = true;
        if (previewObject != null)
        {
            previewObject.SetActive(false);  // 先设为false，让UpdatePreviewPosition控制
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
    }
    
    protected virtual void ExitPlacementMode()
    {
        isPlacementMode = false;
        if (previewObject != null)
        {
            previewObject.SetActive(false);
            DestroyImmediate(previewObject);
            previewObject = null;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }
    
    protected virtual void TryPlaceObject()
    {
        if (previewObject == null || !previewObject.activeInHierarchy) 
        {
            return;
        }
        
        Vector3 placementPosition = previewObject.transform.position;
        Quaternion placementRotation = previewObject.transform.rotation;
        
        
        if (CanPlaceAtPosition(placementPosition))
        {
            PlaceObject(placementPosition, placementRotation);
            
            lastUseTime = Time.time;
            canUse = false;
            hasPlacedObject = true; // 标记已放置对象，防止再次进入放置模式
            PlayUseSound();
            
            // 清理预览对象
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
                previewObject = null;
            }
            
            ExitPlacementMode();
        }
    }
    
    protected virtual bool CanPlaceAtPosition(Vector3 position)
    {
        return true;
    }
    
    protected virtual void PlaceObject(Vector3 position, Quaternion rotation)
    {
        GameObject templateObject = GetTemplateObject();
        if (templateObject != null)
        {
            GameObject placedObject = Instantiate(templateObject, position, rotation);
            
            // 直接保持模板对象的原始缩放，不做任何修改
            
            OnObjectPlaced(placedObject);
        }
        else
        {
        }
    }
    
    protected virtual void OnObjectPlaced(GameObject placedObject)
    {
    }
    
    protected override void UseTool(RaycastHit hit)
    {
    }
    
    public override void Equip()
    {
        base.Equip();
        playerCamera = Camera.main;
    }
    
    public override void Unequip()
    {
        base.Unequip();
        ExitPlacementMode();
    }
    
    protected virtual void OnDestroy()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
    }
}