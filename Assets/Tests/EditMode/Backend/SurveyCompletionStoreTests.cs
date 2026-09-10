using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SurveyCompletionStoreTests
{
    private const string Participant = "11111111-1111-4111-8111-111111111111";
    private const string Run = "22222222-2222-4222-8222-222222222222";
    private const string Session = "33333333-3333-4333-8333-333333333333";
    private Type Store => BackendTestReflection.GetType("Backend.SurveyCompletionStore");

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey("Backend.SurveyCompletion.v1");
        PlayerPrefs.SetString("Backend.ResearchParticipantId", Participant);
    }

    private void Remember(bool complete)
    {
        object snapshot = Activator.CreateInstance(BackendTestReflection.GetType("Backend.ProgressSnapshot"));
        snapshot.GetType().GetField("participantId").SetValue(snapshot, Participant);
        snapshot.GetType().GetField("sessionId").SetValue(snapshot, Session);
        var payload = (Dictionary<string, object>)snapshot.GetType().GetField("payload").GetValue(snapshot);
        payload["runId"] = Run;
        payload["investigationComplete"] = complete;
        BackendTestReflection.InvokeStatic(Store, "Remember", snapshot, "https://example.test/");
    }

    [Test]
    public void Current_Should_IgnoreUnfinishedRuns()
    {
        Remember(false);
        Assert.IsNull(BackendTestReflection.InvokeStatic(Store, "Current", "https://example.test", Run));
    }

    [Test]
    public void Current_Should_KeepExactCompletedSession()
    {
        Remember(true);
        object completion = BackendTestReflection.InvokeStatic(Store, "Current", "https://example.test", Run);
        Assert.AreEqual(Session, BackendTestReflection.GetField(completion, "sessionId"));
    }

    [TestCase("https://other.test", Run, Participant)]
    [TestCase("https://example.test", Session, Participant)]
    [TestCase("https://example.test", Run, Session)]
    public void Current_Should_RejectOtherServerRunOrParticipant(string server, string run, string participant)
    {
        Remember(true);
        PlayerPrefs.SetString("Backend.ResearchParticipantId", participant);
        Assert.IsNull(BackendTestReflection.InvokeStatic(Store, "Current", server, run));
    }

    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n", false)]
    [TestCase("participant-001", false)]
    public void Ticket_Should_AcceptOnlyFullLengthOpaqueCredentials(string value, bool expected)
    {
        var gateway = BackendTestReflection.GetType("Backend.SurveyGateway");
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(gateway, "IsValidTicket", value));
    }
}
