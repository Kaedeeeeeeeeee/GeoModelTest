using UnityEngine;

public class DroneTool : PlaceableTool
{
    private GameObject _placedVehicle;

    protected override void Start()
    {
        base.Start();
        toolID = "1100";
        toolName = "調査用ドローン";
        useRange = 15f;
        useCooldown = 0.5f;
        placementOffset = 0.6f;
        if (prefabToPlace == null) prefabToPlace = Resources.Load<GameObject>("Prefabs/Vehicles/Drone");
    }

    protected override GameObject GetTemplateObject()
    {
        if (prefabToPlace == null) prefabToPlace = Resources.Load<GameObject>("Prefabs/Vehicles/Drone");
        return prefabToPlace;
    }

    protected override Quaternion GetPlacementRotation(RaycastHit hit)
    {
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up);
        return forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
    }

    protected override void UpdatePreviewPosition()
    {
        base.UpdatePreviewPosition();
        if (previewObject == null || !previewObject.activeSelf) return;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, useRange, groundLayers) || hit.normal.y < 0.65f)
            previewObject.SetActive(false);
    }

    protected override bool CanPlaceAtPosition(Vector3 position)
    {
        if (hasPlacedObject || Vector3.Distance(position, transform.position) < 2f) return false;
        foreach (var collider in Physics.OverlapSphere(position, 1.3f, ~0, QueryTriggerInteraction.Ignore))
            if (collider.GetComponentInParent<VehicleController>() != null) return false;
        return true;
    }

    protected override void OnObjectPlaced(GameObject placedObject)
    {
        _placedVehicle = placedObject;
        var controller = placedObject.GetComponent<DroneController>();
        if (controller == null) controller = placedObject.AddComponent<DroneController>();
        controller.Configure(this);
        controller.interactionRange = 5f;
        Core.GameEventBus.RaiseToolUsed(toolID, toolName, placedObject.name, placedObject.tag);
    }

    public GameObject GetPlacedDrone() => _placedVehicle;
}
