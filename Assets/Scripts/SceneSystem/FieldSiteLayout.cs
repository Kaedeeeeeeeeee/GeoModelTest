using UnityEngine;

/// <summary>Production anchors for the phase-shifter mountain arrival and drill quest.
/// ArrivalPosition is the player pivot: MainScene's 2 m controller has center.y = 1, with 0.08 m skin clearance.</summary>
public static class FieldSiteLayout
{
    public static readonly Vector3 ArrivalPosition = new Vector3(110.0000f, 17.0828f, 168.5000f);
    public static readonly Quaternion ArrivalRotation = Quaternion.Euler(0f, 135.9392f, 0f);
    public static readonly Vector3 DrillPosition = new Vector3(142.5000f, 23.0950f, 155.0000f);
}
