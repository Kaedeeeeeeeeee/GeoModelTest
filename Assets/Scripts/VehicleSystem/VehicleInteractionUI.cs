using UnityEngine;
using UnityEngine.UI;
using UISystem;

public sealed class VehicleInteractionUI : MonoBehaviour
{
    private VehicleController _vehicle;
    private Canvas _canvas;
    private Text _title;
    private Text _actionLabel;
    private Button _action;
    private Button _drill;

    public static void Attach(VehicleController vehicle)
    {
        var ui = vehicle.GetComponent<VehicleInteractionUI>();
        if (ui == null) ui = vehicle.gameObject.AddComponent<VehicleInteractionUI>();
        ui._vehicle = vehicle;
    }

    private void Start()
    {
        _canvas = GameUI.Canvas("VehicleActions", 150);
        var panel = GameUI.Box(_canvas.transform, "Panel", GameUI.Surface, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.24f));
        _title = GameUI.Label(panel.transform, "Title", "", 25, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.95f), TextAnchor.MiddleCenter);
        var action = GameUI.Button(panel.transform, "Control", "", new Vector2(0.03f, 0.1f), new Vector2(0.34f, 0.58f), () =>
        {
            if (_vehicle == null || !_vehicle.CanInteract) return;
            if (_vehicle.IsControlled) _vehicle.EndControl(); else _vehicle.BeginControl();
        }, true);
        _actionLabel = action.GetComponentInChildren<Text>();
        _action = action;
        _drill = GameUI.Button(panel.transform, "Drill", GameUI.L("vehicle.drill"), new Vector2(0.36f, 0.1f), new Vector2(0.67f, 0.58f), () =>
        {
            if (_vehicle is DrillCarController car) car.StartDrilling();
        });
        GameUI.Button(panel.transform, "Recall", GameUI.L("vehicle.recall"), new Vector2(0.69f, 0.1f), new Vector2(0.97f, 0.58f), () => _vehicle?.Recall());
    }

    private void Update()
    {
        if (_canvas == null) return;
        bool show = _vehicle != null && (_vehicle.IsControlled || (_vehicle.Owner != null && _vehicle.Owner.IsEquipped)) &&
            (_vehicle.CanInteract || _vehicle.CanRecall);
        _canvas.gameObject.SetActive(show);
        if (!show) return;
        _title.text = string.IsNullOrEmpty(_vehicle.Status) ? _vehicle.DisplayName : _vehicle.Status;
        _actionLabel.text = GameUI.L(_vehicle.IsControlled ? "vehicle.exit" : "vehicle.enter");
        _action.interactable = _vehicle.CanInteract;
        _drill.gameObject.SetActive(_vehicle is DrillCarController);
        _drill.interactable = _vehicle.IsControlled && !(_vehicle is DrillCarController car && car.IsDrilling);
    }

    private void OnDestroy()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
    }
}
