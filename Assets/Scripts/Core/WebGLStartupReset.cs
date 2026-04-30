using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// WebGL 启动时检查 URL 查询参数，按需重置 PlayerPrefs。
/// 主要用于开发/测试：?resetstory=1 清剧情 flag 强制重播；?resetall=1 清所有进度。
/// 非 WebGL 平台 no-op。
/// </summary>
public static class WebGLStartupReset
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int GeoModelTest_QueryUrlFlag(string flag);

    private static bool HasUrlFlag(string flag)
    {
        try { return GeoModelTest_QueryUrlFlag(flag) != 0; }
        catch { return false; }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CheckResetFlags()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (HasUrlFlag("resetstory"))
        {
            PlayerPrefs.DeleteKey("StoryFlags");
            PlayerPrefs.Save();
            Debug.Log("[WebGLStartupReset] StoryFlags 已清除（?resetstory=1）");
        }

        if (HasUrlFlag("resetall"))
        {
            // 复用 ProgressResetService 的清档逻辑（覆盖更全）
            ProgressResetService.ResetAll();
            Debug.Log("[WebGLStartupReset] 全部进度已清除（?resetall=1）");
        }
#endif
    }
}
