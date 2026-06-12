using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器菜单:在编辑模式下直接触发 GeoSurfaceApplier 的应用/还原,
/// 方便不进 Play 就能在 Scene 视图预览「平地草 / 陡坡露地层」。
/// </summary>
public static class OutcropMenu
{
    [MenuItem("Tools/Outcrop/Apply GeoSurface To Scene")]
    public static void ApplyToScene()
    {
        var applier = Object.FindFirstObjectByType<GeoSurfaceApplier>();
        if (applier == null)
        {
            Debug.LogError("[OutcropMenu] 场景里没有 GeoSurfaceApplier 组件。");
            return;
        }
        applier.Apply();
        EditorUtility.SetDirty(applier);
        SceneView.RepaintAll();
        Debug.Log("[OutcropMenu] Apply 已调用。");
    }

    [MenuItem("Tools/Outcrop/Revert GeoSurface In Scene")]
    public static void RevertInScene()
    {
        var applier = Object.FindFirstObjectByType<GeoSurfaceApplier>();
        if (applier != null)
        {
            applier.Revert();
            SceneView.RepaintAll();
            Debug.Log("[OutcropMenu] Revert 已调用。");
        }
    }
}
