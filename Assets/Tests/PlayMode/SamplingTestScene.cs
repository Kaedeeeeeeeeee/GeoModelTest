using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Synthetic physics tests must not unload the Test Runner's own scene.
internal sealed class SamplingTestScene
{
    private readonly HashSet<GameObject> _persistentBefore = new HashSet<GameObject>();
    private Scene _previous;
    private Scene _scene;

    public void Capture()
    {
        _persistentBefore.Clear();
        foreach (var root in PersistentRoots()) _persistentBefore.Add(root);
        _previous = SceneManager.GetActiveScene();
        _scene = SceneManager.CreateScene("SamplingRegression_" + Time.frameCount);
        SceneManager.SetActiveScene(_scene);
    }

    public IEnumerator Restore()
    {
        if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
        if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
        foreach (var root in PersistentRoots())
            if (root != null && !_persistentBefore.Contains(root)) Object.Destroy(root);
        yield return null;
    }

    private static GameObject[] PersistentRoots()
    {
        var probe = new GameObject("SamplingRegressionPersistentProbe");
        Object.DontDestroyOnLoad(probe);
        var roots = probe.scene.GetRootGameObjects();
        Object.DestroyImmediate(probe);
        return System.Array.FindAll(roots, root => root != null);
    }
}
