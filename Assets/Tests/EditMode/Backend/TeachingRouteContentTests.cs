using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>Teaching-route content agreed with the teacher on 2026-09-30.</summary>
public class TeachingRouteContentTests
{
    [Serializable] private class Sequence { public List<Line> dialogues; }
    [Serializable] private class Line { public string textKey; public string questionId; public string illustrationKey; }
    [Serializable] private class Localizations { public List<Entry> texts; }
    [Serializable] private class Entry { public string key; public string value; }

    private static readonly string[] Route = { "quest1.2", "quest2.1", "beat2", "quest3.4", "beat3", "beat4" };

    private static List<Line> Load(string name) =>
        JsonUtility.FromJson<Sequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues;

    private static Dictionary<string, string> Texts(string language) =>
        JsonUtility.FromJson<Localizations>(File.ReadAllText(Path.Combine(Application.dataPath,
            "Resources/Localization/Data", language + ".json"))).texts.ToDictionary(e => e.key, e => e.value);

    [Test]
    public void RockIdentification_ShouldBeAskedInTheLabNotTheField()
    {
        Assert.IsFalse(Load("beat2").Any(line => !string.IsNullOrEmpty(line.questionId)),
            "The field sampling dialogue no longer quizzes rock identification.");
        CollectionAssert.AreEqual(new[] { "q.rock_mudstone", "q.rock_limestone", "q.rock_chert" },
            Load("quest3.4").Where(line => !string.IsNullOrEmpty(line.questionId)).Select(line => line.questionId),
            "The lab-return dialogue asks the three rock questions in their original order.");
        var ids = Route.SelectMany(name => Load(name)).Where(line => !string.IsNullOrEmpty(line.questionId))
            .Select(line => line.questionId).ToList();
        Assert.AreEqual(11, ids.Count);
        Assert.AreEqual(11, ids.Distinct().Count(), "Each formative question stays on the route exactly once.");
    }

    [TestCase("ja-JP")]
    [TestCase("zh-CN")]
    [TestCase("en-US")]
    public void CorePickupPraise_ShouldBeFriendly(string language)
    {
        var lines = Load("core-return");
        Assert.AreEqual("story.core_return.line", lines.Last().textKey);
        string praise = Texts(language)["story.core_return.line"];
        if (language == "ja-JP") StringAssert.StartsWith("うまくできたね！", praise);
        StringAssert.DoesNotContain("よくやった", praise);
        StringAssert.DoesNotContain("做得好", praise);
        StringAssert.DoesNotContain("Well done", praise);
    }

    [Serializable] private class FullLine { public string textKey; public string questionId; public string illustrationKey; public List<Choice> choices; }
    [Serializable] private class Choice { public string id; }
    [Serializable] private class FullSequence { public List<FullLine> dialogues; }

    [TestCase("quest3.4", "03_limestone", "q.rock_limestone")]
    [TestCase("quest3.4", "04_chert", "q.rock_chert")]
    [TestCase("beat3", "06_facies_fossil", "q.fossil_facies_term")]
    [TestCase("beat3", "09_tuff_volcano", "q.tuff_volcano")]
    public void TeachingIllustration_ShouldAppearOnAnExplanationLineBeforeItsQuestion(string sequence, string illustration, string questionId)
    {
        // Question lines hide the banner, so a key placed on a question line is never shown.
        var lines = JsonUtility.FromJson<FullSequence>(Resources.Load<TextAsset>("Story/" + sequence).text).dialogues;
        int shown = lines.FindIndex(line => line.illustrationKey == illustration);
        int question = lines.FindIndex(line => line.questionId == questionId);
        Assert.That(shown, Is.GreaterThanOrEqualTo(0).And.LessThan(question));
        Assert.IsTrue(lines[shown].choices == null || lines[shown].choices.Count == 0,
            illustration + " is set on an explanation line, not on a question line.");
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
            Assert.IsTrue(Texts(language).ContainsKey(lines[shown].textKey), lines[shown].textKey + " exists in " + language);
    }

    [Test]
    public void TeachingIllustrations_ShouldNeverSitOnQuestionLines()
    {
        foreach (string name in Route)
        {
            var lines = JsonUtility.FromJson<FullSequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues;
            foreach (var line in lines.Where(line => line.choices != null && line.choices.Count > 0))
                Assert.IsTrue(string.IsNullOrEmpty(line.illustrationKey),
                    name + "/" + line.questionId + " would hide its illustration immediately.");
        }
    }

    [Test]
    public void LabAnalysis_ShouldRevealCoralFossilInsideTheCoreBeforeTheCoralQuestion()
    {
        var lines = Load("beat3");
        int found = lines.FindIndex(line => line.textKey == "story.beat3.l11");
        int question = lines.FindIndex(line => line.questionId == "q.fossil_coral_env");
        Assert.That(found, Is.GreaterThanOrEqualTo(0).And.LessThan(question));
        Assert.AreEqual("05_coral_env", lines[found].illustrationKey);
        string ja = Texts("ja-JP")["story.beat3.l11"];
        StringAssert.Contains("サンゴ", ja);
        StringAssert.Contains("岩芯", ja);
    }

    [Test]
    public void LabOpening_ShouldHandOverToThePlayerBeforeAnyTeaching()
    {
        // Study 1 lost 19 of 30 players between the long lab introduction and their first own move.
        // The introduction now ends by sending the player to Kaede; teaching starts in that talk.
        var intro = Load("quest1.2");
        Assert.LessOrEqual(intro.Count, 8);
        Assert.IsFalse(intro.Any(line => !string.IsNullOrEmpty(line.questionId)), "The introduction asks no question.");
        Assert.IsFalse(intro.Any(line => !string.IsNullOrEmpty(line.illustrationKey)), "The introduction shows no teaching picture.");
        StringAssert.Contains("こっちに来", Texts("ja-JP")[intro.Last().textKey]);
        var firstTalk = Load("quest2.1");
        Assert.AreEqual("q.weathering_order", firstTalk.Single(line => !string.IsNullOrEmpty(line.questionId)).questionId);
        Assert.AreEqual("01_weathering_order", firstTalk.First().illustrationKey);
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
            foreach (var line in intro.Concat(firstTalk).Concat(Load("quest1.1")))
                Assert.IsTrue(Texts(language).ContainsKey(line.textKey), line.textKey + " exists in " + language);
    }

    [Test]
    public void StoryLines_ShouldStayShortForGradeOneReaders()
    {
        foreach (string name in new[] { "quest1.1", "quest1.2" })
            foreach (var line in Load(name))
            {
                string ja = System.Text.RegularExpressions.Regex.Replace(Texts("ja-JP")[line.textKey], "（[ぁ-ゖー]+）", "");
                Assert.LessOrEqual(ja.Length, 40, name + "/" + line.textKey + " stays within 40 characters without furigana.");
            }
    }
}
