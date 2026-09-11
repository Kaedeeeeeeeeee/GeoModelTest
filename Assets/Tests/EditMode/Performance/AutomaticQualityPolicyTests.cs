using System;
using System.Reflection;
using NUnit.Framework;

public class AutomaticQualityPolicyTests
{
    private object _policy;
    private Func<float, bool, int> _sample;
    private PropertyInfo _level;

    [SetUp]
    public void SetUp()
    {
        var type = Type.GetType("AutomaticQualityPolicy, Assembly-CSharp", true);
        _policy = Activator.CreateInstance(type, 1);
        _sample = (Func<float, bool, int>)type.GetMethod("Sample").CreateDelegate(typeof(Func<float, bool, int>), _policy);
        _level = type.GetProperty("CurrentLevel");
    }

    private int Level => (int)_level.GetValue(_policy);

    private void Run(float fps, float seconds, bool eligible = true)
    {
        for (int i = 0; i < (int)Math.Ceiling(fps * seconds); i++) _sample(1f / fps, eligible);
    }

    [Test]
    public void Sample_Should_KeepLow_When_GameplayIsFastForSeveralMinutes()
    {
        Run(60f, 180f);
        Assert.AreEqual(1, Level, "Automatic mode must never raise quality on its own.");
    }

    [Test]
    public void Sample_Should_IgnoreSlowFrames_When_SceneIsWarmingUp()
    {
        Run(10f, 7f);
        Assert.AreEqual(1, Level);
    }

    [Test]
    public void Sample_Should_LowerQuality_When_GameplayStaysBelowTarget()
    {
        Run(20f, 18f);
        Assert.AreEqual(0, Level);
        Run(60f, 180f);
        Assert.AreEqual(0, Level, "Recovery must not trigger an automatic upgrade.");
    }

    [Test]
    public void Sample_Should_LowerQualityEarlier_When_FrameRateIsSeverelyLow()
    {
        Run(15f, 13f);
        Assert.AreEqual(0, Level);
    }

    [Test]
    public void Sample_Should_KeepQuality_When_OnlyOneLoadStallOccurs()
    {
        Run(30f, 12f);
        _sample(4f, true);
        Run(30f, 20f);
        Assert.AreEqual(1, Level);
    }

    [Test]
    public void Sample_Should_IgnoreFrames_When_GameplayIsPausedOrHidden()
    {
        Run(5f, 60f, false);
        Run(10f, 7f);
        Assert.AreEqual(1, Level, "Resuming must also get a warmup period.");
    }

    [Test]
    public void Sample_Should_ResetSlowStreak_When_PerformanceRecovers()
    {
        Run(30f, 9f);
        Run(20f, 4.2f);
        Run(30f, 9f);
        Run(20f, 4.2f);
        Assert.AreEqual(1, Level);
    }

    [Test]
    public void Sample_Should_NeverGoBelowMinimum_When_DeviceRemainsSlow()
    {
        Run(10f, 120f);
        Assert.AreEqual(0, Level);
    }

    [Test]
    public void Sample_Should_IgnoreInvalidTimes_When_TimingIsUnavailable()
    {
        foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) _sample(value, true);
        Run(20f, 18f);
        Assert.AreEqual(0, Level, "Invalid values must not poison subsequent measurements.");
    }
}
