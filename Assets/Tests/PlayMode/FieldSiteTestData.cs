using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Test-only camera aim points and walking corridors for the real MainScene outcrops.</summary>
public static class FieldSiteTestData
{
    public static readonly Vector3[] HammerFacePoints =
    {
        new Vector3(124.2963f, 20.8598f, 151.3010f),
        new Vector3(144.4155f, 24.7019f, 159.6828f),
        new Vector3(156.0823f, 29.2901f, 127.0865f)
    };

    public static readonly Vector3[][] WalkingPaths =
    {
        new Vector3[]
        {
            new Vector3(110.0000f, 17.0028f, 168.5000f),
            new Vector3(110.5000f, 17.0921f, 168.0000f),
            new Vector3(111.0000f, 17.3301f, 167.5000f),
            new Vector3(111.5000f, 17.3362f, 167.0000f),
            new Vector3(112.0000f, 17.4581f, 166.5000f),
            new Vector3(112.5000f, 17.6356f, 166.0000f),
            new Vector3(113.0000f, 17.6917f, 165.5000f),
            new Vector3(113.5000f, 17.8322f, 165.0000f),
            new Vector3(114.0000f, 17.8741f, 164.5000f),
            new Vector3(114.5000f, 17.9470f, 164.0000f),
            new Vector3(115.0000f, 18.0264f, 163.5000f),
            new Vector3(115.5000f, 18.1323f, 163.0000f),
            new Vector3(116.0000f, 18.2321f, 162.5000f),
            new Vector3(116.5000f, 18.2998f, 162.0000f),
            new Vector3(117.0000f, 18.4079f, 161.5000f),
            new Vector3(117.5000f, 18.4692f, 161.0000f),
            new Vector3(118.0000f, 18.4445f, 160.5000f),
            new Vector3(118.5000f, 18.4594f, 160.0000f),
            new Vector3(119.0000f, 18.4857f, 159.5000f),
            new Vector3(119.5000f, 18.5712f, 159.0000f),
            new Vector3(120.0000f, 18.6538f, 158.5000f),
            new Vector3(120.5000f, 18.7269f, 158.0000f),
            new Vector3(121.0000f, 18.6975f, 157.5000f),
            new Vector3(121.5000f, 18.7689f, 157.0000f),
            new Vector3(122.0000f, 18.8821f, 156.5000f),
            new Vector3(122.5000f, 18.9162f, 156.0000f),
            new Vector3(123.0000f, 18.8934f, 155.5000f),
            new Vector3(123.5000f, 18.9308f, 155.0000f),
            new Vector3(124.0000f, 18.9968f, 154.5000f),
            new Vector3(124.5000f, 18.9956f, 154.0000f),
            new Vector3(124.5000f, 19.0258f, 153.5000f),
            new Vector3(125.0000f, 19.2307f, 153.0000f)
        },
        new Vector3[]
        {
            new Vector3(125.0000f, 19.2307f, 153.0000f),
            new Vector3(125.5000f, 19.0196f, 153.5000f),
            new Vector3(126.0000f, 19.0620f, 154.0000f),
            new Vector3(126.5000f, 19.1080f, 154.5000f),
            new Vector3(126.5000f, 19.2260f, 155.0000f),
            new Vector3(126.5000f, 19.4232f, 155.5000f),
            new Vector3(126.5000f, 19.6101f, 156.0000f),
            new Vector3(126.5000f, 19.8506f, 156.5000f),
            new Vector3(127.0000f, 20.3076f, 157.0000f),
            new Vector3(127.5000f, 20.5202f, 157.0000f),
            new Vector3(128.0000f, 20.6957f, 157.5000f),
            new Vector3(128.5000f, 20.8068f, 157.5000f),
            new Vector3(129.0000f, 20.9258f, 157.5000f),
            new Vector3(129.5000f, 21.0303f, 157.5000f),
            new Vector3(130.0000f, 21.1042f, 157.5000f),
            new Vector3(130.5000f, 21.2000f, 157.5000f),
            new Vector3(131.0000f, 21.3280f, 158.0000f),
            new Vector3(131.5000f, 21.4078f, 158.0000f),
            new Vector3(132.0000f, 21.4564f, 158.0000f),
            new Vector3(132.5000f, 21.5559f, 158.0000f),
            new Vector3(133.0000f, 21.6665f, 158.0000f),
            new Vector3(133.5000f, 21.7685f, 158.0000f),
            new Vector3(134.0000f, 21.8658f, 158.0000f),
            new Vector3(134.5000f, 21.9692f, 158.0000f),
            new Vector3(135.0000f, 22.0592f, 158.0000f),
            new Vector3(135.5000f, 22.1326f, 158.0000f),
            new Vector3(136.0000f, 22.2108f, 158.0000f),
            new Vector3(136.5000f, 22.2781f, 158.0000f),
            new Vector3(137.0000f, 22.3481f, 158.0000f),
            new Vector3(137.5000f, 22.4296f, 158.0000f),
            new Vector3(138.0000f, 22.5108f, 158.0000f),
            new Vector3(138.5000f, 22.5664f, 158.0000f),
            new Vector3(139.0000f, 22.6563f, 158.0000f),
            new Vector3(139.5000f, 22.7469f, 158.0000f),
            new Vector3(140.0000f, 22.8175f, 158.0000f),
            new Vector3(140.5000f, 22.8823f, 158.0000f),
            new Vector3(141.0000f, 22.9614f, 158.5000f),
            new Vector3(141.5000f, 23.0400f, 159.0000f),
            new Vector3(142.0000f, 23.1535f, 159.5000f),
            new Vector3(142.5000f, 23.3008f, 160.0000f),
            new Vector3(143.0000f, 23.4441f, 160.5000f)
        },
        new Vector3[]
        {
            new Vector3(143.0000f, 23.4441f, 160.5000f),
            new Vector3(143.0000f, 23.4961f, 160.0000f),
            new Vector3(142.5000f, 23.3529f, 159.5000f),
            new Vector3(142.5000f, 23.3714f, 159.0000f),
            new Vector3(142.5000f, 23.2584f, 158.5000f),
            new Vector3(142.5000f, 23.1608f, 158.0000f),
            new Vector3(142.5000f, 23.1221f, 157.5000f),
            new Vector3(142.5000f, 23.0842f, 157.0000f),
            new Vector3(142.5000f, 23.1226f, 156.5000f),
            new Vector3(142.5000f, 23.1359f, 156.0000f),
            new Vector3(142.5000f, 23.1213f, 155.5000f),
            new Vector3(143.0000f, 23.1095f, 155.0000f),
            new Vector3(143.5000f, 23.1001f, 154.5000f),
            new Vector3(144.0000f, 23.1072f, 154.0000f),
            new Vector3(144.5000f, 23.1709f, 153.5000f),
            new Vector3(145.0000f, 23.2191f, 153.0000f),
            new Vector3(145.0000f, 23.2038f, 152.5000f),
            new Vector3(145.5000f, 23.2228f, 152.0000f),
            new Vector3(146.0000f, 23.1489f, 151.5000f),
            new Vector3(146.5000f, 23.1312f, 151.0000f),
            new Vector3(147.0000f, 23.1857f, 150.5000f),
            new Vector3(147.5000f, 23.2271f, 150.0000f),
            new Vector3(147.5000f, 23.2909f, 149.5000f),
            new Vector3(147.5000f, 23.3528f, 149.0000f),
            new Vector3(148.0000f, 23.4726f, 148.5000f),
            new Vector3(148.5000f, 23.5760f, 148.0000f),
            new Vector3(149.0000f, 23.6674f, 147.5000f),
            new Vector3(149.5000f, 23.7497f, 147.0000f),
            new Vector3(150.0000f, 23.8017f, 146.5000f),
            new Vector3(150.5000f, 23.9322f, 146.0000f),
            new Vector3(151.0000f, 24.0654f, 145.5000f),
            new Vector3(151.0000f, 24.1388f, 145.0000f),
            new Vector3(151.0000f, 24.2367f, 144.5000f),
            new Vector3(151.0000f, 24.3388f, 144.0000f),
            new Vector3(151.0000f, 24.4357f, 143.5000f),
            new Vector3(151.0000f, 24.5317f, 143.0000f),
            new Vector3(151.0000f, 24.6429f, 142.5000f),
            new Vector3(151.0000f, 24.7515f, 142.0000f),
            new Vector3(151.0000f, 24.8595f, 141.5000f),
            new Vector3(151.0000f, 24.9602f, 141.0000f),
            new Vector3(151.0000f, 25.0638f, 140.5000f),
            new Vector3(151.5000f, 25.1926f, 140.0000f),
            new Vector3(152.0000f, 25.3496f, 139.5000f),
            new Vector3(152.0000f, 25.5133f, 139.0000f),
            new Vector3(152.0000f, 25.6900f, 138.5000f),
            new Vector3(152.0000f, 25.8437f, 138.0000f),
            new Vector3(152.0000f, 25.9685f, 137.5000f),
            new Vector3(152.0000f, 26.0909f, 137.0000f),
            new Vector3(152.0000f, 26.2134f, 136.5000f),
            new Vector3(152.0000f, 26.3387f, 136.0000f),
            new Vector3(152.5000f, 26.4796f, 135.5000f),
            new Vector3(153.0000f, 26.6556f, 135.0000f),
            new Vector3(153.5000f, 26.8803f, 134.5000f),
            new Vector3(154.0000f, 27.0016f, 134.0000f),
            new Vector3(154.0000f, 27.0992f, 133.5000f),
            new Vector3(154.0000f, 27.3797f, 133.0000f),
            new Vector3(154.0000f, 27.6706f, 132.5000f),
            new Vector3(154.0000f, 27.7327f, 132.0000f),
            new Vector3(154.0000f, 27.7368f, 131.5000f),
            new Vector3(154.0000f, 27.7237f, 131.0000f),
            new Vector3(154.0000f, 27.7040f, 130.5000f),
            new Vector3(154.5000f, 27.6572f, 130.0000f),
            new Vector3(154.5000f, 27.6650f, 129.5000f),
            new Vector3(154.5000f, 27.6730f, 129.0000f),
            new Vector3(154.5000f, 27.6705f, 128.5000f),
            new Vector3(154.5000f, 27.6585f, 128.0000f)
        },
    };

