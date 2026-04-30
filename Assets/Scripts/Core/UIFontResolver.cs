using UnityEngine;

/// <summary>
/// UI 字体解析器：所有动态创建 UGUI Text 的地方都应通过这里取字体。
///
/// 解决 WebGL 上 Resources.GetBuiltinResource&lt;Font&gt;("LegacyRuntime.ttf") 不能渲染中日文的问题：
///   - 编辑器/桌面：LegacyRuntime.ttf 找不到的字符会回退到 OS 字体（macOS Hiragino、Windows YaHei 等）。
///   - WebGL：浏览器没有 OS 字体，CJK 字符全部渲染失败 → UI 显示为空黑框。
///
/// 修复策略：优先加载内嵌的 Noto Sans CJK JP（包含全 CJK + 假名 + 拉丁），失败再回退到内置字体。
/// 字体只加载一次，缓存复用。
/// </summary>
public static class UIFontResolver
{
    private const string CJKFontResourcePath = "Fonts/NotoSansCJKjp-Regular";
    private const string FallbackBuiltinFontName = "LegacyRuntime.ttf";

    private static Font cachedFont;
    private static bool resolveAttempted;

    /// <summary>
    /// 取一个支持中日文的 Font。所有动态 UI 创建调用这里替代 Resources.GetBuiltinResource。
    /// </summary>
    public static Font GetUIFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        if (!resolveAttempted)
        {
            resolveAttempted = true;
            cachedFont = Resources.Load<Font>(CJKFontResourcePath);
            if (cachedFont != null)
            {
                Debug.Log($"[UIFontResolver] 已加载 CJK 字体: {CJKFontResourcePath}");
            }
            else
            {
                Debug.LogWarning($"[UIFontResolver] 未找到 Resources/{CJKFontResourcePath}，回退到内置字体（WebGL 下中日文将无法渲染）");
            }
        }

        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>(FallbackBuiltinFontName);
        }

        return cachedFont;
    }
}
