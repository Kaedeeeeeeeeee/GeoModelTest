using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The visible boring rig: a pipe tripod with a sheave, a spindle-type boring machine with engine and
/// hoist winch, the rod string hanging from a water swivel, and a mud pump with its hose. It is built
/// on the existing drill-tower object, whose colliders and rigidbody stay exactly as they were.
/// Drives only its own child transforms; the drilling logic lives in <see cref="DrillTower"/>.
/// </summary>
public class BoringRigVisual : MonoBehaviour
{
    private const string VisualRootName = "RigVisual";
    private const int HosePoints = 9;

    private Transform visualRoot;
    private Transform head;
    private Transform machine;
    private Transform drillHead;
    private Transform swivel;
    private Transform rod;
    private Transform pump;
    private readonly Transform[] legs = new Transform[3];
    private readonly Vector3[] footTargets = new Vector3[3];
    private LineRenderer hoistWire;
    private LineRenderer winchWire;
    private LineRenderer hose;
    private LineRenderer sling;
    private Material wireMaterial;
    private Material hoseMaterial;

    private float swivelTop = BoringRigLayout.SwivelIdleTop;
    private float headRetract;
    private float unfoldElapsed = BoringRigLayout.UnfoldSeconds;
    private bool running;
    private bool feetSnapped;

    public bool IsBuilt => visualRoot != null;
    public bool IsUnfolding => unfoldElapsed < BoringRigLayout.UnfoldSeconds;
    public bool IsRunning => running;
    public float SwivelTop => swivelTop;
    public Vector3 HoleWorldPosition => transform.position;

    /// <summary>World position of the swivel's lower end, where the rod (or the core being pulled) hangs.</summary>
    public Vector3 SwivelBottomWorld =>
        transform.TransformPoint(new Vector3(0f, swivelTop - BoringRigLayout.SwivelHeight, 0f));

    /// <summary>
    /// Builds the rig on a drill-tower object. Returns null (and leaves the old look untouched) when the
    /// models are not available.
    /// </summary>
    public static BoringRigVisual Attach(GameObject tower, bool includeCoreBoxPreview)
    {
        if (tower == null) return null;
        BoringRigVisual visual = tower.GetComponent<BoringRigVisual>();
        if (visual == null) visual = tower.AddComponent<BoringRigVisual>();
        if (!visual.IsBuilt && !visual.Build())
        {
            if (Application.isPlaying) Destroy(visual);
            else DestroyImmediate(visual);
            return null;
        }

        if (includeCoreBoxPreview && visual.visualRoot.Find("CoreBoxPreview") == null)
        {
            GameObject box = FieldToolModels.Instantiate("CoreBox", visual.visualRoot);
            if (box != null)
            {
                box.name = "CoreBoxPreview";
                box.transform.localPosition = BoringRigLayout.CoreBoxOffset;
            }
        }
        return visual;
    }

    private bool Build()
    {
        GameObject machineObject = FieldToolModels.Instantiate("RigMachine");
        if (machineObject == null) return false;

        visualRoot = new GameObject(VisualRootName).transform;
        visualRoot.SetParent(transform, false);
        machine = machineObject.transform;
        machine.SetParent(visualRoot, false);
        drillHead = Part("RigDrillHead");
        head = Part("RigHead");
        head.localPosition = new Vector3(0f, BoringRigLayout.ApexHeight, 0f);
        for (int i = 0; i < legs.Length; i++)
        {
            legs[i] = Part("RigLeg");
            legs[i].name = "RigLeg" + i;
            footTargets[i] = BoringRigLayout.FootDirection(i) * BoringRigLayout.FootRadius;
        }
        swivel = Part("RigSwivel");
        rod = Part("RigRod");
        pump = Part("RigPump");
        pump.localPosition = BoringRigLayout.PumpPosition;

        wireMaterial = FieldToolModels.CreateLineMaterial(new Color(0.16f, 0.16f, 0.17f, 1f));
        hoseMaterial = FieldToolModels.CreateLineMaterial(new Color(0.07f, 0.07f, 0.08f, 1f));
        hoistWire = Line("HoistWire", 2, 0.012f, wireMaterial);
        winchWire = Line("WinchWire", 2, 0.012f, wireMaterial);
        sling = Line("CoreSling", 2, 0.010f, wireMaterial);
        sling.enabled = false;
        hose = Line("MudHose", HosePoints, 0.036f, hoseMaterial);
        hose.numCornerVertices = 2;

        // The old primitive tower keeps its colliders (physics unchanged) but is no longer drawn.
        foreach (Renderer legacy in GetComponentsInChildren<Renderer>(true))
        {
            if (!legacy.transform.IsChildOf(visualRoot)) legacy.enabled = false;
        }

        ApplyPose();
        return true;
    }

