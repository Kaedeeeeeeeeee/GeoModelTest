using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MobileInteractionHintTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private readonly List<GameObject> _objects = new List<GameObject>();
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string name, params object[] args) =>
        (target as Type ?? target.GetType()).GetMethod(name, Flags).Invoke(target is Type ? null : target, args);
    private static object Get(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private GameObject New(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        _objects.Add(go);
        return go;
    }

    private Component CreateHint(Transform parent, string control) => (Component)Call(T("UISystem.MobileControlHint"), "Create",
        parent, control + "Hint", Enum.Parse(T("UISystem.MobileControlHint+Control"), control));

    [Test]
    public void Hint_ShouldBindLateControlsAndReuseExactSpritesWithoutInputHandlers()
    {
        var host = New("HintTestCanvas");
        host.AddComponent<Canvas>();
        var hint = CreateHint(host.transform, "Interact");
        Assert.IsFalse((bool)Call(hint, "Refresh"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(hint.GetComponentInChildren<Text>().text),
            "Help opened before gameplay must still name the touch control.");
        var controls = New("LateControls").AddComponent(T("MobileControlsUI"));
        Call(controls, "CreateVirtualButtons");
        Assert.IsTrue((bool)Call(hint, "Refresh"), "A prompt created before the touch controls must bind when they appear.");

        string[] controlNames = { "Interact", "Secondary", "Tools", "Inventory" };
        string[] fieldNames = { "interactButton", "secondaryInteractButton", "toolWheelButton", "inventoryButton" };
        for (int i = 0; i < controlNames.Length; i++)
        {
            var current = i == 0 ? hint : CreateHint(host.transform, controlNames[i]);
            var source = (Button)Get(controls, fieldNames[i]);
            Assert.AreSame(source.transform.Find("Icon").GetComponent<Image>().sprite,
                current.transform.Find("Visual/Icon").GetComponent<Image>().sprite);
            Assert.AreEqual(((Image)source.targetGraphic).color * source.colors.normalColor * source.colors.colorMultiplier,
                current.transform.Find("Visual").GetComponent<Image>().color);
            var sourceText = source.GetComponentInChildren<Text>();
            sourceText.text = "Updated " + controlNames[i];
            Call(current, "Refresh");
            Assert.AreEqual(sourceText.text, current.GetComponentInChildren<Text>().text,
                "The hint must follow the actual localized button label.");
            Assert.IsNull(current.GetComponentInChildren<Button>(true));
            Assert.IsNull(current.GetComponentInChildren<EventTrigger>(true));
            foreach (var graphic in current.GetComponentsInChildren<Graphic>(true))
                Assert.IsFalse(graphic.raycastTarget, "Instruction graphics must not intercept touch controls.");
        }
        Assert.AreNotSame(hint.transform.Find("Visual/Icon").GetComponent<Image>().sprite,
            ((Button)Get(controls, "secondaryInteractButton")).transform.Find("Icon").GetComponent<Image>().sprite);
    }

    [Test]
    public void NpcPrompt_ShouldShowTouchButtonAndRestoreDesktopKeyPrompt()
    {
        var input = New("PromptInput").AddComponent(T("MobileInputManager"));
        Set(input, "currentInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        Set(input, "isMobileDevice", true);
        var controls = New("PromptControls").AddComponent(T("MobileControlsUI"));
        Call(controls, "CreateVirtualButtons");
        var npc = New("PromptNpc").AddComponent(T("QuestSystem.QuestNpcInteraction"));
        Call(npc, "CreatePromptUI");
        Call(npc, "UpdatePromptLocalization");
        var panel = (RectTransform)Get(npc, "promptPanel");
        var text = (Text)Get(npc, "promptText");
        Assert.IsTrue(panel.Find("TouchControl").gameObject.activeSelf);
        Assert.NotNull(panel.Find("TouchControl/Visual/Icon").GetComponent<Image>().sprite);
        Assert.IsFalse(text.text.Contains("E"), "The mobile action must not suggest a keyboard key.");
        Assert.IsTrue(panel.Find("TouchInstruction").gameObject.activeSelf);

        Set(input, "currentInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Desktop"));
        Call(npc, "UpdatePromptLocalization");
        Assert.IsFalse(panel.Find("TouchControl").gameObject.activeSelf);
        Assert.IsFalse(panel.Find("TouchInstruction").gameObject.activeSelf);
        Assert.IsFalse(text.text.Contains("［E］"));
        Assert.AreEqual("E", panel.Find("KeyCap/Label").GetComponent<Text>().text);
        Assert.IsTrue(panel.Find("KeyCap").gameObject.activeSelf);
        Assert.AreEqual(160f, panel.sizeDelta.y);
        Assert.GreaterOrEqual(text.fontSize, 30);
    }

    [Test]
    public void Guide_ShouldTeachWithActualTouchIconAndKeepDesktopE()
    {
        var controls = New("GuideControls").AddComponent(T("MobileControlsUI"));
        Call(controls, "CreateVirtualButtons");
        foreach (bool touch in new[] { true, false })
        {
            Call(T("UISystem.FirstControlGuide"), "Show", touch);
            var guide = GameObject.Find("FirstControlGuide");
            var component = guide.GetComponent(T("UISystem.FirstControlGuide"));
            Call(component, "ChangePage", 1);
            var talk = guide.transform.Find("GuideCard/Content/Talk");
            if (touch)
            {
                Assert.AreSame(((Button)Get(controls, "interactButton")).transform.Find("Icon").GetComponent<Image>().sprite,
                    talk.Find("TouchControl/Visual/Icon").GetComponent<Image>().sprite);
                Assert.IsNull(talk.Find("Symbol"));
            }
            else Assert.AreEqual("E", talk.Find("Symbol").GetComponent<Text>().text);
            Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        }
    }

    [UnityTest]
    public IEnumerator TouchPrompt_ShouldFitPhoneAndTabletCapture()
    {
        string output = Environment.GetEnvironmentVariable("GEOMODEL_MOBILE_HINT_CAPTURE");
        if (string.IsNullOrEmpty(output)) Assert.Ignore("Set GEOMODEL_MOBILE_HINT_CAPTURE for UI captures.");
        Directory.CreateDirectory(output);
        Call(T("LocalizationManager").GetProperty("Instance").GetValue(null), "SwitchLanguage",
            Enum.Parse(T("LanguageSettings+Language"), "Japanese"));
        var input = (Behaviour)New("CaptureInput").AddComponent(T("MobileInputManager"));
        Set(input, "currentInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        Set(input, "isMobileDevice", true);
        var controls = (Behaviour)New("CaptureControls").AddComponent(T("MobileControlsUI"));
        Call(controls, "CreateVirtualButtons");
        var npc = (Behaviour)New("CaptureNpc").AddComponent(T("QuestSystem.QuestNpcInteraction"));
        npc.enabled = false;
        Call(npc, "CreatePromptUI");
        Call(npc, "UpdatePromptLocalization");
        var collector = (Behaviour)New("CaptureRockSample").AddComponent(T("SampleCollector"));
        collector.enabled = false;
        Set(collector, "sourceToolID", "1002");
        Call(collector, "CreateInteractionPrompt");
        var drillUI = (Behaviour)New("CaptureTowerUI").AddComponent(T("DrillTowerInteractionUI"));
        drillUI.enabled = false;
        Call(drillUI, "CreateInteractionUI");
        var drill = (Behaviour)New("CaptureDrillTool").AddComponent(T("DrillTowerTool"));
        drill.enabled = false;
        var tower = (Behaviour)New("CaptureTower").AddComponent(T("DrillTower"));
        tower.enabled = false;
        Set(tower, "toolReference", drill);
        controls.enabled = false;
        input.enabled = false;
        var camera = New("CaptureCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.08f, 0.1f);
        camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        var shared = new[] { Get(collector, "sharedPrompt"), Get(drillUI, "sharedPrompt"), Get(npc, "sharedPrompt") };
        var canvases = shared.Select(p => (Canvas)p.GetType().GetProperty("Canvas").GetValue(p))
            .Concat(new[] { controls.GetComponent<Canvas>() }).ToArray();
        foreach (var canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = LayerMask.NameToLayer("UI");
        }
        foreach (string action in new[] { "rock", "drill", "maximum", "npc" })
        {
            foreach (var current in shared) Call(current, "SetVisible", false, 0, 0f);
            object prompt;
            string buttonField;
            if (action == "rock")
            {
                Call(collector, "RefreshPrompt");
                prompt = shared[0];
                buttonField = "interactButton";
            }
            else if (action == "npc")
            {
                Call(npc, "UpdatePromptLocalization");
                prompt = shared[2];
                buttonField = "interactButton";
            }
            else
            {
                Set(tower, "currentDrillCount", action == "maximum" ? 5 : 0);
                Call(drillUI, "UpdatePromptText", tower);
                prompt = shared[1];
                buttonField = "secondaryInteractButton";
            }
            var canvas = (Canvas)prompt.GetType().GetProperty("Canvas").GetValue(prompt);
            var panel = (RectTransform)prompt.GetType().GetProperty("Panel").GetValue(prompt);
            var text = (Text)prompt.GetType().GetProperty("Action").GetValue(prompt);
            var hint = (Component)prompt.GetType().GetProperty("TouchControl").GetValue(prompt);
            Call(prompt, "SetVisible", true, 2, 0f);
            Assert.IsTrue(hint.gameObject.activeSelf);
            Assert.AreSame(((Button)Get(controls, buttonField)).transform.Find("Icon").GetComponent<Image>().sprite,
                hint.transform.Find("Visual/Icon").GetComponent<Image>().sprite, action + " must show the matching real control.");
            StringAssert.DoesNotContain("「使う」で", text.text);
            if (action == "rock") StringAssert.Contains("岩石サンプルを拾う", text.text);
            if (action == "maximum")
            {
                StringAssert.Contains("調査できる最大の深さに達しました", text.text);
                StringAssert.Contains("ドリルタワーをしまう", text.text);
            }
            foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(844, 390), new Vector2Int(1024, 768) })
            {
                var target = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = target;
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (Text label in canvas.GetComponentsInChildren<Text>())
                {
                    if (label.resizeTextForBestFit) continue; // Button artwork keeps its source label's fitted size.
                    Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 1f, action + "/" + label.name + " must fit.");
                }
                Assert.IsFalse(ScreenRect(hint.GetComponent<RectTransform>(), camera).Overlaps(ScreenRect(text.rectTransform, camera)),
                    action + " icon and action must not overlap.");
                var panelBounds = ScreenRect(panel, camera);
                Assert.GreaterOrEqual(panelBounds.xMin, 0f);
                Assert.LessOrEqual(panelBounds.xMax, size.x);
                foreach (string control in new[] { "interactButton", "secondaryInteractButton", "toolWheelButton", "inventoryButton" })
                    Assert.IsFalse(panelBounds.Overlaps(ScreenRect(((Button)Get(controls, control)).GetComponent<RectTransform>(), camera)),
                        action + " must not cover " + control + " at " + size);
                if (size == new Vector2Int(960, 540)) Assert.GreaterOrEqual(text.fontSize * canvas.scaleFactor, 18f);
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(output, action + "-touch-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.Destroy(pixels);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
        }
        Set(tower, "isDrilling", true);
        Call(drillUI, "UpdatePromptText", tower);
        var towerPanel = (GameObject)Get(drillUI, "interactionPrompt");
        Assert.IsFalse(towerPanel.transform.Find("TouchControl").gameObject.activeSelf,
            "A tower in progress must hide the unavailable action button.");
    }

    private static Rect ScreenRect(RectTransform rt, Camera camera)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    [TearDown]
    public void Cleanup()
    {
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        _objects.Clear();
    }
}
