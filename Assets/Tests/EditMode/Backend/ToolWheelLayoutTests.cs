using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ToolWheelLayoutTests
{
    [TestCase(4, 0f, 0)]
    [TestCase(4, 44f, 0)]
    [TestCase(4, 46f, 1)]
    [TestCase(4, 90f, 1)]
    [TestCase(4, 134f, 1)]
    [TestCase(4, 136f, 2)]
    [TestCase(4, 180f, 2)]
    [TestCase(4, 226f, 3)]
    [TestCase(4, 270f, 3)]
    [TestCase(4, 316f, 0)]
    [TestCase(4, 359f, 0)]
    [TestCase(4, 45f, 1)]
    [TestCase(4, 315f, 0)]
    [TestCase(1, 0f, 0)]
    [TestCase(1, 180f, 0)]
    [TestCase(1, 359f, 0)]
    [TestCase(3, 59.9f, 0)]
    [TestCase(3, 60f, 1)]
    [TestCase(3, 179.9f, 1)]
    [TestCase(3, 180f, 2)]
    [TestCase(3, 299.9f, 2)]
    [TestCase(3, 300f, 0)]
    [TestCase(8, 22.4f, 0)]
    [TestCase(8, 22.5f, 1)]
    [TestCase(8, 67.5f, 2)]
    [TestCase(8, 337.4f, 7)]
    [TestCase(8, 337.5f, 0)]
    [TestCase(8, -45f, 7)]
    [TestCase(8, 405f, 1)]
    public void Angle_ShouldSelectSectorCenteredOnTeachingDirection(int count, float angle, int expected)
    {
        Assert.AreEqual(expected, Select(angle, count, 150f, 100f));
    }

    [TestCase(0f)]
    [TestCase(99f)]
    [TestCase(100f)]
    public void DeadZone_ShouldNeverSelect(float distance)
    {
        foreach (int count in new[] { 1, 3, 4, 8 })
            Assert.AreEqual(-1, Select(90f, count, distance, 100f));
    }

    [Test]
    public void EmptyWheel_ShouldNotSelect()
    {
        Assert.AreEqual(-1, Select(0f, 0, 150f, 100f));
    }

    [Test]
    public void SectorGraphic_ShouldGenerateSmoothRingMeshWithItsRequiredRenderer()
    {
        var host = new GameObject("SectorMeshTest", typeof(RectTransform));
        try
        {
            var graphic = (Graphic)host.AddComponent(BackendTestReflection.GetType("UISystem.WheelSectorGraphic"));
            Assert.NotNull(host.GetComponent<CanvasRenderer>());
            Assert.IsFalse((bool)BackendTestReflection.GetProperty(graphic, "useLegacyMeshGeneration"));
            BackendTestReflection.InvokeInstance(graphic, "Configure", -45f, 90f, 60f, 250f, 1f);
            using (var vertices = new VertexHelper())
            {
                graphic.GetType().GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(graphic, new object[] { vertices });
                Assert.AreEqual(184, vertices.currentVertCount);
                var vertex = new UIVertex();
                vertices.PopulateUIVertex(ref vertex, 0);
                Assert.AreEqual(0, vertex.color.a, "The inner edge feathers into the dead zone.");
                vertices.PopulateUIVertex(ref vertex, 1);
                Assert.AreEqual(255, vertex.color.a, "The sector interior remains opaque.");
                vertices.PopulateUIVertex(ref vertex, 183);
                Assert.AreEqual(0, vertex.color.a, "The outer edge is anti-aliased.");
            }
        }
        finally { Object.DestroyImmediate(host); }
    }

    private static int Select(float angle, int count, float distance, float deadRadius) =>
        (int)BackendTestReflection.InvokeStatic(BackendTestReflection.GetType("UISystem.ToolWheelLayout"),
            "SectorAtAngle", angle, count, distance, deadRadius);
}
