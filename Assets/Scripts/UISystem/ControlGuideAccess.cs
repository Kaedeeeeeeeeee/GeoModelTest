using Core;
using SceneSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>Persistent help shortcut, including during dialogue and nested modal screens.</summary>
    public sealed class ControlGuideAccess : MonoBehaviour
    {
        private static ControlGuideAccess _instance;
        private Button _button;
        private Text _label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var canvas = GameUI.Canvas("ControlGuideAccess", 30000);
            _instance = canvas.gameObject.AddComponent<ControlGuideAccess>();
            DontDestroyOnLoad(canvas.gameObject);
        }

        private void Start()
        {
            _button = GameUI.Button(transform, "Help", "", new Vector2(0.44f, 0.92f), new Vector2(0.64f, 0.985f), Open);
            _label = _button.GetComponentInChildren<Text>();
        }

        public static void Open()
        {
            if (GameSceneManager.IsLoadingScene) return;
            FirstControlGuide.Show(FirstControlGuide.UsesTouch());
        }

        private void Update()
        {
            bool gameplay = GameSession.IsGameplayScene(SceneManager.GetActiveScene().name);
            if (_button != null)
            {
                _button.gameObject.SetActive(gameplay && !FirstControlGuide.IsOpen);
                _label.text = GameUI.L(FirstControlGuide.UsesTouch() ? "ui.guide.open.touch" : "ui.guide.open.desktop");
            }
            if (!gameplay || GameSceneManager.IsLoadingScene) return;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool typing = selected != null && (selected.GetComponent<InputField>() != null || selected.GetComponent<TMPro.TMP_InputField>() != null);
            if (!typing && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (FirstControlGuide.IsOpen) FirstControlGuide.DismissCurrent();
                else Open();
            }
        }

        private void LateUpdate()
        {
            FirstControlGuide.TryShowForFirstField();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
