using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace WorkbenchSystem
{
    public class PressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float initialDelay = 0.25f;
        [SerializeField] private float repeatInterval = 0.08f;

        private UnityAction onRepeat;
        private bool isHeld;
        private float nextFireTime;

        public void Initialize(UnityAction action, float delaySeconds, float intervalSeconds)
        {
            onRepeat = action;
            initialDelay = Mathf.Max(0f, delaySeconds);
            repeatInterval = Mathf.Max(0.01f, intervalSeconds);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHeld = true;
            nextFireTime = Time.unscaledTime + initialDelay;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHeld = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHeld = false;
        }

        void Update()
        {
            if (!isHeld || onRepeat == null) return;

            if (Time.unscaledTime >= nextFireTime)
            {
                onRepeat.Invoke();
                nextFireTime = Time.unscaledTime + repeatInterval;
            }
        }

        void OnDisable()
        {
            isHeld = false;
        }
    }
}
