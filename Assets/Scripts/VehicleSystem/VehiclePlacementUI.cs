using UnityEngine;
using UnityEngine.UI;
using UISystem;

public sealed class VehiclePlacementUI : MonoBehaviour
{
    private PlaceableTool _tool;
    private Canvas _canvas;
    private Button _place;
    private Text _hint;

    public static void Show(PlaceableTool tool)
    {
        var ui = tool.GetComponent<VehiclePlacementUI>();
        if (ui == null) ui = tool.gameObject.AddComponent<VehiclePlacementUI>();
        ui._tool = tool;
    }

    private void Start()
    {
        _canvas = GameUI.Canvas("VehiclePlacement", 150);
        var panel = GameUI.Box(_canvas.transform, "Panel", GameUI.Surface, new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.25f));
        _hint = GameUI.Label(panel.transform, "Hint", "", 26, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.96f), TextAnchor.MiddleCenter);
        _place = GameUI.Button(panel.transform, "Place", GameUI.L("vehicle.place"), new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.54f),
            () => { if (_tool != null && _tool.CanConfirmPlacement) _tool.RequestPrimaryUse(); }, true);
        GameUI.Button(panel.transform, "Cancel", GameUI.L("ui.common.cancel"), new Vector2(0.52f, 0.08f), new Vector2(0.96f, 0.54f),
            () => _tool?.RequestCancelUse());
    }

    private void Update()
    {
        if (_canvas == null) return;
        bool visible = _tool != null && _tool.IsPlacing;
        _canvas.gameObject.SetActive(visible);
        if (!visible) return;
        _place.interactable = _tool.CanConfirmPlacement;
        _hint.text = GameUI.L(_place.interactable ? "vehicle.place_ready" : "vehicle.place_hint");
    }

    private void OnDestroy()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
    }
}
