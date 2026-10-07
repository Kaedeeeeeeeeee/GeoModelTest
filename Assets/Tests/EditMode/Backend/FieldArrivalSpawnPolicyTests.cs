using NUnit.Framework;

public class FieldArrivalSpawnPolicyTests
{
    [TestCase("Laboratory Scene", "MainScene", true, true)]
    [TestCase("Laboratory Scene", "MainScene", false, false)]
    [TestCase("StartScene", "MainScene", false, false)]
    [TestCase("StartScene", "MainScene", true, false)]
    [TestCase("MainScene", "MainScene", true, false)]
    [TestCase("MainScene", "Laboratory Scene", true, false)]
    [TestCase("Laboratory Scene", "StartScene", true, false)]
    [TestCase("OtherScene", "MainScene", true, false)]
    public void FieldEntrance_ShouldApplyOnlyToPhaseShifterReturnFromLaboratory(
        string sourceScene, string destinationScene, bool fromPhaseShifter, bool expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("GameSceneManager"), "ShouldUseFieldArrival",
            sourceScene, destinationScene, fromPhaseShifter));
    }
}
