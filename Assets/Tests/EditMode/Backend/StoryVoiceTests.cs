using NUnit.Framework;

/// <summary>Classrooms without headphones open the game with ?voice=off.</summary>
public class StoryVoiceTests
{
    private static bool Off(string url) => (bool)BackendTestReflection.InvokeStatic(
        BackendTestReflection.GetType("StorySystem.StoryVoice"), "UrlTurnsVoiceOff", url);

    [Test]
    public void VoiceParameter_ShouldTurnVoicesOff()
    {
        Assert.IsTrue(Off("https://geo-q.jp/play/?voice=off"));
        Assert.IsTrue(Off("https://geo-q.jp/play/?class=1&voice=0"));
        Assert.IsTrue(Off("https://geo-q.jp/play/?voice=OFF#start"));
        Assert.IsFalse(Off("https://geo-q.jp/play/"));
        Assert.IsFalse(Off("https://geo-q.jp/play/?voice=on"));
        Assert.IsFalse(Off(null));
    }
}
