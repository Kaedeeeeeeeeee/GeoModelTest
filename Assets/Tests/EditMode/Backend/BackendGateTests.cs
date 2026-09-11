using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BackendGateTests
{
    [Test]
    public void BackendBootstrap_ShouldNotAutoStartTelemetryForOrdinaryPlay()
    {
        var bootstrapType = BackendTestReflection.GetType("Backend.BackendBootstrap");
        bool hasRuntimeBootstrap = bootstrapType
            .GetMethods(System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)
            .Any(method => method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), true).Length > 0);

        Assert.IsFalse(hasRuntimeBootstrap, "Ordinary play must not start backend authentication or telemetry automatically.");
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
    public void PublishedResearchEntry_ShouldUseParticipantBoundIngest()
    {
        Object settings = Resources.Load("BackendSettings");
        Assert.IsNotNull(settings);
        Assert.IsTrue((bool)BackendTestReflection.GetProperty(settings, "CanShowResearchEntry"));
        Assert.AreEqual("game-ingest-v2", BackendTestReflection.GetProperty(settings, "IngestFunctionName"));
    }
}
