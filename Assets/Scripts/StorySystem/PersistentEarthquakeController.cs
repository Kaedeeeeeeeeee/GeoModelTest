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
        [Header("Audio")]
        [SerializeField] private AudioClip earthquakeClip;
        [SerializeField] private string earthquakeAudioResourcePath = "Audio/Earthquake/quake_loop";
        [SerializeField] private bool loopEarthquakeAudio = true;
        [SerializeField, Range(0f, 1f)] private float earthquakeVolume = 0.8f;

        private Coroutine earthquakeLoop;
        private AudioSource audioSource;
        private bool attemptedClipLoad;

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
            StartEarthquakeAudio();
            earthquakeLoop = StartCoroutine(EarthquakeRoutine());
        }

        public void StopEarthquake()
        {
            if (earthquakeLoop == null) return;
            StopCoroutine(earthquakeLoop);
            earthquakeLoop = null;
            StopEarthquakeAudio();
        }

        private IEnumerator EarthquakeRoutine()
        {
            while (true)
            {
                if (!loopEarthquakeAudio)
                {
                    PlayEarthquakeSfxOneShot();
                }
                yield return StoryDirector.CameraShakeAction.Execute(shakeDuration, shakeAmplitude);
                yield return new WaitForSeconds(pauseBetweenShakes);
            }
        }

        public void PlayEarthquakeSfxOneShot()
        {
            EnsureAudioSource();
            EnsureAudioListener();
            ResolveEarthquakeClip();
            if (audioSource == null || earthquakeClip == null) return;
            audioSource.PlayOneShot(earthquakeClip, earthquakeVolume);
        }

        public void PlayEarthquakeSfxOneShot(float volumeScale)
        {
            EnsureAudioSource();
            EnsureAudioListener();
            ResolveEarthquakeClip();
            if (audioSource == null || earthquakeClip == null) return;
            float scaledVolume = Mathf.Clamp01(earthquakeVolume * Mathf.Clamp01(volumeScale));
            audioSource.PlayOneShot(earthquakeClip, scaledVolume);
        }

        private void StartEarthquakeAudio()
        {
            if (!loopEarthquakeAudio) return;
            EnsureAudioSource();
            EnsureAudioListener();
            ResolveEarthquakeClip();
            if (audioSource == null || earthquakeClip == null) return;
            audioSource.loop = true;
            audioSource.clip = earthquakeClip;
            audioSource.volume = earthquakeVolume;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void StopEarthquakeAudio()
        {
            if (audioSource == null) return;
            if (audioSource.clip == earthquakeClip)
            {
                audioSource.Stop();
            }
            audioSource.loop = false;
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null) return;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        private void EnsureAudioListener()
        {
            var existing = Object.FindFirstObjectByType<AudioListener>();
            if (existing != null) return;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                return;
            }

            var anyCam = Object.FindFirstObjectByType<Camera>();
            if (anyCam != null)
            {
                anyCam.gameObject.AddComponent<AudioListener>();
                return;
            }

            Debug.LogWarning("[PersistentEarthquakeController] 未找到相机，无法自动添加AudioListener。");
        }

        private void ResolveEarthquakeClip()
        {
            if (earthquakeClip != null) return;
            if (attemptedClipLoad) return;
            if (string.IsNullOrWhiteSpace(earthquakeAudioResourcePath)) return;
            attemptedClipLoad = true;
            earthquakeClip = Resources.Load<AudioClip>(earthquakeAudioResourcePath);
            if (earthquakeClip == null)
            {
                Debug.LogWarning($"[PersistentEarthquakeController] 未找到地震音效: {earthquakeAudioResourcePath}");
            }
        }
    }
}
