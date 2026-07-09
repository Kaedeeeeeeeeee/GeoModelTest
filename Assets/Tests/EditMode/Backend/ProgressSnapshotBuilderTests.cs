using NUnit.Framework;
using UnityEngine;

public class ProgressSnapshotBuilderTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey("StoryFlags");
        PlayerPrefs.DeleteKey("QuestSystem.CompletedQuests");
        PlayerPrefs.DeleteKey("QuestSystem.CompletedObjectives");
        PlayerPrefs.DeleteKey("PlayerPersistentData.UnlockedToolIds");
        PlayerPrefs.DeleteKey("PlayerPersistentData.Inventory");
        PlayerPrefs.Save();
    }

    [Test]
    public void Build_ShouldReadProgressKeysFromPlayerPrefs()
    {
        PlayerPrefs.SetString("StoryFlags", "story.a|story.b");
        PlayerPrefs.SetString("QuestSystem.CompletedQuests", "quest.b|quest.a");
        PlayerPrefs.SetString("QuestSystem.CompletedObjectives", "objective.a|objective.b");
        PlayerPrefs.SetString("PlayerPersistentData.UnlockedToolIds", "hammer,drill");
        PlayerPrefs.SetString("PlayerPersistentData.Inventory", "{\"items\":[{\"sampleID\":\"s1\"},{\"sampleID\":\"s2\"}]}");
        PlayerPrefs.Save();

        var builderType = BackendTestReflection.GetType("Backend.ProgressSnapshotBuilder");
        var snapshot = BackendTestReflection.InvokeStatic(builderType, "Build", "TestScene");

        Assert.AreEqual("TestScene", BackendTestReflection.GetField(snapshot, "currentScene"));
        CollectionAssert.AreEquivalent(new[] { "quest.a", "quest.b" }, (System.Collections.IEnumerable)BackendTestReflection.GetField(snapshot, "completedQuests"));
        CollectionAssert.AreEquivalent(new[] { "objective.a", "objective.b" }, (System.Collections.IEnumerable)BackendTestReflection.GetField(snapshot, "completedObjectives"));
        CollectionAssert.AreEquivalent(new[] { "story.a", "story.b" }, (System.Collections.IEnumerable)BackendTestReflection.GetField(snapshot, "storyFlags"));
        CollectionAssert.AreEquivalent(new[] { "drill", "hammer" }, (System.Collections.IEnumerable)BackendTestReflection.GetField(snapshot, "unlockedToolIds"));
        Assert.AreEqual(2, BackendTestReflection.GetField(snapshot, "inventoryCount"));
    }
}
