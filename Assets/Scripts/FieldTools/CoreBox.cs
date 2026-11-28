using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The wooden core box that stands beside the boring rig. Each pulled core is laid in the next
/// trough, shallowest first, with its depth range written on the end board, so the row of cores
/// already reads like the geologic column taught later in the story.
/// It is its own scene object: the box and any cores still in it stay when the rig is put away.
/// </summary>
public class CoreBox : MonoBehaviour
{
    public DrillTower tower;

    private readonly GameObject[] slots = new GameObject[BoringRigLayout.CoreSlots];
    private bool everHadTower;

    public static CoreBox Create(DrillTower owner, float depthPerDrill)
    {
        if (owner == null) return null;
        // The name contains "Drill" so the drilling ray-casts skip it like the rest of the rig.
        var boxObject = new GameObject("DrillCoreBox");
        Transform rig = owner.transform;
        Vector3 position = rig.TransformPoint(BoringRigLayout.CoreBoxOffset);
        Quaternion yaw = Quaternion.Euler(0f, rig.eulerAngles.y, 0f);
        boxObject.transform.SetPositionAndRotation(position, yaw);
        SitOnGround(boxObject.transform, rig);

        GameObject model = FieldToolModels.Instantiate("CoreBox", boxObject.transform);
        if (model == null)
        {
            Destroy(boxObject);
            return null;
        }

        var box = boxObject.AddComponent<CoreBox>();
        box.tower = owner;
        box.everHadTower = true;
        box.CreateDepthLabels(depthPerDrill);
        return box;
    }

    /// <summary>World position of the top end of the core kept in the slot for this drilling run.</summary>
    public Vector3 SlotPosition(int drillIndex)
    {
        return transform.TransformPoint(BoringRigLayout.SlotLocalPosition(drillIndex));
    }

    /// <summary>World rotation of a core lying in the box.</summary>
    public Quaternion CoreRotation => transform.rotation * BoringRigLayout.CoreLocalRotation;

    public void Place(GameObject core, int drillIndex)
    {
        if (core == null) return;
        slots[BoringRigLayout.SlotIndex(drillIndex)] = core;
        core.transform.SetPositionAndRotation(SlotPosition(drillIndex), CoreRotation);
    }

    public bool IsEmpty
    {
        get
        {
            foreach (GameObject core in slots)
            {
                if (core != null) return false;
            }
            return true;
        }
    }

    void Update()
    {
        // Once the rig is gone and the last core has been picked up, the empty box goes too.
        if (everHadTower && tower == null && IsEmpty) Destroy(gameObject);
    }

    /// <summary>
    /// Lays the box on the ground under its whole footprint: tilted to follow the slope (within a
    /// sensible limit) and lifted so no corner of the terrain pokes through the floor of the troughs.
    /// </summary>
    private static void SitOnGround(Transform box, Transform rig)
    {
        const float halfWidth = BoringRigLayout.CoreSlots * BoringRigLayout.SlotPitch * 0.5f + 0.03f;
        const float halfLength = BoringRigLayout.BoxInnerLength * 0.5f + 0.03f;
        const float maxTilt = 18f;
        Vector3 centre = box.position;
        Vector3 right = box.right, forward = box.forward;
        float fallback = rig.position.y;
        float front = GroundHeight(centre - forward * halfLength, rig, fallback);
        float back = GroundHeight(centre + forward * halfLength, rig, fallback);
        float left = GroundHeight(centre - right * halfWidth, rig, fallback);
        float rightSide = GroundHeight(centre + right * halfWidth, rig, fallback);
        float pitch = Mathf.Clamp(Mathf.Atan2(front - back, halfLength * 2f) * Mathf.Rad2Deg, -maxTilt, maxTilt);
        float roll = Mathf.Clamp(Mathf.Atan2(rightSide - left, halfWidth * 2f) * Mathf.Rad2Deg, -maxTilt, maxTilt);
        box.rotation = Quaternion.Euler(0f, box.eulerAngles.y, 0f) * Quaternion.Euler(pitch, 0f, roll);

        // Lift the tilted box until its floor clears the ground at every corner and edge midpoint.
        float lift = float.NegativeInfinity;
        box.position = new Vector3(centre.x, (front + back + left + rightSide) * 0.25f, centre.z);
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 floor = box.TransformPoint(new Vector3(x * halfWidth, 0f, z * halfLength));
                lift = Mathf.Max(lift, GroundHeight(floor, rig, floor.y) - floor.y);
            }
        }
        box.position += Vector3.up * Mathf.Max(0f, lift);
    }

    private static float GroundHeight(Vector3 position, Transform rig, float fallback)
    {
        float nearest = float.PositiveInfinity;
        float height = fallback;
        foreach (RaycastHit hit in Physics.RaycastAll(position + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance >= nearest || hit.collider.transform.IsChildOf(rig)) continue;
            if (hit.collider.GetComponentInParent<SampleCollector>() != null ||
                hit.collider.GetComponentInParent<FirstPersonController>() != null ||
                hit.collider.GetComponentInParent<DrillTower>() != null) continue;
            nearest = hit.distance;
            height = hit.point.y;
        }
        return height;
    }

    /// <summary>Depth range of each trough ("0-2m"), written on the tags of the near end board.</summary>
    private void CreateDepthLabels(float depthPerDrill)
    {
        const float unitsPerPixel = 0.001f;
        var canvasObject = new GameObject("DepthLabels", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;

        var rect = (RectTransform)canvasObject.transform;
        float boardZ = -(BoringRigLayout.BoxInnerLength * 0.5f + 0.0125f) - 0.006f;
        rect.localPosition = new Vector3(0f, BoringRigLayout.BoxHeight + 0.03f, boardZ);
        rect.localRotation = Quaternion.identity; // readable from the -Z side, where the cores' top ends are
        rect.localScale = Vector3.one * unitsPerPixel;
        rect.sizeDelta = new Vector2(1400f, 60f);

        Font font = UIFontResolver.GetUIFont();
        for (int i = 0; i < BoringRigLayout.CoreSlots; i++)
        {
            var labelObject = new GameObject("Depth" + i, typeof(RectTransform));
            labelObject.transform.SetParent(canvasObject.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.sizeDelta = new Vector2(200f, 60f);
            labelRect.anchoredPosition = new Vector2(BoringRigLayout.SlotLocalPosition(i).x / unitsPerPixel, 0f);
            var label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = 40;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.12f, 0.14f, 0.18f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = DepthLabel(i, depthPerDrill);
        }
    }

    public static string DepthLabel(int slot, float depthPerDrill)
    {
        return string.Format("{0:0.#}-{1:0.#}m", slot * depthPerDrill, (slot + 1) * depthPerDrill);
    }
}
