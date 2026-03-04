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
        public string speakerKey;
        public string textKey;
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

            var localizationManager = LocalizationManager.Instance;
            bool canLocalize = localizationManager != null && localizationManager.IsInitialized;

            foreach (var entry in sequence.dialogues)
            {
                if (entry == null) continue;

                string speaker = entry.speaker ?? string.Empty;
                if (!string.IsNullOrEmpty(entry.speakerKey) && canLocalize && localizationManager.HasText(entry.speakerKey))
                {
                    speaker = localizationManager.GetText(entry.speakerKey);
                }

                string text = entry.text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(entry.textKey))
                {
                    if (canLocalize && localizationManager.HasText(entry.textKey))
                    {
                        text = localizationManager.GetText(entry.textKey)?.Trim();
                    }
                    else if (string.IsNullOrEmpty(text))
                    {
                        text = entry.textKey; // 至少显示键值，便于调试
                    }
                }

                if (string.IsNullOrEmpty(text)) continue;
                bool triggerShake = entry.shake;
                float overrideAmplitude = entry.shakeAmplitude;
                result.Add(new StoryDirector.SubtitleUI.SubtitleLine(speaker, text, triggerShake, overrideAmplitude));
            }

            return result;
        }
    }
}