    public static readonly Vector3[] DrillWalkingPath =
    {
        new Vector3(110.0000f, 17.0028f, 168.5000f),
        new Vector3(110.5000f, 17.0921f, 168.0000f),
        new Vector3(111.0000f, 17.3301f, 167.5000f),
        new Vector3(111.5000f, 17.3362f, 167.0000f),
        new Vector3(112.0000f, 17.4581f, 166.5000f),
        new Vector3(112.5000f, 17.6356f, 166.0000f),
        new Vector3(113.0000f, 17.6917f, 165.5000f),
        new Vector3(113.5000f, 17.8322f, 165.0000f),
        new Vector3(114.0000f, 17.8741f, 164.5000f),
        new Vector3(114.5000f, 17.9470f, 164.0000f),
        new Vector3(115.0000f, 18.0264f, 163.5000f),
        new Vector3(115.5000f, 18.1323f, 163.0000f),
        new Vector3(116.0000f, 18.2321f, 162.5000f),
        new Vector3(116.5000f, 18.2998f, 162.0000f),
        new Vector3(117.0000f, 18.4079f, 161.5000f),
        new Vector3(117.5000f, 18.4692f, 161.0000f),
        new Vector3(118.0000f, 18.4445f, 160.5000f),
        new Vector3(118.5000f, 18.4594f, 160.0000f),
        new Vector3(119.0000f, 18.4857f, 159.5000f),
        new Vector3(119.5000f, 18.5712f, 159.0000f),
        new Vector3(120.0000f, 18.6538f, 158.5000f),
        new Vector3(120.5000f, 18.7269f, 158.0000f),
        new Vector3(121.0000f, 18.6975f, 157.5000f),
        new Vector3(121.5000f, 18.7689f, 157.0000f),
        new Vector3(122.0000f, 18.8821f, 156.5000f),
        new Vector3(122.5000f, 18.9162f, 156.0000f),
        new Vector3(123.0000f, 18.8934f, 155.5000f),
        new Vector3(123.5000f, 18.9308f, 155.0000f),
        new Vector3(124.0000f, 18.9968f, 154.5000f),
        new Vector3(124.5000f, 18.9956f, 154.0000f),
        new Vector3(124.5000f, 19.0258f, 153.5000f),
        new Vector3(125.0000f, 19.2307f, 153.0000f),
        new Vector3(125.5000f, 19.0196f, 153.5000f),
        new Vector3(126.0000f, 19.0620f, 154.0000f),
        new Vector3(126.5000f, 19.1080f, 154.5000f),
        new Vector3(126.5000f, 19.2260f, 155.0000f),
        new Vector3(126.5000f, 19.4232f, 155.5000f),
        new Vector3(126.5000f, 19.6101f, 156.0000f),
        new Vector3(126.5000f, 19.8506f, 156.5000f),
        new Vector3(127.0000f, 20.3076f, 157.0000f),
        new Vector3(127.5000f, 20.5202f, 157.0000f),
        new Vector3(128.0000f, 20.6957f, 157.5000f),
        new Vector3(128.5000f, 20.8068f, 157.5000f),
        new Vector3(129.0000f, 20.9258f, 157.5000f),
        new Vector3(129.5000f, 21.0303f, 157.5000f),
        new Vector3(130.0000f, 21.1042f, 157.5000f),
        new Vector3(130.5000f, 21.2000f, 157.5000f),
        new Vector3(131.0000f, 21.3280f, 158.0000f),
        new Vector3(131.5000f, 21.4078f, 158.0000f),
        new Vector3(132.0000f, 21.4564f, 158.0000f),
        new Vector3(132.5000f, 21.5559f, 158.0000f),
        new Vector3(133.0000f, 21.6665f, 158.0000f),
        new Vector3(133.5000f, 21.7685f, 158.0000f),
        new Vector3(134.0000f, 21.8658f, 158.0000f),
        new Vector3(134.5000f, 21.9692f, 158.0000f),
        new Vector3(135.0000f, 22.0592f, 158.0000f),
        new Vector3(135.5000f, 22.1326f, 158.0000f),
        new Vector3(136.0000f, 22.2108f, 158.0000f),
        new Vector3(136.5000f, 22.2781f, 158.0000f),
        new Vector3(137.0000f, 22.3481f, 158.0000f),
        new Vector3(137.5000f, 22.4296f, 158.0000f),
        new Vector3(138.0000f, 22.5108f, 158.0000f),
        new Vector3(138.5000f, 22.5664f, 158.0000f),
        new Vector3(139.0000f, 22.6563f, 158.0000f),
        new Vector3(139.5000f, 22.7469f, 158.0000f),
        new Vector3(140.0000f, 22.8175f, 158.0000f),
        new Vector3(140.5000f, 22.8823f, 158.0000f),
        new Vector3(141.0000f, 22.9614f, 158.5000f),
        new Vector3(141.5000f, 23.0400f, 159.0000f),
        new Vector3(142.0000f, 23.1535f, 159.5000f),
        new Vector3(142.5000f, 23.3008f, 160.0000f),
        new Vector3(143.0000f, 23.4441f, 160.5000f),
        new Vector3(143.0000f, 23.4961f, 160.0000f),
        new Vector3(142.5000f, 23.3529f, 159.5000f),
        new Vector3(142.5000f, 23.3714f, 159.0000f),
        new Vector3(142.5000f, 23.2584f, 158.5000f),
        new Vector3(142.5000f, 23.1608f, 158.0000f),
        new Vector3(142.5000f, 23.1221f, 157.5000f),
        new Vector3(142.5000f, 23.0842f, 157.0000f),
        new Vector3(142.5000f, 23.1226f, 156.5000f),
        new Vector3(142.5000f, 23.1359f, 156.0000f),
        new Vector3(142.5000f, 23.1213f, 155.5000f),
        new Vector3(142.5000f, 23.0950f, 155.0000f),
        new Vector3(142.5000f, 23.0619f, 154.5000f),
        new Vector3(142.5000f, 23.0214f, 154.0000f),
        new Vector3(142.5000f, 22.9860f, 153.5000f),
        new Vector3(142.5000f, 22.9668f, 153.0000f)
    };

