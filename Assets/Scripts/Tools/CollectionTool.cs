using UnityEngine;
using UnityEngine.InputSystem;
using Core;

public abstract class CollectionTool : MonoBehaviour
{
    [Header("Tool Settings")]
    public string toolID = "0000"; // 工具ID用于排序和分类
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
            if (GetPlayerCamera() == null) return;
            HandleInput();
            CheckCooldown();
        }
    }
    
    protected virtual void HandleInput()
    {
        if (WasPrimaryUsePressed() && canUse)
        {
            RequestPrimaryUse();
        }
    }

    protected bool WasPrimaryUsePressed()
    {
        var mouse = Mouse.current;
        bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetMouseButtonDown(0);
#endif
        return pressed;
    }

    protected bool WasCancelPressed()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        bool pressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        pressed |= mouse != null && mouse.rightButton.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
#endif
        return pressed;
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
        Camera camera = GetPlayerCamera();
        if (camera == null) return;

        Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, useRange))
        {
            if (CanUseOnTarget(hit))
            {
                UseTool(hit);
                var target = hit.collider != null ? hit.collider.gameObject : null;
                GameEventBus.RaiseToolUsed(
                    toolID,
                    toolName,
                    target != null ? target.name : "",
                    target != null ? target.tag : "");
                lastUseTime = Time.time;
                canUse = false;
                
                PlayUseSound();
            }
        }
    }

    public virtual bool RequestPrimaryUse()
    {
        if (!isEquipped || !canUse)
        {
            return false;
        }

        TryUseTool();
        return true;
    }

    public virtual bool RequestCancelUse()
    {
        return false;
    }
    
    protected virtual bool CanUseOnTarget(RaycastHit hit)
    {
        return true;
    }
    
    protected abstract void UseTool(RaycastHit hit);

    protected Camera GetPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
            {
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }

        return playerCamera;
    }
    
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
        
    }
    
    protected virtual void OnUnequip()
    {
        
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
