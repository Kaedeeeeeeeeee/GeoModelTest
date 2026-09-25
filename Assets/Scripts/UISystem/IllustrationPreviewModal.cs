using Core;
using UnityEngine;

namespace UISystem
{
    /// <summary>Owns the preview's input scope even when its parent dialogue is destroyed.</summary>
    public sealed class IllustrationPreviewModal : MonoBehaviour
    {
        private GameInputState.Scope _input;

        private void OnEnable()
        {
            _input = GameInputState.Acquire(Close);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            _input?.Dispose();
            _input = null;
        }
    }
}
