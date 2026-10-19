using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

// Exercise the same primary-use path used by the mobile Inspect button.
public sealed class HammerSamplingRegressionTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private Touchscreen _touch;
    private Mouse _mouse;
    private GameObject _host;
    private GameObject _cameraHost;
    private GameObject _rock;
    private Component _hammer;
    private InputSettings.UpdateMode _previousUpdate;
    private InputSettings.BackgroundBehavior _previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode _previousEditorBehavior;
#endif
    private int _previousSelectionFrame;
    private bool _previousWaiting;
    private float _previousTimeScale;
    private readonly SamplingTestScene _cleanup = new SamplingTestScene();
    private readonly Dictionary<BaseInputModule, bool> _previousModules = new Dictionary<BaseInputModule, bool>();
    private static Type Tool => Type.GetType("CollectionTool, Assembly-CSharp", true);
    private static Type Hammer => Type.GetType("HammerTool, Assembly-CSharp", true);
    private static bool Suppressed => (bool)Tool.GetProperty("IsSelectionInputSuppressed", Flags).GetValue(null);
    private static void SetField(object obj, string field, object value)
    {
        for (Type type = obj.GetType(); type != null; type = type.BaseType)
        {
            var f = type.GetField(field, Flags | BindingFlags.DeclaredOnly);
            if (f == null) continue;
            f.SetValue(obj, value);
            return;
        }
        throw new MissingFieldException(field);
    }
    private bool Request() => (bool)Hammer.GetMethod("RequestPrimaryUse", Flags).Invoke(_hammer, null);
    private int Hits => (int)Hammer.GetProperty("CurrentHitCount", Flags).GetValue(_hammer);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _cleanup.Capture();
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        _previousModules.Clear();
        foreach (var module in UnityEngine.Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            _previousModules[module] = module.enabled;
            module.enabled = false;
        }
        _previousUpdate = InputSystem.settings.updateMode;
        _previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        _previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        _touch = InputSystem.AddDevice<Touchscreen>();
        _mouse = InputSystem.AddDevice<Mouse>();
        _touch.MakeCurrent();
        _mouse.MakeCurrent();
        InputSystem.EnableDevice(_touch);
        InputSystem.EnableDevice(_mouse);
        InputSystem.QueueStateEvent(_mouse, new MouseState());
        InputSystem.Update();
        _previousSelectionFrame = (int)Tool.GetField("selectionFrame", Flags).GetValue(null);
        _previousWaiting = (bool)Tool.GetField("waitingForSelectionRelease", Flags).GetValue(null);
        Tool.GetField("selectionFrame", Flags).SetValue(null, -10);
        Tool.GetField("waitingForSelectionRelease", Flags).SetValue(null, false);
        _host = new GameObject("HammerSamplingTestTool");
        _host.transform.position = new Vector3(0, 2, 0);
        _hammer = _host.AddComponent(Hammer);
        ((Behaviour)_hammer).enabled = false;
        SetField(_hammer, "isEquipped", true);
        SetField(_hammer, "canUse", true);
        _cameraHost = new GameObject("HammerSamplingTestCamera");
        _cameraHost.transform.position = _host.transform.position;
        var camera = _cameraHost.AddComponent<Camera>();
        _cameraHost.AddComponent<AudioListener>();
        SetField(_hammer, "playerCamera", camera);
        _rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _rock.name = "HammerSamplingTestRockA";
        _rock.transform.position = new Vector3(0, 2, 1.5f);
        var layer = (Behaviour)_rock.AddComponent(Type.GetType("GeologyLayer, Assembly-CSharp", true));
        layer.enabled = false;
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        Assert.IsFalse(Suppressed);
    }

    private object Collection => Hammer.GetField("currentCollection", Flags).GetValue(_hammer);
    private Vector3 Target => (Vector3)Collection.GetType().GetField("targetPosition", Flags).GetValue(Collection);
    private void CreateSecondRock()
    {
        var other = GameObject.CreatePrimitive(PrimitiveType.Cube);
        other.name = "HammerSamplingTestRockB";
        other.transform.SetParent(_host.transform, true);
        other.transform.position = new Vector3(3, 2, 1.5f);
        Physics.SyncTransforms();
    }
    private void AimAt(float x)
    {
        _cameraHost.transform.position = new Vector3(x, 2, 0);
        Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator ChangingTarget_ShouldRestartAtOneHit_WithoutReequipping()
    {
        CreateSecondRock();
        Assert.IsTrue(Request());
        Assert.AreEqual(1, Hits);
        AimAt(3);
        Assert.IsTrue(Request());
        Assert.AreEqual(1, Hits, "A new point starts from its own first hit.");
        Assert.Greater(Target.x, 2f, "The old target must not keep owning the new hit.");
        Assert.IsTrue(Request());
        Assert.AreEqual(2, Hits, "The same new point accumulates hits normally.");
        AimAt(0);
        Assert.IsTrue(Request());
        Assert.AreEqual(1, Hits, "Returning after changing targets starts a new collection.");
        Assert.Less(Target.x, 1f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExpiredCollection_ShouldRestartThroughMobilePrimaryUse()
    {
        Assert.IsTrue(Request());
        SetField(Collection, "lastHitTime", Time.time - 10f);
        Assert.IsTrue(Request());
        Assert.AreEqual(1, Hits, "The first press after timeout must not count as the old collection's second hit.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ThreeHits_ShouldGenerateExactlyOneSample_AndKeepNewTargetAfterOldCleanup()
    {
        CreateSecondRock();
        Assert.IsTrue(Request());
        Assert.IsTrue(Request());
        Assert.IsTrue(Request());
        Assert.AreEqual(3, Hits);
        var firstSample = (GameObject)Hammer.GetProperty("PendingSample", Flags).GetValue(_hammer);
        Assert.IsNotNull(firstSample);
        foreach (var script in firstSample.GetComponents<MonoBehaviour>()) script.enabled = false;
        SetField(_hammer, "canUse", true);
        Assert.IsTrue(Request());
        Assert.AreEqual(3, Hits, "An extra press cannot complete an already completed collection again.");
        Assert.AreSame(firstSample, Hammer.GetProperty("PendingSample", Flags).GetValue(_hammer));
        AimAt(3);
        Assert.IsTrue(Request());
        Assert.AreEqual(1, Hits);
        Assert.Greater(Target.x, 2f);
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(1.3f);
        Assert.AreEqual(1, Hits, "The old target's delayed cleanup cannot cancel the new collection.");
        UnityEngine.Object.Destroy(firstSample);
    }

    [UnityTest]
    public IEnumerator RockWallSample_ShouldSpawnOutsideTheStruckSurface()
    {
        _rock.transform.localScale = new Vector3(2f, 3f, 2f);
        _rock.transform.position = new Vector3(0, 1.5f, 2f);
        Physics.SyncTransforms();
        Assert.IsTrue(Request());
        var point = Target;
        Hammer.GetMethod("GenerateSlabSampleWithMaterial", Flags).Invoke(_hammer, new object[] { point, null, null, null });
        var sample = (GameObject)Hammer.GetProperty("PendingSample", Flags).GetValue(_hammer);
        Assert.IsNotNull(sample);
        foreach (var script in sample.GetComponents<MonoBehaviour>()) script.enabled = false;
        Physics.SyncTransforms();
        var slab = sample.GetComponent<Collider>();
        Assert.IsFalse(Physics.ComputePenetration(slab, sample.transform.position, sample.transform.rotation,
            _rock.GetComponent<Collider>(), _rock.transform.position, _rock.transform.rotation, out _, out _),
            "A thin rock generated on a cliff must start on the outside of that cliff.");
        UnityEngine.Object.Destroy(sample);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var module in UnityEngine.Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            module.enabled = false;
        if (_hammer != null) Hammer.GetMethod("CancelCollection", Flags).Invoke(_hammer, null);
        if (_host != null) UnityEngine.Object.Destroy(_host);
        if (_cameraHost != null) UnityEngine.Object.Destroy(_cameraHost);
        if (_rock != null) UnityEngine.Object.Destroy(_rock);
        yield return null;
        yield return _cleanup.Restore();
        Time.timeScale = _previousTimeScale;
        if (_touch != null) InputSystem.RemoveDevice(_touch);
        if (_mouse != null) InputSystem.RemoveDevice(_mouse);
        foreach (var item in _previousModules)
            if (item.Key != null) item.Key.enabled = item.Value;
        Tool.GetField("selectionFrame", Flags).SetValue(null, _previousSelectionFrame);
        Tool.GetField("waitingForSelectionRelease", Flags).SetValue(null, _previousWaiting);
        InputSystem.settings.updateMode = _previousUpdate;
        InputSystem.settings.backgroundBehavior = _previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorBehavior;
#endif
    }
}
