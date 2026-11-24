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
        int found = lines.FindIndex(line => line.textKey == "slide.coral.intro");
        int question = lines.FindIndex(line => line.questionId == "q.fossil_coral_env");
        Assert.That(found, Is.GreaterThanOrEqualTo(0).And.LessThan(question));
        Assert.AreEqual("05_coral_env", lines[found].illustrationKey);
        StringAssert.Contains("岩芯", Texts("ja-JP")["slide.coral.intro"]);
        StringAssert.Contains("サンゴ", Texts("ja-JP")["slide.coral.s1"]);
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
    [Serializable] private class Card { public string id; public string type; public string doneKey; public List<CardPart> parts; public List<CardStep> steps; }
    [Serializable] private class CardPart { public string id; public string textKey; public float x; public float y; public float w; public float h; }
    [Serializable] private class CardStep { public string textKey; public List<string> show; public List<string> hide; public List<string> pulse; public List<CardMove> move; public string sfx; public float sfxDelay; }
    [Serializable] private class CardMove { public string id; public float x; public float y; }

    // 2026-11-23: quiz-like cards (order, sort, columns) felt like extra tests. Teaching is now a tap-through
    // slide show before each question: one picture, small changes per tap, no right or wrong.
    [TestCase("quest2.1", "slide.rock", "q.weathering_order")]
    [TestCase("quest3.4", "slide.grain", "q.rock_mudstone")]
    [TestCase("quest3.4", "slide.lime", "q.rock_limestone")]
    [TestCase("beat3", "slide.coral", "q.fossil_coral_env")]
    [TestCase("beat3", "slide.facies", "q.fossil_facies_term")]
    [TestCase("beat3", "slide.era", "q.fossil_ammonite_era")]
    [TestCase("beat3", "slide.tuff", "q.tuff_volcano")]
    [TestCase("beat4", "slide.keybed", "q.strata_tilt")]
    [TestCase("beat4", "slide.fold", "q.fold_term")]
    public void TeachingSlides_ShouldStepThroughBeforeTheirQuestion(string sequence, string slidesId, string questionId)
    {
        var lines = JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + sequence).text).dialogues;
        int slides = lines.FindIndex(line => line.interaction != null && line.interaction.id == slidesId);
        int question = lines.FindIndex(line => line.questionId == questionId);
        Assert.That(slides, Is.GreaterThanOrEqualTo(0).And.LessThan(question), slidesId + " comes before " + questionId);
        Assert.IsFalse(lines.Skip(slides + 1).Take(question - slides - 1).Any(line => line.interaction != null),
            "No other card sits between the slides and the question.");
        var c = lines[slides].interaction;
        Assert.AreEqual("slides", c.type);
        Assert.That(c.steps.Count, Is.InRange(3, 6), "A handful of taps, not a lecture.");
        var ids = new HashSet<string>(c.parts.Select(p => p.id));
        Assert.AreEqual(c.parts.Count, ids.Count, "Part ids are unique.");
        foreach (var p in c.parts)
        {
            Assert.That(p.w, Is.GreaterThan(0f), p.id);
            Assert.That(p.h, Is.GreaterThan(0f), p.id);
            Assert.That(p.x, Is.InRange(0f, 1f), p.id);
            Assert.That(p.y, Is.InRange(0f, 1f), p.id);
        }
        var shown = new HashSet<string>();
        foreach (var step in c.steps)
        {
            foreach (string id in (step.show ?? new List<string>()).Concat(step.hide ?? new List<string>())
                         .Concat(step.pulse ?? new List<string>()).Concat((step.move ?? new List<CardMove>()).Select(m => m.id)))
                Assert.IsTrue(ids.Contains(id), step.textKey + " uses part " + id);
            shown.UnionWith(step.show ?? new List<string>());
        }
        CollectionAssert.IsSubsetOf(ids, shown, "Every part appears at some step.");

        var keys = new List<string> { lines[slides].textKey, c.doneKey };
        keys.AddRange(c.steps.Select(s => s.textKey));
        keys.AddRange(c.parts.Select(p => p.textKey));
        var regex = new System.Text.RegularExpressions.Regex("（[ぁ-ゖー]+）");
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
        {
            var texts = Texts(language);
            foreach (string key in keys.Where(k => !string.IsNullOrEmpty(k)))
                Assert.IsTrue(texts.ContainsKey(key), key + " exists in " + language);
        }
        foreach (var step in c.steps)
            Assert.LessOrEqual(regex.Replace(Texts("ja-JP")[step.textKey], "").Length, 40, step.textKey + " fits one dialogue line.");

        // 2026-11-23: each tap can play a small sound that matches the change (bubbles for acid, a rumble for folding).
        var clips = new HashSet<string>(Resources.LoadAll<AudioClip>("Audio/SFX").Select(clip => clip.name));
        foreach (var step in c.steps.Where(s => !string.IsNullOrEmpty(s.sfx)))
        {
            Assert.IsTrue(clips.Contains(step.sfx), step.textKey + " plays " + step.sfx + " from Resources/Audio/SFX");
            Assert.That(step.sfxDelay, Is.InRange(0f, 3f), step.textKey + " sound starts during its animation");
        }
    }

    [Test]
    public void QuizLikeCards_ShouldBeGoneFromTheRoute()
    {
        foreach (string name in Route)
            foreach (var line in JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues)
                if (line.interaction != null && !string.IsNullOrEmpty(line.interaction.type))
                    Assert.AreEqual("slides", line.interaction.type, name + "/" + line.textKey);
    }

    [Test]
    public void MysteryEvidence_ShouldBeCollectedAlongTheRouteAndExplainedAtTheEnd()
    {
        var collected = Route.SelectMany(name => JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/" + name).text).dialogues)
            .Where(line => !string.IsNullOrEmpty(line.evidenceKey)).Select(line => line.evidenceKey).ToList();
        CollectionAssert.AreEqual(new[] { "evidence.e1", "evidence.e2", "evidence.e3", "evidence.e4" }, collected,
            "Four evidence cards are found in story order.");
        // 2026-11-23: the unscored conclusion choice was removed; Dr.Kaede states the conclusion herself.
        var ending = JsonUtility.FromJson<CardSequence>(Resources.Load<TextAsset>("Story/beat4").text).dialogues;
        int fold = ending.FindIndex(line => line.questionId == "q.fold_term");
        var conclusion = new[] { "story.s9.c1", "story.s9.c2", "story.s9.c3", "story.s9.c4" };
        CollectionAssert.AreEqual(conclusion, ending.Skip(fold + 1).Take(4).Select(line => line.textKey));
        Assert.AreEqual("evidence.e4", ending[fold + 1].evidenceKey);
        foreach (string language in new[] { "ja-JP", "zh-CN", "en-US" })
        {
            var texts = Texts(language);
            foreach (string key in collected.Concat(conclusion).Concat(new[] { "story.evidence.got" }))
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
