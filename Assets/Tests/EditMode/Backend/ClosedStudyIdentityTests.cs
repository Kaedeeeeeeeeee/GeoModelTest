using NUnit.Framework;
using UnityEngine;

// 2026-11-26: a browser bound to the closed Testee study kept showing 「接続を確認して…」 at the survey button.
public class ClosedStudyIdentityTests
{
    private static string Key(string name) =>
        (string)BackendTestReflection.GetField(BackendTestReflection.GetType("Backend.BackendSessionStore"), name);

    private static string L(string key) =>
        (string)BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("UISystem.GameUI"), "L", key);

    [Test]
    public void DiscardClosedStudyIdentity_ShouldForgetTheOldIdentityAndItsUnsentData()
    {
        var clientType = BackendTestReflection.GetType("Backend.TelemetryClient");
        string pendingEnd = (string)BackendTestReflection.GetField(clientType, "PendingSessionEndPrefsKey");
        PlayerPrefs.SetString(Key("AccessTokenKey"), "old-access");
        PlayerPrefs.SetString(Key("RefreshTokenKey"), "old-refresh");
        PlayerPrefs.SetString(Key("UserIdKey"), "00000000-0000-4000-8000-000000000001");
        PlayerPrefs.SetString(Key("ResearchParticipantIdKey"), "00000000-0000-4000-8000-000000000002");
        PlayerPrefs.SetString(Key("ResearchStudyIdKey"), "00000000-0000-4000-8000-000000000003");
        PlayerPrefs.SetString(pendingEnd, "{}");
        PlayerPrefs.Save();

        BackendTestReflection.InvokeStatic(clientType, "DiscardClosedStudyIdentity");

        Assert.IsFalse(PlayerPrefs.HasKey(Key("AccessTokenKey")));
        Assert.IsFalse(PlayerPrefs.HasKey(Key("RefreshTokenKey")));
        Assert.IsFalse(PlayerPrefs.HasKey(Key("UserIdKey")));
        Assert.IsFalse(PlayerPrefs.HasKey(Key("ResearchParticipantIdKey")));
        Assert.IsFalse(PlayerPrefs.HasKey(Key("ResearchStudyIdKey")));
        Assert.IsFalse(PlayerPrefs.HasKey(pendingEnd));
        Assert.AreEqual(0, (int)BackendTestReflection.GetProperty(
            System.Activator.CreateInstance(BackendTestReflection.GetType("Backend.TelemetryQueue"), 1000), "Count"));
    }

    [Test]
    public void SurveyButton_ShouldSayEntryIsClosedInsteadOfBlamingTheNetwork()
    {
        var clientType = BackendTestReflection.GetType("Backend.TelemetryClient");
        var gatewayType = BackendTestReflection.GetType("Backend.SurveyGateway");
        string closed = (string)BackendTestReflection.InvokeStatic(clientType, "ParticipationErrorMessage", 410L, null);
        Assert.AreEqual(L("backend.closed"), closed);
        Assert.AreEqual(closed, BackendTestReflection.InvokeStatic(gatewayType, "SurveyActivationMessage", closed));
        Assert.AreEqual(L("survey.retry"),
            BackendTestReflection.InvokeStatic(gatewayType, "SurveyActivationMessage", L("backend.network")));
    }
}
