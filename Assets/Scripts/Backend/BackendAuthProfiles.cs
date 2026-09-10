using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Backend
{
    /// <summary>Keep each participation code's anonymous identity separate on a shared device.</summary>
    public static class BackendAuthProfiles
    {
        public const string CurrentCodeKey = "Backend.CurrentCodeHash.v1";
        private const string Prefix = "Backend.AuthProfile.v1.";
        private const string VerifiedKey = "Backend.CurrentCodeVerified.v1";
        private const string LastVerifiedCodeKey = "Backend.LastVerifiedCodeHash.v1";
        [Serializable]
        private sealed class Profile
        {
            public string accessToken, refreshToken, userId, expiresAt;
        }
        public static string CodeHash(string server, string code)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server.TrimEnd('/') + "|" + code.Trim().ToUpperInvariant()))).Replace("-", "").ToLowerInvariant();
        }
        public static bool IsCurrentCode(string server, string code) => PlayerPrefs.GetString(CurrentCodeKey, "") == CodeHash(server, code);

        public static bool SelectCode(string server, string code, out bool changed)
        {
            string target = CodeHash(server, code);
            string previous = PlayerPrefs.GetString(CurrentCodeKey, "");
            string lastVerified = PlayerPrefs.GetString(LastVerifiedCodeKey, "");
            changed = lastVerified.Length > 0 && lastVerified != target;
            if (previous.Length == 0 || previous == target)
            {
                PlayerPrefs.SetString(CurrentCodeKey, target);
                return true;
            }
            if (new TelemetryQueue(1000).Count > 0 || PlayerPrefs.HasKey(TelemetryClient.PendingSessionEndPrefsKey)) return false;
            CacheVerifiedIdentity();
            BackendSessionStore.ClearAuthSession();
            BackendSessionStore.ClearResearchContext();
            PlayerPrefs.SetString(CurrentCodeKey, target);
            PlayerPrefs.SetInt(VerifiedKey, 0);
            try
            {
                var profile = JsonUtility.FromJson<Profile>(PlayerPrefs.GetString(Prefix + target, ""));
                if (profile != null)
                {
                    PlayerPrefs.SetString(BackendSessionStore.AccessTokenKey, profile.accessToken ?? "");
                    PlayerPrefs.SetString(BackendSessionStore.RefreshTokenKey, profile.refreshToken ?? "");
                    PlayerPrefs.SetString(BackendSessionStore.UserIdKey, profile.userId ?? "");
                    PlayerPrefs.SetString(BackendSessionStore.AccessTokenExpiresAtKey, profile.expiresAt ?? "0");
                    PlayerPrefs.SetInt(VerifiedKey, 1);
                }
            }
            catch { Debug.LogWarning("[BackendAuthProfiles] Saved identity could not be read."); }
            PlayerPrefs.Save();
            return true;
        }
        public static void MarkVerified()
        {
            PlayerPrefs.SetInt(VerifiedKey, 1);
            PlayerPrefs.SetString(LastVerifiedCodeKey, PlayerPrefs.GetString(CurrentCodeKey, ""));
            CacheVerifiedIdentity();
        }
        private static void CacheVerifiedIdentity()
        {
            string key = PlayerPrefs.GetString(CurrentCodeKey, "");
            if (key.Length == 0 || PlayerPrefs.GetInt(VerifiedKey, 0) == 0) return;
            var profile = new Profile
            {
                accessToken = PlayerPrefs.GetString(BackendSessionStore.AccessTokenKey, ""),
                refreshToken = PlayerPrefs.GetString(BackendSessionStore.RefreshTokenKey, ""),
                userId = PlayerPrefs.GetString(BackendSessionStore.UserIdKey, ""),
                expiresAt = PlayerPrefs.GetString(BackendSessionStore.AccessTokenExpiresAtKey, "0")
            };
            if (string.IsNullOrEmpty(profile.userId)) return;
            PlayerPrefs.SetString(Prefix + key, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }
    }
}
