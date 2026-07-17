# Control Schemes

This document describes all input control schemes for SharpOpenGL Space RTS.

---

## Desktop (Keyboard + Mouse)

### Camera

| Action | Keyboard | Mouse |
|--------|----------|-------|
| Pan camera forward / back | W / S (also ↑ / ↓) | Right-drag *(no units selected)* |
| Pan camera left / right | Q / E; A / D when no units selected | Right-drag *(no units selected)* |
| Edge scroll | — | Move pointer within **22 px** of viewport edge *(toggle in Settings)* |
| Camera override with units selected | Hold **Shift** + WASD / Q E Z X | — |
| Zoom in / out | Scroll wheel *(zoom-to-cursor)* | Scroll wheel |
| Camera height up / down | Z / X *(X height only with camera override)* | — |

### Selection & commands

| Action | Keyboard | Mouse |
|--------|----------|-------|
| Select unit | — | Left click |
| Box multi-select | — | Left drag |
| Move command | — | Right click ground *(immediate on press when units selected)* |
| Attack command | — | Right click enemy *(immediate on press when units selected)* |
| Repair command | — | Right click friendly damaged unit/building |
| Append waypoint | — | Shift + right click ground |
| Select all friendly units | Ctrl + A | — |
| Control group assign | Ctrl + 1–9 | — |
| Control group recall | 5–9 *(1–4 are abilities)* | — |
| Pause / cancel placement | Escape | — |
| Confirm (menus) | Enter | — |
| Cancel (menus) | Escape | — |

> **Note:** When units are selected, **right-click** issues move/attack/repair immediately on **mouse down** — no need to wait for mouse release. **A / S / D** are reserved for unit-command shortcuts (attack-move, stop, etc.) unless **Shift** is held for camera override.

### Ship Control Bar shortcuts *(units selected)*

These mirror the on-screen **Ship Control Bar** (bottom-right HUD). Activate a mode, then left-click the target (or right-click for immediate move/attack when not in a mode).

| Command | Key | Bar button | Follow-up |
|---------|-----|------------|-----------|
| Move | **M** | Move | Left-click destination |
| Stop | **S** or **X** | Stop | Immediate |
| Patrol | **P** | Patrol | Left-click patrol point |
| Attack | **T** | Attack | Left-click enemy |
| Attack-move | **F** or **A** | Attack Move | Left-click destination |
| Cycle stance | — | Stance | Defensive ↔ Aggressive |
| Hold position | **H** *(no harvester)* | Hold | Sets passive stance |
| Harvest | **H** *(collector selected)* | Harvest | Click resource node |
| Formation | **G** | Formations | Cycles line / wedge / box / column |
| Build structures | **B** | Build | Opens build map panel |

> **H key context:** When a resource collector is selected, **H** enters harvest mode. Otherwise **H** sets **hold position** (passive stance). **V** sets defensive stance directly.

### Production & abilities

| Action | Keyboard | Mouse |
|--------|----------|-------|
| Ability 1–4 | 1 / 2 / 3 / 4 | Click ability button |
| Build map panel | B | Click base / Build button |
| Set rally point | R + right click | — |
| Cycle squad formation | G | Formation button |

---

## Mobile / Touch

### Single-finger gestures

| Gesture | Action |
|---------|--------|
| Tap | Select unit / UI button at tap location |
| Double-tap | Issue move command to tapped location *(browser gameplay)* |
| Long press | Issue attack command on enemy under finger |
| Drag | Box multi-select |

### Two-finger gestures

| Gesture | Action |
|---------|--------|
| Two-finger drag | Pan camera (in the drag direction) |
| Pinch inward | Zoom out |
| Pinch outward | Zoom in |

### On-screen controls

| Control | Description |
|---------|-------------|
| Virtual Joystick | On-screen joystick (bottom-left) for camera panning on phone/tablet layouts; deadzone + response curve tuned for snappy touch pan |
| Camera Height Slider | Drag slider (HUD right edge) to adjust vertical camera height |
| Ability buttons | Enlarged HUD buttons (≥ 44 px touch targets) for abilities 1–4 |
| Build panel | Tap a base to open the build panel with large, spaced buttons |
| Ship Control Bar | Same command grid as desktop (move, stop, patrol, attack, attack-move, stance, formation, build, harvest) |

> **Note:** Edge-of-screen scrolling is disabled on touch devices; use two-finger drag or the virtual joystick instead.

---

## Adaptive UI Layouts

The UI automatically adapts to the detected screen size and orientation:

| Profile | Viewport | UI changes |
|---------|----------|------------|
| Desktop | ≥ 1280 × 720 | Compact HUD, standard button sizes |
| Tablet Landscape | wide tablet | Medium HUD, larger buttons, virtual joystick optional |
| Tablet Portrait | tall tablet | Stacked HUD panels, larger buttons |
| Phone Landscape | small landscape | Minimal HUD, maximum button sizes, virtual joystick optional |
| Phone Portrait | small portrait | Full-width ability bar at bottom, virtual joystick prominent |

Minimum touch target size: **44 px** on all mobile profiles.

---

## Customising Bindings

Desktop key bindings are defined in [`GameData/Config/controls.json`](../GameData/Config/controls.json).

```json
{
  "keyboard": {
    "CameraMoveForward": "W",
    "CameraMoveBack": "S",
    "CameraStrafeLeft": "Q",
    "CameraStrafeRight": "E",
    "CameraPanLeft": "A",
    "CameraPanRight": "D",
    "CameraHeightUp": "Z",
    "CameraHeightDown": "X",
    "CameraOverride": "LeftShift",
    "Pause": "Escape",
    "Ability1": "D1",
    "Ability2": "D2",
    "Ability3": "D3",
    "Ability4": "D4",
    "BuildMenu": "B",
    "Confirm": "Enter",
    "Cancel": "Escape"
  },
  "shipControlBar": {
    "Move": "M",
    "Stop": "S",
    "StopAlt": "X",
    "Patrol": "P",
    "Attack": "T",
    "AttackMove": "A",
    "AttackMoveAlt": "F",
    "HoldPosition": "H",
    "DefensiveStance": "V",
    "Harvest": "H",
    "FormationCycle": "G",
    "BuildStructures": "B"
  },
  "mouse": {
    "Select": "LeftButton",
    "MoveCommand": "RightButton",
    "CameraPan": "RightButtonDrag",
    "AttackCommand": "RightButtonOnEnemy",
    "AppendWaypoint": "Shift+RightButton"
  },
  "touch": {
    "Select": "Tap",
    "MoveCommand": "DoubleTap",
    "AttackCommand": "LongPress",
    "CameraPan": "TwoFingerDrag",
    "CameraZoom": "Pinch"
  }
}
```

Ship Control Bar shortcuts are defined in the `shipControlBar` section of `controls.json` and implemented in `ShipControlBar` / `EngineWindow.TryHandleUnitShortcut`. **X** doubles as camera height-down when no units are selected and as **Stop** when units are selected.

Touch gesture-to-action mappings are read from the `touch` section of `controls.json`. Gesture thresholds (tap duration, long-press duration, double-tap window) are configurable via `GestureRecognizer` properties.

---

*Last Updated: 2026-07-14*