    public static Transform[] GetHammerTargetsFromMainScene()
    {
        var scene = SceneManager.GetSceneByName("MainScene");
        Assert.IsTrue(scene.IsValid() && scene.isLoaded, "Hammer anchors must come from the loaded MainScene asset.");
        var transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        return new[] { "FieldGuidanceTarget_A", "FieldGuidanceTarget_B", "FieldGuidanceTarget_C" }
            .Select(name =>
            {
                var matches = transforms.Where(transform => transform.name == name).ToArray();
                Assert.AreEqual(1, matches.Length, "MainScene contains exactly one " + name + ".");
                return matches[0];
            }).ToArray();
    }

    public static Vector3[] GetHammerFootPositionsFromMainScene()
    {
        return GetHammerTargetsFromMainScene().Select(target => target.position).ToArray();
    }

    public static void AssertWalkingPathAnchors(Vector3 arrivalPosition, Vector3[] sceneFeet)
    {
        Assert.AreEqual(3, WalkingPaths.Length, "The field route has three consecutive segments.");
        Assert.AreEqual(3, sceneFeet.Length);
        for (int site = 0; site < WalkingPaths.Length; site++)
        {
            Vector3 start = site == 0 ? arrivalPosition - Vector3.up * 0.08f : sceneFeet[site - 1];
            Assert.Less(Vector3.Distance(WalkingPaths[site][0], start), 0.01f,
                "Walking segment " + (site + 1) + " starts at the production arrival or previous scene anchor.");
            Assert.Less(Vector3.Distance(WalkingPaths[site][WalkingPaths[site].Length - 1], sceneFeet[site]), 0.01f,
                "Walking segment " + (site + 1) + " ends at the actual MainScene hammer anchor.");
        }
    }

    public static void AssertDrillWalkingPathAnchors(Vector3 arrivalPosition, Vector3 drillPosition, Vector3[] sceneFeet)
    {
        Assert.Less(Vector3.Distance(DrillWalkingPath[0], arrivalPosition - Vector3.up * 0.08f), 0.01f,
            "The drill route starts at the production arrival surface.");
        for (int site = 0; site < 2; site++)
        {
            Assert.IsTrue(DrillWalkingPath.Any(point => Vector3.Distance(point, sceneFeet[site]) < 0.01f),
                "The drill route passes the actual MainScene hammer anchor " + (site + 1) + ".");
        }
        Assert.IsTrue(DrillWalkingPath.Any(point => Vector3.Distance(point, drillPosition) < 0.01f),
            "The drill route passes the production drill guidance anchor.");
        // The route ends at a standing position 2 m south of the tower, rather than inside its footprint.
        Vector3 endpoint = DrillWalkingPath[DrillWalkingPath.Length - 1];
        Assert.Less(Vector2.Distance(new Vector2(endpoint.x, endpoint.z),
            new Vector2(drillPosition.x, drillPosition.z - 2f)), 0.01f,
            "The drill route's standing endpoint remains aligned with the production tower position.");
    }
}
