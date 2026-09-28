using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EncyclopediaResourceLoadingTests
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private readonly List<GameObject> objects = new List<GameObject>();
    private Component data;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static object Property(object target, string name) => target.GetType().GetProperty(name, Flags).GetValue(target);
    private object[] Entries => ((IDictionary)Property(data, "AllEntries")).Values.Cast<object>().ToArray();

    private GameObject New(string name, bool active = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.SetActive(active);
        objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        if ((UnityEngine.Object)T("Encyclopedia.EncyclopediaData").GetProperty("Instance", Flags).GetValue(null) != null)
            Assert.Ignore("Run this isolated resource fixture without a pre-existing gameplay EncyclopediaData singleton.");
        data = New("ResourceLoadingTestData").AddComponent(T("Encyclopedia.EncyclopediaData"));
        Assert.IsTrue((bool)Property(data, "IsDataLoaded"));
    }

    private object Entry(string suffix) => Entries.First(e => ((string)Get(e, "id")).EndsWith(suffix, StringComparison.Ordinal));

    private void AssertMetadataOnly()
    {
        Assert.Greater(Entries.Length, 1);
        foreach (var entry in Entries)
        {
            Assert.IsNull(Get(entry, "icon"), "Metadata/list operations must not load standalone images.");
            Assert.IsNull(Get(entry, "model3D"), "Metadata/list operations must not load every model and its texture dependencies.");
        }
        Assert.IsNull(Get(data, "fallbackRoot"), "Missing models must not create hidden or active placeholder objects at startup.");
    }

    [Test]
    public void StartupAndMetadataQueries_ShouldNotLoadImagesModelsOrFallbackObjects()
    {
        AssertMetadataOnly();
        Assert.IsNotEmpty((IList)Call(data, "GetAllMinerals"));
        Assert.IsNotEmpty((IList)Call(data, "GetAllFossils"));
        foreach (string layer in (IEnumerable)Property(data, "LayerNames"))
            Assert.IsNotEmpty((IList)Call(data, "GetEntriesByLayer", layer));
        var first = Entries[0];
        Assert.AreSame(first, Call(data, "GetEntryById", Get(first, "id")));
        AssertMetadataOnly();
    }

    [Test]
    public void ImageRequest_ShouldLoadOnlyRequestedImageAndReuseItUntilReleased()
    {
        var entry = Entry("_plagioclase");
        // MineralData photos currently use Default, not Sprite import settings. Use an actual
        // Sprite resource through the configurable path to test loading/caching without changing assets.
        Set(data, "mineralImagePath", "Tachie/");
        Set(entry, "imageFile", "player.png");
        var icon = (Sprite)Call(data, "LoadEntryIcon", entry);
        Assert.IsNotNull(icon, "The real Sprite-imported fixture must resolve through the on-demand loader.");
        Assert.AreSame(icon, Call(data, "LoadEntryIcon", entry));
        Assert.AreEqual(1, Entries.Count(e => Get(e, "icon") != null));
        Assert.AreEqual(0, Entries.Count(e => Get(e, "model3D") != null));
        Call(data, "ReleaseEntryResources", entry);
        Assert.IsNull(Get(entry, "icon"));
        Assert.IsNotNull(Call(data, "LoadEntryIcon", entry), "An entry must remain usable after its previous view closes.");
        Call(data, "ReleaseEntryResources", entry);
    }

    [Test]
    public void NonSpritePhoto_ShouldReturnNullWithoutLoadingModelsOrRetryingUntilReleased()
    {
        var entry = Entry("_plagioclase");
        Assert.IsNull(Call(data, "LoadEntryIcon", entry), "The current Default-imported photo is not a Sprite; preserve its existing null result.");
        Assert.IsNull(Call(data, "LoadEntryIcon", entry));
        var requests = Get(data, "requestedIcons");
        Assert.AreEqual(1, Property(requests, "Count"), "Remember the missing Sprite for this visible view.");
        Assert.AreEqual(0, Entries.Count(e => Get(e, "model3D") != null));
        Call(data, "ReleaseEntryResources", entry);
        Assert.AreEqual(0, Property(requests, "Count"));
    }

    [Test]
    public void ModelRequest_ShouldLoadOneRealModelWithoutLoadingStandalonePhotos()
    {
        var entry = Entry("_plagioclase");
        var model = (GameObject)Call(data, "LoadEntryModel", entry);
        Assert.IsNotNull(model);
        Assert.IsFalse(model.name.StartsWith("DefaultModel_"), "This fixture exercises a real model, not only the fallback.");
        Assert.AreSame(model, Call(data, "LoadEntryModel", entry));
        Assert.AreEqual(1, Entries.Count(e => Get(e, "model3D") != null));
        Assert.AreEqual(0, Entries.Count(e => Get(e, "icon") != null));
        Call(data, "ReleaseEntryResources", entry);
        Assert.IsNull(Get(entry, "model3D"));
        Assert.IsNotNull(Call(data, "LoadEntryModel", entry));
        Call(data, "ReleaseEntryResources", entry);
    }

    [UnityTest]
    public IEnumerator MissingModel_ShouldCreateOnlyAnInactiveTemplateAndDestroyItOnRelease()
    {
        var entry = Activator.CreateInstance(T("Encyclopedia.EncyclopediaEntry"));
        Set(entry, "displayName", "Missing fixture");
        Set(entry, "modelFile", "nonexistent_resource_loading_fixture.glb");
        var model = (GameObject)Call(data, "LoadEntryModel", entry);
        var material = model.GetComponent<Renderer>().sharedMaterial;
        Assert.IsTrue(model.activeSelf, "An instantiated preview must remain visible.");
        Assert.IsFalse(model.activeInHierarchy, "The cached fallback must not be rendered in the world.");
        Assert.AreSame(model, Call(data, "LoadEntryModel", entry));
        Assert.AreEqual(1, ((Transform)Get(data, "fallbackRoot")).childCount);
        Call(data, "ReleaseEntryResources", entry);
        yield return null;
        Assert.IsTrue(model == null);
        Assert.IsTrue(material == null, "The owned fallback material must not leak after repeated missing-model views.");
        Assert.IsNull(Get(entry, "model3D"));
    }

    [TestCase("Encyclopedia.SimpleEncyclopediaManager")]
    [TestCase("Encyclopedia.EncyclopediaUI")]
    public void TextListAndDetailViews_ShouldLoadCurrentModelAndReleaseItOnSwitchAndClose(string typeName)
    {
        var host = New("ResourceLoadingTestUI");
        var ui = (Behaviour)host.AddComponent(T(typeName));
        ui.enabled = false; // Exercise real view methods without auto-creating collection/save systems in Start.
        var list = New("EntryList");
        Set(ui, "entryListContainer", list.transform);
        if (typeName.EndsWith("EncyclopediaUI", StringComparison.Ordinal))
        {
            var prefab = New("EntryPrefab");
            prefab.AddComponent<Image>();
            prefab.AddComponent<Button>();
            var name = New("NameText");
            name.transform.SetParent(prefab.transform, false);
            name.AddComponent<Text>();
            Set(ui, "entryItemPrefab", prefab);
        }
        foreach (var entry in Entries.Take(3)) Call(ui, "CreateEntryItem", entry);
        Assert.AreEqual(3, list.transform.childCount, "The real text list must remain populated.");
        AssertMetadataOnly();

        var detail = New("Detail");
        Set(ui, "detailPanel", detail);
        var viewerHost = New("Viewer", false);
        viewerHost.transform.SetParent(detail.transform, false);
        var rawImage = viewerHost.AddComponent<RawImage>();
        var viewer = viewerHost.AddComponent(T("SampleCuttingSystem.Sample3DModelViewer"));
        Set(viewer, "rawImage", rawImage);
        viewerHost.SetActive(true); // Awake now sees the assigned RawImage.
        Set(ui, "model3DViewer", viewer);
        string show = typeName.EndsWith("SimpleEncyclopediaManager", StringComparison.Ordinal) ? "ShowEntryDetail" : "SetDetailContent";
        var first = Entry("_plagioclase");
        var second = Entry("_pyroxene");
        Call(ui, show, first);
        Assert.IsNotNull(Get(first, "model3D"));
        Assert.IsNotNull(Get(viewer, "currentSample"), "The requested model must be instantiated into the actual preview.");
        Call(ui, show, second);
        Assert.IsNull(Get(first, "model3D"), "Paging must not retain all previously viewed models.");
        Assert.IsNotNull(Get(second, "model3D"));
        Assert.AreEqual(1, Entries.Count(e => Get(e, "model3D") != null));
        Assert.AreEqual(0, Entries.Count(e => Get(e, "icon") != null));
        Call(ui, "CloseEncyclopedia");
        Assert.IsNull(Get(second, "model3D"));
        Assert.IsNull(Get(viewer, "currentSample"));
        Assert.IsFalse(detail.activeSelf);
        detail.SetActive(true);
        Call(ui, show, first);
        Assert.IsNotNull(Get(viewer, "currentSample"), "Reopening a released entry must still display its model.");
        Call(ui, "CloseDetailPanel");
        Assert.IsNull(Get(first, "model3D"));
    }

    [UnityTest]
    public IEnumerator LegacyInactiveParent_ShouldMoveOnlyPanelAndKeepFirstOpenCloseReopenWorking()
    {
        var oldCanvas = New("MobileControlsCanvas", false);
        oldCanvas.AddComponent<Canvas>();
        oldCanvas.transform.localScale = Vector3.zero;
        var panel = New("EncyclopediaPanel");
        panel.transform.SetParent(oldCanvas.transform, false);
        panel.AddComponent<Image>();
        var ui = panel.AddComponent(T("Encyclopedia.EncyclopediaUI"));
        Set(ui, "encyclopediaPanel", panel);
        var list = New("EntryList");
        list.transform.SetParent(panel.transform, false);
        Set(ui, "entryListContainer", list.transform);
        var prefab = New("EntryPrefab");
        prefab.transform.SetParent(oldCanvas.transform, false);
        prefab.AddComponent<Image>();
        prefab.AddComponent<Button>();
        Set(ui, "entryItemPrefab", prefab);
        var closeObject = New("Close");
        closeObject.transform.SetParent(panel.transform, false);
        closeObject.AddComponent<Image>();
        var close = closeObject.AddComponent<Button>();
        Set(ui, "closeButton", close);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            New("EntryPointEventSystem").AddComponent<EventSystem>();

        Call(ui, "PrepareForUse");
        var canvas = (Canvas)Get(ui, "dedicatedCanvas");
        Assert.IsNotNull(canvas);
        Assert.IsFalse(oldCanvas.activeSelf, "Do not activate the obsolete HUD to repair the encyclopedia.");
        Assert.AreEqual(Vector3.zero, oldCanvas.transform.localScale);
        Assert.AreSame(canvas.transform, panel.transform.parent);
        Assert.AreSame(oldCanvas.transform, prefab.transform.parent, "Unrelated legacy siblings must not migrate.");
        Assert.AreEqual(Vector3.one, canvas.transform.localScale);
        Assert.AreEqual(Vector3.one, panel.transform.localScale);
        Assert.IsFalse(panel.activeSelf);
        AssertMetadataOnly();

        float previousScale = Time.timeScale;
        Call(ui, "OpenEncyclopedia");
        yield return null;
        yield return null; // Start must not close a panel opened before its first active frame.
        Assert.IsTrue(panel.activeInHierarchy);
        Assert.IsTrue((bool)Call(ui, "IsOpen"));
        Assert.AreEqual(0f, Time.timeScale);
        Assert.AreEqual(32767, canvas.sortingOrder);
        Assert.AreEqual(Entries.Length, list.transform.childCount);
        Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>());
        AssertMetadataOnly();
        close.onClick.Invoke();
        Assert.IsFalse(panel.activeInHierarchy);
        Assert.AreEqual(previousScale, Time.timeScale);
        Assert.AreEqual(10001, canvas.sortingOrder);
        yield return null;

        Call(ui, "OpenEncyclopedia");
        Call(ui, "OpenEncyclopedia");
        yield return null;
        Assert.IsTrue(panel.activeInHierarchy);
        Assert.AreSame(canvas, Get(ui, "dedicatedCanvas"), "Reopen must reuse the same dedicated canvas.");
        Assert.AreEqual(Entries.Length, list.transform.childCount, "Reopen must not duplicate the list.");
        Call(ui, "ToggleEncyclopedia");
        Call(ui, "ToggleEncyclopedia");
        Assert.IsFalse(panel.activeInHierarchy, "One frame of desktop O routing cannot close then immediately reopen.");
        Assert.AreEqual(previousScale, Time.timeScale);
    }

    [Test]
    public void DestroyingViewer_ShouldDetachItsRenderTextureFromSurvivingCameraAndImage()
    {
        var host = New("ViewerDisposalFixture", false);
        var image = host.AddComponent<RawImage>();
        var viewer = host.AddComponent(T("SampleCuttingSystem.Sample3DModelViewer"));
        Set(viewer, "rawImage", image);
        host.SetActive(true);
        var camera = (Camera)Get(viewer, "renderCamera");
        var texture = (RenderTexture)Get(viewer, "renderTexture");
        Assert.AreSame(texture, camera.targetTexture);
        image.texture = texture;
        UnityEngine.Object.DestroyImmediate(viewer);
        Assert.IsTrue(camera != null, "Destroy only the component so camera and UI bindings must be explicitly cleared.");
        Assert.IsNull(camera.targetTexture);
        Assert.IsNull(image.texture);
        Assert.IsTrue(texture == null, "The owned texture must be disposed, not merely detached.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // No PlayerPrefs, collection progress or backend settings are written by this fixture.
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
        data = null;
        yield return Resources.UnloadUnusedAssets();
    }
}
