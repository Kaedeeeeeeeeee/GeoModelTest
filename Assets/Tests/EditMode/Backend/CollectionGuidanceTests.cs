using NUnit.Framework;

public class CollectionGuidanceTests
{
    [TestCase(false, 0, false, "equip")]
    [TestCase(true, 0, false, "hit")]
    [TestCase(true, 1, false, "hit_progress")]
    [TestCase(true, 2, false, "hit_progress")]
    [TestCase(true, 3, true, "pickup")]
    [TestCase(false, 0, true, "pickup")]
    public void Hammer_ShouldShowNextAction(bool equipped, int hits, bool pending, string expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("UISystem.CollectionGuidanceHUD"), "HammerStage", equipped, hits, pending));
    }

    [TestCase(false, false, false, false, false, false, true, false, "equip")]
    [TestCase(true, false, false, false, false, false, true, false, "preview")]
    [TestCase(true, true, false, false, false, false, true, false, "place_invalid")]
    [TestCase(true, true, true, false, false, false, true, false, "place")]
    [TestCase(true, false, false, true, false, false, true, false, "approach")]
    [TestCase(true, false, false, true, false, false, true, true, "drill")]
    [TestCase(true, false, false, true, true, false, false, true, "drilling")]
    [TestCase(false, false, false, true, true, true, false, false, "drilling")]
    [TestCase(false, false, false, true, false, true, true, false, "pickup_core")]
    [TestCase(true, false, false, true, false, true, false, true, "pickup_core")]
    [TestCase(true, false, false, true, false, false, false, true, "finished")]
    public void Tower_ShouldPrioritizeDrillingAndUncollectedCores(bool equipped, bool placing, bool valid,
        bool placed, bool drilling, bool pending, bool canDrill, bool nearby, string expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("UISystem.CollectionGuidanceHUD"), "TowerStage",
            equipped, placing, valid, placed, drilling, pending, canDrill, nearby));
    }

    [TestCase("Laboratory Scene", true, false, "MainScene")]
    [TestCase("MainScene", false, true, "Laboratory Scene")]
    [TestCase("MainScene", true, false, null)]
    [TestCase("Laboratory Scene", false, true, null)]
    [TestCase("Laboratory Scene", false, false, null)]
    [TestCase("MainScene", false, false, null)]
    [TestCase("Laboratory Scene", true, true, null)]
    [TestCase("MainScene", true, true, null)]
    [TestCase("StartScene", true, false, null)]
    public void Travel_ShouldPointToTheOtherSceneOnlyWhenTheObjectiveIsThere(string scene, bool field, bool lab, string expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("UISystem.CollectionGuidanceHUD"), "TravelDestination", scene, field, lab));
    }

    [TestCase(true, false, "coach")]
    [TestCase(false, true, "recall")]
    [TestCase(false, false, "quiet")]
    public void Trip_ShouldFadeGuidanceAsThePlayerPractises(bool firstTrip, bool firstReturn, string expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("UISystem.CollectionGuidanceHUD"), "TripCueLevel", firstTrip, firstReturn));
    }

    [TestCase(false, false, false, "OpenTools", 1)]
    [TestCase(false, true, false, "PickShifter", 1)]
    [TestCase(false, false, true, "UseShifter", 2)]
    [TestCase(true, false, true, "ChooseDestination", 3)]
    public void Coach_ShouldFollowTheRealControls(bool list, bool wheel, bool holding, string step, int number)
    {
        var coach = BackendTestReflection.GetType("UISystem.PhaseShifterCoach");
        var resolved = BackendTestReflection.InvokeStatic(coach, "ResolveStep", list, wheel, holding);
        Assert.AreEqual(step, resolved.ToString());
        Assert.AreEqual(number, BackendTestReflection.InvokeStatic(coach, "StepNumber", resolved));
    }

    [TestCase("ja-JP")]
    [TestCase("en-US")]
    [TestCase("zh-CN")]
    public void Coach_ShouldHaveTextInEveryLanguage(string language)
    {
        foreach (string root in new[] { "Assets/Resources/Localization/Data/", "Assets/Scripts/Localization/Data/" })
        {
            string text = System.IO.File.ReadAllText(root + language + ".json");
            foreach (string key in new[] { "ui.collection.shift_recall", "ui.shifter_coach.title", "ui.shifter_coach.why",
                "ui.shifter_coach.open.desktop", "ui.shifter_coach.open.touch", "ui.shifter_coach.pick",
                "ui.shifter_coach.choose", "ui.shifter_coach.done" })
                StringAssert.Contains("\"" + key + "\"", text, root + language);
        }
    }
}
