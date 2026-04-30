using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// WebGL 持久化辅助：把 emscripten IDBFS 同步到 IndexedDB，避免页面刷新时存档丢失。
/// 其他平台（编辑器、桌面、移动）所有方法是 no-op，可以安全地在跨平台代码里直接调用。
/// </summary>
public static class WebGLFileSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GeoModelTest_SyncFsToIDB();
#endif

    /// <summary>
    /// 触发一次 IDB 同步。WebGL 下应在每次关键 File.Write/Delete 后调用。
    /// 同步是异步的，浏览器空闲时完成，不会阻塞当前帧。
    /// </summary>
    public static void Flush()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            GeoModelTest_SyncFsToIDB();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WebGLFileSync] Flush 失败: {e.Message}");
        }
#endif
    }
}
