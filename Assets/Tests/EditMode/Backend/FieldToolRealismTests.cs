using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>Pure logic and asset checks for the realistic rock hammer and boring rig.</summary>
public class FieldToolRealismTests
{
    private static readonly string[] ModelNames =
    {
        "RockHammer", "RigLeg", "RigHead", "RigMachine", "RigDrillHead", "RigSwivel", "RigRod", "RigPump", "CoreBox"
    };

    private static readonly string[] NewTextKeys =
    {
        "hammer.goggles.on", "sample.bag.label.site", "sample.bag.label.core", "sample.bag.label.free",
        "sample.bag.label.depth", "drill_tower.gauge.depth"
    };

    private static Type T(string name) => BackendTestReflection.GetType(name);
    private static object Static(string type, string method, params object[] args) =>
        BackendTestReflection.InvokeStatic(T(type), method, args);
    private static float Const(string name) => (float)BackendTestReflection.GetField(T("BoringRigLayout"), name);

    [TestCase(1, 3, 1)]
    [TestCase(2, 3, 2)]
    [TestCase(3, 3, 3)]
    [TestCase(1, 1, 3)]
    [TestCase(1, 5, 1)]
    [TestCase(4, 5, 2)]
    [TestCase(5, 5, 3)]
    public void HammerHit_ShouldEscalateFromCrustToCrackToBreak(int hits, int required, int expectedStage)
    {
        Assert.AreEqual(expectedStage, (int)Static("HammerTool", "HitStage", hits, required));
    }

    [Test]
    public void HammerHit_ShouldSoundHigherAsTheRockOpens()
    {
        float first = (float)Static("HammerTool", "HitPitch", 1);
        float second = (float)Static("HammerTool", "HitPitch", 2);
        float last = (float)Static("HammerTool", "HitPitch", 3);
        Assert.Less(first, second);
        Assert.Less(second, last);
    }

    [Test]
    public void DrillingCycle_ShouldStayWithinTheClassroomTimeBudget()
    {
        float cycle = Const("DrillSeconds") + Const("PullSeconds") + Const("BoxSeconds");
        Assert.AreEqual(cycle, Const("CycleSeconds"), 0.0001f);
        // The old cycle was 2 s. The required route drills once, so the added wait stays under 5 s.
        Assert.LessOrEqual(cycle, 7f);
        Assert.Greater(Const("DrillSeconds"), Const("PullSeconds"));
    }

    [Test]
    public void CoreSlots_ShouldLineUpInsideTheBoxInDepthOrder()
    {
        int slots = (int)BackendTestReflection.GetField(T("BoringRigLayout"), "CoreSlots");
        float halfWidth = slots * Const("SlotPitch") * 0.5f;
        float previousX = float.NegativeInfinity;
        for (int i = 0; i < slots; i++)
        {
            Assert.AreEqual(i, (int)Static("BoringRigLayout", "SlotIndex", i));
            var position = (Vector3)Static("BoringRigLayout", "SlotLocalPosition", i);
            Assert.Greater(position.x, previousX, "Deeper cores go further along the box.");
            Assert.Less(Mathf.Abs(position.x) + Const("CoreRadius"), halfWidth + 0.001f, "Every core fits between the box walls.");
            Assert.Less(position.y + Const("CoreRadius"), Const("BoxHeight") + 0.1f);
            Assert.LessOrEqual(position.z + 2f, Const("BoxInnerLength") * 0.5f, "A 2 m core fits lengthwise.");
            previousX = position.x;
        }
        Assert.AreEqual(0, (int)Static("BoringRigLayout", "SlotIndex", slots));
        Assert.AreEqual(slots - 1, (int)Static("BoringRigLayout", "SlotIndex", -1));
    }

    [Test]
    public void BoxedCore_ShouldLieAlongTheBox()
    {
        var rotation = (Quaternion)BackendTestReflection.GetProperty(T("BoringRigLayout"), "CoreLocalRotation");
        Vector3 axis = rotation * Vector3.down;
        Assert.AreEqual(1f, Vector3.Dot(axis, Vector3.forward), 0.0001f);
    }

    [TestCase(0f, 0)]
    [TestCase(0.24f, 0)]
    [TestCase(0.26f, 1)]
    [TestCase(0.74f, 1)]
    [TestCase(0.76f, 2)]
    [TestCase(1f, 2)]
    [TestCase(1.5f, 2)]
    public void Segment_ShouldFollowTheBitThroughTheLayers(float progress, int expected)
    {
        Assert.AreEqual(expected, (int)Static("BoringRigLayout", "SegmentAt", progress, new[] { 0.25f, 0.5f, 0.25f }));
    }

