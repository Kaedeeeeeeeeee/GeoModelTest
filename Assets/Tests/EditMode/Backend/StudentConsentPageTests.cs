using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>The 生徒の同意 answer from the web page reaches the code-free activation unchanged.</summary>
public class StudentConsentPageTests
{
    private const string Student =
        "{\"consentVersion\":\"student-ja-2026-11-11\",\"studentAssented\":true,\"studentAssentedAt\":\"2026-11-11T01:02:03.456Z\"}";

    private static Type Page => BackendTestReflection.GetType("Backend.StudentConsentPage");
    private static Type Client => BackendTestReflection.GetType("Backend.TelemetryClient");

    [Serializable]
    public class Body
    {
        public string entryMode;
        public string participantCode;
        public Consent consent;
    }

    [Serializable]
    public class Consent
    {
        public string consentVersion;
        public bool studentAssented;
        public string studentAssentedAt;
        public string guardianName;
    }

    [SetUp, TearDown]
    public void ResetPage() => BackendTestReflection.InvokeStatic(Page, "ResetForTests");

    private static void SetPage(string json)
    {
        Func<string> source = () => json;
        Page.GetField("PayloadOverride", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, source);
    }

    private static string RequestBody(string participantCode) =>
        (string)BackendTestReflection.InvokeStatic(Client, "BuildParticipationBody", new object[] { participantCode });

    [Test]
    public void OpenPlay_ShouldCarryTheStudentAssentMadeOnThePage()
    {
        SetPage(Student);
        string json = RequestBody(null);
        var body = JsonUtility.FromJson<Body>(json);
        Assert.AreEqual("open_play", body.entryMode);
        Assert.AreEqual("student-ja-2026-11-11", body.consent.consentVersion);
        Assert.IsTrue(body.consent.studentAssented);
        Assert.AreEqual("2026-11-11T01:02:03.456Z", body.consent.studentAssentedAt,
            "The page's confirmation time must reach the server unchanged.");
        StringAssert.DoesNotContain("guardian", json, "No guardian form is sent any more.");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("{broken")]
    [TestCase("{\"consentVersion\":\"student-ja-2026-11-11\",\"studentAssented\":false,\"studentAssentedAt\":\"2026-11-11T01:02:03Z\"}")]
    [TestCase("{\"consentVersion\":\"student-ja-2026-11-11\",\"studentAssented\":true}")]
    public void OpenPlay_ShouldSendNoConsentBeforeTheStudentConfirmed(string page)
    {
        SetPage(page);
        StringAssert.DoesNotContain("studentAssented", RequestBody(null));
    }

    [Test]
    public void InvitationCodes_ShouldNeverCarryThePageAnswer()
    {
        SetPage(Student);
        StringAssert.DoesNotContain("studentAssented", RequestBody("ABCD-1234"));
    }
}
