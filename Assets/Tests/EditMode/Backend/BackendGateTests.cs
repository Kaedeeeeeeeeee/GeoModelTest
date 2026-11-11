using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BackendGateTests
{
    [Test]
    public void BackendBootstrap_ShouldWaitForPlayerToStartGame()
    {
        var bootstrapType = BackendTestReflection.GetType("Backend.BackendBootstrap");
        bool hasRuntimeBootstrap = bootstrapType
            .GetMethods(System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)
            .Any(method => method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), true).Length > 0);

        Assert.IsFalse(hasRuntimeBootstrap, "Loading the title alone must not start backend authentication or telemetry.");
    }

    [Test]
    public void BackendSettings_ShouldKeepProductionResearchEntryLockedByDefault()
    {
        var settings = ScriptableObject.CreateInstance(BackendTestReflection.GetType("Backend.BackendSettings"));
        try
        {
            Assert.IsFalse((bool)BackendTestReflection.GetProperty(settings, "EnableProductionResearchEntry"));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void PublishedSettings_ShouldKeepTheEndedOnlineStudyClosed()
    {
        // The online study and its consent pages ended in 2026-11: nothing is sent from the published game.
        Object settings = Resources.Load("BackendSettings");
        Assert.IsNotNull(settings);
        Assert.IsFalse((bool)BackendTestReflection.GetProperty(settings, "EnableBackend"));
        Assert.IsFalse((bool)BackendTestReflection.GetProperty(settings, "EnableProductionResearchEntry"));
        Assert.AreEqual("game-ingest-v2", BackendTestReflection.GetProperty(settings, "IngestFunctionName"));
    }
}
