using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MenuIconDrawingTests
{
    [Theory]
    [InlineData(MenuIconKind.Move)]
    [InlineData(MenuIconKind.Stop)]
    [InlineData(MenuIconKind.Patrol)]
    [InlineData(MenuIconKind.Attack)]
    [InlineData(MenuIconKind.AttackMove)]
    [InlineData(MenuIconKind.StancePassive)]
    [InlineData(MenuIconKind.StanceDefensive)]
    [InlineData(MenuIconKind.StanceAggressive)]
    [InlineData(MenuIconKind.FormationLine)]
    [InlineData(MenuIconKind.FormationWedge)]
    [InlineData(MenuIconKind.FormationColumn)]
    [InlineData(MenuIconKind.FormationBox)]
    [InlineData(MenuIconKind.FormationScatter)]
    [InlineData(MenuIconKind.Build)]
    [InlineData(MenuIconKind.Harvest)]
    [InlineData(MenuIconKind.UnitFriendly)]
    [InlineData(MenuIconKind.UnitHostile)]
    [InlineData(MenuIconKind.UnitNeutral)]
    [InlineData(MenuIconKind.UnitHarvestable)]
    [InlineData(MenuIconKind.UnitScenery)]
    [InlineData(MenuIconKind.StatHP)]
    [InlineData(MenuIconKind.StatShield)]
    [InlineData(MenuIconKind.StatArmor)]
    [InlineData(MenuIconKind.StatCargo)]
    [InlineData(MenuIconKind.StatHarvest)]
    [InlineData(MenuIconKind.HullMilitary)]
    [InlineData(MenuIconKind.HullEngineering)]
    [InlineData(MenuIconKind.HullPolitical)]
    [InlineData(MenuIconKind.NavNewGame)]
    [InlineData(MenuIconKind.NavSandbox)]
    [InlineData(MenuIconKind.NavMultiplayer)]
    [InlineData(MenuIconKind.NavContinue)]
    [InlineData(MenuIconKind.NavLoadGame)]
    [InlineData(MenuIconKind.NavShipDesigner)]
    [InlineData(MenuIconKind.NavSettings)]
    [InlineData(MenuIconKind.NavQuit)]
    [InlineData(MenuIconKind.NavBack)]
    [InlineData(MenuIconKind.NavResume)]
    [InlineData(MenuIconKind.NavSave)]
    [InlineData(MenuIconKind.NavStartMission)]
    [InlineData(MenuIconKind.NavBriefing)]
    [InlineData(MenuIconKind.NavObjectives)]
    [InlineData(MenuIconKind.NavCompleted)]
    public void MenuIconDrawing_draws_each_kind_without_throw(MenuIconKind kind)
    {
        var renderer = new RecordingRenderer();
        var primary = MenuTheme.ButtonText;
        var accent = MenuTheme.ButtonBorder;

        var exception = Record.Exception(() =>
            MenuIconDrawing.Draw(renderer, kind, Vector2.Zero, 36f, primary, accent));

        Assert.Null(exception);
        Assert.True(renderer.RectDrawCount > 0);
    }

    [Theory]
    [InlineData(MenuIconKind.NavNewGame)]
    [InlineData(MenuIconKind.NavSandbox)]
    [InlineData(MenuIconKind.NavMultiplayer)]
    [InlineData(MenuIconKind.NavContinue)]
    [InlineData(MenuIconKind.NavLoadGame)]
    [InlineData(MenuIconKind.NavShipDesigner)]
    [InlineData(MenuIconKind.NavSettings)]
    [InlineData(MenuIconKind.NavQuit)]
    [InlineData(MenuIconKind.NavBack)]
    [InlineData(MenuIconKind.NavResume)]
    [InlineData(MenuIconKind.NavSave)]
    [InlineData(MenuIconKind.NavStartMission)]
    [InlineData(MenuIconKind.NavBriefing)]
    [InlineData(MenuIconKind.NavObjectives)]
    [InlineData(MenuIconKind.NavCompleted)]
    public void MenuIconDrawing_nav_kinds_meet_24px_minimum(MenuIconKind kind)
    {
        var renderer = new BoundsRecordingRenderer();
        var primary = MenuTheme.ButtonText;
        var accent = MenuTheme.ButtonBorder;
        const float navIconSize = MenuIconDrawing.MinimumListSize;
        const float undersizedRequest = 8f;

        var exception = Record.Exception(() =>
            MenuIconDrawing.Draw(renderer, kind, Vector2.Zero, navIconSize, primary, accent));

        Assert.Null(exception);
        Assert.True(renderer.RectDrawCount > 0,
            $"{kind} should emit rect draws at {navIconSize}px request.");

        var undersized = new BoundsRecordingRenderer();
        MenuIconDrawing.Draw(undersized, kind, Vector2.Zero, undersizedRequest, primary, accent);
        Assert.True(undersized.RectDrawCount > 0);
        Assert.True(undersized.MaxExtent > undersizedRequest,
            $"{kind} should upscale sub-{navIconSize}px requests beyond the raw {undersizedRequest}px input.");
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public int RectDrawCount { get; private set; }

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) => RectDrawCount++;

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }

    private sealed class BoundsRecordingRenderer : IUIRenderer
    {
        private float _minX = float.MaxValue;
        private float _minY = float.MaxValue;
        private float _maxX = float.MinValue;
        private float _maxY = float.MinValue;

        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public int RectDrawCount { get; private set; }

        public float MaxExtent =>
            RectDrawCount == 0 ? 0f : MathF.Max(_maxX - _minX, _maxY - _minY);

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            RectDrawCount++;
            _minX = MathF.Min(_minX, position.X);
            _minY = MathF.Min(_minY, position.Y);
            _maxX = MathF.Max(_maxX, position.X + size.X);
            _maxY = MathF.Max(_maxY, position.Y + size.Y);
        }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}