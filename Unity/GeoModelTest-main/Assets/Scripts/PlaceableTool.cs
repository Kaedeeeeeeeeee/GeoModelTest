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
        if (previewObject != null) return;
        
        GameObject templateObject = GetTemplateObject();
        if (templateObject != null)
        {
            try
            {
                previewObject = Instantiate(templateObject);
                previewObject.name = templateObject.name + "_Preview";
                
                SetupPreviewObject();
                previewObject.SetActive(false);
                
                Debug.Log($"创建预览对象: {previewObject.name}");
            }
            catch (System.InvalidCastException e)
            {
                Debug.LogError($"类型转换错误，模板对象类型: {templateObject.GetType()}, 错误: {e.Message}");
                previewObject = null;
            }
        }
        else
        {
            Debug.LogWarning($"无法创建预览对象，未找到模板对象: {toolName}");
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
        Debug.Log($"设置预览对象 {previewObject.name}，发现 {renderers.Length} 个渲染器");
        
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
                Debug.Log($"为渲染器设置了半透明材质");
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
            
            Debug.Log($"射线击中: {hit.collider.name}, 位置: {hit.point}, Layer: {hit.collider.gameObject.layer}");
        }
        else
        {
            previewObject.SetActive(false);
            
            if (Physics.Raycast(ray, out RaycastHit anyHit, useRange))
            {
                Debug.LogWarning($"射线击中了其他层级的对象: {anyHit.collider.name}, Layer: {anyHit.collider.gameObject.layer}, 期望Layer: {groundLayers.value}");
            }
            else
            {
                Debug.LogWarning($"射线没有击中任何对象，射线长度: {useRange}, groundLayers: {groundLayers.value}");
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
            Debug.Log($"道具 {toolName} 已经使用过，无法再次放置");
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
        
        Debug.Log($"进入放置模式: {toolName}, useRange: {useRange}, groundLayers: {groundLayers.value}");
    }
    
    protected virtual void ExitPlacementMode()
    {
        isPlacementMode = false;
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log($"退出放置模式: {toolName}");
    }
    
    protected virtual void TryPlaceObject()
    {
        if (previewObject == null || !previewObject.activeInHierarchy) 
        {
            Debug.LogWarning($"无法放置对象 - 预览对象: {previewObject?.name}, 激活状态: {previewObject?.activeInHierarchy}");
            return;
        }
        
        Vector3 placementPosition = previewObject.transform.position;
        Quaternion placementRotation = previewObject.transform.rotation;
        
        Debug.Log($"尝试放置对象在位置: {placementPosition}");
        
        if (CanPlaceAtPosition(placementPosition))
        {
            PlaceObject(placementPosition, placementRotation);
            
            lastUseTime = Time.time;
            canUse = false;
            hasPlacedObject = true; // 标记已放置对象，防止再次进入放置模式
            PlayUseSound();
            
            ExitPlacementMode();
        }
        else
        {
            Debug.LogWarning("位置检查失败，无法放置对象");
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
            Debug.Log($"放置前玩家位置: {playerCamera.transform.position}");
            GameObject placedObject = Instantiate(templateObject, position, rotation);
            
            // 直接保持模板对象的原始缩放，不做任何修改
            Debug.Log($"模板对象缩放: {templateObject.transform.localScale}");
            Debug.Log($"保持模型原始缩放: {placedObject.transform.localScale}");
            
            OnObjectPlaced(placedObject);
            Debug.Log($"成功放置了 {toolName} 在位置: {position}，放置后玩家位置: {playerCamera.transform.position}");
        }
        else
        {
            Debug.LogWarning($"无法放置 {toolName} - 模板对象为空");
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