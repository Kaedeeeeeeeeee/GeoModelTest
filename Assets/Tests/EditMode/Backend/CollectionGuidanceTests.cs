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
}
