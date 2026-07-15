# Backlog JSON Format

Canonical store: `.grok/backlog/backlog.json`

## Schema

```json
{
  "version": 1,
  "target_count": 30,
  "project": "SharpOpenGl",
  "created_at": "2026-06-20",
  "updated_at": "2026-06-20",
  "items": [
    {
      "id": 1,
      "title": "Short title (≤80 chars)",
      "description": "What to build or fix, 1-3 sentences.",
      "area": "ecs",
      "acceptance": [
        "Observable behavior or test passes",
        "Second criterion"
      ],
      "status": "pending",
      "created_at": "2026-06-20",
      "completed_at": null
    }
  ]
}
```

## Fields

| Field | Required | Notes |
|-------|----------|-------|
| `id` | yes | 1-based, sequential, never reused |
| `title` | yes | Imperative mood: "Add minimap fog overlay" |
| `description` | yes | Enough context for `/implement` without re-asking |
| `area` | yes | One of: `ecs`, `combat`, `ui`, `rendering`, `missions`, `economy`, `audio`, `browser`, `content`, `tests`, `infra`, `polish` |
| `acceptance` | yes | 2–4 testable bullets |
| `status` | yes | `pending` \| `in_progress` \| `done` \| `skipped` |
| `created_at` | yes | ISO date `YYYY-MM-DD` |
| `completed_at` | no | Set when `status` becomes `done` |

## Empty scaffold

Use this when the file does not exist:

```json
{
  "version": 1,
  "target_count": 30,
  "project": "SharpOpenGl",
  "created_at": "<today>",
  "updated_at": "<today>",
  "items": []
}
```

## Sizing guide (one PR each)

| Good | Too big |
|------|---------|
| "Add patrol waypoint visualization" | "Rewrite entire combat system" |
| "Fix box-select at 1024×768" | "Complete Phase 11 multiplayer" |
| "Add corvette JSON + mesh variant" | "Implement all 12 GAME_PLAN phases" |

## Area hints

| Area | Typical paths |
|------|---------------|
| `ecs` | `SharpOpenGl.Engine/ECS/` |
| `combat` | `CombatSystem`, `WeaponProfiles`, `GameData/Ships/` |
| `ui` | `SharpOpenGl.Engine/UI/`, `ScaledUIRenderer` |
| `rendering` | `SharpOpenGl/Rendering/`, `ProceduralMeshes` |
| `missions` | `GameData/Missions/`, `MissionLoader` |
| `economy` | `ResourceManager`, `BuildSystem` |
| `audio` | OpenAL, `SoundRequestedEvent` |
| `browser` | `SharpOpenGl.Browser/` |
| `content` | `GameData/` JSON only |
| `tests` | `SharpOpenGl.Tests/` |
| `infra` | CI, `AGENTS.md`, build scripts |
| `polish` | UX, balance, visual tweaks |