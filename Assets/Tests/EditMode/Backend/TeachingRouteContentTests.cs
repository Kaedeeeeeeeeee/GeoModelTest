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

    private static readonly string[] Route = { "quest1.2", "beat2", "quest3.4", "beat3", "beat4" };

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
}
