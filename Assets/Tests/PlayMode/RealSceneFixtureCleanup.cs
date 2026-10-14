using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Returns play mode to the state a real-scene fixture found. The game's scenes and the
/// DontDestroyOnLoad singletons they create would otherwise win singleton Awake and
/// FindFirstObjectByType in the synthetic fixtures that run later in the same suite.
/// </summary>
public sealed class RealSceneFixtureCleanup
{
    private readonly HashSet<GameObject> _persistentRootsBefore = new HashSet<GameObject>();

    public void Capture()
    {
        _persistentRootsBefore.Clear();
        foreach (var root in PersistentRoots()) _persistentRootsBefore.Add(root);
    }

    public IEnumerator Restore()
    {
        var empty = SceneManager.CreateScene("RealSceneFixtureCleanup_" + Time.frameCount);
        SceneManager.SetActiveScene(empty);
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene != empty && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        foreach (var root in PersistentRoots())
            if (!_persistentRootsBefore.Contains(root)) UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private static GameObject[] PersistentRoots()
    {
        var probe = new GameObject("RealSceneFixtureProbe");
        UnityEngine.Object.DontDestroyOnLoad(probe);
        var roots = probe.scene.GetRootGameObjects();
        UnityEngine.Object.DestroyImmediate(probe);
        return Array.FindAll(roots, root => root != null);
    }
}
