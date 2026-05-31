# SharpOpenGL — Player Guide

Welcome to **SharpOpenGL Space RTS**, a real-time strategy game set in the depths of space.
Command your hero ship, build a fleet, gather resources, and complete missions across an expanding universe.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Controls](#2-controls)
3. [Resources](#3-resources)
4. [Units & Ships](#4-units--ships)
5. [Buildings](#5-buildings)
6. [Combat](#6-combat)
7. [Missions](#7-missions)
8. [Settings & Accessibility](#8-settings--accessibility)
9. [Saving & Loading](#9-saving--loading)
10. [Tips & Tricks](#10-tips--tricks)

---

## 1. Getting Started

### Desktop

1. Download the build for your platform from the [Releases](../../releases) page.
2. Extract the archive and run `SharpOpenGl` (or `SharpOpenGl.exe` on Windows).
3. From the **Main Menu**, choose **New Game** to begin the tutorial mission.

### Browser (WebGL2)

Visit the [GitHub Pages deployment](https://kylewiering.github.io/SharpOpenGl) and click **Play**.
No installation required — runs in any modern browser with WebGL2 support.

---

## 2. Controls

### Desktop (Keyboard + Mouse)

| Action | Input |
|--------|-------|
| Select unit | Left-click on a unit |
| Move selected unit | Right-click on map |
| Box-select multiple units | Left-click drag |
| Camera pan | WASD or arrow keys |
| Camera zoom | Mouse wheel |
| Camera edge-scroll | Move pointer to screen edge |
| Pause | Escape |
| Ability 1 | Q |
| Ability 2 | W |
| Ability 3 | E |

### Touch (Mobile / Tablet)

| Action | Gesture |
|--------|---------|
| Select unit | Tap |
| Move | Double-tap on map |
| Camera pan | One-finger drag on empty space |
| Camera zoom | Pinch |
| Ability | Tap ability button in HUD |

---

## 3. Resources

The game tracks four resource types displayed in the top bar:

| Icon | Name | Description |
|------|------|-------------|
| ⚡  | **Energy** | Primary currency — powers ships and abilities. |
| 🪨  | **Minerals** | Construction material — required to build most units. |
| 📊  | **Data** | Research currency — unlocks advanced units. |
| 👥  | **Crew** | Population cap — each ship consumes crew to operate. |

### Gathering Resources

- Fly a **Resource Collector** unit near a **Resource Node** to begin harvesting.
- Nodes deplete over time and respawn after ~2 minutes.
- Building additional harvesters increases income rate.

---

## 4. Units & Ships

### Hero Ship

Your hero is your most powerful unit and cannot be replaced if destroyed — the mission ends.
Keep it safe while using its high firepower to lead assaults.

### Fighter (Basic)

| Stat | Value |
|------|-------|
| HP | 100 |
| Speed | 80 |
| Weapon damage | 15 |
| Weapon range | 200 |
| Cost | 50 ⚡  75 🪨  1 👥 |

Fast and cheap — great for scouting and swarming light targets.

### Fighter (Advanced)

Upgraded fighter with improved shields, higher damage, and longer range.
Unlocked after completing Mission 01.

### Bomber (Basic)

| Stat | Value |
|------|-------|
| HP | 120 |
| Weapon damage | 60 |
| Fire rate | Slow |
| Cost | 80 ⚡  120 🪨  10 📊  2 👥 |

High damage per hit, ideal against buildings and capital ships.  Slow and vulnerable to fighters.

### Gunship (Basic)

All-rounder with medium HP, shields, and a balanced weapon.  Expensive but durable.
Unlocked after completing Mission 03.

### Capital Ship (Basic)

A massive vessel with enormous HP, shields, and a heavy cannon.
Slow to build and slow to move, but can anchor a battle line.
Unlocked after completing Mission 04.

---

## 5. Buildings

### Command Center

Your starting structure.  Loss of the command center is not immediately fatal but severely limits economy.

---

## 6. Combat

### Basics

- Units attack the nearest enemy automatically within their **weapon range**.
- Select units and right-click an enemy to **assign a target manually**.
- Units move to within weapon range before firing.

### Damage Formula

```
Final Damage = Base Damage × (100 / (100 + Armor)) − Shield Absorption
Minimum damage is always 1.
```

- **Shields** absorb damage first and do not regenerate automatically.
- **Armor** reduces incoming damage proportionally.

### Abilities

Abilities are activated with Q/W/E (keyboard) or by tapping the HUD buttons.
Each ability has a cooldown shown in the HUD.

| Ability | Effect | Cooldown |
|---------|--------|----------|
| Shield Boost | Restores 50 % of shields instantly | 30 s |
| EMP Burst | Disables enemy shields in an area for 5 s | 60 s |

---

## 7. Missions

### Campaign Missions

| # | Name | Summary |
|---|------|---------|
| 01 | First Contact | Tutorial — destroy an enemy scout. |
| 02 | Mineral Rush | Harvest 500 minerals before enemy reinforcements arrive. |
| 03 | Rescue Op | Escort an escape pod through a pirate blockade. |
| 04 | Station Defence | Hold the command station against three attack waves. |
| 05 | The Reckoning | Assault the enemy flagship; destroy four generators, then the dreadnought. |

### Objectives

- **Primary** objectives must all be completed for victory.
- **Secondary** objectives are optional but award bonus XP and resources.

### Triggers

Certain in-mission events spawn reinforcements, display dialogue, or pan the camera.
Pay attention to the **event log** on the left side of the HUD.

---

## 8. Settings & Accessibility

Open **Settings** from the main menu or pause screen.

### Accessibility

| Option | Description |
|--------|-------------|
| Colorblind Mode | None / Red-Green / Blue-Yellow / Monochrome |
| Font Scale | Increase UI text size (0.5 × – 3.0 ×) |
| Visual Alerts | Screen-edge flash on critical events (no audio required) |
| High-Contrast Selections | Thick white outlines on selected units |

### Graphics Quality

- **High**: All effects, maximum draw distance (desktop GPU).
- **Medium**: Reduced particles, shorter LOD — suitable for mid-range devices.
- **Low**: Minimal effects — targets 30 fps on low-end mobile.

---

## 9. Saving & Loading

- The game **auto-saves** at the start of each mission and when objectives are completed.
- Use **Save Game** in the pause menu to save manually to a named slot.
- Up to 10 save slots are supported. Slots are stored as JSON files in your user data directory.

| Platform | Save Location |
|----------|---------------|
| Windows  | `%APPDATA%\SharpOpenGl\saves\` |
| Linux/macOS | `~/.config/SharpOpenGl/saves/` |
| Browser  | Browser localStorage (auto only) |

---

## 10. Tips & Tricks

- **Scout early**: send a fighter ahead to reveal fog of war before committing your hero.
- **Protect harvesters**: resource collectors are weak — assign a fighter escort.
- **Focus fire**: manually target the same enemy with multiple units to burst it down before it can retreat.
- **Use terrain**: asteroids and debris act as line-of-sight blockers; use them for ambushes.
- **Save often**: manual saves before high-risk engagements let you retry without replaying the whole mission.
- **Secondary objectives** are worth completing — the extra XP accelerates unit unlocks.