    private Transform Part(string modelName)
    {
        GameObject part = FieldToolModels.Instantiate(modelName, visualRoot);
        if (part == null)
        {
            part = new GameObject(modelName);
            part.transform.SetParent(visualRoot, false);
        }
        return part.transform;
    }

    private LineRenderer Line(string lineName, int points, float width, Material material)
    {
        var lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(visualRoot, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = points;
        line.widthMultiplier = width;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        if (material != null) line.sharedMaterial = material;
        return line;
    }

    /// <summary>Starts the one-second erection: the legs spread and the machinery rises into place.</summary>
    public void PlayUnfold()
    {
        if (!IsBuilt) return;
        unfoldElapsed = 0f;
        // The tower is placed directly on the ground, so the feet can be planted straight away.
        SnapFeetToGround();
        ApplyPose();
    }

    /// <summary>Finishes the erection at once (drilling may begin before it has played out).</summary>
    public void CompleteUnfold()
    {
        if (!IsBuilt) return;
        unfoldElapsed = BoringRigLayout.UnfoldSeconds;
        if (!feetSnapped) SnapFeetToGround();
        ApplyPose();
    }

    public void SetRunning(bool isRunning)
    {
        running = isRunning;
        if (!running && IsBuilt)
        {
            machine.localPosition = Vector3.zero;
            ApplyPose();
        }
    }

    public void SetSwivelTop(float top)
    {
        swivelTop = top;
        if (IsBuilt) ApplyPose();
    }

    /// <summary>0 = drill head over the hole, 1 = slid back on its rails so the core can come out.</summary>
    public void SetHeadRetract(float amount)
    {
        headRetract = Mathf.Clamp01(amount);
        if (IsBuilt) ApplyPose();
    }

    public void SetRodVisible(bool visible)
    {
        if (rod != null) rod.gameObject.SetActive(visible);
    }

    /// <summary>Draws (or hides, with null) the sling between the swivel and the core being hoisted.</summary>
    public void SetSling(Vector3? coreTopWorld)
    {
        if (sling == null) return;
        sling.enabled = coreTopWorld.HasValue;
        if (!coreTopWorld.HasValue) return;
        sling.SetPosition(0, new Vector3(0f, swivelTop - BoringRigLayout.SwivelHeight, 0f));
        sling.SetPosition(1, visualRoot.InverseTransformPoint(coreTopWorld.Value));
    }

    /// <summary>Plants each foot on the terrain under it, the way a real tripod's legs are adjusted.</summary>
    public void SnapFeetToGround()
    {
        feetSnapped = true;
        for (int i = 0; i < legs.Length; i++)
        {
            Vector3 nominal = BoringRigLayout.FootDirection(i) * BoringRigLayout.FootRadius;
            footTargets[i] = nominal;
            Vector3 origin = transform.TransformPoint(nominal + Vector3.up * 2f);
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= nearest || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<SampleCollector>() != null ||
                    hit.collider.GetComponentInParent<FirstPersonController>() != null ||
                    hit.collider.GetComponentInParent<DrillTower>() != null) continue;
                nearest = hit.distance;
                Vector3 local = transform.InverseTransformPoint(hit.point);
                footTargets[i] = new Vector3(nominal.x, Mathf.Clamp(local.y, -1.2f, 1.2f), nominal.z);
            }
        }
    }

    void Update()
    {
        if (!IsBuilt) return;

        if (IsUnfolding)
        {
            unfoldElapsed += Time.deltaTime;
            if (!IsUnfolding && !feetSnapped) SnapFeetToGround();
            ApplyPose();
        }

        if (running)
        {
            // Engine vibration and the turning rod string.
            Vector3 jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 0.004f;
            machine.localPosition = jitter;
            drillHead.localPosition = new Vector3(-BoringRigLayout.HeadRetract * headRetract, 0f, 0f) + jitter;
            rod.Rotate(0f, 540f * Time.deltaTime, 0f, Space.Self);
        }
    }

    private void ApplyPose()
    {
        float unfold = Mathf.Clamp01(unfoldElapsed / BoringRigLayout.UnfoldSeconds);
        float spread = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(unfold / 0.7f));
        // The machinery pops up once the legs are mostly open, with a slight overshoot.
        float rise = Mathf.Clamp01((unfold - 0.35f) / 0.65f);
        float scale = rise >= 1f ? 1f : Mathf.Max(0.001f, Mathf.Sin(rise * Mathf.PI * 0.5f) * (1f + 0.12f * Mathf.Sin(rise * Mathf.PI)));

        Vector3 apex = new Vector3(0f, BoringRigLayout.ApexHeight, 0f);
        for (int i = 0; i < legs.Length; i++)
        {
            Vector3 folded = BoringRigLayout.FootDirection(i) * BoringRigLayout.FoldedFootRadius;
            Vector3 toFoot = Vector3.Lerp(folded, footTargets[i], spread) - apex;
            legs[i].localPosition = apex;
            legs[i].localRotation = Quaternion.FromToRotation(Vector3.down, toFoot.normalized);
            legs[i].localScale = new Vector3(1f, toFoot.magnitude / BoringRigLayout.LegLength, 1f);
        }

        Vector3 machineScale = Vector3.one * scale;
        machine.localScale = machineScale;
        drillHead.localScale = machineScale;
        pump.localScale = machineScale;
        drillHead.localPosition = new Vector3(-BoringRigLayout.HeadRetract * headRetract, 0f, 0f);

        bool strung = unfold >= 1f;
        swivel.gameObject.SetActive(rise > 0.5f);
        swivel.localPosition = new Vector3(0f, swivelTop, 0f);
        rod.localPosition = new Vector3(0f, swivelTop - BoringRigLayout.SwivelHeight, 0f);
        rod.localScale = new Vector3(1f, rise > 0.5f ? 1f : 0.001f, 1f);

        Vector3 pulley = apex + BoringRigLayout.PulleyCenter;
        hoistWire.enabled = strung;
        winchWire.enabled = strung;
        hose.enabled = strung;
        hoistWire.SetPosition(0, pulley + Vector3.right * BoringRigLayout.PulleyRadius);
        hoistWire.SetPosition(1, new Vector3(0f, swivelTop, 0f));
        winchWire.SetPosition(0, BoringRigLayout.WinchWirePoint);
        winchWire.SetPosition(1, pulley + Vector3.left * BoringRigLayout.PulleyRadius);

        Vector3 hoseStart = BoringRigLayout.PumpPosition + BoringRigLayout.PumpOutlet;
        Vector3 hoseEnd = new Vector3(0f, swivelTop, 0f) + BoringRigLayout.SwivelHoseNipple;
        for (int i = 0; i < HosePoints; i++)
        {
            float t = i / (float)(HosePoints - 1);
            Vector3 point = Vector3.Lerp(hoseStart, hoseEnd, t);
            point.y -= Mathf.Sin(t * Mathf.PI) * 0.35f;
            hose.SetPosition(i, point);
        }
    }

    void OnDestroy()
    {
        DestroyOwned(wireMaterial);
        DestroyOwned(hoseMaterial);
    }

    private static void DestroyOwned(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
