using System;
using System.Collections;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class IllustrationPreviewTests
{
    private GameObject _canvas;
    private Texture2D _texture;
    private Sprite _sprite;
    private static Type Subtitle => Type.GetType("StorySystem.StoryDirector+SubtitleUI, Assembly-CSharp", true);
    private static Type InputState => Type.GetType("Core.GameInputState, Assembly-CSharp", true);
    private static bool ModalOpen => (bool)InputState.GetProperty("IsModalOpen").GetValue(null);
    private static object Call(string method, params object[] args) => Subtitle.GetMethod(method,
        BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

    [SetUp]
    public void SetUp()
    {
        _canvas = new GameObject("PreviewTestCanvas", typeof(RectTransform), typeof(Canvas));
        _texture = new Texture2D(8, 8);
        var source = _texture;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GEOMODEL_FEEDBACK_SCREENSHOTS")))
            source = Resources.Load<Texture2D>("Story/Illustrations/04_chert");
        _sprite = Sprite.Create(source, new Rect(0, 0, source.width, source.height), Vector2.one * 0.5f);
    }

    [UnityTest]
    public IEnumerator Preview_ShouldBlockInputAndCloseWithoutLeakingScope()
    {
        Assert.IsFalse(ModalOpen);
        Call("ShowIllustrationFullscreen", _canvas.transform, _sprite);
        Assert.IsTrue(ModalOpen);
        var overlay = _canvas.transform.Find("IllustrationFullscreen");
        Assert.IsNotNull(overlay);
        var close = overlay.Find("CloseButton").GetComponent<Button>();
        Assert.IsTrue(close.interactable);
        CaptureIfRequested("illustration-fullscreen");
        Call("ShowIllustrationFullscreen", _canvas.transform, _sprite);
        Assert.AreEqual(1, _canvas.transform.childCount, "Repeated clicks cannot stack previews.");
        close.onClick.Invoke();
        Assert.IsFalse(ModalOpen);
        yield return null;
        Assert.AreEqual(0, _canvas.transform.childCount);
    }

    [UnityTest]
    public IEnumerator DestroyingDialogue_ShouldReleasePreviewInputScope()
    {
        Call("ShowIllustrationFullscreen", _canvas.transform, _sprite);
        Assert.IsTrue(ModalOpen);
        UnityEngine.Object.Destroy(_canvas);
        yield return null;
        Assert.IsFalse(ModalOpen);
    }

    [UnityTest]
    public IEnumerator Banner_ShouldOnlyShowZoomHintWhenImageVisible()
    {
        var banner = (Image)Call("CreateIllustrationBanner", _canvas.transform);
        var hint = banner.transform.Find("ZoomHint");
        Assert.IsFalse(hint.gameObject.activeSelf);
        yield return (IEnumerator)Call("SwapIllustration", banner, _sprite, 0f);
        Assert.IsTrue(hint.gameObject.activeSelf);
        Assert.IsNotNull(hint.Find("Magnifier").GetComponent<CanvasRenderer>());
        Assert.IsTrue(banner.raycastTarget);
        CaptureIfRequested("illustration-hint");
        Call("HideIllustrationNow", banner);
        Assert.IsFalse(hint.gameObject.activeSelf);
        Assert.IsFalse(banner.raycastTarget);
    }

    private void CaptureIfRequested(string name)
    {
        string directory = Environment.GetEnvironmentVariable("GEOMODEL_FEEDBACK_SCREENSHOTS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var cameraObject = new GameObject("PreviewCaptureCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.12f, 0.16f);
        camera.orthographic = true;
        var canvas = _canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var scaler = _canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        foreach (var size in new[] { new Vector2Int(1600, 900), new Vector2Int(960, 540) })
        {
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var capture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            capture.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + "-" + size.x + ".png"), capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(capture);
        }
        UnityEngine.Object.Destroy(cameraObject);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_canvas != null) UnityEngine.Object.Destroy(_canvas);
        if (_sprite != null) UnityEngine.Object.Destroy(_sprite);
        if (_texture != null) UnityEngine.Object.Destroy(_texture);
        yield return null;
    }
}
