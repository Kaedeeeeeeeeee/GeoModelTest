using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>Owns nested modal input and restores the state that preceded the first modal.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameInputState : MonoBehaviour
    {
        private static GameInputState _instance;
        private static readonly List<Scope> Scopes = new List<Scope>();
        private static int _escapeFrame = -1;
        private static int _releaseFrame = -1;
        private static double _pausedSeconds;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private FirstPersonController _player;
        private bool _playerWasEnabled;

        public static bool IsModalOpen => Scopes.Count > 0;
        public static bool GameplayBlocked => IsModalOpen || Time.frameCount <= _releaseFrame;
        public static bool EscapeConsumed => _escapeFrame == Time.frameCount;
        public static double ActiveTime => Time.realtimeSinceStartupAsDouble - _pausedSeconds;

        public static Scope Acquire(Action onEscape = null)
        {
            if (_instance == null)
            {
                var host = new GameObject("GameInputState");
                _instance = host.AddComponent<GameInputState>();
                DontDestroyOnLoad(host);
            }
            if (Scopes.Count == 0) _instance.CaptureState();
            var scope = new Scope(onEscape);
            Scopes.Add(scope);
            _instance.ApplyModalState();
            return scope;
        }

        public static bool TryConsumeEscape()
        {
            if (EscapeConsumed || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return false;
            _escapeFrame = Time.frameCount;
            return true;
        }

        public static void ReleaseAll()
        {
            if (Scopes.Count == 0) return;
            foreach (var scope in Scopes) scope.Released = true;
            Scopes.Clear();
            if (_instance != null) _instance.RestoreState();
        }

        private void CaptureState()
        {
            _previousTimeScale = Time.timeScale;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _player = FindFirstObjectByType<FirstPersonController>();
            _playerWasEnabled = _player != null && _player.enabled;
        }

        private void ApplyModalState()
        {
            Time.timeScale = 0f;
            if (_player != null) _player.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreState()
        {
            Time.timeScale = _previousTimeScale;
            if (_player != null) _player.enabled = _playerWasEnabled;
            bool keepFree = SceneManager.GetActiveScene().name == "StartScene" ||
                StorySystem.StoryDirector.IsStoryPlaybackActive || MobileInputManager.IsRuntimeMobileDevice();
            Cursor.lockState = keepFree ? CursorLockMode.None : _previousCursorLock;
            Cursor.visible = keepFree || _previousCursorVisible;
            _releaseFrame = Time.frameCount + 1;
        }

        private void Update()
        {
            if (!IsModalOpen) return;
            _pausedSeconds += Time.unscaledDeltaTime;
            var top = Scopes[Scopes.Count - 1];
            if (top.OnEscape != null && TryConsumeEscape()) top.OnEscape();
        }

        private void LateUpdate()
        {
            if (IsModalOpen) ApplyModalState();
        }

        public sealed class Scope : IDisposable
        {
            internal readonly Action OnEscape;
            internal bool Released;

            internal Scope(Action onEscape) { OnEscape = onEscape; }

            public void Dispose()
            {
                if (Released) return;
                Released = true;
                Scopes.Remove(this);
                if (Scopes.Count == 0 && _instance != null) _instance.RestoreState();
            }
        }
    }
}
