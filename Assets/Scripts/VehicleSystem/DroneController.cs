using UnityEngine;
using UnityEngine.InputSystem;

public class DroneController : VehicleController
{
    public float hoverHeight = 2f;
    public float hoverSpeed = 1f;
    public float rotationSpeed = 30f;
    public float flySpeed = 8f;
    public float verticalSpeed = 5f;
    public float turnSpeed = 120f;
    public float groundCheckDistance = 1f;
    public LayerMask groundLayers = 1;
    public override string DisplayName => LocalizationManager.Resolve("tool.drone.name", "調査用ドローン");

    public void SetHoverHeight(float height) => hoverHeight = height;
    public void StopControlling() => EndControl();

    protected override void Move(Vector2 input)
    {
        float vertical = 0f;
        var keys = Keyboard.current;
        if (keys != null) vertical = (keys.jKey.isPressed ? 1f : 0f) - (keys.kKey.isPressed ? 1f : 0f);
        if (Mobile != null && Mathf.Abs(Mobile.VerticalInput) > 0.01f) vertical = Mobile.VerticalInput;
        Body.linearVelocity = transform.forward * input.y * flySpeed + Vector3.up * vertical * verticalSpeed;
        Body.MoveRotation(Body.rotation * Quaternion.Euler(0f, input.x * turnSpeed * Time.fixedDeltaTime, 0f));
    }
}
