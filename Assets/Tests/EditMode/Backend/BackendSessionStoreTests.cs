using System;
using NUnit.Framework;
using UnityEngine;

public class BackendSessionStoreTests
{
    [SetUp]
    public void SetUp()
    {
        var storeType = BackendTestReflection.GetType("Backend.BackendSessionStore");
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(storeType, "InstallIdKey"));
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(storeType, "AccessTokenKey"));
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(storeType, "RefreshTokenKey"));
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(storeType, "UserIdKey"));
        PlayerPrefs.DeleteKey((string)BackendTestReflection.GetField(storeType, "AccessTokenExpiresAtKey"));
        PlayerPrefs.Save();
    }

    [Test]
    public void GetOrCreateInstallId_ShouldGenerateStableUuid_WhenMissing()
    {
        var storeType = BackendTestReflection.GetType("Backend.BackendSessionStore");
        string first = (string)BackendTestReflection.InvokeStatic(storeType, "GetOrCreateInstallId");
        string second = (string)BackendTestReflection.InvokeStatic(storeType, "GetOrCreateInstallId");

        Assert.AreEqual(first, second);
        Assert.IsTrue(Guid.TryParse(first, out _));
    }
}
