using UnityEngine;
using UnityEngine.InputSystem;

public abstract class CollectionTool : MonoBehaviour
{
    [Header("Tool Settings")]
    public string toolName = "Collection Tool";
    public Sprite toolIcon;
    public GameObject toolModel;
    public float useRange = 5f;
    public float useCooldown = 1f;
    
    [Header("Audio")]
    public AudioClip useSound;
    
    protected bool isEquipped = false;
    protected bool canUse = true;
    protected Camera playerCamera;
    protected AudioSource audioSource;
    protected float lastUseTime = 0f;
    
    protected virtual void Start()
    {
        playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (toolModel != null)
        {
            toolModel.SetActive(false);
        }
    }
    
    protected virtual void Update()
    {
        if (isEquipped)
        {
            HandleInput();
            CheckCooldown();
        }
    }
    
    protected virtual void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && canUse)
        {
            TryUseTool();
        }
    }
    
    protected virtual void CheckCooldown()
    {
        if (!canUse && Time.time - lastUseTime >= useCooldown)
        {
            canUse = true;
        }
    }
    
    protected virtual void TryUseTool()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, useRange))
        {
            if (CanUseOnTarget(hit))
            {
                UseTool(hit);
                lastUseTime = Time.time;
                canUse = false;
                
                PlayUseSound();
            }
        }
    }
    
    protected virtual bool CanUseOnTarget(RaycastHit hit)
    {
        return true;
    }
    
    protected abstract void UseTool(RaycastHit hit);
    
    protected virtual void PlayUseSound()
    {
        if (useSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(useSound);
        }
    }
    
    public virtual void Equip()
    {
        isEquipped = true;
        if (toolModel != null)
        {
            toolModel.SetActive(true);
        }
        OnEquip();
    }
    
    public virtual void Unequip()
    {
        isEquipped = false;
        if (toolModel != null)
        {
            toolModel.SetActive(false);
        }
        OnUnequip();
    }
    
    protected virtual void OnEquip()
    {
        Debug.Log($"装备了工具: {toolName}");
    }
    
    protected virtual void OnUnequip()
    {
        Debug.Log($"卸下了工具: {toolName}");
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 forward = playerCamera.transform.forward;
            Gizmos.DrawRay(playerCamera.transform.position, forward * useRange);
        }
    }
}