    [Test]
    public void Segment_ShouldReportNoLayerForAnEmptyCore()
    {
        Assert.AreEqual(-1, (int)Static("BoringRigLayout", "SegmentAt", 0.5f, new float[0]));
        Assert.AreEqual(-1, (int)Static("BoringRigLayout", "SegmentAt", 0.5f, null));
    }

    [Test]
    public void Swivel_ShouldDescendWithTheRodAndStayUnderThePulley()
    {
        float idle = (float)Static("BoringRigLayout", "SwivelTopWhileDrilling", 0f);
        float drilled = (float)Static("BoringRigLayout", "SwivelTopWhileDrilling", 1f);
        Assert.AreEqual(Const("SwivelIdleTop"), idle, 0.0001f);
        Assert.AreEqual(Const("SwivelDrilledTop"), drilled, 0.0001f);
        Assert.Less(drilled, idle);
        Assert.Less(Const("SwivelPulledTop"), Const("ApexHeight"));
        Assert.AreEqual(drilled, (float)Static("BoringRigLayout", "SwivelTopWhileDrilling", 2f), 0.0001f);
    }

    [Test]
    public void TripodFeet_ShouldBeSpreadEvenly()
    {
        var a = (Vector3)Static("BoringRigLayout", "FootDirection", 0);
        var b = (Vector3)Static("BoringRigLayout", "FootDirection", 1);
        var c = (Vector3)Static("BoringRigLayout", "FootDirection", 2);
        Assert.AreEqual(1f, a.magnitude, 0.0001f);
        Assert.AreEqual(120f, Vector3.Angle(a, b), 0.01f);
        Assert.AreEqual(120f, Vector3.Angle(b, c), 0.01f);
        Assert.AreEqual(0f, (a + b + c).magnitude, 0.0001f);
        // The legs are long enough to reach the ground when spread.
        float needed = Mathf.Sqrt(Const("ApexHeight") * Const("ApexHeight") + Const("FootRadius") * Const("FootRadius"));
        Assert.AreEqual(needed, Const("LegLength"), 0.05f);
    }

    [Test]
    public void CoreTransfer_ShouldStartAtTheRodEndInTheBoxAndArcOverTheBoxWall()
    {
        var from = new Vector3(0f, 2.4f, 0f);
        var to = new Vector3(2f, 0.12f, -1f);
        Assert.AreEqual(from, (Vector3)Static("BoringRigLayout", "CoreTransferPoint", from, to, 0f));
        Assert.AreEqual(to, (Vector3)Static("BoringRigLayout", "CoreTransferPoint", from, to, 1f));
        var nearlyThere = (Vector3)Static("BoringRigLayout", "CoreTransferPoint", from, to, 0.9f);
        Assert.Greater(nearlyThere.y, to.y + Const("BoxHeight") * 0.5f, "The core comes down into the box from above.");
    }

    [Test]
    public void CoreBoxLabels_ShouldShowTheDepthRangeOfEachSlot()
    {
        Assert.AreEqual("0-2m", (string)Static("CoreBox", "DepthLabel", 0, 2f));
        Assert.AreEqual("8-10m", (string)Static("CoreBox", "DepthLabel", 4, 2f));
        Assert.AreEqual("1.5-3m", (string)Static("CoreBox", "DepthLabel", 1, 1.5f));
    }

    [Test]
    public void Goggles_ShouldFadeInHoldAndFadeOut()
    {
        float fadeIn = (float)BackendTestReflection.GetField(T("UISystem.SafetyGogglesOverlay"), "FadeIn");
        float hold = (float)BackendTestReflection.GetField(T("UISystem.SafetyGogglesOverlay"), "Hold");
        float fadeOut = (float)BackendTestReflection.GetField(T("UISystem.SafetyGogglesOverlay"), "FadeOut");
        Assert.AreEqual(0f, (float)Static("UISystem.SafetyGogglesOverlay", "AlphaAt", -1f));
        Assert.AreEqual(0.5f, (float)Static("UISystem.SafetyGogglesOverlay", "AlphaAt", fadeIn * 0.5f), 0.001f);
        Assert.AreEqual(1f, (float)Static("UISystem.SafetyGogglesOverlay", "AlphaAt", fadeIn + hold * 0.5f));
        Assert.AreEqual(0.5f, (float)Static("UISystem.SafetyGogglesOverlay", "AlphaAt", fadeIn + hold + fadeOut * 0.5f), 0.001f);
        Assert.AreEqual(0f, (float)Static("UISystem.SafetyGogglesOverlay", "AlphaAt", fadeIn + hold + fadeOut + 1f));
        // Equipping the hammer must not hold the player up.
        Assert.LessOrEqual(fadeIn + hold + fadeOut, 2f);
    }

