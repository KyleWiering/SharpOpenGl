using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>Shared gameplay HUD bindings for active campaign / training missions.</summary>
public static class MissionHudBinder
{
    /// <summary>First training mission shown on the galactic map (no prerequisite).</summary>
    public const string FirstTrainingMissionId = "training_01_interceptor";

    /// <summary>Populate the objective tracker and first-run onboarding hints.</summary>
    public static void BindObjectivePanel(GameplayHUD hud, MissionState? mission, bool hideForSandbox = false)
    {
        if (hideForSandbox || mission == null)
        {
            hud.ObjectivePanel.Visible = false;
            hud.ShowFirstMissionOnboardingHint = false;
            if (hideForSandbox)
                hud.ObjectivePanel.Objectives = [];
            return;
        }

        hud.ObjectivePanel.Visible = true;
        hud.ObjectivePanel.MissionTitle = mission.Definition.DisplayName;

        var lines = new List<ObjectiveLine>();
        int completed = 0;
        foreach (ObjectiveProgress obj in mission.PrimaryObjectives)
        {
            string text = obj.Definition.Description;
            if (string.IsNullOrWhiteSpace(text))
                text = obj.Definition.Id.Replace('_', ' ');

            if (obj.IsCompleted)
                completed++;

            lines.Add(new ObjectiveLine { Text = text, IsCompleted = obj.IsCompleted });
        }

        hud.ObjectivePanel.Objectives = lines;
        hud.ObjectivePanel.ProgressText = lines.Count > 0
            ? $"{completed}/{lines.Count} complete"
            : string.Empty;

        string missionId = mission.Definition.Id;
        bool isFirstTraining = missionId.Equals(FirstTrainingMissionId, StringComparison.OrdinalIgnoreCase);
        hud.ShowFirstMissionOnboardingHint = isFirstTraining;

        hud.ObjectivePanel.HintText = isFirstTraining
            ? "LClick select interceptor · RClick or A to attack — objectives update live"
            : HasBuildObjectives(mission)
                ? "Select builder · Build (B) · Place structure on map"
                : "Select ships (LClick) · Move (RClick) · Attack enemies (RClick)";

        float panelHeight = ObjectivePanel.EstimateContentHeight(
            hud.ObjectivePanel.MissionTitle,
            lines,
            hud.ObjectivePanel.HintText,
            hud.ObjectivePanel.TitleFontSize,
            hud.ObjectivePanel.BodyFontSize,
            hud.ObjectivePanel.HintFontSize,
            hud.ObjectivePanel.Size.X);
        hud.ObjectivePanel.Size = new OpenTK.Mathematics.Vector2(hud.ObjectivePanel.Size.X, panelHeight);
    }

    private static bool HasBuildObjectives(MissionState mission) =>
        mission.PrimaryObjectives.Any(o =>
            o.Definition.Type.Equals("construct", StringComparison.OrdinalIgnoreCase));
}