using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptTests
{
    private GameObject _localizationFixture;

    [SetUp]
    public void PrepareLocalization()
    {
        var type = BackendTestReflection.GetType("LocalizationManager");
        var field = type.GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if ((UnityEngine.Object)field.GetValue(null) != null) return;
        _localizationFixture = new GameObject("PromptLocalizationFixture");
        var manager = _localizationFixture.AddComponent(type);
        field.SetValue(null, manager);
        BackendTestReflection.InvokeInstance(manager, "InitializeLocalization");
    }

    [TearDown]
    public void CleanupLocalization()
    {
        if (_localizationFixture != null)
        {
            UnityEngine.Object.DestroyImmediate(_localizationFixture);
            BackendTestReflection.GetType("LocalizationManager").GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, null);
        }
    }

    [TestCase("1002", "sample.pickup.rock")]
    [TestCase("1001", "sample.pickup.core")]
    [TestCase("1000", "sample.pickup.generic")]
    [TestCase(null, "sample.pickup.generic")]
    public void Pickup_ShouldUseActionNameInsteadOfInternalId(string tool, string expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("SampleCollector"), "PickupActionKey", new object[] { tool }));
    }

    [TestCase(0, 5, false, false)]
    [TestCase(4, 5, false, false)]
    [TestCase(5, 5, true, false)]
    [TestCase(5, 5, false, true)]
    [TestCase(6, 5, false, true)]
    [TestCase(3, 3, false, true)]
    public void Tower_ShouldAllowPutAwayOnlyAtMaximumWhenIdle(int count, int maximum, bool drilling, bool expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("DrillTower"), "IsReadyToPutAway", count, maximum, drilling));
    }

    [TestCase("ja-JP", "採集地点 2/3")]
    [TestCase("zh-CN", "采集点 2/3")]
    [TestCase("en-US", "Site 2/3")]
    public void HammerTitle_ShouldDistinguishSitesFromHits(string language, string expected)
    {
        var json = JsonUtility.FromJson<TextData>(System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "Resources/Localization/Data", language + ".json")));
        var entry = Array.Find(json.texts, x => x.key == "ui.collection.site_progress");
        Assert.AreEqual(expected, string.Format(entry.value, 2, 3));
        foreach (var key in new[] { "ui.collection.hit_progress.desktop", "ui.collection.hit_progress.touch" })
            StringAssert.DoesNotContain("採集 ", Array.Find(json.texts, x => x.key == key).value);
    }

    [Test]
    public void SharedPrompt_ShouldHaveReadableSizeAndOpaqueSurfaceAtEmbeddedResolution()
    {
        var prompt = BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("UISystem.InteractionPrompt"),
            "Create", "PromptSizeTest", "Panel", 1, TouchControl("Interact"));
        var canvas = (Canvas)BackendTestReflection.GetProperty(prompt, "Canvas");
        try
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.AreEqual(new Vector2(1600f, 900f), scaler.referenceResolution);
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);
            // CanvasScaler uses the geometric mean at match 0.5.
            canvas.scaleFactor = Mathf.Sqrt(960f / scaler.referenceResolution.x * 540f / scaler.referenceResolution.y);
            var action = (Text)BackendTestReflection.GetProperty(prompt, "Action");
            Assert.GreaterOrEqual(action.fontSize * canvas.scaleFactor, 18f);
            Assert.IsFalse(action.resizeTextForBestFit, "The action must never shrink below the readability minimum.");
            var panel = (RectTransform)BackendTestReflection.GetProperty(prompt, "Panel");
            Assert.GreaterOrEqual(panel.GetComponent<Image>().color.a, 0.92f);
            Assert.NotNull(panel.GetComponent<Outline>());
            Assert.AreEqual(160f, panel.sizeDelta.y);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
    }

    [Test]
    public void EditorCheckpoint_ShouldNotUnlockUnusedSimpleDrill()
    {
        var ids = (string[])BackendTestReflection.GetField(
            BackendTestReflection.GetType("StorySystem.EditorTools.StoryTools"), "AllToolIds");
        CollectionAssert.DoesNotContain(ids, "1000");
        CollectionAssert.Contains(ids, "1001");
        CollectionAssert.Contains(ids, "1002");
    }

    private static object TouchControl(string name) => Enum.Parse(
        BackendTestReflection.GetType("UISystem.MobileControlHint+Control"), name);

    [Test]
    public void Toast_ShouldReuseInstanceAndResetEveryStyleWhenSuccessFollowsWarning()
    {
        var type = BackendTestReflection.GetType("UISystem.GameToast");
        BackendTestReflection.InvokeStatic(type, "Show", "success");
        var toast = BackendTestReflection.GetField(type, "_instance");
        var canvas = ((Component)toast).gameObject;
        try
        {
            var icon = (Text)BackendTestReflection.GetField(toast, "_icon");
            var outline = (Outline)BackendTestReflection.GetField(toast, "_outline");
            var message = (Text)BackendTestReflection.GetField(toast, "_message");
            var successColor = icon.color;
            Assert.AreEqual("✓", icon.text);
            BackendTestReflection.InvokeStatic(type, "ShowWarning", "outside site");
            Assert.AreSame(toast, BackendTestReflection.GetField(type, "_instance"));
            Assert.AreEqual("!", icon.text);
            Assert.AreEqual("outside site", message.text);
            Assert.AreEqual(BackendTestReflection.GetField(
                BackendTestReflection.GetType("UISystem.InteractionPrompt"), "Warning"), icon.color);
            Assert.AreEqual(icon.color, outline.effectColor);
            BackendTestReflection.InvokeStatic(type, "Show", "collected");
            Assert.AreSame(toast, BackendTestReflection.GetField(type, "_instance"));
            Assert.AreEqual("✓", icon.text);
            Assert.AreEqual("collected", message.text);
            Assert.AreEqual(successColor, icon.color);
            Assert.AreEqual(successColor, outline.effectColor);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas); }
    }

    [TestCase("1000", false)]
    [TestCase("1100", false)]
    [TestCase("1101", false)]
    [TestCase("0", true)]
    [TestCase("999", true)]
    [TestCase("1002", true)]
    [TestCase("1001", true)]
    public void Wheel_ShouldAlwaysFilterUnusedTools(string id, bool expected)
    {
        Assert.AreEqual(expected, BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("InventoryUISystem"), "IsToolVisibleInWheel", id));
    }

    [Test]
    public void Wheel_ShouldPutTeachingOrderBeforeUnknownToolsAndKeepTheirIdOrder()
    {
        var ids = new[] { "z", "1001", "2000", "999", "a", "1002", "50", "0" };
        Array.Sort(ids, (a, b) => (int)BackendTestReflection.InvokeStatic(
            BackendTestReflection.GetType("InventoryUISystem"), "CompareToolIds", a, b));
        CollectionAssert.AreEqual(new[] { "0", "999", "1002", "1001", "50", "2000", "a", "z" }, ids);
    }

    [Test]
    public void Wheel_ShouldGiveSameOrderAfterLegacyInitializationAndIncrementalAdds()
    {
        var host = new GameObject("LegacyToolTest");
        var ui = new GameObject("WheelOrderTest");
        try
        {
            var manager = host.AddComponent(BackendTestReflection.GetType("ToolManager"));
            var wheel = ui.AddComponent(BackendTestReflection.GetType("InventoryUISystem"));
            var empty = host.AddComponent(BackendTestReflection.GetType("EmptyHandTool"));
            BackendTestReflection.SetField(empty, "toolID", "0");
            var ids = new[] { "1101", "1001", "1000", "1002", "1100", "999" };
            var tools = Array.CreateInstance(BackendTestReflection.GetType("CollectionTool"), ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                var tool = host.AddComponent(BackendTestReflection.GetType("EmptyHandTool"));
                BackendTestReflection.SetField(tool, "toolID", ids[i]);
                tools.SetValue(tool, i);
            }
            BackendTestReflection.SetField(manager, "availableTools", tools);
            BackendTestReflection.InvokeInstance(wheel, "InitializeTools");
            var list = (IList)BackendTestReflection.GetField(wheel, "availableTools");
            string[] initialized = list.Cast<object>().Select(t => (string)BackendTestReflection.GetField(t, "toolID")).ToArray();
            CollectionAssert.AreEqual(new[] { "0", "999", "1002", "1001" }, initialized);
            list.Clear();
            foreach (var tool in tools) BackendTestReflection.InvokeInstance(wheel, "AddTool", tool);
            BackendTestReflection.InvokeInstance(wheel, "AddTool", empty);
            CollectionAssert.AreEqual(initialized, list.Cast<object>().Select(t => BackendTestReflection.GetField(t, "toolID")));
            Assert.AreEqual(6, ((Array)BackendTestReflection.GetField(manager, "availableTools")).Length,
                "Filtering must not erase the restored unlock list.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(ui);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [TestCase("Interact", "E")]
    [TestCase("Secondary", "F")]
    public void TouchPrompt_ShouldUseRequestedControlAndHideItWhileUnavailable(string control, string key)
    {
        var prompt = BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("UISystem.InteractionPrompt"),
            "Create", "TouchMappingTest", "Panel", 1, TouchControl(control));
        var canvas = (Canvas)BackendTestReflection.GetProperty(prompt, "Canvas");
        try
        {
            var hint = (Component)BackendTestReflection.GetProperty(prompt, "TouchControl");
            Assert.AreEqual(TouchControl(control), BackendTestReflection.GetField(hint, "_control"));
            BackendTestReflection.InvokeInstance(prompt, "SetContent", "Action", key, true, false, true);
            Assert.IsTrue(hint.gameObject.activeSelf);
            BackendTestReflection.InvokeInstance(prompt, "SetContent", "Drilling", key, true, false, false);
            Assert.IsFalse(hint.gameObject.activeSelf);
            BackendTestReflection.InvokeInstance(prompt, "SetContent", "Maximum\nPut away", key, true, true, true);
            Assert.IsTrue(hint.gameObject.activeSelf);
            Assert.IsFalse(((Text)BackendTestReflection.GetProperty(prompt, "TouchInstruction")).gameObject.activeSelf);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
    }

    [Serializable] private class TextData { public Entry[] texts; }
    [Serializable] private class Entry { public string key; public string value; }
}
