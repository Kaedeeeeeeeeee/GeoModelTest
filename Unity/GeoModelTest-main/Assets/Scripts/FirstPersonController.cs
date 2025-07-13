using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float jumpHeight = 1.0f;
    public float gravity = -15.0f;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 8.0f;
    public float maxLookAngle = 80.0f;
    public bool enableMouseLook = true; // 控制是否启用鼠标视角控制
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask = 1;
    
    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool runInput;
    private bool jumpInput;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        if (playerCamera == null)
        {
            
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        HandleInput();
        GroundCheck();
        HandleMovement();
        HandleLook();
    }
    
    void HandleInput()
    {
        if (Keyboard.current != null)
        {
            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y = 1;
            if (Keyboard.current.sKey.isPressed) move.y = -1;
            if (Keyboard.current.aKey.isPressed) move.x = -1;
            if (Keyboard.current.dKey.isPressed) move.x = 1;
            moveInput = move;
            
            runInput = Keyboard.current.leftShiftKey.isPressed;
            
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpInput = true;
            }
        }
        
        if (Mouse.current != null && enableMouseLook)
        {
            lookInput = Mouse.current.delta.ReadValue();
        }
        else
        {
            lookInput = Vector2.zero; // 禁用鼠标视角时，清零输入
        }
    }
    
    void GroundCheck()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    
    void HandleMovement()
    {
        float currentSpeed = runInput ? runSpeed : walkSpeed;
        
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
        jumpInput = false;
    }
    
    void HandleLook()
    {
        if (playerCamera == null) return;
        
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        if (enableMouseLook)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }
    
    public void OnRun(InputAction.CallbackContext context)
    {
        runInput = context.ReadValueAsButton();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpInput = true;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}