using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Exports the last build's packed asset sizes for download-size audits.</summary>
public static class WebGLAssetReport
{
    [Serializable]
    private class AssetEntry
    {
        public string path;
        public long bytes;
    }

    [Serializable]
    private class AssetReport
    {
        public AssetEntry[] assets;
    }

    [MenuItem("Tools/WebGL/Export Asset Size Report")]
    public static void Export()
    {
        var report = BuildReport.GetLatestReport();
        if (report == null)
        {
            throw new InvalidOperationException("No previous build report is available.");
        }

        var output = new AssetReport
        {
            assets = report.packedAssets.SelectMany(pack => pack.contents)
                .GroupBy(asset => asset.sourceAssetPath)
                .Select(group => new AssetEntry
                {
                    path = group.Key,
                    bytes = group.Sum(asset => (long)asset.packedSize)
                })
                .OrderByDescending(asset => asset.bytes).ToArray()
        };
        Directory.CreateDirectory("Logs/webgl");
        File.WriteAllText("Logs/webgl/asset-sizes.json", JsonUtility.ToJson(output, true));
        Debug.Log($"[WebGLAssetReport] Exported {output.assets.Length} assets to Logs/webgl/asset-sizes.json");
    }
}
