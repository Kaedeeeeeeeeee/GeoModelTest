using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class VehicleToolTests
{
    private readonly List<GameObject> _objects = new List<GameObject>();
    private const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Methods).Invoke(target, args);
    private static object Read(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private GameObject New(string name) { var go = new GameObject(name); _objects.Add(go); return go; }

    [TestCase("Drone")]
    [TestCase("DrillCar")]
    public void PackagedVehicle_ShouldHaveRealMeshAndController(string name)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Vehicles/" + name);
        Assert.NotNull(prefab, "Vehicle must be loadable in a built player, without AssetDatabase.");
        Assert.NotNull(prefab.GetComponent(TypeOf(name + "Controller")));
        Assert.NotNull(prefab.GetComponent<MeshFilter>().sharedMesh);
        Assert.NotNull(prefab.GetComponent<Renderer>().sharedMaterial);
        Assert.NotNull(prefab.GetComponent<Collider>());
    }

    [UnityTest]
    public IEnumerator Drone_ShouldMoveWithMobileInputAndRestorePlayerAfterRecall() => ControlAndRecall("Drone");

    [UnityTest]
    public IEnumerator DrillCar_ShouldMoveWithMobileInputAndRestorePlayerAfterRecall() => ControlAndRecall("DrillCar");

    [TestCase("Drone", "1100", "tool.drone.name")]
    [TestCase("DrillCar", "1101", "tool.drill_car.name")]
    public void ToolWheel_ShouldResolveVehicleNamesById(string name, string id, string expectedKey)
    {
        var tool = New("VehicleNameTest").AddComponent(TypeOf(name + "Tool"));
        tool.GetType().GetField("toolID").SetValue(tool, id);
        tool.GetType().GetField("toolName").SetValue(tool, "任意の名前");
        var inventoryObject = New("VehicleNameInventory");
        inventoryObject.SetActive(false);
        var inventory = inventoryObject.AddComponent(TypeOf("InventoryUISystem"));
        Assert.AreEqual(expectedKey, Call(inventory, "GetToolNameKey", tool));
    }

    private IEnumerator ControlAndRecall(string name)
    {
        var player = New("VehicleTestPlayer");
        var operatorPosition = player.transform.position;
        var camera = New("VehicleTestCamera").AddComponent<Camera>();
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var person = (Behaviour)player.AddComponent(TypeOf("FirstPersonController"));
        person.enabled = false;
        var tool = player.AddComponent(TypeOf(name + "Tool"));
        var prefab = Resources.Load<GameObject>("Prefabs/Vehicles/" + name);
        var vehicle = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 1f, 2.5f), Quaternion.identity);
        _objects.Add(vehicle);
        var controller = vehicle.GetComponent(TypeOf(name + "Controller"));
        Call(controller, "Configure", tool);
        tool.GetType().GetField("hasPlacedObject").SetValue(tool, true);
        yield return null;

        var state = TypeOf("Core.GameInputState");
        using ((IDisposable)state.GetMethod("Acquire").Invoke(null, new object[] { null }))
            Assert.IsFalse((bool)Call(controller, "BeginControl"), "A modal must block vehicle entry.");
        yield return null;
        yield return null;
        Assert.IsTrue((bool)Call(controller, "BeginControl"), "Nearby vehicle entry must succeed after the modal closes.");
        Assert.IsTrue(player.activeInHierarchy, "Player services must remain active during vehicle control.");
        Assert.IsFalse(player.GetComponent<CharacterController>().enabled);
        Assert.IsNull(camera.transform.parent);
        var body = vehicle.GetComponent<Rigidbody>();
        Assert.IsFalse(body.isKinematic);

        var mobile = (Behaviour)TypeOf("MobileInputManager").GetProperty("Instance").GetValue(null);
        if (mobile == null) mobile = (Behaviour)New("VehicleTestMobileInput").AddComponent(TypeOf("MobileInputManager"));
        bool wasEnabled = mobile.enabled;
        mobile.enabled = false;
        try
        {
            Call(mobile, "SetMoveInput", new Vector2(0f, 1f));
            if (name == "Drone") Call(mobile, "SetAscendInput", true);
            var before = vehicle.transform.position;
            Call(controller, "FixedUpdate");
            Assert.Greater(body.linearVelocity.z, 0.1f);
            if (name == "Drone") Assert.Greater(body.linearVelocity.y, 0.1f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(vehicle.transform.position.z, before.z);
        }
        finally
        {
            Call(mobile, "SetMoveInput", Vector2.zero);
            Call(mobile, "SetAscendInput", false);
            mobile.enabled = wasEnabled;
        }

        if (name == "DrillCar")
        {
            var layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(layer);
            layer.name = "VehicleTestStratum";
            layer.transform.position = new Vector3(0f, -1f, 0f);
            layer.transform.localScale = new Vector3(100f, 2f, 100f);
            var geology = layer.AddComponent(TypeOf("GeologyLayer"));
            geology.GetType().GetField("layerMaterial").SetValue(geology, layer.GetComponent<Renderer>().sharedMaterial);
            Physics.SyncTransforms();
            Call(controller, "StartDrilling");
            Assert.IsTrue((bool)Read(controller, "IsDrilling"));
            yield return new WaitForSeconds(2.2f);
            Assert.IsFalse((bool)Read(controller, "IsDrilling"));
            int samples = 0;
            foreach (var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (item.name.StartsWith("GeometricSample_") && item.parent == null)
                {
                    samples++;
                    _objects.Add(item.gameObject);
                }
            Assert.Greater(samples, 0, "Drilling must create an actual geological sample.");
            var reconstructor = GameObject.Find("GeometricSampleReconstructor");
            if (reconstructor != null) _objects.Add(reconstructor);
        }

        Call(controller, "Recall");
        Assert.IsFalse((bool)Read(controller, "IsControlled"));
        Assert.AreSame(player.transform, camera.transform.parent);
        Assert.AreEqual(new Vector3(0f, 1.5f, 0f), camera.transform.localPosition);
        Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
        Assert.IsFalse((bool)tool.GetType().GetField("hasPlacedObject").GetValue(tool), "Recall must allow placement again.");
        Assert.IsFalse(person.enabled, "Restore the actual pre-control state.");
        if (name == "Drone") Assert.AreEqual(operatorPosition, player.transform.position,
            "The drone operator must stay at the launch point when remote control ends.");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var active = TypeOf("VehicleController").GetProperty("Active").GetValue(null);
        if (active != null) Call(active, "EndControl");
        foreach (var go in _objects) if (go != null) UnityEngine.Object.Destroy(go);
        _objects.Clear();
        Time.timeScale = 1f;
        yield return null;
    }
}
