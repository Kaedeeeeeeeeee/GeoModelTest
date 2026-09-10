using System;
using NUnit.Framework;
using UnityEngine;

public class InvestigationProgressTests
{
    private Type _progress;
    private object _scores;
    [SetUp]
    public void SetUp()
    {
        _progress = BackendTestReflection.GetType("StorySystem.InvestigationProgress");
        _scores = BackendTestReflection.GetProperty(BackendTestReflection.GetType("StorySystem.QuizScoreManager"), "Instance");
        BackendTestReflection.InvokeInstance(_scores, "StartNewRun");
        BackendTestReflection.InvokeStatic(_progress, "Reset");
    }

    [TestCase("1002", true, true, false)]
    [TestCase("1000", true, true, false)]
    [TestCase("1001", false, true, false)]
    [TestCase("1001", true, false, false)]
    [TestCase("1001", true, true, true)]
    public void CoreSample_ShouldRequireTowerFieldAndActiveQuest(string tool, bool field, bool active, bool accepted)
    {
        Assert.AreEqual(accepted, BackendTestReflection.InvokeStatic(_progress, "AcceptCoreSample", "sample-1", tool, field, active));
        Assert.AreEqual(accepted, BackendTestReflection.GetProperty(_progress, "HasCore"));
    }

    [Test]
    public void CoreSample_ShouldCountOnlyOneSuccessfulSamplePerRun()
    {
        Assert.AreEqual(true, BackendTestReflection.InvokeStatic(_progress, "AcceptCoreSample", "sample-1", "1001", true, true));
        Assert.AreEqual(false, BackendTestReflection.InvokeStatic(_progress, "AcceptCoreSample", "sample-1", "1001", true, true));
        Assert.AreEqual(false, BackendTestReflection.InvokeStatic(_progress, "AcceptCoreSample", "sample-2", "1001", true, true));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(_progress, "ActivityCount"));
        BackendTestReflection.InvokeInstance(_scores, "StartNewRun");
        Assert.AreEqual(false, BackendTestReflection.GetProperty(_progress, "HasCore"));
    }

    [Test]
    public void Practice_ShouldCountMilestonesOnceAndKeepQuizDenominator()
    {
        BackendTestReflection.InvokeStatic(_progress, "AcceptCoreSample", "sample-1", "1001", true, true);
        for (int i = 0; i < 2; i++)
        {
            BackendTestReflection.InvokeStatic(_progress, "MarkFieldSample");
            BackendTestReflection.InvokeStatic(_progress, "MarkLabAnalysis");
            BackendTestReflection.InvokeStatic(_progress, "MarkComplete");
        }
        Assert.AreEqual(4, BackendTestReflection.GetProperty(_progress, "ActivityCount"));
        Assert.AreEqual(10, BackendTestReflection.InvokeStatic(_progress, "GetStep"));
        var summary = BackendTestReflection.InvokeInstance(_scores, "BuildSummary");
        Assert.AreEqual(11, BackendTestReflection.GetProperty(summary, "ExpectedQuestionCount"));
    }

    [Test]
    public void Checkpoint_ShouldRetainAcknowledgedLinesAndResetWithRun()
    {
        var checkpoints = BackendTestReflection.GetType("StorySystem.StoryCheckpoint");
        BackendTestReflection.InvokeStatic(checkpoints, "Acknowledge", "Story/beat3#1");
        Assert.AreEqual(true, BackendTestReflection.InvokeStatic(checkpoints, "IsPassed", "Story/beat3#1"));
        Assert.AreEqual(false, BackendTestReflection.InvokeStatic(checkpoints, "IsPassed", "Story/beat3#2"));
        BackendTestReflection.InvokeInstance(_scores, "StartNewRun");
        Assert.AreEqual(false, BackendTestReflection.InvokeStatic(checkpoints, "IsPassed", "Story/beat3#1"));
    }

    [Test]
    public void LegacyCompletion_ShouldNotInventPracticeEvidence()
    {
        BackendTestReflection.InvokeStatic(_progress, "MigrateLegacyCompletion", true);
        Assert.AreEqual(true, BackendTestReflection.GetProperty(_progress, "IsComplete"));
        Assert.AreEqual(false, BackendTestReflection.GetProperty(_progress, "HasCore"));
        Assert.AreEqual(1, BackendTestReflection.GetProperty(_progress, "ActivityCount"));
    }
}
