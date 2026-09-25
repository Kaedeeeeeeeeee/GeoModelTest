using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class FixedQuizOrderTests
{
    [Serializable] private class Sequence { public List<Line> dialogues; }
    [Serializable] private class Line { public string questionId; public string questionVersion; public List<Choice> choices; }
    [Serializable] private class Choice { public string id; public bool isCorrect; }
    [Serializable] private class Localizations { public List<Entry> texts; }
    [Serializable] private class Entry { public string key; public string value; }

    [Test]
    public void ReloadingAndConverting_ShouldPreserveAuthoredChoiceIdsAndCorrectness()
    {
        int total = 0;
        var positions = new int[3];
        var ids = new HashSet<string>();
        foreach (var asset in Resources.LoadAll<TextAsset>("Story"))
        {
            if (!asset.text.TrimStart().StartsWith("{")) continue;
            var sequence = JsonUtility.FromJson<Sequence>(asset.text);
            var questions = sequence?.dialogues?.Where(line => line.choices != null && line.choices.Count > 0).ToList();
            if (questions == null || questions.Count == 0) continue;
            for (int reload = 0; reload < 3; reload++)
            {
                var runtime = BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("StorySystem.StorySequenceLoader"),
                    "LoadFromResources", "Story/" + asset.name, false);
                var converted = (IList)BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("StorySystem.StorySequenceExtensions"),
                    "ToSubtitleLines", runtime);
                foreach (var question in questions)
                {
                    Assert.AreEqual("story-formative-v2-fixed-order", question.questionVersion);
                    object line = converted.Cast<object>().Single(item =>
                        (string)BackendTestReflection.GetField(item, "QuestionId") == question.questionId);
                    var choices = ((IList)BackendTestReflection.GetField(line, "Choices")).Cast<object>().ToList();
                    CollectionAssert.AreEqual(question.choices.Select(c => c.id), choices.Select(c => BackendTestReflection.GetField(c, "ChoiceId")));
                    CollectionAssert.AreEqual(question.choices.Select(c => c.isCorrect), choices.Select(c => BackendTestReflection.GetField(c, "IsCorrect")));
                }
            }
            foreach (var question in questions)
            {
                Assert.IsTrue(ids.Add(question.questionId), "Question IDs must be unique.");
                Assert.AreEqual(1, question.choices.Count(c => c.isCorrect));
                Assert.AreEqual(question.choices.Count, question.choices.Select(c => c.id).Distinct().Count());
                positions[question.choices.FindIndex(c => c.isCorrect)]++;
                total++;
            }
        }
        Assert.AreEqual(11, total);
        CollectionAssert.AreEqual(new[] { 4, 4, 3 }, positions, "Keep the reviewed fixed distribution, not all answers in one position.");
    }

    [Test]
    public void GuidanceAndZoomLabels_ShouldExistAndFormatInAllLanguages()
    {
        var stages = new[] { "travel", "equip", "hit", "hit_progress", "pickup", "preview", "place", "place_invalid",
            "approach", "drill", "drilling", "pickup_core", "finished" };
        foreach (string lang in new[] { "ja-JP", "zh-CN", "en-US" })
        {
            var file = JsonUtility.FromJson<Localizations>(Resources.Load<TextAsset>("Localization/Data/" + lang).text);
            var entries = file.texts.ToDictionary(e => e.key, e => e.value);
            foreach (string stage in stages)
            foreach (string input in new[] { "desktop", "touch" })
            {
                string key = "ui.collection." + stage + "." + input;
                Assert.IsTrue(entries.ContainsKey(key), lang + ": " + key);
                Assert.IsNotEmpty(string.Format(entries[key], "Tool", 2, 3));
            }
            foreach (string key in new[] { "ui.dialog.zoom.desktop", "ui.dialog.zoom.touch", "ui.dialog.zoom.close" })
                Assert.IsTrue(entries.ContainsKey(key), lang + ": " + key);
        }
    }
}
