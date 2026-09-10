using UnityEngine;
using UnityEngine.InputSystem;
using Core;

public abstract class VehicleController : MonoBehaviour
{
    public float interactionRange = 5f;
    public float cameraDistance = 4f;
    public float cameraHeight = 2f;
    public static VehicleController Active { get; private set; }
    public bool IsControlled => Active == this;
    public FirstPersonController Player { get; private set; }
    public PlaceableTool Owner { get; private set; }
    public string Status { get; protected set; }
    public abstract string DisplayName { get; }
    protected Rigidbody Body;
    protected MobileInputManager Mobile => MobileInputManager.Instance;
    protected bool InputBlocked => GameInputState.GameplayBlocked || StorySystem.StoryDirector.IsStoryPlaybackActive;
    private InventoryUISystem _inventory;
    private Camera _camera;
    private Transform _cameraParent;
    private Vector3 _cameraPosition;
    private Quaternion _cameraRotation;
    private Renderer[] _playerRenderers;
    private bool[] _rendererStates;
    private bool _playerWasEnabled;
    private CharacterController _character;
    private bool _characterWasEnabled;
    private bool _previousSecondary;
    private AudioSource _engine;
    private int _startedFrame;
    private CursorLockMode _cursorLock;
    private bool _cursorVisible;

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody>();
        if (Body == null) Body = gameObject.AddComponent<Rigidbody>();
        Body.isKinematic = true;
        Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Configure(PlaceableTool owner)
    {
        Owner = owner;
        _inventory = FindFirstObjectByType<InventoryUISystem>();
        Player = owner.GetComponent<FirstPersonController>();
        VehicleInteractionUI.Attach(this);
    }

    public bool CanInteract => Player != null && !InputBlocked && (_inventory == null || !_inventory.IsWheelOpen) &&
        (IsControlled || (Active == null && Vector3.Distance(Player.transform.position, transform.position) <= interactionRange));

    public bool BeginControl()
    {
        if (IsControlled || !CanInteract) return false;
        _camera = Player.GetComponentInChildren<Camera>();
        if (_camera == null) return false;
        _cameraParent = _camera.transform.parent;
        _cameraPosition = _camera.transform.localPosition;
        _cameraRotation = _camera.transform.localRotation;
        _playerWasEnabled = Player.enabled;
        _character = Player.GetComponent<CharacterController>();
        _characterWasEnabled = _character != null && _character.enabled;
        _playerRenderers = Player.GetComponentsInChildren<Renderer>();
        _rendererStates = new bool[_playerRenderers.Length];
        for (int i = 0; i < _playerRenderers.Length; i++)
        {
            _rendererStates[i] = _playerRenderers[i].enabled;
            _playerRenderers[i].enabled = false;
        }
        Player.enabled = false;
        if (_character != null) _character.enabled = false;
        _camera.transform.SetParent(null, true);
        _cursorLock = Cursor.lockState;
        _cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Active = this;
        _startedFrame = Time.frameCount;
        Body.isKinematic = false;
        Body.useGravity = !(this is DroneController);
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        _engine = GeoModel.AudioSystem.AudioManager.Instance.StartLoopSFX3D(
            this is DroneController ? GeoModel.AudioSystem.AudioKeys.SFX.DroneLoop : GeoModel.AudioSystem.AudioKeys.SFX.DrillCarLoop,
            transform, 0.5f);
        var controls = MobileControlsUI.ActiveInstance;
        if (controls != null) controls.SetDroneControlsVisible(this is DroneController);
        Status = null;
        return true;
    }

    public void EndControl()
    {
        if (!IsControlled) return;
        OnControlEnded();
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.isKinematic = true;
        if (_engine != null) GeoModel.AudioSystem.AudioManager.Instance.StopLoop(_engine, 0.2f);
        _engine = null;
        if (Player != null)
        {
            Vector3 exit = transform.position - transform.forward * 3f;
            if (TryFindGround(exit + Vector3.up * 2f, out RaycastHit ground)) exit = ground.point + Vector3.up * 0.15f;
            Player.transform.position = exit;
            Player.enabled = _playerWasEnabled;
            if (_character != null) _character.enabled = _characterWasEnabled;
        }
        if (_camera != null)
        {
            _camera.transform.SetParent(_cameraParent, false);
            _camera.transform.localPosition = _cameraPosition;
            _camera.transform.localRotation = _cameraRotation;
        }
        for (int i = 0; _playerRenderers != null && i < _playerRenderers.Length; i++)
            if (_playerRenderers[i] != null) _playerRenderers[i].enabled = _rendererStates[i];
        Active = null;
        if (!GameInputState.IsModalOpen)
        {
            Cursor.lockState = MobileInputManager.IsRuntimeMobileDevice() ? CursorLockMode.None : _cursorLock;
            Cursor.visible = MobileInputManager.IsRuntimeMobileDevice() || _cursorVisible;
        }
        var controls = MobileControlsUI.ActiveInstance;
        if (controls != null) controls.SetDroneControlsVisible(false);
    }

    public void Recall()
    {
        if (!CanInteract) return;
        EndControl();
        if (Owner != null) Owner.ResetPlacement();
        Destroy(gameObject);
    }

    protected virtual void Update()
    {
        bool secondary = Mobile != null && Mobile.IsSecondaryInteracting;
        bool pressed = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || (secondary && !_previousSecondary);
        _previousSecondary = secondary;
        if (InputBlocked) return;
        if (CanInteract && pressed && Time.frameCount > _startedFrame)
        {
            if (IsControlled) EndControl();
            else BeginControl();
        }
        if (CanInteract && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) Recall();
    }

    protected virtual void FixedUpdate()
    {
        if (!IsControlled || InputBlocked) return;
        Vector2 move = Vector2.zero;
        var keys = Keyboard.current;
        if (keys != null)
            move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
        if (Mobile != null && Mobile.MoveInput.sqrMagnitude > 0.01f) move = Mobile.MoveInput;
        Move(Vector2.ClampMagnitude(move, 1f));
    }

    protected virtual void LateUpdate()
    {
        if (!IsControlled || _camera == null) return;
        _camera.transform.position = transform.position + Vector3.up * cameraHeight - transform.forward * cameraDistance;
        _camera.transform.LookAt(transform.position + Vector3.up * 0.4f);
    }

    protected bool TryFindGround(Vector3 origin, out RaycastHit nearest)
    {
        nearest = default;
        float distance = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) ||
                (Player != null && hit.collider.transform.IsChildOf(Player.transform)) || hit.normal.y < 0.5f) continue;
            if (hit.distance < distance) { nearest = hit; distance = hit.distance; }
        }
        return distance < float.PositiveInfinity;
    }

    protected virtual void OnDisable() => EndControl();
    protected virtual void OnControlEnded() { }
    protected abstract void Move(Vector2 input);
}
