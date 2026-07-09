using UnityEngine;

namespace Backend
{
    public static class BackendSettingsProvider
    {
        private const string ResourcePath = "BackendSettings";

        public static BackendSettings Load()
        {
            return Resources.Load<BackendSettings>(ResourcePath);
        }
    }
}
