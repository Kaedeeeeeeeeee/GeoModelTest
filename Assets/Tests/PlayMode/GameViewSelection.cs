using System;
using System.Reflection;

/// <summary>
/// The editor game view size a fixture found. Layout assertions measure whatever size the
/// previous fixture left selected, so a fixture that changes it puts the selection back.
/// </summary>
public static class GameViewSelection
{
    public static int Index
    {
#if UNITY_EDITOR
        get => (int)Property.GetValue(View);
        set => Property.SetValue(View, value);
#else
        get => -1;
        set { }
#endif
    }

#if UNITY_EDITOR
    private static Type ViewType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
    private static UnityEditor.EditorWindow View => UnityEditor.EditorWindow.GetWindow(ViewType);
    private static PropertyInfo Property =>
        ViewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
}
