# Continuity Flow Map (skill reference)

Summary of menu navigation edges for `menu-continuity` scoring. **Canonical source:** `.grok/org/superior-menu-skills/artifacts/continuity-flow-map.md` — keep in sync when edges change.

## Quick graph

```
MainMenu
  ├─→ MissionSelect ─→ Briefing ─→ Loading ─→ Gameplay
  ├─→ LoadGame ─→ Gameplay
  ├─→ MultiplayerSetup ─→ Gameplay
  ├─→ Settings
  └─→ ShipDesigner

Gameplay → Pause → Resume | SaveGame | Settings | MainMenu
Gameplay → MissionVictory → MainMenu | Replay
```

## Evaluation flows

| Flow ID | Edges to walk |
|---------|---------------|
| `campaign_entry` | MainMenu → MissionSelect → Briefing → (Loading) → Gameplay |
| `pause_cycle` | Gameplay → Pause → Resume / Save / Settings / Quit |
| `save_load` | MainMenu ↔ LoadGame; Pause → SaveGame; Continue |
| `full_menu_tour` | All primary screens + one gameplay pause |

## Stack + theme reminders

- **Stack:** `UIManager.Push` / `Pop` / `Clear` — see `UIManager.cs`
- **Theme:** `MenuTheme` + `MenuStarfieldBackground` on title-stack screens
- **Esc:** MainMenu pops stack; Playing toggles Pause (`EngineWindow.HandleEscapePressed`)

## Known baseline gaps

1. `LoadingScreen` not wired between Briefing and Gameplay
2. `PauseScreen` has no Load Game entry
3. Overlay screens use hardcoded panel colours vs `MenuTheme.ApplyPanel`

Full per-edge tables (back, context, file paths): see canonical artifact.