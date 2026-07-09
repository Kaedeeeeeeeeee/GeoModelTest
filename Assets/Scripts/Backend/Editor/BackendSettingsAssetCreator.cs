#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Backend.Editor
{
    public static class BackendSettingsAssetCreator
    {
        private const string ResourcesPath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/BackendSettings.asset";

        [MenuItem("Tools/Backend/Create Supabase Backend Settings")]
        public static void CreateOrSelectSettings()
        {
            Directory.CreateDirectory(ResourcesPath);

            var existing = AssetDatabase.LoadAssetAtPath<BackendSettings>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var settings = ScriptableObject.CreateInstance<BackendSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static void ConfigureFromEnvironment()
        {
            string supabaseUrl = System.Environment.GetEnvironmentVariable("GEOMODEL_SUPABASE_URL");
            string publishableKey = System.Environment.GetEnvironmentVariable("GEOMODEL_SUPABASE_PUBLISHABLE_KEY");

            if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(publishableKey))
            {
                Debug.LogError("[BackendSettingsAssetCreator] Missing GEOMODEL_SUPABASE_URL or GEOMODEL_SUPABASE_PUBLISHABLE_KEY");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(ResourcesPath);
            var settings = AssetDatabase.LoadAssetAtPath<BackendSettings>(AssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BackendSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("enableBackend").boolValue = true;
            serialized.FindProperty("supabaseUrl").stringValue = supabaseUrl.TrimEnd('/');
            serialized.FindProperty("publishableKey").stringValue = publishableKey;
            serialized.FindProperty("ingestFunctionName").stringValue = "game-ingest";
            serialized.FindProperty("flushIntervalSeconds").floatValue = 30f;
            serialized.FindProperty("maxBatchSize").intValue = 25;
            serialized.FindProperty("maxPersistedEvents").intValue = 100;
            serialized.FindProperty("verboseLogging").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[BackendSettingsAssetCreator] BackendSettings.asset configured from environment");
        }
    }
}
#endif
