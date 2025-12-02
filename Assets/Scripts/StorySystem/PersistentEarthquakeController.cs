using System.Collections;
using Core;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Keeps a looping camera shake active until explicitly stopped or the lab scene loads.
    /// </summary>
    public class PersistentEarthquakeController : MonoBehaviour
    {
        private static PersistentEarthquakeController _instance;
        public static PersistentEarthquakeController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PersistentEarthquakeController");
                    _instance = go.AddComponent<PersistentEarthquakeController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [SerializeField] private float shakeDuration = 0.9f;
        [SerializeField] private float shakeAmplitude = 0.6f;
        [SerializeField] private float pauseBetweenShakes = 0.15f;

        private Coroutine earthquakeLoop;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            GameEventBus.SceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                GameEventBus.SceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(string sceneName)
        {
            if (sceneName == "Laboratory Scene")
            {
                StopEarthquake();
            }
        }

        public void StartEarthquake()
        {
            if (earthquakeLoop != null)
            {
                return;
            }
            earthquakeLoop = StartCoroutine(EarthquakeRoutine());
        }

        public void StopEarthquake()
        {
            if (earthquakeLoop == null) return;
            StopCoroutine(earthquakeLoop);
            earthquakeLoop = null;
        }

        private IEnumerator EarthquakeRoutine()
        {
            while (true)
            {
                yield return StoryDirector.CameraShakeAction.Execute(shakeDuration, shakeAmplitude);
                yield return new WaitForSeconds(pauseBetweenShakes);
            }
        }
    }
}
