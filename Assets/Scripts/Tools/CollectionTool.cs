using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using StorySystem;

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
    private bool wasToolInputBlocked = false;

    /// <summary>
    /// 剧情对话显示期间，所有工具共用同一个输入锁。
    /// </summary>
    protected bool IsToolInputBlocked => StoryDirector.IsStoryPlaybackActive;
    
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
        bool inputBlocked = IsToolInputBlocked;
        if (inputBlocked)
        {
            if (!wasToolInputBlocked && isEquipped)
            {
                OnToolInputBlocked();
            }

            wasToolInputBlocked = true;
            return;
        }

        wasToolInputBlocked = false;

        if (isEquipped)
        {
            if (GetPlayerCamera() == null) return;

            // 真机移动端统一由 MobileInputManager/InventoryUISystem 路由工具按钮。
            // 不再把任意触摸（看向、点剧情、点 UI）当作鼠标左键直接使用工具。
            if (!MobileInputManager.IsRuntimeMobileDevice())
            {
                HandleInput();
            }

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
        if (IsToolInputBlocked || !isEquipped || !canUse)
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

    /// <summary>
    /// 剧情开始的首帧取消尚未完成的工具动作（例如锤击采集或放置预览）。
    /// </summary>
    protected virtual void OnToolInputBlocked()
    {
        RequestCancelUse();
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
