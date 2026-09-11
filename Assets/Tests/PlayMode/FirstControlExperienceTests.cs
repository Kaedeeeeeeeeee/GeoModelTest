using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class FirstControlExperienceTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string GuideKey = "FirstControlGuide.Completed.v2";
    private readonly List<GameObject> _objects = new List<GameObject>();
    private UnityEngine.Object _settings;
    private string _settingsJson;
    private int _guideValue;
    private bool _hadGuide;

    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(string type, string method, object target = null, params object[] args) =>
        T(type).GetMethod(method, Flags).Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private GameObject New(string name)
    {
        var go = new GameObject(name);
        _objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        _settings = Resources.Load("ResearchExperienceSettings");
        Assert.NotNull(_settings);
        _settingsJson = JsonUtility.ToJson(_settings);
        _hadGuide = PlayerPrefs.HasKey(GuideKey);
        _guideValue = PlayerPrefs.GetInt(GuideKey, 0);
        PlayerPrefs.DeleteKey(GuideKey);
        Call("Core.GameInputState", "ReleaseAll");
    }

    [UnityTest]
    public IEnumerator GuideButton_ShouldPauseThenRestoreAndRememberCompletion()
    {
        var player = (Behaviour)New("GuideTestPlayer").AddComponent(T("FirstPersonController"));
        Time.timeScale = 0.75f;
        Call("UISystem.FirstControlGuide", "Show", null, false);
        Assert.IsFalse(player.enabled);
        Assert.AreEqual(0f, Time.timeScale);
        yield return null;
        var canvas = GameObject.Find("FirstControlGuide");
        Assert.NotNull(canvas);
        var cards = canvas.transform.Find("GuideCard");
        foreach (string name in new[] { "move", "look", "interact", "tools" })
            Assert.IsFalse(string.IsNullOrWhiteSpace(cards.Find(name + "/Instruction").GetComponent<Text>().text));
        cards.Find("Begin").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, PlayerPrefs.GetInt(GuideKey));
        Assert.IsTrue(player.enabled);
        Assert.AreEqual(0.75f, Time.timeScale);
        Assert.IsFalse((bool)Call("UISystem.FirstControlGuide", "TryShowForFirstControl"));
        player.enabled = false;
    }

    [UnityTest]
    public IEnumerator TouchGuide_ShouldUseTouchInstructionsAndSurviveNestedModal()
    {
        Time.timeScale = 1f;
        Call("UISystem.FirstControlGuide", "Show", null, true);
        var canvas = GameObject.Find("FirstControlGuide");
        var move = canvas.transform.Find("GuideCard/move/Instruction").GetComponent<Text>().text;
        var expected = Call("UISystem.GameUI", "L", null, "ui.guide.touch.move");
        Assert.AreEqual(expected, move);
        var nested = (IDisposable)Call("Core.GameInputState", "Acquire", null, new object[] { null });
        Call("UISystem.FirstControlGuide", "CloseCurrent");
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(PlayerPrefs.HasKey(GuideKey), "An interrupted guide must be offered again.");
        nested.Dispose();
        yield return null;
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator Player_ShouldAcceptJoystickCreatedAfterPlayerAndGuide()
    {
        PlayerPrefs.SetInt(GuideKey, 1);
        Time.timeScale = 1f;
        var host = New("LateMobilePlayer");
        var controller = host.AddComponent<CharacterController>();
        var camera = New("LateMobileCamera").AddComponent<Camera>();
        camera.transform.SetParent(host.transform);
        var player = (Behaviour)host.AddComponent(T("FirstPersonController"));
        yield return null;
        Set(player, "mobileInputManager", null);
        var input = (Behaviour)New("LateMobileInput").AddComponent(T("MobileInputManager"));
        Set(input, "currentInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        yield return null;

        Call("UISystem.FirstControlGuide", "Show", null, true);
        var guide = GameObject.Find("FirstControlGuide");
        guide.transform.Find("GuideCard/Begin").GetComponent<Button>().onClick.Invoke();
        yield return null;
        yield return null;
        var before = host.transform.position;
        Call("MobileInputManager", "SetMoveInput", input, Vector2.up);
        for (int i = 0; i < 5; i++) yield return null;
        Assert.Greater(host.transform.position.z - before.z, 0.01f,
            "The first movement after the guide must use a manager created after the player.");
        Assert.IsTrue(controller.enabled);
        player.enabled = false;
    }

    [UnityTest]
    public IEnumerator PublishedWarehouseGate_ShouldHideLegacyUIAndPreserveScenery()
    {
        Assert.IsFalse((bool)T("Core.ResearchExperienceSettings").GetProperty("WarehouseInteractionEnabled").GetValue(null));
        Time.timeScale = 0.5f;
        var scenery = New("WarehouseScenery");
        var trigger = (Behaviour)scenery.AddComponent(T("WarehouseTrigger"));
        var prompt = New("WarehousePromptTest");
        Set(trigger, "interactionPrompt", prompt);
        Call("WarehouseTrigger", "Start", trigger);
        Assert.IsFalse(trigger.enabled);
        Assert.IsTrue(scenery.activeSelf);
        Assert.IsFalse(prompt.activeSelf);

        var ui = New("BlockedWarehouseUI").AddComponent(T("WarehouseUI"));
        var panel = New("LegacyWarehousePanel");
        Set(ui, "warehousePanel", panel);
        Call("WarehouseUI", "Start", ui);
        Call("WarehouseUI", "OpenWarehouseInterface", ui);
        Call("WarehouseTrigger", "OpenWarehouse", trigger);
        Assert.IsFalse((bool)Call("WarehouseUI", "IsWarehouseOpen", ui));
        Assert.IsFalse(panel.activeSelf);
        Assert.AreEqual(0.5f, Time.timeScale, "Disabled warehouse initialization must not change pause state.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator WarehouseCloseButton_ShouldRestoreControlsIfFeatureIsReenabled()
    {
        yield return CloseWarehouse(false);
    }

    [UnityTest]
    public IEnumerator WarehouseEscapeCallback_ShouldRestoreControlsIfFeatureIsReenabled()
    {
        yield return CloseWarehouse(true);
    }

    private IEnumerator CloseWarehouse(bool escape)
    {
        Set(_settings, "_warehouseInteractionEnabled", true);
        yield return null;
        yield return null;
        var host = New("WarehouseCloseTest");
        var ui = (Behaviour)host.AddComponent(T("WarehouseUI"));
        ui.enabled = false;
        var canvas = New("WarehouseTestCanvas").AddComponent<Canvas>();
        Set(ui, "warehouseCanvas", canvas);
        Set(ui, "warehousePanel", canvas.gameObject);
        var button = New("WarehouseCloseButton").AddComponent<Button>();
        Set(ui, "closeButton", button);
        Call("WarehouseUI", "SetupEventListeners", ui);
        Time.timeScale = 0.6f;
        Call("WarehouseUI", "OpenWarehouseInterface", ui);
        Assert.IsTrue((bool)Call("WarehouseUI", "IsWarehouseOpen", ui));
        Assert.AreEqual(0f, Time.timeScale);
        if (escape) Call("WarehouseUI", "HandleEscape", ui);
        else button.onClick.Invoke();
        Assert.IsFalse(canvas.gameObject.activeSelf);
        Assert.IsFalse((bool)Call("WarehouseUI", "IsWarehouseOpen", ui));
        Assert.IsFalse((bool)T("Core.GameInputState").GetProperty("IsModalOpen").GetValue(null));
        Assert.AreEqual(0.6f, Time.timeScale);
    }

    [TearDown]
    public void TearDown()
    {
        Call("UISystem.FirstControlGuide", "CloseCurrent");
        Call("Core.GameInputState", "ReleaseAll");
        foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        _objects.Clear();
        JsonUtility.FromJsonOverwrite(_settingsJson, _settings);
        if (_hadGuide) PlayerPrefs.SetInt(GuideKey, _guideValue);
        else PlayerPrefs.DeleteKey(GuideKey);
        Time.timeScale = 1f;
    }
}