    [TestCase(1, "A")]
    [TestCase(2, "B")]
    [TestCase(3, "C")]
    [TestCase(0, "A")]
    [TestCase(99, "Z")]
    public void BagLabel_ShouldNameSitesByLetter(int site, string expected)
    {
        Assert.AreEqual(expected, (string)Static("UISystem.SampleBagOverlay", "SiteLetter", site));
    }

    [TestCase(0, 0f, 0f, 0f)]
    [TestCase(0, 0.1f, 0f, 0.1f)]
    [TestCase(0, 0.6f, 0f, 0.25f)]
    [TestCase(1, 0.1f, 0.25f, 0.25f)]
    [TestCase(1, 0.6f, 0.25f, 0.6f)]
    [TestCase(2, 1f, 0.75f, 1f)]
    public void DepthGauge_ShouldRevealEachLayerOnlyAsFarAsTheBitHasReached(int index, float progress, float from, float to)
    {
        var args = new object[] { new[] { 0.25f, 0.5f, 0.25f }, index, progress, 0f, 0f };
        T("UISystem.DrillDepthGauge").GetMethod("VisibleSpan").Invoke(null, args);
        Assert.AreEqual(from, (float)args[3], 0.0001f);
        Assert.AreEqual(to, (float)args[4], 0.0001f);
    }

    [Test]
    public void Models_ShouldImportWithKnownMaterialsAndRealisticSizes()
    {
        foreach (string modelName in ModelNames)
        {
            var model = Resources.Load<GameObject>("Models/FieldTools/" + modelName);
            Assert.IsNotNull(model, "Missing model: " + modelName);
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
            Assert.IsNotEmpty(renderers, modelName + " has no mesh.");
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.IsNotNull(material, modelName + " has an empty material slot.");
                    Assert.IsNotNull(Static("FieldToolModels", "ResolveKey", material.name),
                        modelName + " uses an unknown material: " + material.name);
                }
            }
        }

        Bounds hammer = ModelBounds("RockHammer");
        Assert.AreEqual(0.315f, hammer.size.y, 0.03f, "A rock hammer is about 30 cm long.");
        Assert.Less(hammer.size.z, 0.22f);
        Assert.Less(hammer.size.x, 0.06f);

        Bounds machine = ModelBounds("RigMachine");
        Assert.Less(machine.center.x, -0.1f, "The drilling machine sits behind the hole (-X), away from the core box.");
        Assert.AreEqual(0f, machine.min.y, 0.03f, "The machine stands on the ground.");

        Bounds box = ModelBounds("CoreBox");
        Assert.Greater(box.size.z, 2f, "The core box holds 2 m cores lengthwise.");
        Assert.AreEqual(0f, box.min.y, 0.02f);

        Bounds leg = ModelBounds("RigLeg");
        Assert.AreEqual(Const("LegLength"), -leg.min.y, 0.1f, "A tripod leg hangs down from the apex.");
    }

    [TestCase("ja-JP")]
    [TestCase("zh-CN")]
    [TestCase("en-US")]
    public void NewTexts_ShouldExistInEveryLanguage(string language)
    {
        string json = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Localization/Data/" + language + ".json"));
        foreach (string key in NewTextKeys)
        {
            StringAssert.Contains("\"" + key + "\"", json, language + " is missing " + key);
        }
    }

    private static Bounds ModelBounds(string modelName)
    {
        var model = Resources.Load<GameObject>("Models/FieldTools/" + modelName);
        var filters = model.GetComponentsInChildren<MeshFilter>(true);
        bool any = false;
        var bounds = new Bounds();
        foreach (var filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Matrix4x4 toRoot = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 point = toRoot.MultiplyPoint3x4(vertex);
                if (!any)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }
        Assert.IsTrue(any, modelName + " has no vertices.");
        return bounds;
    }
}
