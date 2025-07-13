using UnityEngine;
using UnityEngine.InputSystem;

public class InputSetup : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;
    
    private InputActionMap playerActionMap;
    private FirstPersonController firstPersonController;
    
    void Awake()
    {
        firstPersonController = GetComponent<FirstPersonController>();
        
        if (firstPersonController == null)
        {
            
            return;
        }
        
        if (inputActions != null)
        {
            playerActionMap = inputActions.FindActionMap("Player");
            
            if (playerActionMap != null)
            {
                SetupInputBindings();
            }
            else
            {
                
            }
        }
        else
        {
            
        }
    }
    
    void SetupInputBindings()
    {
        var moveAction = playerActionMap.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.performed += firstPersonController.OnMove;
            moveAction.canceled += firstPersonController.OnMove;
        }
        
        var lookAction = playerActionMap.FindAction("Look");
        if (lookAction != null)
        {
            lookAction.performed += firstPersonController.OnLook;
            lookAction.canceled += firstPersonController.OnLook;
        }
        
        var runAction = playerActionMap.FindAction("Run");
        if (runAction != null)
        {
            runAction.performed += firstPersonController.OnRun;
            runAction.canceled += firstPersonController.OnRun;
        }
        
        var jumpAction = playerActionMap.FindAction("Jump");
        if (jumpAction != null)
        {
            jumpAction.performed += firstPersonController.OnJump;
        }
    }
    
    void OnEnable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
        }
    }
    
    void OnDisable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }
    
    void OnDestroy()
    {
        if (playerActionMap != null && firstPersonController != null)
        {
            RemoveInputBindings();
        }
    }
    
    void RemoveInputBindings()
    {
        var moveAction = playerActionMap.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.performed -= firstPersonController.OnMove;
            moveAction.canceled -= firstPersonController.OnMove;
        }
        
        var lookAction = playerActionMap.FindAction("Look");
        if (lookAction != null)
        {
            lookAction.performed -= firstPersonController.OnLook;
            lookAction.canceled -= firstPersonController.OnLook;
        }
        
        var runAction = playerActionMap.FindAction("Run");
        if (runAction != null)
        {
            runAction.performed -= firstPersonController.OnRun;
            runAction.canceled -= firstPersonController.OnRun;
        }
        
        var jumpAction = playerActionMap.FindAction("Jump");
        if (jumpAction != null)
        {
            jumpAction.performed -= firstPersonController.OnJump;
        }
    }
}