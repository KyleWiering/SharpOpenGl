using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class CampaignProgressManagerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"sgcampaign_{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_dir, "campaign_progress.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void CampaignProgressManager_MarkCompleted_persists_and_round_trips()
    {
        const string missionId = "training_01_interceptor";

        var mgr = new CampaignProgressManager(FilePath);
        Assert.True(mgr.MarkCompleted(missionId));
        Assert.Contains(missionId, mgr.CompletedMissionIds);

        var mgr2 = new CampaignProgressManager(FilePath);
        Assert.True(mgr2.Load());
        Assert.Contains(missionId, mgr2.CompletedMissionIds);
    }
}