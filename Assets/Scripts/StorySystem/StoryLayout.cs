namespace StorySystem
{
    /// <summary>
    /// 会話画面の配置（画面の割合）。会話欄は画面の下のふちにつけ、その上を図に使う。
    /// スマホでは画面が小さいので、すき間をなくして図をなるべく大きく見せる（2026-11-23）。
    /// </summary>
    public static class StoryLayout
    {
        /// <summary>会話欄の下のふち。</summary>
        public const float DialogBottom = 0f;
        /// <summary>会話欄の上のふち（知識スライドの枠はここから画面の上まで）。</summary>
        public const float DialogTop = 0.305f;
    }
}
