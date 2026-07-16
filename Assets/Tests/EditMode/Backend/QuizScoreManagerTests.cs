using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class QuizScoreManagerTests
{
    private object _manager;

    [SetUp]
    public void SetUp()
    {
        var managerType = BackendTestReflection.GetType("StorySystem.QuizScoreManager");
        string prefsKey = (string)BackendTestReflection.GetField(managerType, "PersistedStatePrefsKey");
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.Save();
        _manager = BackendTestReflection.GetProperty(managerType, "Instance");
        BackendTestReflection.InvokeInstance(_manager, "ReloadFromPersistence");
        BackendTestReflection.InvokeInstance(_manager, "StartNewRun");
    }

    [Test]
    public void FirstWrongThenCorrect_ShouldPreserveFirstAnswerAndShowFinalMastery()
    {
        Record("wrong", false, false, 800);
        Record("correct", true, false, 1600);

        object summary = BackendTestReflection.InvokeInstance(_manager, "BuildSummary");

        Assert.AreEqual(0, BackendTestReflection.GetProperty(summary, "FirstCorrectCount"));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(summary, "FinalMasteredCount"));
        Assert.AreEqual(2f, (float)BackendTestReflection.GetProperty(summary, "AverageAttemptCount"), 0.001f);
        Assert.AreEqual(2, Attempts.Count);
    }

    [Test]
    public void AnswerAfterHint_ShouldPersistUsedHint()
    {
        Record("correct", true, true, 2100);

        object summary = BackendTestReflection.InvokeInstance(_manager, "BuildSummary");
        object attempt = Attempts[0];

        Assert.IsTrue((bool)BackendTestReflection.GetField(attempt, "usedHint"));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(summary, "HintUsedQuestionCount"));
        Assert.AreEqual(1, Attempts.Count, "Opening a hint is not itself an answer attempt.");
    }

    [Test]
    public void ReloadFromPersistence_ShouldRestoreAttemptHistory()
    {
        Record("wrong", false, false, 500);
        string eventId = (string)BackendTestReflection.GetField(Attempts[0], "eventId");

        BackendTestReflection.InvokeInstance(_manager, "ReloadFromPersistence");

        Assert.AreEqual(1, Attempts.Count);
        Assert.AreEqual(eventId, BackendTestReflection.GetField(Attempts[0], "eventId"));
    }

    [Test]
    public void PartialCompletion_ShouldUseElevenQuestionDenominator()
    {
        Record("correct", true, false, 700);
        object summary = BackendTestReflection.InvokeInstance(_manager, "BuildSummary");

        Assert.AreEqual(11, BackendTestReflection.GetProperty(summary, "ExpectedQuestionCount"));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(summary, "AnsweredQuestionCount"));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(summary, "FirstCorrectCount"));
        Assert.AreEqual(1f / 11f, (float)BackendTestReflection.GetProperty(summary, "CompletionRate"), 0.0001f);
        Assert.AreEqual(1f / 11f, (float)BackendTestReflection.GetProperty(summary, "FirstCorrectRate"), 0.0001f);
    }

    [Test]
    public void StartNewRun_ShouldNotCarryOldAttemptsIntoReport()
    {
        Record("correct", true, false, 700);
        string oldRunId = (string)BackendTestReflection.GetProperty(_manager, "RunId");

        BackendTestReflection.InvokeInstance(_manager, "StartNewRun");

        string newRunId = (string)BackendTestReflection.GetProperty(_manager, "RunId");
        object summary = BackendTestReflection.InvokeInstance(_manager, "BuildSummary");
        Assert.AreNotEqual(oldRunId, newRunId);
        Assert.AreEqual(0, Attempts.Count);
        Assert.AreEqual(0, BackendTestReflection.GetProperty(summary, "FirstCorrectCount"));
    }

    private IList Attempts => (IList)BackendTestReflection.GetProperty(_manager, "Attempts");

    private void Record(string choiceId, bool isCorrect, bool usedHint, long responseTimeMs)
    {
        BackendTestReflection.InvokeInstance(
            _manager,
            "Record",
            "q.weathering_order",
            "story-formative-v1",
            choiceId,
            isCorrect,
            usedHint,
            responseTimeMs,
            null);
    }
}
