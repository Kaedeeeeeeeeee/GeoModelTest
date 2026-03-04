using UnityEngine;

public static class RuntimeLogFilter
{
    private const string InfoLogPrefKey = "GeoModel.InfoLogsEnabled";
    private static bool? cachedInfoLogEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyFilter()
    {
        SetFilter(IsInfoLoggingEnabled());
    }

    public static bool IsInfoLoggingEnabled()
    {
        if (!cachedInfoLogEnabled.HasValue)
        {
            cachedInfoLogEnabled = PlayerPrefs.GetInt(InfoLogPrefKey, 0) == 1;
        }

        return cachedInfoLogEnabled.Value;
    }

    public static void SetInfoLoggingEnabled(bool enabled)
    {
        cachedInfoLogEnabled = enabled;
        PlayerPrefs.SetInt(InfoLogPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        SetFilter(enabled);
    }

    private static void SetFilter(bool infoLogsEnabled)
    {
        Debug.unityLogger.filterLogType = infoLogsEnabled ? LogType.Log : LogType.Warning;
    }
}
