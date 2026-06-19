using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MissionSelectScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void SetMissions_auto_selects_first_mission_and_enables_start()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry { Id = "a", Title = "Alpha", Description = "First." },
            new MissionEntry { Id = "b", Title = "Beta", Description = "Second." },
        ]);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;

        // Start button centre at reference resolution (BottomLeft 480,-80 size 280×56).
        Vector2 tap = new(620f, 972f);
        bool consumed = screen.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("a", startedId);
    }

    [Fact]
    public void SetMissions_empty_list_keeps_start_disabled()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions([]);

        bool started = false;
        screen.MissionStartRequested += _ => started = true;

        Vector2 tap = new(620f, 972f);
        screen.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.False(started);
    }

    [Fact]
    public void Selecting_second_mission_starts_that_mission()
    {
        var screen = new MissionSelectScreen();
        screen.SetMissions(
        [
            new MissionEntry { Id = "a", Title = "Alpha", Description = "First." },
            new MissionEntry { Id = "b", Title = "Beta", Description = "Second." },
        ]);

        // Second list item: panel (40,80) + button offset (8, 8+68) + half size
        Vector2 listTap = new(48f + 192f, 88f + 68f + 30f);
        screen.HandlePointerTapped(listTap, 0, ReferenceViewport);

        string? startedId = null;
        screen.MissionStartRequested += id => startedId = id;
        screen.HandlePointerTapped(new(620f, 972f), 0, ReferenceViewport);

        Assert.Equal("b", startedId);
    }
}