using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ResearchConsentRecordTests
{
    private const string Guardian =
        "{\"consentVersion\":\"guardian-ja-2026-09-29\",\"guardianAgreed\":true,\"guardianConfirmed\":true," +
        "\"guardianName\":\"山田　花子\",\"respondentId\":\"p1234567\",\"guardianConsentedAt\":\"2026-09-29T01:02:03.456Z\"}";

    private static Type Record => BackendTestReflection.GetType("Backend.ResearchConsentRecord");
    private static Type Client => BackendTestReflection.GetType("Backend.TelemetryClient");

    [Serializable]
    public class Body
    {
        public string entryMode;
        public Consent consent;
    }

    [Serializable]
    public class Consent
    {
        public string consentVersion;
        public bool guardianAgreed;
        public bool guardianConfirmed;
        public string guardianName;
        public string respondentId;
        public string guardianConsentedAt;
        public bool studentAssented;
        public string studentAssentedAt;
    }

    [SetUp, TearDown]
    public void ResetRecord() => BackendTestReflection.InvokeStatic(Record, "ResetForTests");

    private static void SetGuardian(string json)
    {
        Func<string> source = () => json;
        Record.GetField("GuardianPayloadOverride", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, source);
    }

    private static void AssentAsStudent() => BackendTestReflection.InvokeStatic(Record, "RecordStudentAssent");

    private static string RequestBody(string participantCode) =>
        (string)BackendTestReflection.InvokeStatic(Client, "BuildParticipationBody", new object[] { participantCode });

    [Test]
    public void OpenPlay_ShouldCarryGuardianFormAndStudentAssent()
    {
        SetGuardian(Guardian);
        DateTime before = DateTime.UtcNow;
        AssentAsStudent();
        var body = JsonUtility.FromJson<Body>(RequestBody(null));
        Assert.AreEqual("open_play", body.entryMode);
        Assert.AreEqual("guardian-ja-2026-09-29", body.consent.consentVersion);
        Assert.AreEqual("山田　花子", body.consent.guardianName);
        Assert.AreEqual("p1234567", body.consent.respondentId);
        Assert.AreEqual("2026-09-29T01:02:03.456Z", body.consent.guardianConsentedAt,
            "The page's answer time must reach the server unchanged.");
        Assert.IsTrue(body.consent.guardianAgreed && body.consent.guardianConfirmed && body.consent.studentAssented);
        DateTime assented = DateTime.Parse(body.consent.studentAssentedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.That(assented.ToUniversalTime(), Is.InRange(before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1)));
    }

    [Test]
    public void OpenPlay_ShouldCarryTheStudentConfirmationMadeOnThePage()
    {
        // The web page now asks the student too and adds the time to the guardian's answer.
        SetGuardian(Guardian.Replace("}", ",\"studentAssented\":true,\"studentAssentedAt\":\"2026-10-01T02:03:04.567Z\"}"));
        var body = JsonUtility.FromJson<Body>(RequestBody(null));
        Assert.AreEqual("open_play", body.entryMode);
        Assert.IsTrue(body.consent.guardianAgreed && body.consent.studentAssented);
        Assert.AreEqual("2026-10-01T02:03:04.567Z", body.consent.studentAssentedAt,
            "The page's confirmation time must reach the server unchanged.");
        Assert.AreEqual("p1234567", body.consent.respondentId);
    }

    [Test]
    public void OpenPlay_ShouldOmitConsentUntilBothAnswersExist()
    {
        SetGuardian(Guardian);
        StringAssert.DoesNotContain("\"consent\"", RequestBody(null), "The student has not confirmed the title-screen statements.");
        ResetRecord();
        AssentAsStudent();
        StringAssert.DoesNotContain("\"consent\"", RequestBody(null), "No guardian answer from the page.");
        SetGuardian("{not json");
        StringAssert.DoesNotContain("\"consent\"", RequestBody(null), "Malformed page data is ignored.");
        SetGuardian(Guardian.Replace("\"guardianAgreed\":true", "\"guardianAgreed\":false"));
        StringAssert.DoesNotContain("\"consent\"", RequestBody(null), "A declined answer never becomes consent.");
    }

    [Test]
    public void InvitationCodes_ShouldNotSendTheGuardianForm()
    {
        SetGuardian(Guardian);
        AssentAsStudent();
        string json = RequestBody("ABCD-EFGH-JKLM");
        StringAssert.DoesNotContain("\"consent\"", json);
        Assert.AreEqual("invitation", JsonUtility.FromJson<Body>(json).entryMode);
    }
}
