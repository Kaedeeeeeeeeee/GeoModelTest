using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ResearchConsentTests
{
    private static Type Dialog => Type.GetType("UISystem.ResearchConsentDialog, Assembly-CSharp", true);
    private static object Call(Type type, string name, params object[] args) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    private static GameObject Root => GameObject.Find("ResearchConsentDialog");
    private static Transform Card => Root.transform.Find("ConsentCard");
    private static Button Continue => Card.Find("Continue").GetComponent<Button>();
    private static Toggle Agreement(int number) => Card.Find("Agreement" + number).GetComponent<Toggle>();
    private static void CheckAll(bool value)
    {
        for (int i = 1; i <= 3; i++) Agreement(i).isOn = value;
    }
    private int _starts;

    [UnityTest]
    public IEnumerator TitleEntry_ShouldShowAutomatically_AndOnlyUnlockNewGameAfterConfirmation()
    {
        var type = Type.GetType("SceneSystem.StartMenuBootstrap, Assembly-CSharp", true);
        var host = new GameObject("ConsentMenuTest");
        var menu = host.AddComponent(type);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var scene = SceneManager.GetActiveScene();
        type.GetField("_startSceneName", flags).SetValue(menu, scene.name);
        var load = type.GetMethod("SceneLoaded", flags);
        try
        {
            load.Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            yield return null;
            Assert.IsNotNull(Root, "Entering the title must show consent before New Game is clicked.");
            var newGame = (Button)type.GetField("_newGame", flags).GetValue(menu);
            var review = (Button)type.GetField("_reviewConsent", flags).GetValue(menu);
            Assert.IsFalse(newGame.interactable);
            string before = PlayerPrefs.GetString("GameSession.ResumeScene.v1", "missing");
            newGame.onClick.Invoke();
            Assert.AreEqual(before, PlayerPrefs.GetString("GameSession.ResumeScene.v1", "missing"));
            Card.Find("Cancel").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsFalse(newGame.interactable);
            Assert.IsTrue(review.gameObject.activeSelf);
            review.onClick.Invoke();
            Assert.IsNotNull(Root);
            CheckAll(true);
            Continue.onClick.Invoke();
            yield return null;
            Assert.IsTrue(newGame.interactable);
            Assert.IsFalse(review.gameObject.activeSelf);
            Assert.AreEqual(scene, SceneManager.GetActiveScene(), "Consent must not start the game.");
            Assert.AreEqual(before, PlayerPrefs.GetString("GameSession.ResumeScene.v1", "missing"));
            Assert.IsNull(Root);
            type.GetMethod("RefreshLanguage", flags).Invoke(menu, null);
            Assert.IsTrue(((Button)type.GetField("_newGame", flags).GetValue(menu)).interactable);
            Assert.IsNull(Root, "Changing menu language must preserve consent for this visit.");
            load.Invoke(menu, new object[] { scene, LoadSceneMode.Single });
            Assert.IsNotNull(Root, "Returning to the title requires a fresh acknowledgement.");
            for (int i = 1; i <= 3; i++) Assert.IsFalse(Agreement(i).isOn);
            Assert.IsFalse(((Button)type.GetField("_newGame", flags).GetValue(menu)).interactable);
        }
        finally
        {
            Call(Dialog, "CloseCurrent");
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private void Show() => Call(Dialog, "Show", (Action)(() => _starts++));

    [SetUp]
    public void SetUp()
    {
        _starts = 0;
        Call(Dialog, "CloseCurrent");
        Time.timeScale = 0.75f;
    }

    [UnityTest]
    public IEnumerator Checkmarks_ShouldBeVisibleWhenChecked_WhileGameIsPaused()
    {
        Show();
        yield return null;
        for (int i = 1; i <= 3; i++)
        {
            var agreement = Agreement(i);
            agreement.isOn = true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var graphic = agreement.graphic;
            Assert.AreEqual(1f, graphic.canvasRenderer.GetAlpha(), 0.01f);
            Assert.IsFalse(graphic.canvasRenderer.cull);
            var mesh = graphic.canvasRenderer.GetMesh();
            Assert.Greater(mesh.vertexCount, 0);
            Assert.Greater(mesh.bounds.size.x, 0);
            Assert.Greater(mesh.bounds.size.y, 0);
            agreement.isOn = false;
            Assert.AreEqual(0f, graphic.canvasRenderer.GetAlpha(), 0.01f);
        }
    }

    [UnityTest]
    public IEnumerator Continue_ShouldRequireCurrentCheck_AndOnlyRunOnce()
    {
        Show();
        yield return null;
        for (int i = 1; i <= 3; i++) Assert.IsFalse(Agreement(i).isOn);
        Assert.IsFalse(Continue.interactable);
        Assert.AreEqual(0f, Time.timeScale);
        Continue.onClick.Invoke();
        Assert.AreEqual(0, _starts, "A direct click event must not bypass agreement.");
        // All seven incomplete combinations must remain blocked, regardless of check order.
        for (int mask = 0; mask < 7; mask++)
        {
            for (int i = 1; i <= 3; i++) Agreement(i).isOn = (mask & (1 << (i - 1))) != 0;
            Assert.IsFalse(Continue.interactable, "Incomplete mask: " + mask);
            Continue.onClick.Invoke();
            Assert.AreEqual(0, _starts);
        }
        CheckAll(true);
        Assert.IsTrue(Continue.interactable);
        for (int i = 1; i <= 3; i++)
        {
            Agreement(i).isOn = false;
            Assert.IsFalse(Continue.interactable);
            Continue.onClick.Invoke();
            Assert.AreEqual(0, _starts);
            Agreement(i).isOn = true;
        }
        var button = Continue;
        button.onClick.Invoke();
        button.onClick.Invoke();
        Assert.AreEqual(1, _starts);
        Assert.AreEqual(0.75f, Time.timeScale);
        Assert.IsNull(Root);
    }

    [UnityTest]
    public IEnumerator CancelAndReopen_ShouldDiscardAgreement_WithoutChangingProgress()
    {
        string before = PlayerPrefs.GetString("GameSession.ResumeScene.v1", "missing");
        Show();
        CheckAll(true);
        Card.Find("Cancel").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(0, _starts);
        Assert.AreEqual(before, PlayerPrefs.GetString("GameSession.ResumeScene.v1", "missing"));
        yield return null;
        Show();
        for (int i = 1; i <= 3; i++) Assert.IsFalse(Agreement(i).isOn);
        Assert.IsFalse(Continue.interactable);
        var root = Root;
        Show();
        Assert.AreSame(root, Root, "Repeated clicks must not stack consent dialogs.");
        Call(Dialog, "CloseCurrent");
        Assert.AreEqual(0.75f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator EscapeScope_ShouldCancelRatherThanAccept()
    {
        Show();
        CheckAll(true);
        var component = Root.GetComponent(Dialog);
        var scope = Dialog.GetField("_input", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(component);
        var escape = (Action)scope.GetType().GetField("OnEscape", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scope);
        escape();
        yield return null;
        Assert.AreEqual(0, _starts);
        Assert.IsNull(Root);
        Assert.AreEqual(0.75f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator LongCopy_ShouldScroll_WhileAgreementAndActionsStayFixed()
    {
        Show();
        var scroll = Card.Find("Information").GetComponent<ScrollRect>();
        var body = scroll.content.GetComponent<Text>();
        body.text = string.Join("\n\n", new string[30]).Replace("\n\n", "Long research information to review.\n\n");
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
        Assert.Greater(scroll.viewport.rect.height, 0);
        Assert.IsFalse(scroll.horizontal);
        Assert.AreEqual(1f, scroll.verticalNormalizedPosition, 0.01f);
        Vector3 position = Agreement(1).transform.position;
        scroll.verticalNormalizedPosition = 0;
        yield return null;
        Assert.AreEqual(position, Agreement(1).transform.position);
        Assert.IsTrue(Continue.gameObject.activeInHierarchy);
        Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.01f);
    }

    [UnityTest]
    public IEnumerator FocusAndNavigation_ShouldStayInsideDialog_AndRestoreOnClose()
    {
        // Create the EventSystem through production UI before choosing a background control.
        Show();
        Call(Dialog, "CloseCurrent");
        yield return null;
        var background = new GameObject("ConsentTestBackground", typeof(RectTransform), typeof(Button));
        try
        {
            background.GetComponent<Button>().Select();
            Show();
            Assert.AreSame(Agreement(1).gameObject, EventSystem.current.currentSelectedGameObject);
            foreach (var selectable in Root.GetComponentsInChildren<Selectable>())
            {
                var navigation = selectable.navigation;
                if (navigation.mode == Navigation.Mode.None) continue;
                Assert.AreEqual(Navigation.Mode.Explicit, navigation.mode);
                foreach (var target in new[] { navigation.selectOnUp, navigation.selectOnDown, navigation.selectOnLeft, navigation.selectOnRight })
                    Assert.IsTrue(target.transform.IsChildOf(Card));
            }
            UnityEngine.Object.Destroy(Root);
            yield return null;
            Assert.IsFalse((bool)Dialog.GetProperty("IsOpen").GetValue(null));
            Assert.AreEqual(0.75f, Time.timeScale);
            Assert.AreSame(background, EventSystem.current.currentSelectedGameObject);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(background);
        }
    }

    [TearDown]
    public void TearDown()
    {
        Call(Dialog, "CloseCurrent");
        Time.timeScale = 1;
    }
}
