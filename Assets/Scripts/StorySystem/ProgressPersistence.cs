using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// 简易进度存储（MVP）：仅存剧情旗标到 PlayerPrefs。
    /// 后续可替换为更完整的 JSON 落盘方案。
    /// </summary>
    public static class ProgressPersistence
    {
        private const string FlagsKey = "StoryFlags";

        public static HashSet<string> LoadFlags()
        {
            var raw = PlayerPrefs.GetString(FlagsKey, "");
            var set = new HashSet<string>();
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split('|');
                foreach (var p in parts)
                {
                    if (!string.IsNullOrEmpty(p)) set.Add(p);
                }
            }
            return set;
        }

        public static void SaveFlags(HashSet<string> flags)
        {
            if (flags == null) return;
            var raw = string.Join("|", flags);
            PlayerPrefs.SetString(FlagsKey, raw);
            PlayerPrefs.Save();
        }
    }
}

