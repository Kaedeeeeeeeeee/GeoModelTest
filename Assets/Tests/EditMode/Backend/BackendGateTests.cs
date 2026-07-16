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
        Object settings = Resources.Load("BackendSettings");
        Assert.IsNotNull(settings);
        Assert.IsFalse((bool)BackendTestReflection.GetProperty(settings, "EnableProductionResearchEntry"));
    }
}
