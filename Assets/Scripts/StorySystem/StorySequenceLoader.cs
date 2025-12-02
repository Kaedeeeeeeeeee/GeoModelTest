using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    [Serializable]
    public class StorySequence
    {
        public string scene;
        public string background;
        public string bgm;
        public List<StoryDialogueLine> dialogues;
    }

    [Serializable]
    public class StoryDialogueLine
    {
        public string speaker;
        public string text;
        public bool shake;
        public float shakeAmplitude;
    }

    public static class StorySequenceLoader
    {
        public static StorySequence LoadFromResources(string resourcePath, bool logWarnings = true)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                if (logWarnings) Debug.LogWarning($"[StorySequenceLoader] 未找到资源: {resourcePath}");
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<StorySequence>(asset.text);
                if (data == null)
                {
                    if (logWarnings) Debug.LogWarning($"[StorySequenceLoader] 解析失败，返回空数据: {resourcePath}");
                }
                return data;
            }
            catch (Exception ex)
            {
                if (logWarnings) Debug.LogWarning($"[StorySequenceLoader] 解析 JSON 失败: {resourcePath}\n{ex.Message}");
                return null;
            }
        }
    }

    public static class StorySequenceExtensions
    {
        public static List<StoryDirector.SubtitleUI.SubtitleLine> ToSubtitleLines(this StorySequence sequence)
        {
            var result = new List<StoryDirector.SubtitleUI.SubtitleLine>();
            if (sequence?.dialogues == null) return result;

            foreach (var entry in sequence.dialogues)
            {
                if (entry == null || string.IsNullOrEmpty(entry.text)) continue;
                string speaker = entry.speaker ?? string.Empty;
                string text = entry.text.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                bool triggerShake = entry.shake;
                float overrideAmplitude = entry.shakeAmplitude;
                result.Add(new StoryDirector.SubtitleUI.SubtitleLine(speaker, text, triggerShake, overrideAmplitude));
            }

            return result;
        }
    }
}
