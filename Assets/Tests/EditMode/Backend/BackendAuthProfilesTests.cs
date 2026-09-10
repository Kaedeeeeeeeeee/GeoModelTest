using System;
using NUnit.Framework;
using UnityEngine;

public class BackendAuthProfilesTests
{
    private Type _profiles;
    private const string Server = "http://127.0.0.1:55329";
    private const string CodeA = "QA-PROFILE-A";
    private const string CodeB = "QA-PROFILE-B";

    [SetUp]
    public void SetUp()
    {
        _profiles = BackendTestReflection.GetType("Backend.BackendAuthProfiles");
        foreach (string key in new[] { "Backend.CurrentCodeHash.v1", "Backend.CurrentCodeVerified.v1", "Backend.LastVerifiedCodeHash.v1", "Backend.PendingSessionEnd.v1", "Backend.AccessToken", "Backend.RefreshToken", "Backend.UserId", "Backend.AccessTokenExpiresAtUnix" })
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(BackendTestReflection.GetType("Backend.TelemetryQueue"), "PendingEventsPrefsKey"));
        foreach (string code in new[] { CodeA, CodeB })
            PlayerPrefs.DeleteKey("Backend.AuthProfile.v1." + BackendTestReflection.InvokeStatic(_profiles, "CodeHash", Server, code));
    }

    private bool Select(string code, out bool changed)
    {
        object[] args = { Server, code, false };
        bool result = (bool)_profiles.GetMethod("SelectCode").Invoke(null, args);
        changed = (bool)args[2];
        return result;
    }

    private void VerifyIdentity(string id)
    {
        PlayerPrefs.SetString("Backend.UserId", id);
        PlayerPrefs.SetString("Backend.RefreshToken", "test-refresh-" + id);
        BackendTestReflection.InvokeStatic(_profiles, "MarkVerified");
    }

    [Test]
    public void SharedDevice_ShouldRestoreEachParticipantsOwnIdentity()
    {
        Assert.IsTrue(Select(CodeA, out bool first));
        Assert.IsFalse(first);
        VerifyIdentity("participant-a");
        Assert.IsTrue(Select(CodeB, out bool changed));
        Assert.IsTrue(changed);
        Assert.IsFalse(PlayerPrefs.HasKey("Backend.UserId"));
        VerifyIdentity("participant-b");
        Assert.IsTrue(Select(CodeA, out changed));
        Assert.IsTrue(changed);
        Assert.AreEqual("participant-a", PlayerPrefs.GetString("Backend.UserId"));
        Assert.AreEqual("test-refresh-participant-a", PlayerPrefs.GetString("Backend.RefreshToken"));
    }

    [Test]
    public void FailedCodeThenRetry_ShouldStillIdentifyParticipantChange()
    {
        Select(CodeA, out _);
        VerifyIdentity("participant-a");
        Select(CodeB, out _); // Simulates a failed validation, so MarkVerified is not called.
        Assert.IsTrue(Select(CodeB, out bool changed));
        Assert.IsTrue(changed, "Successful retry must reset the previous participant's learning progress.");
        VerifyIdentity("participant-b");
        Select(CodeB, out changed);
        Assert.IsFalse(changed);
    }

    [Test]
    public void PendingOfflineExit_ShouldBlockIdentitySwitchWithoutClearingCredentials()
    {
        Select(CodeA, out _);
        VerifyIdentity("participant-a");
        PlayerPrefs.SetString("Backend.PendingSessionEnd.v1", "pending-test");
        Assert.IsFalse(Select(CodeB, out _));
        Assert.AreEqual("participant-a", PlayerPrefs.GetString("Backend.UserId"));
        Assert.AreEqual("pending-test", PlayerPrefs.GetString("Backend.PendingSessionEnd.v1"));
        Assert.IsTrue(Select(CodeA, out _), "Same participant must be allowed to reconnect and drain the queue.");
    }

    [Test]
    public void CodeHash_ShouldNormalizeWhitespaceCaseAndServerSlash()
    {
        Assert.AreEqual(BackendTestReflection.InvokeStatic(_profiles, "CodeHash", Server, CodeA),
            BackendTestReflection.InvokeStatic(_profiles, "CodeHash", Server + "/", "  qa-profile-a  "));
    }

    [TearDown]
    public void TearDown() => SetUp();
}
