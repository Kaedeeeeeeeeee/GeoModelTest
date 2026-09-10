using UnityEngine;
using UnityEngine.InputSystem;

public class DrillCarController : VehicleController
{
    public bool isActive = true;
    public float drillAnimationSpeed = 2f;
    public GameObject drillPart;
    public float driveSpeed = 5f;
    public float turnSpeed = 90f;
    public Transform playerSeatPosition;
    public Transform cameraPosition;
    public float drillingDepth = 2f;
    public float drillingRadius = 0.1f;
    public bool IsDrilling { get; private set; }
    public override string DisplayName => LocalizationManager.Resolve("tool.drill_car.name", "ボーリング調査車");
    private float _drillSeconds;
    private GeometricSampleReconstructor _reconstructor;

    protected override void Awake()
    {
        base.Awake();
        Body.mass = 100f;
        Body.linearDamping = 2f;
        Body.angularDamping = 10f;
    }

    protected override void Update()
    {
        base.Update();
        if (InputBlocked || !IsControlled) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) StartDrilling();
        if (!IsDrilling) return;
        _drillSeconds += Time.deltaTime;
        if (drillPart != null) drillPart.transform.Rotate(Vector3.up, 360f * drillAnimationSpeed * Time.deltaTime);
        if (_drillSeconds < 2f) return;
        StopDrilling();
        if (!TryFindGround(transform.position + transform.forward * 1.5f + Vector3.up * 2f, out RaycastHit ground))
        {
            Status = UISystem.GameUI.L("vehicle.no_sample");
            return;
        }
        if (_reconstructor == null) _reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (_reconstructor == null) _reconstructor = new GameObject("GeometricSampleReconstructor").AddComponent<GeometricSampleReconstructor>();
        var sample = _reconstructor.ReconstructSample(ground.point, Vector3.down, drillingRadius, drillingDepth,
            ground.point + transform.right * 2f + Vector3.up * (drillingDepth * 1.1f), 0f, drillingDepth);
        if (sample == null || sample.sampleContainer == null)
        {
            Status = UISystem.GameUI.L("vehicle.no_sample");
            return;
        }
        DrillToolSampleIntegrator.IntegrateSampleAfterDrilling(sample.sampleContainer, "1101", DisplayName);
        Core.GameEventBus.RaiseToolUsed("1101", DisplayName, ground.collider.name, ground.collider.tag);
        Status = UISystem.GameUI.L("vehicle.sample_ready");
    }

    public void StartDrilling()
    {
        if (!IsControlled || InputBlocked || IsDrilling) return;
        IsDrilling = true;
        _drillSeconds = 0f;
        Status = UISystem.GameUI.L("vehicle.drilling");
    }

    public void StopDrilling() => IsDrilling = false;
    public void ToggleDrilling() { if (IsDrilling) StopDrilling(); else StartDrilling(); }
    public void StopDriving() => EndControl();
    protected override void OnControlEnded() => StopDrilling();

    protected override void Move(Vector2 input)
    {
        if (IsDrilling) input = Vector2.zero;
        var horizontal = transform.forward * input.y * Mathf.Clamp(driveSpeed, 1f, 12f);
        Body.linearVelocity = new Vector3(horizontal.x, Body.linearVelocity.y, horizontal.z);
        if (Mathf.Abs(input.y) > 0.05f)
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0f, input.x * turnSpeed * Time.fixedDeltaTime, 0f));
    }
}
