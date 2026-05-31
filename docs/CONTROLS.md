# Control Schemes

This document describes all input control schemes for SharpOpenGL Space RTS.

---

## Desktop (Keyboard + Mouse)

| Action | Keyboard | Mouse |
|--------|----------|-------|
| Pan camera left/right | Q / E | Middle-drag |
| Pan camera forward/back | W / S | Middle-drag |
| Edge scroll | — | Move pointer to screen edge |
| Zoom in / out | Scroll wheel | Scroll wheel |
| Camera height up / down | Z / X | — |
| Rotate camera left / right | A / D | — |
| Select unit | — | Left click |
| Box multi-select | — | Left drag |
| Move command | — | Right click |
| Attack command | A + right click | A + right click |
| Ability 1–4 | 1 / 2 / 3 / 4 | Click ability button |
| Build menu | B | Click base |
| Pause | Escape | — |
| Confirm | Enter | — |
| Cancel | Escape | — |

---

## Mobile / Touch

### Single-finger gestures

| Gesture | Action |
|---------|--------|
| Tap | Select unit / building at tap location |
| Double-tap | Issue move command to tapped location |
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
| Virtual Joystick | Optional on-screen joystick (bottom-left) for camera panning |
| Camera Height Slider | Drag slider (HUD right edge) to adjust vertical camera height |
| Ability buttons | Enlarged HUD buttons (≥ 44 px touch targets) for abilities 1–4 |
| Build panel | Tap a base to open the build panel with large, spaced buttons |

> **Note**: Edge-of-screen scrolling is automatically disabled on touch devices; use two-finger drag instead.

---

## Adaptive UI Layouts

The UI automatically adapts to the detected screen size and orientation:

| Profile | Viewport | UI changes |
|---------|----------|------------|
| Desktop | ≥ 1280 × 720 | Compact HUD, standard button sizes, edge scroll enabled |
| Tablet Landscape | wide tablet | Medium HUD, larger buttons, edge scroll disabled |
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
    ...
  },
  "mouse": {
    "Select": "LeftButton",
    "MoveCommand": "RightButton"
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

Touch gesture-to-action mappings are read from the `touch` section of `controls.json`. Gesture thresholds (tap duration, long-press duration, double-tap window) are configurable via `GestureRecognizer` properties.

---

*Last Updated: 2026-05-31*
