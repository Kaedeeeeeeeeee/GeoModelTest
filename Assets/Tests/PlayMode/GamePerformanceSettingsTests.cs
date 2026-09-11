using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GamePerformanceSettingsTests
{
    private const string ManualKey = "GeoModel.ManualQualityEnabled";
    private const string QualityKey = "GeoModel.QualityLevel";
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private object _settings;
    private bool _hadManual, _hadQuality;
    private int _manual, _quality, _level, _frameRate, _mipmapLimit, _vSync;
    private float _timeScale;

    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Methods).Invoke(target, args);
    private static object Read(object target, string name) => target.GetType().GetProperty(name).GetValue(target);

    [SetUp]
    public void SetUp()
    {
        _hadManual = PlayerPrefs.HasKey(ManualKey);
        _hadQuality = PlayerPrefs.HasKey(QualityKey);
        _manual = PlayerPrefs.GetInt(ManualKey);
        _quality = PlayerPrefs.GetInt(QualityKey);
        _level = QualitySettings.GetQualityLevel();
        _frameRate = Application.targetFrameRate;
        _mipmapLimit = QualitySettings.globalTextureMipmapLimit;
        _vSync = QualitySettings.vSyncCount;
        _timeScale = Time.timeScale;
        _settings = TypeOf("GamePerformanceSettings").GetProperty("Instance").GetValue(null);
    }

    [TearDown]
    public void TearDown()
    {
        if (_hadManual) PlayerPrefs.SetInt(ManualKey, _manual); else PlayerPrefs.DeleteKey(ManualKey);
        if (_hadQuality) PlayerPrefs.SetInt(QualityKey, _quality); else PlayerPrefs.DeleteKey(QualityKey);
        PlayerPrefs.Save();
        Call(_settings, "ApplyQualityPreference", false);
        QualitySettings.SetQualityLevel(_level, true);
        QualitySettings.globalTextureMipmapLimit = _mipmapLimit;
        QualitySettings.vSyncCount = _vSync;
        Application.targetFrameRate = _frameRate;
        Time.timeScale = _timeScale;
    }

    [Test]
    public void AutoQuality_Should_StartAtLow_When_SelectedOnAnyDevice()
    {
        Call(_settings, "SetAutoQuality");
        Assert.AreEqual(Array.IndexOf(QualitySettings.names, "Low"), QualitySettings.GetQualityLevel());
        Assert.AreEqual(30, Application.targetFrameRate);
        Assert.AreEqual(1, QualitySettings.globalTextureMipmapLimit);
        Assert.AreEqual(ShadowQuality.Disable, QualitySettings.shadows);
        Assert.AreEqual(0, QualitySettings.antiAliasing);
    }

    [Test]
    public void ManualQuality_Should_RemainSelected_When_LayoutRefreshesOrAutoHasSlowHistory()
    {
        Call(_settings, "SetAutoQuality");
        var policy = _settings.GetType().GetField("_automaticQuality", Methods).GetValue(_settings);
        for (int i = 0; i < 400; i++) Call(policy, "Sample", 0.1f, true);
        int high = Array.IndexOf(QualitySettings.names, "High");
        Call(_settings, "SetManualQualityLevel", high);
        Call(_settings, "ApplyQualityPreference", false);
        Call(_settings, "Update");
        Assert.AreEqual(high, QualitySettings.GetQualityLevel());
        Assert.AreEqual(1, PlayerPrefs.GetInt(ManualKey));
        Assert.AreEqual(high, PlayerPrefs.GetInt(QualityKey));
    }

    [Test]
    public void AutoQuality_Should_KeepDowngrade_When_LayoutReappliesPreferences()
    {
        Call(_settings, "SetAutoQuality");
        var policy = _settings.GetType().GetField("_automaticQuality", Methods).GetValue(_settings);
        for (int i = 0; i < 400; i++) Call(policy, "Sample", 0.1f, true);
        Call(_settings, "ApplyQualityPreference", false);
        Assert.AreEqual(0, QualitySettings.GetQualityLevel());
        Assert.AreEqual(2, QualitySettings.globalTextureMipmapLimit);
        Assert.IsFalse((bool)Read(_settings, "IsManualQualityEnabled"));
    }

    [Test]
    public void QualityChange_Should_PreservePause_When_LowEndLayoutOptimizationRuns()
    {
        Time.timeScale = 0f;
        Call(_settings, "SetManualQualityLevel", 0);
        TypeOf("AnimationOptimizer").GetMethod("ReduceAnimations").Invoke(null, null);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator MicroscopePreview_Should_ReallocateAndRelease_When_QualityChanges()
    {
        Call(_settings, "SetAutoQuality");
        var host = new GameObject("PerformancePreviewTest");
        host.SetActive(false);
        var microscope = host.AddComponent(TypeOf("WorkbenchSystem.MicroscopeController"));
        microscope.GetType().GetField("previewSize").SetValue(microscope, 2048);
        microscope.GetType().GetField("previewAntiAliasing").SetValue(microscope, 8);
        Call(microscope, "EnsureCameraAndPreview");
        var camera = (Camera)microscope.GetType().GetField("microscopeCamera").GetValue(microscope);
        var lowTexture = camera.targetTexture;
        Assert.AreEqual(512, lowTexture.width);
        Assert.AreEqual(1, lowTexture.antiAliasing);
        Assert.IsFalse(camera.enabled, "Closed previews must not render.");
        Call(microscope, "OnEnable");
        try
        {
            Call(_settings, "SetManualQualityLevel", Array.IndexOf(QualitySettings.names, "High"));
            Assert.AreEqual(1024, camera.targetTexture.width);
            Assert.AreEqual(2, camera.targetTexture.antiAliasing);
            Assert.AreNotSame(lowTexture, camera.targetTexture);
            yield return null;
            Assert.IsTrue(lowTexture == null, "The replaced texture must be destroyed.");
            Call(_settings, "SetAutoQuality");
            Assert.AreEqual(512, camera.targetTexture.width);
            Assert.AreEqual(1, camera.targetTexture.antiAliasing);
        }
        finally
        {
            Call(microscope, "OnDisable");
            Call(microscope, "OnDestroy");
            UnityEngine.Object.Destroy(host);
        }
        yield return null;
        Assert.IsTrue(camera == null, "The owned preview camera must not leak after teardown.");
    }
}
