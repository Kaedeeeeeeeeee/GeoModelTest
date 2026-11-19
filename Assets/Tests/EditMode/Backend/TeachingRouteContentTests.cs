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

    [Serializable] private class CardSequence { public List<CardLine> dialogues; }
    [Serializable] private class CardLine { public string textKey; public string questionId; public string evidenceKey; public Card interaction; }
    [Serializable] private class Card { public string id; public string type; public string titleKey; public string wrongKey; public string doneKey; public List<CardItem> items; public List<CardBin> bins; public List<CardColumn> columns; public List<CardText> evidence; }
    [Serializable] private class CardItem { public string textKey; public string noteKey; public string hintKey; public string bin; public int order; public bool correct; }
    [Serializable] private class CardText { public string textKey; }
    [Serializable] private class CardBin { public string id; public string textKey; }
    [Serializable] private class CardColumn { public string labelKey; public List<CardLayer> layers; }
    [Serializable] private class CardLayer { public string textKey; public bool target; }

    [TestCase("quest2.1", "card.weathering_order", "order", "q.weathering_order")]
    [TestCase("beat3", "card.fossil_tells", "sort", "q.fossil_facies_term")]
    [TestCase("beat3", "card.era_line", "sort", "q.fossil_ammonite_era")]
    [TestCase("beat4", "card.key_bed", "columns", "q.strata_tilt")]
    public void KnowledgeCard_ShouldBePlayableBeforeItsQuestion(string sequence, string cardId, string type, string questionId)
    {
        var lines = JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + sequence).text).dialogues;
        int card = lines.FindIndex(line => line.interaction != null && line.interaction.id == cardId);
        int question = lines.FindIndex(line => line.questionId == questionId);
        Assert.That(card, Is.GreaterThanOrEqualTo(0).And.LessThan(question), cardId + " comes before " + questionId);
        var c = lines[card].interaction;
        Assert.AreEqual(type, c.type);
        var keys = new List<string> { lines[card].textKey, c.titleKey, c.wrongKey, c.doneKey };
        if (type == "order")
        {
            CollectionAssert.AreEquivalent(Enumerable.Range(1, c.items.Count), c.items.Select(i => i.order), "Each step has one place in the order.");
            CollectionAssert.AreNotEqual(Enumerable.Range(1, c.items.Count), c.items.Select(i => i.order), "Cards do not start already in order.");
        }
        if (type == "sort")
        {
            Assert.GreaterOrEqual(c.bins.Count, 2);
            foreach (var item in c.items) Assert.IsTrue(c.bins.Any(b => b.id == item.bin), item.textKey + " has a box.");
            foreach (var bin in c.bins) Assert.IsTrue(c.items.Any(i => i.bin == bin.id), bin.id + " receives a card.");
            keys.AddRange(c.bins.Select(b => b.textKey));
        }
        if (type == "columns")
        {
            Assert.GreaterOrEqual(c.columns.Count, 2);
            foreach (var column in c.columns)
            {
                Assert.AreEqual(1, column.layers.Count(l => l.target), column.labelKey + " has one layer to tap.");
                keys.Add(column.labelKey);
                keys.AddRange(column.layers.Select(l => l.textKey));
            }
        }
        if (c.items != null)
            foreach (var item in c.items)
                keys.AddRange(new[] { item.textKey, item.noteKey, item.hintKey }.Where(k => !string.IsNullOrEmpty(k)));
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
        {
            var texts = Texts(language);
            foreach (string key in keys) Assert.IsTrue(texts.ContainsKey(key), key + " exists in " + language);
        }
    }

    [Test]
    public void MysteryEvidence_ShouldBeCollectedAlongTheRouteAndUsedInTheConclusion()
    {
        var collected = Route.SelectMany(name => JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues)
            .Where(line => !string.IsNullOrEmpty(line.evidenceKey)).Select(line => line.evidenceKey).ToList();
        CollectionAssert.AreEqual(new[] { "evidence.e1", "evidence.e2", "evidence.e3", "evidence.e4" }, collected,
            "Four evidence cards are found in story order.");
        var ending = JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/beat4").text).dialogues;
        var conclusion = ending.Single(line => line.interaction != null && line.interaction.id == "card.conclusion");
        Assert.IsTrue(string.IsNullOrEmpty(conclusion.questionId), "The conclusion is not scored.");
        Assert.AreEqual("choose", conclusion.interaction.type);
        CollectionAssert.AreEqual(collected, conclusion.interaction.evidence.Select(e => e.textKey));
        Assert.AreEqual(1, conclusion.interaction.items.Count(i => i.correct));
        Assert.That(ending.IndexOf(conclusion), Is.GreaterThan(ending.FindIndex(line => line.questionId == "q.fold_term")));
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
        {
            var texts = Texts(language);
            foreach (string key in collected.Concat(conclusion.interaction.items.SelectMany(i => new[] { i.textKey, i.hintKey }))
                         .Concat(new[] { "story.evidence.got", conclusion.interaction.titleKey, conclusion.interaction.doneKey })
                         .Where(k => !string.IsNullOrEmpty(k)))
                Assert.IsTrue(texts.ContainsKey(key), key + " exists in " + language);
            StringAssert.Contains("{0}", texts["story.evidence.got"]);
        }
    }

    [Test]
    public void StudyTwoScript_ShouldKeepEveryStoryLineShort()
    {
        // Explanation lines right before a question may run longer; story lines stay within 40 characters.
        var regex = new System.Text.RegularExpressions.Regex("（[ぁ-ゖー]+）");
        var ja = Texts("ja-JP");
        foreach (string name in new[] { "quest3.2", "beat2", "quest4.1", "quest4.2", "core-return", "beat4" })
            foreach (var line in JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues)
            {
                if (!string.IsNullOrEmpty(line.questionId)) continue;
                Assert.LessOrEqual(regex.Replace(ja[line.textKey], "").Length, 40, name + "/" + line.textKey);
            }
    }
}
