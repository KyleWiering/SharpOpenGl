using System.Text;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Reads and writes the iterative model-improvement brief (do-better.md).</summary>
public static class ModelImprovementMarkdown
{
    public sealed record Brief(
        string RaceId,
        string HullId,
        int Loop,
        float? LastScore,
        IReadOnlyList<string> Goals,
        IReadOnlyList<string> Actions,
        IReadOnlyList<string> Suggestions);

    public static string DefaultPath(string repoRoot, string raceId, string hullId) =>
        Path.Combine(repoRoot, "model-improvement", raceId, hullId, "do-better.md");

    public static Brief CreateInitial(string raceId, string hullId)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();
        RaceVisualSchema.TryGetRace(raceId, out var race);
        string style = race?.Style ?? "unknown";
        string display = race?.DisplayName ?? raceId;

        return new Brief(
            raceId,
            hullId,
            Loop: 1,
            LastScore: null,
            Goals:
            [
                $"Make the {display} {hullId} instantly readable as a {style}-style spacecraft.",
                "Improve shape: stronger bow, visible dorsal mass, clean engine read.",
                "Improve textures/materials: accent bands, engine glow wells, panel depth.",
                "Improve shadows: bake stronger directional lighting for depth separation.",
                "Raise visual appeal without breaking the hull envelope or gameplay scale.",
            ],
            Actions:
            [
                "Add vasudan belly segmentation and dorsal keel ridges.",
                "Increase accent luminance on leading edges and engine nozzles.",
                "Add lateral intake scoops and stern stabilizer fins.",
                "Tune substrate grit so race shader panels read in preview.",
            ],
            Suggestions: []);
    }

    public static void Write(string path, Brief brief)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sb = new StringBuilder();
        sb.AppendLine($"# Do Better — {brief.RaceId} / {brief.HullId}");
        sb.AppendLine();
        sb.AppendLine($"**Loop:** {brief.Loop}  ");
        if (brief.LastScore.HasValue)
            sb.AppendLine($"**Last score:** {brief.LastScore.Value:F1} / 100  ");
        sb.AppendLine();
        sb.AppendLine("## Goals");
        foreach (string goal in brief.Goals)
            sb.AppendLine($"- {goal}");
        sb.AppendLine();
        sb.AppendLine("## Actions (this iteration)");
        foreach (string action in brief.Actions)
            sb.AppendLine($"- [ ] {action}");
        sb.AppendLine();
        sb.AppendLine("## Scorer suggestions");
        if (brief.Suggestions.Count == 0)
            sb.AppendLine("- _(awaiting first score)_");
        else
            foreach (string s in brief.Suggestions)
                sb.AppendLine($"- {s}");
        sb.AppendLine();
        File.WriteAllText(path, sb.ToString());
    }

    public static Brief UpdateFromReport(Brief current, ModelQualityScorer.ModelQualityReport report, int nextLoop)
    {
        var actions = report.Suggestions.Take(5).Select(s => s).ToList();
        if (actions.Count == 0)
            actions.Add("Polish silhouette edges and accent highlights.");

        return current with
        {
            Loop = nextLoop,
            LastScore = report.TotalScore,
            Actions = actions,
            Suggestions = report.Suggestions,
        };
    }
}