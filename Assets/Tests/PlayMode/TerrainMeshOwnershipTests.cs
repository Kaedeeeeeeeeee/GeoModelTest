using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class TerrainMeshOwnershipTests
{
    private GameObject _terrain;
    private Mesh _sourceMesh;
    private Material _holeMaterial;
    private string _meshName;
    private Vector3[] _sourceVertices;
    private int[] _sourceTriangles;

    [SetUp]
    public void SetUp()
    {
        _meshName = "TerrainOwnershipTest_" + Guid.NewGuid().ToString("N");
        _sourceMesh = new Mesh
        {
            name = _meshName,
            vertices = new[]
            {
                new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f),
                new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f),
                new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f)
            },
            triangles = new[]
            {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2
            }
        };
        _sourceMesh.RecalculateBounds();
        _sourceMesh.RecalculateNormals();
        _sourceVertices = _sourceMesh.vertices;
        _sourceTriangles = _sourceMesh.triangles;

        _terrain = new GameObject("TerrainOwnershipFixture", typeof(MeshFilter), typeof(MeshRenderer));
        _terrain.GetComponent<MeshFilter>().sharedMesh = _sourceMesh;
        _holeMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
    }

    [UnityTest]
    public IEnumerator Startup_Should_KeepSharedTerrainMesh_WhenBothSystemsInitialize()
    {
        MeshCollider collider = _terrain.AddComponent<MeshCollider>();
        collider.sharedMesh = _sourceMesh;
        AddSystem("TerrainHoleSystem");
        Component layer = AddSystem("GeologyLayer");

        yield return null;

        AssertSharedMeshUnchanged();
        Assert.AreSame(_sourceMesh, collider.sharedMesh);
        Assert.AreEqual(Vector3.up, Invoke(layer, "GetNormalAtPoint", Vector3.up * 2f));
        AssertSurfaceHit(collider);

        Object.Destroy(_terrain);
        yield return null;

        Assert.IsTrue(_sourceMesh != null, "Destroying a terrain must not destroy its shared source mesh.");
        AssertSourceDataUnchanged();
        Assert.AreEqual(1, CountFixtureMeshes(), "Initialization must not leave orphaned mesh copies.");
    }

    [UnityTest]
    public IEnumerator Holes_Should_ClearVisualsWithoutCopyingTerrain_WhenRepeatedlyCreatedAndRemoved()
    {
        MeshCollider collider = _terrain.AddComponent<MeshCollider>();
        collider.sharedMesh = _sourceMesh;
        Component holes = AddSystem("TerrainHoleSystem");
        holes.GetType().GetField("holeMaterial").SetValue(holes, _holeMaterial);
        yield return null;

        for (int cycle = 0; cycle < 3; cycle++)
        {
            Invoke(holes, "CreateCylindricalHole", Vector3.up, 0.25f, 0.5f, Vector3.up);
            Assert.AreEqual(1, _terrain.transform.childCount);
            Transform visual = _terrain.transform.GetChild(0);
            Assert.AreEqual(Vector3.up, visual.position);
            Assert.AreEqual(1, visual.childCount);
            Assert.IsNotNull(visual.GetChild(0).GetComponent<MeshRenderer>());
            Assert.IsTrue((bool)Invoke(holes, "HasHoleAt", Vector3.up, 0f));
            Assert.IsFalse((bool)Invoke(holes, "HasHoleAt", Vector3.right * 5f, 0f));
            AssertSharedMeshUnchanged();
            AssertSurfaceHit(collider);

            Invoke(holes, "RemoveAllHoles");
            yield return null;

            Assert.AreEqual(0, _terrain.transform.childCount, "Clearing holes must remove their visual containers.");
            Assert.AreEqual(0, ((IList)holes.GetType().GetField("holes").GetValue(holes)).Count);
            Assert.IsFalse((bool)Invoke(holes, "HasHoleAt", Vector3.up, 0f));
            AssertSharedMeshUnchanged();
            Assert.AreSame(_sourceMesh, collider.sharedMesh);
            AssertSurfaceHit(collider);
        }

        Invoke(holes, "RemoveAllHoles");
        AssertSharedMeshUnchanged();
    }

    [UnityTest]
    public IEnumerator Bounds_Should_ContainTransformedPointsWithoutCopyingMesh_WhenColliderIsAbsent()
    {
        _terrain.transform.position = new Vector3(3f, 4f, 5f);
        _terrain.transform.localScale = new Vector3(2f, 3f, 4f);
        Component layer = AddSystem("GeologyLayer");
        yield return null;

        Assert.IsTrue((bool)Invoke(layer, "ContainsPoint", new Vector3(4.9f, 6.9f, 8.9f)));
        Assert.IsFalse((bool)Invoke(layer, "ContainsPoint", new Vector3(5.1f, 4f, 5f)));
        AssertSharedMeshUnchanged();
    }

    private Component AddSystem(string typeName)
    {
        return _terrain.AddComponent(Type.GetType(typeName + ", Assembly-CSharp", true));
    }

    private static object Invoke(Component target, string method, params object[] arguments)
    {
        return target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
            .Invoke(target, arguments);
    }

    private void AssertSharedMeshUnchanged()
    {
        Assert.AreSame(_sourceMesh, _terrain.GetComponent<MeshFilter>().sharedMesh);
        Assert.AreEqual(1, CountFixtureMeshes(), "Read-only terrain operations must not allocate mesh copies.");
        AssertSourceDataUnchanged();
    }

    private void AssertSourceDataUnchanged()
    {
        CollectionAssert.AreEqual(_sourceVertices, _sourceMesh.vertices);
        CollectionAssert.AreEqual(_sourceTriangles, _sourceMesh.triangles);
    }

    private int CountFixtureMeshes()
    {
        int count = 0;
        foreach (Mesh mesh in Resources.FindObjectsOfTypeAll<Mesh>())
        {
            if (mesh.name.StartsWith(_meshName, StringComparison.Ordinal))
            {
                count++;
            }
        }
        return count;
    }

    private static void AssertSurfaceHit(MeshCollider collider)
    {
        Physics.SyncTransforms();
        Assert.IsTrue(collider.Raycast(new Ray(Vector3.up * 3f, Vector3.down), out RaycastHit hit, 5f));
        Assert.That(hit.point.y, Is.EqualTo(1f).Within(0.001f));
        Assert.That(hit.normal.y, Is.EqualTo(1f).Within(0.001f));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_terrain != null)
        {
            Object.Destroy(_terrain);
        }
        if (_holeMaterial != null)
        {
            Object.Destroy(_holeMaterial);
        }
        yield return null;

        // Also clean up accidental clones if the ownership regression causes a test to fail.
        foreach (Mesh mesh in Resources.FindObjectsOfTypeAll<Mesh>())
        {
            if (mesh.name.StartsWith(_meshName, StringComparison.Ordinal))
            {
                Object.Destroy(mesh);
            }
        }
        yield return null;
    }
}
