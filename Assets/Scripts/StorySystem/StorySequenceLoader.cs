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

        // 知识讲解插画 banner 资源键（可选）。Resources/Story/Illustrations/<key>，"-" 收起。
        public string illustrationKey;

        // 环境音（可选）。Resources/Audio/Ambience/<key> 循环播放并交叉淡入；"-" 淡出停止；空/缺省保持不变。
        public string ambience;

        // 音效（可选）。这句显示时播放一次 Resources/Audio/SFX/<key>。
        public string sfx;

        // 知識インタラクションカード（可選）。行の text が指示、カードを終えると done の一言が出る。
        public StoryInteraction interaction;

        // 証拠カード（可選）。この行で手に入れた証拠の名前。上に小さく「証拠カード ゲット」を出す。
        public string evidence;
        public string evidenceKey;

        // 选择题（可选）。choices 为空时该行是普通台词。
        public string questionId;
        public string questionVersion;
        public List<StoryChoice> choices;

        // 选择题"ヒント"按钮文本（可选）；选择题专用，普通台词上无效。
        public string hint;
        public string hintKey;
    }

    /// <summary>
    /// 对话内嵌选择题的单个选项（教材题目做成的分支）。
    /// </summary>
    [Serializable]
    public class StoryChoice
    {
        public string id;          // 分析用の安定ID（表示文言や並び順に依存させない）
        public string text;        // 直接文本
        public string textKey;     // 本地化键（优先）
        public bool isCorrect;     // 是否正确答案（用于计分）
        public string feedback;    // 选后反馈文本
        public string feedbackKey; // 反馈本地化键（优先）
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
        private const string PlayerNamePlaceholder = "{name}";
        private const string DefaultPlayerDisplayName = "Lily";

        public static List<StoryDirector.SubtitleUI.SubtitleLine> ToSubtitleLines(this StorySequence sequence)
        {
            var result = new List<StoryDirector.SubtitleUI.SubtitleLine>();
            if (sequence?.dialogues == null) return result;

            var localizationManager = ResolveLocalizationManager();
            bool canLocalize = localizationManager != null && localizationManager.IsInitialized;

            foreach (var entry in sequence.dialogues)
            {
                if (entry == null) continue;

                string speaker = entry.speaker ?? string.Empty;
                if (!string.IsNullOrEmpty(entry.speakerKey) && canLocalize && localizationManager.HasText(entry.speakerKey))
                {
                    speaker = localizationManager.GetText(entry.speakerKey);
                }

                if (IsSfxLine(entry, speaker))
                {
                    continue;
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

                text = ApplyPlaceholders(text);
                if (string.IsNullOrEmpty(text)) continue;
                bool triggerShake = entry.shake;
                float overrideAmplitude = entry.shakeAmplitude;
                var subtitleLine = new StoryDirector.SubtitleUI.SubtitleLine(speaker, text, triggerShake, overrideAmplitude);
                subtitleLine.IllustrationKey = entry.illustrationKey;
                subtitleLine.Ambience = entry.ambience;
                subtitleLine.Sfx = entry.sfx;
                subtitleLine.VoiceKey = entry.textKey;
                subtitleLine.Evidence = string.IsNullOrEmpty(entry.evidence) && string.IsNullOrEmpty(entry.evidenceKey)
                    ? null : ResolveText(entry.evidence, entry.evidenceKey, localizationManager, canLocalize);
                subtitleLine.EvidenceId = entry.evidenceKey;
                subtitleLine.Card = KnowledgeCard.From(entry.interaction,
                    (direct, key) => ResolveText(direct, key, localizationManager, canLocalize));

                if (entry.choices != null && entry.choices.Count > 0)
                {
                    subtitleLine.QuestionId = entry.questionId;
                    subtitleLine.QuestionVersion = string.IsNullOrWhiteSpace(entry.questionVersion)
                        ? QuizScoreManager.DefaultQuestionVersion
                        : entry.questionVersion.Trim();
                    subtitleLine.Hint = ResolveText(entry.hint, entry.hintKey, localizationManager, canLocalize);
                    subtitleLine.HintKey = entry.hintKey;
                    subtitleLine.Choices = new List<StoryDirector.SubtitleUI.SubtitleChoice>();
                    foreach (var choice in entry.choices)
                    {
                        if (choice == null) continue;
                        subtitleLine.Choices.Add(new StoryDirector.SubtitleUI.SubtitleChoice
                        {
                            ChoiceId = choice.id,
                            Text = ResolveText(choice.text, choice.textKey, localizationManager, canLocalize),
                            IsCorrect = choice.isCorrect,
                            Feedback = ResolveText(choice.feedback, choice.feedbackKey, localizationManager, canLocalize),
                            FeedbackKey = choice.feedbackKey
                        });
                    }
                }

                result.Add(subtitleLine);
            }

            return result;
        }

        /// <summary>
        /// 解析文本：优先使用本地化键（若可用且存在），否则回退到直接文本，再否则回退到键本身。
        /// </summary>
        private static string ResolveText(string directText, string key, LocalizationManager localizationManager, bool canLocalize)
        {
            string resolved = directText?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(key))
            {
                if (canLocalize && localizationManager.HasText(key))
                {
                    resolved = localizationManager.GetText(key)?.Trim();
                }
                else if (string.IsNullOrEmpty(resolved))
                {
                    resolved = key; // 至少显示键值，便于调试
                }
            }
            return ApplyPlaceholders(resolved);
        }

        private static LocalizationManager ResolveLocalizationManager()
        {
            if (Application.isPlaying)
            {
                return LocalizationManager.Instance;
            }

            return UnityEngine.Object.FindFirstObjectByType<LocalizationManager>();
        }

        private static bool IsSfxLine(StoryDialogueLine entry, string resolvedSpeaker)
        {
            if (entry == null) return false;

            if (!string.IsNullOrEmpty(entry.speakerKey) &&
                entry.speakerKey.EndsWith(".sfx", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(entry.speaker, "sfx", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(resolvedSpeaker, "sfx", StringComparison.OrdinalIgnoreCase);
        }

        private static string ApplyPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains(PlayerNamePlaceholder))
            {
                return text;
            }

            return text.Replace(PlayerNamePlaceholder, DefaultPlayerDisplayName);
        }
    }
}
