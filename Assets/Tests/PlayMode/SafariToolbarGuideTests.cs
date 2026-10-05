using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The title screen after the web consent page, and the Safari toolbar hint on iPhone/iPad.</summary>
public class SafariToolbarGuideTests
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Type Guide => T("UISystem.SafariToolbarGuide");
    private static Type Record => T("Backend.ResearchConsentRecord");
    private static GameObject GuideRoot => GameObject.Find("SafariToolbarGuide");

    private int _pageState;
    private int _device;

    [SetUp]
    public void SetUp()
    {
        Guide.GetMethod("ResetForTests", Static).Invoke(null, null);
        Record.GetMethod("ResetForTests", Static).Invoke(null, null);
        T("UISystem.ResearchConsentDialog").GetMethod("CloseCurrent", Static).Invoke(null, null);
        // Func<ResearchConsentRecord.PageState> reading the test's current state.
        var stateType = T("Backend.ResearchConsentRecord+PageState");
        Func<int> read = () => _pageState;
        var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(stateType),
            Expression.Convert(Expression.Invoke(Expression.Constant(read)), stateType));
        Record.GetField("PageStateOverride", Static).SetValue(null, lambda.Compile());
        Guide.GetField("DeviceOverride", Static).SetValue(null, (Func<int>)(() => _device));
    }

    [TearDown]
    public void TearDown()
    {
        Guide.GetMethod("ResetForTests", Static).Invoke(null, null);
        Record.GetMethod("ResetForTests", Static).Invoke(null, null);
    }

    [Test]
    public void MenuStyle_ShouldFollowTheSafariGenerationSeenInTheSimulators()
    {
        string Style(int device) => (string)Guide.GetMethod("Style", Static).Invoke(null, new object[] { device });
        Assert.AreEqual("classic", Style(1018), "iPhone Safari 18: 「…」");
        Assert.AreEqual("classic", Style(1026), "iPhone Safari 26: 「…」");
        Assert.AreEqual("new", Style(1027), "iPhone Safari 27: scrolling list");
        Assert.AreEqual("classic", Style(2026), "iPad Safari 26: 「…」");
        Assert.AreEqual("new", Style(2027), "iPad Safari 27: scrolling list");
        foreach (string image in new[] { "iphone-classic", "iphone-new", "ipad-classic", "ipad-new" })
            Assert.IsNotNull(Resources.Load<Texture2D>("UI/SafariToolbar/" + image), image);
    }

    [UnityTest]
    public IEnumerator Title_ShouldWaitForTheConsentPage_ThenShowTheSafariHintOncePerPageLoad()
    {
        var type = T("SceneSystem.StartMenuBootstrap");
        var host = new GameObject("ConsentPageMenuTest");
        var menu = host.AddComponent(type);
        var scene = SceneManager.GetActiveScene();
        type.GetField("_startSceneName", Instance).SetValue(menu, scene.name);
        var load = type.GetMethod("SceneLoaded", Instance);
        Button NewGame() => (Button)type.GetField("_newGame", Instance).GetValue(menu);
        Button Review() => (Button)type.GetField("_reviewConsent", Instance).GetValue(menu);
        try
        {
            _pageState = 1; // the guardian or the student is still answering on the page
            _device = 1027;
            load.Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            yield return null;
            Assert.IsNull(GameObject.Find("ResearchConsentDialog"), "The page asks the student; the title must not ask again.");
            Assert.IsFalse(NewGame().interactable);
            Assert.IsFalse(Review().gameObject.activeSelf);
            Assert.IsNull(GuideRoot, "Nothing covers the title before the page is done.");

            _pageState = 2; // both answered on the page
            yield return null;
            yield return null;
            Assert.IsTrue(NewGame().interactable, "The page consent unlocks New Game.");
            Assert.IsFalse(Review().gameObject.activeSelf);
            Assert.IsNotNull(GuideRoot, "iPhone Safari gets the toolbar hint as the title appears.");
            var card = GuideRoot.transform.Find("GuideCard");
            Assert.IsNotNull(card.Find("Screenshot").GetComponent<Image>().sprite, "The real Safari screenshot is shown.");
            string step2 = card.Find("Steps/Step2/Text").GetComponent<Text>().text;
            var localization = T("LocalizationManager").GetProperty("Instance", Static).GetValue(null);
            Assert.AreEqual(localization.GetType().GetMethod("GetText", new[] { typeof(string) })
                .Invoke(localization, new object[] { "ui.safari.new.step.2" }), step2);
            Assert.IsTrue((bool)T("Core.GameInputState").GetProperty("IsModalOpen", Static).GetValue(null),
                "The hint blocks the title until it is closed.");

            card.Find("Close").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNull(GuideRoot);
            Assert.IsFalse((bool)T("Core.GameInputState").GetProperty("IsModalOpen", Static).GetValue(null));

            load.Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            yield return null;
            Assert.IsTrue(NewGame().interactable, "Returning to the title keeps this page load's consent.");
            Assert.IsNull(GuideRoot, "The hint is shown once per page load.");
        }
        finally
        {
            Guide.GetMethod("CloseCurrent", Static).Invoke(null, null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [UnityTest]
    public IEnumerator OtherBrowsers_ShouldNotSeeTheSafariHint()
    {
        var type = T("SceneSystem.StartMenuBootstrap");
        var host = new GameObject("ConsentPageMenuTest");
        var menu = host.AddComponent(type);
        var scene = SceneManager.GetActiveScene();
        type.GetField("_startSceneName", Instance).SetValue(menu, scene.name);
        try
        {
            _pageState = 2;
            _device = 0; // desktop, Android, Chrome on iPhone, in-app browsers
            type.GetMethod("SceneLoaded", Instance).Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            yield return null;
            Assert.IsTrue(((Button)type.GetField("_newGame", Instance).GetValue(menu)).interactable);
            Assert.IsNull(GuideRoot);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
