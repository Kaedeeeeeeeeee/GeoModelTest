using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        Assert.IsTrue(text.text.Contains("E"));
        Assert.AreEqual(new Vector2(420f, 64f), panel.sizeDelta);
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
        var input = (Behaviour)New("CaptureInput").AddComponent(T("MobileInputManager"));
        Set(input, "currentInputMode", Enum.Parse(T("MobileInputManager+InputMode"), "Mobile"));
        Set(input, "isMobileDevice", true);
        var controls = (Behaviour)New("CaptureControls").AddComponent(T("MobileControlsUI"));
        Call(controls, "CreateVirtualButtons");
        var npc = (Behaviour)New("CaptureNpc").AddComponent(T("QuestSystem.QuestNpcInteraction"));
        npc.enabled = false;
        Call(npc, "CreatePromptUI");
        Call(npc, "UpdatePromptLocalization");
        var prompt = ((GameObject)Get(npc, "promptCanvasGO")).GetComponent<Canvas>();
        prompt.gameObject.SetActive(true);
        controls.enabled = false;
        input.enabled = false;
        var camera = New("CaptureCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.08f, 0.1f);
        camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        var canvases = new[] { prompt, controls.GetComponent<Canvas>() };
        foreach (var canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = LayerMask.NameToLayer("UI");
        }
        foreach (var size in new[] { new Vector2Int(844, 390), new Vector2Int(1024, 768) })
        {
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (Text text in prompt.GetComponentsInChildren<Text>())
                Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 1f, text.name + " must fit.");
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(output, "npc-touch-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(pixels);
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
    }

    [TearDown]
    public void Cleanup()
    {
        Call(T("UISystem.FirstControlGuide"), "CloseCurrent");
        foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        _objects.Clear();
    }
}
