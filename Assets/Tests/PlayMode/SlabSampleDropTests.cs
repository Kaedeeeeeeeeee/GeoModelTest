using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SlabSampleDropTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly SamplingTestScene _cleanup = new SamplingTestScene();
    private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
    private SimulationMode _simulation;
    private Component _generator;
    private Material _material;
    private GameObject _floor;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Flags).Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _cleanup.Capture();
        _owned.Clear();
        _simulation = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        _material = new Material(Shader.Find("Sprites/Default"));
        _owned.Add(_material);
        var generator = new GameObject("SlabDropTestGenerator");
        _generator = generator.AddComponent(T("SlabSampleGenerator"));
        ((Behaviour)_generator).enabled = false;
        _floor = new GameObject("SlabDropTestGround");
        var mesh = new Mesh
        {
            vertices = new[] { new Vector3(-10, 0, -10), new Vector3(10, 0, -10), new Vector3(10, 0, 10), new Vector3(-10, 0, 10) },
            triangles = new[] { 0, 2, 1, 0, 3, 2 }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _owned.Add(mesh);
        _floor.AddComponent<MeshCollider>().sharedMesh = mesh;
        Physics.SyncTransforms();
        yield return null;
    }

    private Component CreateDrop()
    {
        var sample = (GameObject)Call(_generator, "GenerateSlabSampleWithMaterial", new Vector3(0, 2, 0), _material, null, false);
        _owned.Add(sample.GetComponent<MeshFilter>().sharedMesh);
        var drop = sample.AddComponent(T("SampleDropController"));
        ((Behaviour)drop).enabled = false;
        Set(drop, "enableDustEffect", false);
        Set(drop, "soundVolume", 0f);
        Call(drop, "InitializeDropController");
        Physics.SyncTransforms();
        return drop;
    }

    [UnityTest]
    public IEnumerator ThinSlab_ShouldKeepPrimitiveColliderAndContinuousDetection()
    {
        var drop = CreateDrop();
        Assert.IsNotNull(drop.GetComponent<BoxCollider>(), "The thin slab needs a collider supported by sweep-based CCD.");
        var body = drop.GetComponent<Rigidbody>();
        Assert.AreEqual(CollisionDetectionMode.ContinuousDynamic, body.collisionDetectionMode);
        Assert.AreEqual(RigidbodyConstraints.FreezeRotation, body.constraints & RigidbodyConstraints.FreezeRotation);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FastFall_ShouldNotTunnelThroughThinGround()
    {
        var drop = CreateDrop();
        var body = drop.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.down * 60f;
        for (int i = 0; i < 8; i++)
        {
            Call(drop, "FixedUpdate");
            Physics.Simulate(0.1f);
            Assert.GreaterOrEqual(body.position.y, -0.03f, "A large physics step must not lose the sample below the surface.");
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator MissedGroundCollision_ShouldRecoverSameCollectibleAboveTheSurface()
    {
        var drop = CreateDrop();
        var body = drop.GetComponent<Rigidbody>();
        int identity = drop.gameObject.GetInstanceID();
        body.position = new Vector3(0, -1, 0);
        drop.transform.position = body.position;
        body.linearVelocity = Vector3.down * 20f;
        Physics.SyncTransforms();
        Call(drop, "FixedUpdate");
        Assert.Greater(body.position.y, 0f, "A missed collision must recover using the known surface above it.");
        Assert.AreEqual(identity, drop.gameObject.GetInstanceID(), "Recovery must preserve the existing sample and its data.");
        Assert.IsTrue((bool)Call(drop, "HasLanded"));
        Assert.IsNotNull(drop.GetComponent(T("SampleCollector")));
        yield return null;
    }

    [UnityTest]
    public IEnumerator GroundProbe_ShouldIgnoreInteractionTriggers()
    {
        var trigger = new GameObject("SlabDropTestInteractionTrigger");
        trigger.transform.position = Vector3.up;
        var shape = trigger.AddComponent<BoxCollider>();
        shape.size = new Vector3(2, 0.1f, 2);
        shape.isTrigger = true;
        Physics.SyncTransforms();
        var drop = CreateDrop();
        Assert.AreEqual(2.5f, drop.transform.position.y, 0.01f, "An interaction trigger cannot become the landing surface.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SlopedGround_ShouldRecognizeOffCenterSupportAndFinishLanding()
    {
        _floor.transform.rotation = Quaternion.Euler(0, 0, 45f);
        Physics.SyncTransforms();
        var drop = CreateDrop();
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!(bool)Call(drop, "HasLanded") && Time.realtimeSinceStartup < deadline)
        {
            Call(drop, "FixedUpdate");
            Physics.Simulate(0.02f);
            Call(drop, "Update");
            yield return null;
        }
        Assert.IsTrue((bool)Call(drop, "HasLanded"),
            "A slab resting on its edge on a real slope must finish landing even when its centre ray misses support.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return _cleanup.Restore();
        foreach (var item in _owned)
            if (item != null) UnityEngine.Object.Destroy(item);
        Physics.simulationMode = _simulation;
        yield return null;
    }
}
