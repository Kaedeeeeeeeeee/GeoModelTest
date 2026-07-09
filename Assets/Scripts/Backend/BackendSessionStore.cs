using System;
using UnityEngine;

namespace Backend
{
    public static class BackendSessionStore
    {
        public const string InstallIdKey = "Backend.InstallId";
        public const string AccessTokenKey = "Backend.AccessToken";
        public const string RefreshTokenKey = "Backend.RefreshToken";
        public const string UserIdKey = "Backend.UserId";
        public const string AccessTokenExpiresAtKey = "Backend.AccessTokenExpiresAtUnix";

        public static string GetOrCreateInstallId()
        {
            string installId = PlayerPrefs.GetString(InstallIdKey, "");
            if (Guid.TryParse(installId, out _))
            {
                return installId;
            }

            installId = Guid.NewGuid().ToString("D");
            PlayerPrefs.SetString(InstallIdKey, installId);
            PlayerPrefs.Save();
            return installId;
        }

        public static string CreateSessionId()
        {
            return Guid.NewGuid().ToString("D");
        }

        public static void SaveAuthSession(BackendAuthResponse authResponse)
        {
            if (authResponse == null || string.IsNullOrEmpty(authResponse.accessToken))
            {
                return;
            }

            PlayerPrefs.SetString(AccessTokenKey, authResponse.accessToken);

            if (!string.IsNullOrEmpty(authResponse.refreshToken))
            {
                PlayerPrefs.SetString(RefreshTokenKey, authResponse.refreshToken);
            }

            if (authResponse.user != null && !string.IsNullOrEmpty(authResponse.user.id))
            {
                PlayerPrefs.SetString(UserIdKey, authResponse.user.id);
            }

            int expiresIn = authResponse.expiresIn > 0 ? authResponse.expiresIn : 3600;
            long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
            PlayerPrefs.SetString(AccessTokenExpiresAtKey, expiresAt.ToString());
            PlayerPrefs.Save();
        }

        public static bool TryGetValidAccessToken(out string accessToken)
        {
            accessToken = PlayerPrefs.GetString(AccessTokenKey, "");
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            string rawExpiresAt = PlayerPrefs.GetString(AccessTokenExpiresAtKey, "0");
            if (!long.TryParse(rawExpiresAt, out long expiresAt))
            {
                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return expiresAt - now > 90;
        }

        public static bool TryGetRefreshToken(out string refreshToken)
        {
            refreshToken = PlayerPrefs.GetString(RefreshTokenKey, "");
            return !string.IsNullOrEmpty(refreshToken);
        }

        public static void ClearAuthSession()
        {
            PlayerPrefs.DeleteKey(AccessTokenKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.DeleteKey(UserIdKey);
            PlayerPrefs.DeleteKey(AccessTokenExpiresAtKey);
            PlayerPrefs.Save();
        }
    }
}
