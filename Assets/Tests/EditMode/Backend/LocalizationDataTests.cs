using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class LocalizationDataTests
{
    private static readonly string[] ForbiddenFragments =
    {
        "非反应性",
        "収集進度",
        "切断進度",
        "世界座標",
        "样本",
        "仓库",
        "背包",
        "切割失败",
        "次（つぎ）がいつ、どこで来（く）るか",
        "地震防止",
        "危（あぶ）ないのは、ここ",
        "「警告（けいこく）」"
    };

    private static readonly string[] RequiredKeys =
    {
        "sample.collection.mobile",
        "sample.retrieve.mobile",
        "warehouse.interaction.mobile",
        "cutting_station.interaction.mobile",
        "drill_tower.recall_prompt_mobile",
        "drill_tower.drill_prompt_mobile",
        "story.content_notice.title",
        "story.content_notice.continue",
        "story.content_notice.skip",
        "warehouse.button.confirm_discard",
        "report.first_correct",
        "report.final_mastery",
        "report.completion",
        "report.average_attempts",
        "report.hint_usage",
        "ui.start.research_test",
        "ui.start.research_code.title",
        "ui.start.research_code.confirm"
    };

    private string _json;
    private List<string> _keys;

    [TestCase("ja-JP")]
    [TestCase("zh-CN")]
    [TestCase("en-US")]
    public void RemediationCopy_ShouldExistWithoutDuplicatesInEveryLanguage(string language)
    {
        string source = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Localization/Data", language + ".json"));
        var keys = Regex.Matches(source, "\\\"key\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        Assert.AreEqual(keys.Count, keys.Distinct().Count());
        foreach (string key in new[] { "ui.start.continue", "ui.start.new_game.title", "ui.start.new_game.message",
            "ui.start.new_game.warning", "ui.start.new_game.confirm", "ui.start.new_game.cancel", "ui.session.save_return",
            "ui.history.title", "ui.tool.current", "report.wrong_attempts", "report.practice", "backend.bound", "backend.pending_upload" })
            CollectionAssert.Contains(keys, key);
        for (int i = 1; i <= 10; i++) CollectionAssert.Contains(keys, "quest.step." + i);
        Assert.AreEqual(source, File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Localization/Data", language + ".json")));
    }

    [SetUp]
    public void SetUp()
    {
        string path = Path.Combine(Application.dataPath, "Resources/Localization/Data/ja-JP.json");
        _json = File.ReadAllText(path);
        _keys = Regex.Matches(_json, "\\\"key\\\"\\s*:\\s*\\\"(?<key>(?:\\\\.|[^\\\"])*)\\\"")
            .Cast<Match>()
            .Select(match => Regex.Unescape(match.Groups["key"].Value))
            .ToList();
    }

    [Test]
    public void JapaneseLocalization_ShouldHaveUniqueNonEmptyKeysAndValues()
    {
        Assert.IsNotEmpty(_keys, "No localization keys were found.");

        var duplicates = _keys
            .GroupBy(key => key)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        CollectionAssert.IsEmpty(duplicates, $"Duplicate keys: {string.Join(", ", duplicates)}");

        var values = Regex.Matches(_json, "\\\"value\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"")
            .Cast<Match>()
            .Select(match => match.Groups["value"].Value)
            .ToList();
        Assert.AreEqual(_keys.Count, values.Count, "Each localization key must have one value.");
        Assert.IsFalse(values.Any(string.IsNullOrWhiteSpace), "Localization values must not be empty.");
    }

    [Test]
    public void JapaneseLocalization_ShouldContainRequiredMobileAndSafetyKeys()
    {
        foreach (string requiredKey in RequiredKeys)
        {
            CollectionAssert.Contains(_keys, requiredKey);
        }
    }

    [Test]
    public void JapaneseLocalization_ShouldNotContainKnownUnsafeOrMixedLanguageCopy()
    {
        foreach (string fragment in ForbiddenFragments)
        {
            StringAssert.DoesNotContain(fragment, _json, $"Forbidden copy remains: {fragment}");
        }

        StringAssert.DoesNotContain("[missing.", _json.ToLowerInvariant());
    }

    [Test]
    public void JapaneseLocalization_ShouldHaveBalancedFormatPlaceholders()
    {
        var values = Regex.Matches(_json, "\\\"value\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"")
            .Cast<Match>()
            .Select(match => match.Groups["value"].Value);

        foreach (string value in values)
        {
            int openingBraces = value.Count(character => character == '{');
            int closingBraces = value.Count(character => character == '}');
            Assert.AreEqual(openingBraces, closingBraces, $"Unbalanced placeholder braces: {value}");
        }
    }
}
