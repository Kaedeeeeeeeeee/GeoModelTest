using UnityEngine;

/// <summary>
/// Dimensions and timing of the boring rig, in tower-local metres and seconds.
/// The geometry numbers mirror RIG / CORE_BOX in tools/art-pipeline/field-tools/build_models.py.
/// </summary>
public static class BoringRigLayout
{
    // Tripod
    public const float ApexHeight = 3.2f;
    public const float LegLength = 3.47f;
    public const float FootRadius = 1.35f;
    public const float FoldedFootRadius = 0.12f;
    public static readonly Vector3 PulleyCenter = new Vector3(-0.115f, -0.34f, 0f); // relative to the apex
    public const float PulleyRadius = 0.115f;

    // Drill string
    public const float SwivelHeight = 0.26f;
    public const float SwivelIdleTop = 2.6f;
    public const float SwivelDrilledTop = 1.34f;
    public const float SwivelPulledTop = 2.7f;
    public const float HeadRetract = 0.26f;
    public static readonly Vector3 SwivelHoseNipple = new Vector3(-0.11f, -0.13f, 0f); // swivel-local

    // Machine and pump
    public static readonly Vector3 WinchWirePoint = new Vector3(-0.62f, 1.12f, 0f);
    public static readonly Vector3 PumpPosition = new Vector3(-0.85f, 0f, 1.05f);
    public static readonly Vector3 PumpOutlet = new Vector3(-0.10f, 0.43f, -0.32f); // pump-local

    // Core box
    public static readonly Vector3 CoreBoxOffset = new Vector3(2.1f, 0f, 0f);
    public const int CoreSlots = 5;
    public const float SlotPitch = 0.26f;
    public const float BoxInnerLength = 2.08f;
    public const float BoxFloor = 0.02f;
    public const float BoxHeight = 0.15f;
    public const float CoreRadius = 0.1f;
    /// <summary>A boxed core is picked up from anywhere around its 2 m length.</summary>
    public const float BoxedCorePickupRange = 2.6f;

    // Timeline of one staged drilling cycle
    public const float UnfoldSeconds = 1.0f;
    public const float DrillSeconds = 4.0f;
    public const float PullSeconds = 1.1f;
    public const float BoxSeconds = 0.9f;
    public const float CycleSeconds = DrillSeconds + PullSeconds + BoxSeconds;
    /// <summary>Share of the pull phase spent lifting the rods and sliding the drill head clear.</summary>
    public const float RodLiftShare = 0.25f;

    /// <summary>Horizontal unit direction of tripod leg 0..2: one in front (+Z), two behind.</summary>
    public static Vector3 FootDirection(int leg)
    {
        float yaw = leg * 120f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
    }

    public static int SlotIndex(int drillIndex)
    {
        return ((drillIndex % CoreSlots) + CoreSlots) % CoreSlots;
    }

    /// <summary>Where the top end of a core rests in the box, box-local. The core then runs along +Z.</summary>
    public static Vector3 SlotLocalPosition(int drillIndex)
    {
        float x = (SlotIndex(drillIndex) - (CoreSlots - 1) * 0.5f) * SlotPitch;
        return new Vector3(x, BoxFloor + CoreRadius, -BoxInnerLength * 0.5f + 0.03f);
    }

    /// <summary>Box-local rotation that lays an upright core (which hangs down -Y from its origin) along +Z.</summary>
    public static Quaternion CoreLocalRotation => Quaternion.FromToRotation(Vector3.down, Vector3.forward);

    public static float SwivelTopWhileDrilling(float progress)
    {
        return Mathf.Lerp(SwivelIdleTop, SwivelDrilledTop, Mathf.Clamp01(progress));
    }

    /// <summary>
    /// Index of the layer segment the bit is in. <paramref name="fractions"/> are each segment's share
    /// of the core, top to bottom; returns -1 when there are none.
    /// </summary>
    public static int SegmentAt(float progress, float[] fractions)
    {
        if (fractions == null || fractions.Length == 0) return -1;
        float end = 0f;
        for (int i = 0; i < fractions.Length; i++)
        {
            end += fractions[i];
            if (progress < end) return i;
        }
        return fractions.Length - 1;
    }

    /// <summary>Path of the core from hanging under the swivel to lying in its slot: a lifted arc.</summary>
    public static Vector3 CoreTransferPoint(Vector3 from, Vector3 to, float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 control = Vector3.Lerp(from, to, 0.5f);
        control.y = Mathf.Max(from.y, to.y) + 0.15f;
        float u = 1f - t;
        return u * u * from + 2f * u * t * control + t * t * to;
    }
}
