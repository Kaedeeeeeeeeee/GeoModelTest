using System;
using System.Collections;
using System.Collections.Generic;
using GeoModel.AudioSystem;
using UnityEngine;
using UnityEngine.Networking;

namespace StorySystem
{
    /// <summary>
    /// Plays the recorded voice of a story line. Files live in StreamingAssets/Voice/&lt;lang&gt;/&lt;textKey&gt;.mp3
    /// next to a manifest.json that lists the keys, so the browser only downloads lines that exist and
    /// only when they are spoken. Without a manifest the game stays silent and nothing else changes.
    /// Turn off with ?voice=off in the page URL, or with the voice volume slider.
    /// </summary>
    public static class StoryVoice
    {
        public const string PrefsKey = "story.voice.enabled";
        private const string Folder = "Voice";

        [Serializable] private class Manifest { public List<string> keys; }

        /// <summary>Downloads run here, not on StoryDirectorRunner: cancelling story playback (back to title)
        /// stops that runner's coroutines and would leave a manifest or clip stuck as "loading".</summary>
        private sealed class Host : MonoBehaviour
        {
            private static Host _instance;

            public static void Run(IEnumerator routine)
            {
                if (_instance == null)
                {
                    var go = new GameObject("StoryVoiceHost");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Host>();
                }
                _instance.StartCoroutine(routine);
            }
        }

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> Loading = new HashSet<string>();
        private static HashSet<string> _available;
        private static bool _manifestRequested;
        private static string _manifestLanguage;
        private static string _wanted;

        /// <summary>False when the URL or the player turned voices off.</summary>
        public static bool Enabled => !UrlTurnsVoiceOff(SafeAbsoluteUrl()) && PlayerPrefs.GetInt(PrefsKey, 1) != 0;

        /// <summary>True once the manifest for the current language has loaded and lists at least one line.</summary>
        public static bool Available => _available != null && _available.Count > 0;

        public static bool UrlTurnsVoiceOff(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            int q = url.IndexOf('?');
            if (q < 0) return false;
            foreach (var part in url.Substring(q + 1).Split('&', '#'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0].Equals("voice", StringComparison.OrdinalIgnoreCase) &&
                    (kv[1].Equals("off", StringComparison.OrdinalIgnoreCase) || kv[1] == "0"))
                    return true;
            }
            return false;
        }

        public static void Play(string key)
        {
            Stop();
            _wanted = key;
            if (string.IsNullOrEmpty(key) || !Enabled) return;
            EnsureManifest();
            if (_available == null || !_available.Contains(key)) return;
            if (Cache.TryGetValue(key, out var clip))
            {
                if (clip != null) AudioManager.Instance?.PlayVoice(clip);
                return;
            }
            Load(key);
        }

        /// <summary>Starts downloading lines that come next, so they play without a gap.</summary>
        public static void Prefetch(string key)
        {
            if (string.IsNullOrEmpty(key) || !Enabled || _available == null || !_available.Contains(key)) return;
            if (Cache.ContainsKey(key)) return;
            Load(key);
        }

        public static void Stop()
        {
            _wanted = null;
            AudioManager.Instance?.StopVoice();
        }

        private static string Language()
        {
            var lm = LocalizationManager.Instance;
            return lm != null ? LanguageSettings.GetLanguageCode(lm.CurrentLanguage) : "ja-JP";
        }

        private static string Url(string language, string file)
        {
            string path = Application.streamingAssetsPath + "/" + Folder + "/" + language + "/" + file;
            return path.Contains("://") ? path : "file://" + path;
        }

        private static string SafeAbsoluteUrl()
        {
            try { return Application.absoluteURL; }
            catch { return null; }
        }

        private static void EnsureManifest()
        {
            string language = Language();
            if (_manifestRequested && _manifestLanguage == language) return;
            _manifestRequested = true;
            _manifestLanguage = language;
            _available = null;
            Cache.Clear();
            Host.Run(LoadManifest(language));
        }

        private static IEnumerator LoadManifest(string language)
        {
            using (var request = UnityWebRequest.Get(Url(language, "manifest.json")))
            {
                yield return request.SendWebRequest();
                if (language != _manifestLanguage) yield break;
                var keys = new HashSet<string>();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var manifest = JsonUtility.FromJson<Manifest>(request.downloadHandler.text);
                        if (manifest?.keys != null) keys.UnionWith(manifest.keys);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[StoryVoice] manifest.json could not be read: " + e.Message);
                    }
                }
                _available = keys;
                // 通信エラー（404 以外）なら、次の台詞でもう一度 manifest を取りに行く
                if (request.result != UnityWebRequest.Result.Success && request.responseCode != 404) _manifestRequested = false;
                Backend.TelemetryClient.Instance?.Track("voice_status", new Dictionary<string, object>
                {
                    ["language"] = language,
                    ["enabled"] = Enabled,
                    ["lines"] = keys.Count,
                    ["volume"] = AudioManager.Instance != null ? AudioManager.Instance.GetVolume(AudioChannel.Voice) : -1f
                });
                // The first line of the game asks before the manifest arrives.
                if (_wanted != null && keys.Contains(_wanted)) Load(_wanted);
            }
        }

        private static void Load(string key)
        {
            if (!Loading.Add(key)) return;
            Host.Run(LoadClip(_manifestLanguage, key));
        }

        private static IEnumerator LoadClip(string language, string key)
        {
            using (var request = UnityWebRequestMultimedia.GetAudioClip(Url(language, key + ".mp3"), AudioType.MPEG))
            {
                yield return request.SendWebRequest();
                Loading.Remove(key);
                if (language != _manifestLanguage) yield break;
                AudioClip clip = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null) clip.name = key;
                }
                if (clip != null) Cache[key] = clip; // 失敗した行は覚えず、次に話すときにもう一度取りに行く
                if (clip != null && _wanted == key) // 先読み中に話し始めた行も、届いたら流す
                    AudioManager.Instance?.PlayVoice(clip);
            }
        }
    }
}
