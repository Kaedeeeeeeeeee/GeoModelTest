using UnityEngine;

/// <summary>A wheel entry that puts away the equipped tool; it has no world action.</summary>
public sealed class EmptyHandTool : CollectionTool
{
    private void Awake()
    {
        toolID = "0";
        toolName = "何も持たない";
    }

    public override bool RequestPrimaryUse() => false;
    protected override void UseTool(RaycastHit hit) { }
}
