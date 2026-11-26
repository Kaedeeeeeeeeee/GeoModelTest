using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>The title-screen 「全画面にする」 card for browsers that allow page fullscreen.</summary>
public class FullscreenPromptTests
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Type Prompt => T("UISystem.FullscreenPrompt");
    private static Type Safari => T("UISystem.SafariToolbarGuide");
    private static Type Page => T("Backend.StudentConsentPage");
    private static GameObject Root => GameObject.Find("FullscreenPrompt");

    private int _availability;
    private int _safari;
    private readonly List<string> _requests = new List<string>();

    [SetUp]
    public void SetUp()
    {
        // NUnit keeps one instance per fixture, so each test starts from "no browser feature".
        _safari = 0;
        _availability = 0;
        Reset();
        var stateType = T("Backend.StudentConsentPage+PageState");
        Func<int> read = () => 2; // the student already confirmed on the page
        var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(stateType),
            Expression.Convert(Expression.Invoke(Expression.Constant(read)), stateType));
        Page.GetField("StateOverride", Static).SetValue(null, lambda.Compile());
        Safari.GetField("DeviceOverride", Static).SetValue(null, (Func<int>)(() => _safari));
        Prompt.GetField("AvailabilityOverride", Static).SetValue(null, (Func<int>)(() => _availability));
        Prompt.GetField("RequestOverride", Static).SetValue(null, (Action<string>)(phase => _requests.Add(phase)));
        _requests.Clear();
    }

    [TearDown]
    public void TearDown() => Reset();

    private static void Reset()
    {
        Prompt.GetMethod("ResetForTests", Static).Invoke(null, null);
        Safari.GetMethod("ResetForTests", Static).Invoke(null, null);
        Page.GetMethod("ResetForTests", Static).Invoke(null, null);
    }

    private IEnumerator WithTitle(Func<Type, Component, IEnumerator> body)
    {
        var type = T("SceneSystem.StartMenuBootstrap");
        var host = new GameObject("TitleMenuTest");
        var menu = host.AddComponent(type);
        var scene = SceneManager.GetActiveScene();
        type.GetField("_startSceneName", Instance).SetValue(menu, scene.name);
        try
        {
            type.GetMethod("SceneLoaded", Instance).Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            yield return null;
            yield return body(type, menu);
        }
        finally
        {
            Prompt.GetMethod("CloseCurrent", Static).Invoke(null, null);
            Safari.GetMethod("CloseCurrent", Static).Invoke(null, null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [UnityTest]
    public IEnumerator Title_ShouldOfferFullscreenOnce_AndAskTheBrowserOnPressAndClick()
    {
        _availability = 2; // Android / touch Chromebook
        yield return WithTitle((type, menu) => Check(type, menu));
    }

    private IEnumerator Check(Type type, Component menu)
    {
        Assert.IsNotNull(Root, "The card appears with the title.");
        Assert.IsTrue((bool)T("Core.GameInputState").GetProperty("IsModalOpen", Static).GetValue(null));
        var card = Root.transform.Find("Card");
        var localization = T("LocalizationManager").GetProperty("Instance", Static).GetValue(null);
        string L(string key) => (string)localization.GetType().GetMethod("GetText", new[] { typeof(string) })
            .Invoke(localization, new object[] { key });
        Assert.AreEqual(L("ui.fullscreen.restore.touch"), card.Find("Restore").GetComponent<Text>().text);

        var go = card.Find("Fullscreen").gameObject;
        ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
        go.GetComponent<Button>().onClick.Invoke();
        yield return null;
        CollectionAssert.AreEqual(new[] { "arm", "click" }, _requests);
        Assert.IsNull(Root, "The card closes after the tap.");
        Assert.IsFalse((bool)T("Core.GameInputState").GetProperty("IsModalOpen", Static).GetValue(null));

        type.GetMethod("SceneLoaded", Instance).Invoke(menu, new object[] { SceneManager.GetActiveScene(), LoadSceneMode.Single });
        yield return null;
        Assert.IsNull(Root, "Shown once per page load.");
    }

    [UnityTest]
    public IEnumerator Skip_ShouldCloseWithoutAskingForFullscreen()
    {
        _availability = 1;
        yield return WithTitle((type, menu) => SkipBody());
    }

    private IEnumerator SkipBody()
    {
        Assert.IsNotNull(Root);
        Root.transform.Find("Card/Skip").GetComponent<Button>().onClick.Invoke();
        yield return null;
        Assert.IsNull(Root);
        Assert.IsEmpty(_requests);
    }

    [UnityTest]
    public IEnumerator AppleSafari_ShouldGetTheToolbarGuide_AndBrowsersWithoutFullscreenNothing()
    {
        _safari = 2026;
        _availability = 0;
        yield return WithTitle((type, menu) => AppleBody());
    }

    private IEnumerator AppleBody()
    {
        Assert.IsNotNull(GameObject.Find("SafariToolbarGuide"));
        Assert.IsNull(Root);
        yield break;
    }
}
