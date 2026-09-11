using System.IO;
using NUnit.Framework;
using UnityEditor;

public class WebGLTextureSettingsTests
{
    [Test]
    public void EnvironmentTextures_Should_UseWebOverrides_When_BuiltForSchoolBrowsers()
    {
        string[] names =
        {
            "Black_Sand_BaseColor", "Black_Sand_Normal", "Black_Sand_MaskMap",
            "Grass_Soil_BaseColor", "Grass_Soil_Normal", "Grass_Soil_MaskMap",
            "Soil_Rocks_BaseColor", "Soil_Rocks_Normal", "Soil_Rocks_MaskMap",
            "Pebbles_A_BaseColor", "Pebbles_A_Normal", "Pebbles_A_MaskMap",
            "Rock_BaseColor", "Rock_Normal", "Rock_MaskMap",
            "Grass_Dry_BaseColor", "Grass_Dry_Normal", "Grass_A_MaskMap"
        };
        foreach (string name in names)
        {
            string path = "Assets/TerrainSampleAssets/Textures/Terrain/" + name + ".tif";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = importer.GetPlatformTextureSettings("WebGL");
            Assert.IsTrue(settings.overridden, path);
            Assert.LessOrEqual(settings.maxTextureSize, name.EndsWith("BaseColor") ? 1024 : 512, path);
            Assert.IsTrue(importer.mipmapEnabled, path);
            Assert.IsFalse(importer.isReadable, path);
        }
    }

    [TestCase("Assets/Model/Drone/Polygonal_Drone_Desig_0701040626_texture.png")]
    [TestCase("Assets/Model/Microscope/Microscope_on_Transpa_1203043229_texture.png")]
    [TestCase("Assets/Model/Drill/Retro_Drill_Bot_0628054134_texture.png")]
    [TestCase("Assets/Model/Hammer/Geometric_Hammer_0714042033_texture.png")]
    public void PropTextures_Should_CapAt1024_When_BuiltForWeb(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = importer.GetPlatformTextureSettings("WebGL");
        Assert.IsTrue(settings.overridden, Path.GetFileName(path));
        Assert.LessOrEqual(settings.maxTextureSize, 1024, path);
    }
}
