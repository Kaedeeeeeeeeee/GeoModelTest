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
