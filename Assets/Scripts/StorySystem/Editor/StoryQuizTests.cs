#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StorySystem.EditorTests
{
    /// <summary>
    /// 教学版剧情/测验系统的健壮性自检。
    /// 因为游戏代码都在预定义的 Assembly-CSharp（NUnit 测试程序集无法引用它），
    /// 这里采用编辑器菜单驱动的断言式自检——与本项目既有的 *Test.cs 模式一致，
    /// 编译进 Assembly-CSharp-Editor，可直接访问游戏代码。
    ///
    /// 运行方式：Unity 菜单 → Tools/Tests/Run Story & Quiz Tests
    /// 结果输出到 Console（全部通过为一条 Log，有失败为 LogError）。
    /// </summary>
    public static class StoryQuizTests
    {
        [Serializable] private class LocEntry { public string key; public string value; }
        [Serializable] private class LocFile { public List<LocEntry> texts; }

        // 教学流程实际加载的剧情资源（含内嵌选择题）。
        private static readonly string[] BeatFiles =
        {
            "Story/quest1.1", "Story/quest1.2", "Story/quest4.2",
            "Story/beat2", "Story/beat3", "Story/beat4"
        };

        private static readonly string[] Langs = { "ja-JP", "zh-CN", "en-US" };

        // 代码里直接用到的本地化键（选择题 UI / 报告界面）。
        private static readonly string[] CodeKeys =
        {
            "ui.dialog.continue", "ui.dialog.choose",
            "quiz.correct", "quiz.incorrect",
            "report.title", "report.grade", "report.score"
        };

        private static int _pass;
        private static int _fail;

        private static void Check(bool cond, string msg)
        {
            if (cond) { _pass++; }
            else { _fail++; Debug.LogError($"[StoryQuizTests] FAIL: {msg}"); }
        }

        [MenuItem("Tools/Tests/Run Story & Quiz Tests")]
        public static void RunAll()
        {
            _pass = 0;
            _fail = 0;

            TestQuizScoreManager();
            TestBeatParsing();
            TestStoryLineSanitization();
            TestLocalizationCoverage();
            TestFurigana();

            string summary = $"[StoryQuizTests] DONE — {_pass} passed, {_fail} failed.";
            if (_fail == 0) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        // 1) 计分器逻辑：评级边界、去重、重置。
        private static void TestQuizScoreManager()
        {
            var qs = QuizScoreManager.Instance;

            qs.Reset();
            Check(qs.Total == 0, "score empty after reset");
            Check(qs.Grade == "-", "grade is '-' with no answers");

            qs.Reset();
            for (int i = 0; i < 5; i++) qs.Record("p" + i, true);
            Check(qs.Total == 5 && qs.CorrectCount == 5, "5/5 recorded");
            Check(qs.Grade == "A", "100% -> A");

            qs.Reset();
            for (int i = 0; i < 4; i++) qs.Record("q" + i, true);
            qs.Record("q4", false);
            Check(qs.Grade == "A", "80% -> A (boundary)");

            qs.Reset();
            for (int i = 0; i < 3; i++) qs.Record("q" + i, true);
            qs.Record("q3", false);
            qs.Record("q4", false);
            Check(qs.Grade == "B", "60% -> B (boundary)");

            qs.Reset();
            for (int i = 0; i < 2; i++) qs.Record("q" + i, true);
            for (int i = 2; i < 5; i++) qs.Record("q" + i, false);
            Check(qs.Grade == "C", "40% -> C");

            // 同一 questionId 重复作答：只算一题，最后一次为准。
            qs.Reset();
            qs.Record("dup", false);
            qs.Record("dup", true);
            Check(qs.Total == 1, "dedup keeps a single entry for same questionId");
            Check(qs.CorrectCount == 1, "dedup is last-write-wins");

            // 空 id 每次都追加。
            qs.Reset();
            qs.Record("", true);
            qs.Record("", false);
            Check(qs.Total == 2, "empty questionId appends each time");

            qs.Reset();
        }

        // 2) 剧情解析：每个 beat 能加载；选择题结构合法（>=2 选项、恰好 1 个正确、有 questionId）。
        private static void TestBeatParsing()
        {
            foreach (var path in BeatFiles)
            {
                var seq = StorySequenceLoader.LoadFromResources(path, false);
                Check(seq != null, $"{path} loads from Resources");
                if (seq == null) continue;

                Check(seq.dialogues != null && seq.dialogues.Count > 0, $"{path} has dialogues");
                if (seq.dialogues == null) continue;

                foreach (var line in seq.dialogues)
                {
                    bool hasChoices = line.choices != null && line.choices.Count > 0;
                    if (!hasChoices) continue;

                    Check(line.choices.Count >= 2, $"{path} quiz has >=2 choices");
                    int correct = line.choices.Count(c => c != null && c.isCorrect);
                    Check(correct == 1, $"{path} quiz '{line.questionId}' must have exactly 1 correct (got {correct})");
                    Check(!string.IsNullOrEmpty(line.questionId), $"{path} quiz line needs a questionId");

                    foreach (var c in line.choices)
                    {
                        Check(c != null, $"{path} choice not null");
                        if (c == null) continue;
                        Check(!string.IsNullOrEmpty(c.text) || !string.IsNullOrEmpty(c.textKey),
                            $"{path} choice has text or textKey");
                    }
                }
            }
        }

        // 3) 字幕清理：SFX 指令行不直接显示，占位符不会漏到玩家可见文本。
        private static void TestStoryLineSanitization()
        {
            var seq = new StorySequence
            {
                dialogues = new List<StoryDialogueLine>
                {
                    new StoryDialogueLine
                    {
                        speaker = "sfx",
                        text = "rumble_start"
                    },
                    new StoryDialogueLine
                    {
                        speaker = "Dr.Kaede",
                        text = "{name}、すぐ退避して！",
                        choices = new List<StoryChoice>
                        {
                            new StoryChoice
                            {
                                text = "{name} understands",
                                isCorrect = true,
                                feedback = "{name} moved safely."
                            },
                            new StoryChoice
                            {
                                text = "Wait",
                                isCorrect = false,
                                feedback = "{name} hesitated."
                            }
                        }
                    }
                }
            };

            var lines = seq.ToSubtitleLines();
            Check(lines.Count == 1, "sfx dialogue line is skipped");
            if (lines.Count == 0) return;

            Check(!lines[0].Text.Contains("{name}"), "line placeholder is replaced");
            Check(lines[0].Text.Contains("Lily"), "line placeholder uses player display name");
            Check(lines[0].Choices != null && lines[0].Choices.Count == 2, "choices preserved after sanitization");
            if (lines[0].Choices != null && lines[0].Choices.Count > 0)
            {
                Check(!lines[0].Choices[0].Text.Contains("{name}"), "choice placeholder is replaced");
                Check(!lines[0].Choices[0].Feedback.Contains("{name}"), "feedback placeholder is replaced");
            }
        }

        // 4) 本地化覆盖：beat 引用的每个键 + 代码用到的通用键，都必须在三种语言里存在。
        private static void TestLocalizationCoverage()
        {
            var keysByLang = new Dictionary<string, HashSet<string>>();
            foreach (var lang in Langs)
            {
                var ta = Resources.Load<TextAsset>($"Localization/Data/{lang}");
                Check(ta != null, $"localization file {lang} loads");

                var set = new HashSet<string>();
                if (ta != null)
                {
                    var lf = JsonUtility.FromJson<LocFile>(ta.text);
                    if (lf?.texts != null)
                    {
                        foreach (var e in lf.texts)
                            if (!string.IsNullOrEmpty(e.key)) set.Add(e.key);
                    }
                }
                keysByLang[lang] = set;
            }

            var referenced = new HashSet<string>();
            foreach (var path in BeatFiles)
            {
                var seq = StorySequenceLoader.LoadFromResources(path, false);
                if (seq?.dialogues == null) continue;

                foreach (var line in seq.dialogues)
                {
                    if (!string.IsNullOrEmpty(line.textKey)) referenced.Add(line.textKey);
                    if (!string.IsNullOrEmpty(line.speakerKey)) referenced.Add(line.speakerKey);
                    if (line.choices == null) continue;
                    foreach (var c in line.choices)
                    {
                        if (c == null) continue;
                        if (!string.IsNullOrEmpty(c.textKey)) referenced.Add(c.textKey);
                        if (!string.IsNullOrEmpty(c.feedbackKey)) referenced.Add(c.feedbackKey);
                    }
                }
            }

            foreach (var lang in Langs)
            {
                foreach (var key in referenced)
                    Check(keysByLang[lang].Contains(key), $"referenced key '{key}' missing in {lang}");

                foreach (var key in CodeKeys)
                    Check(keysByLang[lang].Contains(key), $"code key '{key}' missing in {lang}");
            }
        }

        // 5) 注音转换：地震（じしん）→ ruby；旁注/嵌套括号正确处理；无括号原样返回。
        private static void TestFurigana()
        {
            Check(FuriganaProcessor.IsKanji('地') && !FuriganaProcessor.IsKanji('A'), "IsKanji basic");
            Check(FuriganaProcessor.IsKana('じ') && FuriganaProcessor.IsKana('ジ') && !FuriganaProcessor.IsKana('地'), "IsKana basic");

            string r1 = FuriganaProcessor.ToRubyMarkup("地震（じしん）だ");
            Check(r1.Contains("地震") && r1.Contains("じしん"), "ruby keeps base + reading");
            Check(r1.Contains("<voffset") && r1.Contains("<size"), "ruby emits voffset/size markup");
            Check(!r1.Contains("（じしん）"), "ruby removes the inline parens");
            Check(r1.EndsWith("だ"), "trailing text preserved");

            string r2 = FuriganaProcessor.ToRubyMarkup("ABC 123");
            Check(r2 == "ABC 123", "no parens -> unchanged");

            // 旁注/嵌套：外层括号是旁注(含汉字)，内层 二酸化炭素（にさんかたんそ）才是注音
            string r3 = FuriganaProcessor.ToRubyMarkup("あわ（二酸化炭素（にさんかたんそ））");
            Check(r3.StartsWith("あわ（"), "aside outer paren preserved");
            Check(r3.Contains("にさんかたんそ") && r3.Contains("<voffset"), "nested real reading becomes ruby");
            Check(r3.EndsWith("）"), "aside closing paren preserved");
        }
    }
}
#endif
