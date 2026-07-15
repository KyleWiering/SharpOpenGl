# SharpOpenGL — Player Guide

Welcome to **SharpOpenGL Space RTS**, a real-time strategy game set in the depths of space.
Command your hero ship, build a fleet, gather resources, and complete missions across an expanding universe.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Controls](#2-controls)
3. [Resources](#3-resources)
4. [Units & Ships](#4-units--ships)
5. [Buildings & Tech Tree](#5-buildings--tech-tree)
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

Full binding reference: [`docs/CONTROLS.md`](CONTROLS.md).

### Camera (desktop)

| Action | Input |
|--------|-------|
| Pan forward / back | W / S (also ↑ / ↓) |
| Strafe left / right | Q / E |
| Pan left / right (no units selected) | A / D |
| Camera override with units selected | Hold **Shift** + WASD / Q E Z X |
| Zoom | Mouse wheel |
| Pan (empty map) | Right-drag |
| Camera height | Z (up) / X (down) |

### Selection & movement

| Action | Input |
|--------|-------|
| Select unit | Left-click |
| Box-select | Left-drag |
| Move / attack / repair | Right-click *(fires on press when units are selected)* |
| Append waypoint | Shift + right-click ground |
| Select all friendly | Ctrl + A |
| Control group assign | Ctrl + 1–9 |
| Control group recall | 5–9 *(1–4 are abilities)* |

### Ship Control Bar (with units selected)

When military or worker ships are selected, the **Ship Control Bar** (bottom-right) exposes command modes. Keyboard shortcuts mirror the bar:

| Command | Key | How to use |
|---------|-----|------------|
| Move | **M** | Enter move mode, then left-click destination |
| Stop | **S** or **X** | Halt immediately |
| Patrol | **P** | Enter patrol mode, then left-click loop point |
| Attack | **T** | Enter attack mode, then click enemy |
| Attack-move | **F** or **A** | Enter attack-move, then click destination |
| Cycle stance | Stance button | Passive → Defensive → Aggressive |
| Hold position | **H** *(non-harvesters)* | Sets passive stance |
| Formation | **G** | Cycle line / wedge / box / column *(multi-select)* |
| Build | **B** | Open build map for builder ships |
| Harvest | **H** *(collectors only)* | Enter harvest assignment mode |

> **Note:** **H** assigns harvest mode when a resource collector is selected; otherwise it sets **hold position** (passive stance).

### Production & abilities

| Action | Input |
|--------|-------|
| Build map panel | **B** or click **Build** on a station |
| Set rally point | **R**, then right-click |
| Ability 1–4 | **1** / **2** / **3** / **4** or HUD buttons |
| Pause | Escape |

### Touch (mobile / tablet)

| Action | Gesture |
|--------|---------|
| Select | Tap |
| Move | Double-tap on map |
| Attack | Long-press on enemy |
| Camera pan | Two-finger drag or virtual joystick |
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
- Nodes deplete over time and **respawn after ~2 minutes** — a comeback lever when you lose map control.
- Building additional harvesters increases income rate.
- Use **H** or the **Harvest** button to assign collectors to a node.
- The unit info panel shows harvest mode and state (**harvesting**, **returning**, **depositing**) plus cargo fill.

### Comeback mechanics (skirmish & sandbox)

Trailing players can recover when:

| Mechanic | Effect |
|----------|--------|
| **Node respawn** | Depleted mineral/energy nodes refill after their countdown, reopening income routes |
| **Difficulty tiers** | Easy skirmish grants AI fewer starting resources; Hard grants more — tune in Settings → Skirmish |
| **AI retreat** | Enemy ships below 25% HP path home, giving breathing room to rebuild |

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

## 5. Buildings & Tech Tree

Structures are placed from the **Build Map** panel (**B** key or station **Build** button). Each building requires prerequisites from earlier tiers — locked entries show what you still need.

Data source: `GameData/Config/build_map.json` (16 structures across five branches).

### Production branch

| Building | Prerequisites | Role |
|----------|---------------|------|
| **Command Center** | *(starting base)* | Economy hub, light defense, unlocks all branches |
| **Small Shipyard** | Command Center + Power Reactor | Builds scouts, fighters, interceptors, drones, miners |
| **Medium Shipyard** | CC + Small Shipyard + Resource Refinery | Adds bombers, corvettes, frigates, destroyers, gunships, transports |
| **Large Shipyard** | CC + Medium Shipyard + Power Reactor | Capital ships: cruiser, carrier, dreadnought, freighter, support |

### Economy branch

| Building | Prerequisites | Role |
|----------|---------------|------|
| **Power Reactor** | Command Center | Gates shipyard tiers and powers advanced structures |
| **Resource Refinery** | Command Center | Boosts mineral throughput; required for medium shipyard |
| **Supply Depot** | Command Center | Extends supply range for distant operations |
| **Fabrication Hub** | CC + Refinery + Reactor | Advanced production bonuses |

### Defense branch

| Building | Prerequisites | Role |
|----------|---------------|------|
| **Defense Turret** | Command Center | Cheap point defense |
| **Sensor Array** | CC + Power Reactor | Extends vision; gates shield and missile tech |
| **Shield Emitter** | CC + Reactor + Sensor Array | Area shield coverage for nearby structures |
| **Missile Battery** | CC + Turret + Sensor Array | Long-range missile defense |

### Support branch

| Building | Prerequisites | Role |
|----------|---------------|------|
| **Repair Bay** | CC + Small Shipyard | Repairs docked ships between waves |
| **Comms Relay** | CC + Sensor Array | Coordination buffs; gates orbital uplink |

### Capstone branch

| Building | Prerequisites | Role |
|----------|---------------|------|
| **Orbital Uplink** | CC + Medium Shipyard + Sensor + Comms | Strategic tech capstone |
| **Fortress Core** | CC + Large Shipyard + Shield + Missile + Repair Bay | Ultimate defensive anchor |

### Typical build order

1. **Power Reactor** + **Resource Refinery** for economy.
2. **Small Shipyard** for fighters and miners.
3. **Sensor Array** + **Defense Turret** before the first attack wave.
4. Upgrade to **Medium** / **Large Shipyard** as missions unlock heavier hulls.
5. Capstones when the full prerequisite chain is satisfied.

### Command Center

Your starting structure.  Loss of the command center is not immediately fatal but severely limits economy.

---

## 6. Combat

### Basics

- Units attack the nearest enemy automatically within their **weapon range** when set to **Aggressive** stance.
- Select units and right-click an enemy to **assign a target manually** *(command fires on mouse press)*.
- Units move to within weapon range before firing.
- **Focus fire** by attack-clicking the same target with multiple selected units.

### Stances

| Stance | Behavior |
|--------|----------|
| **Passive** | Hold position; do not chase |
| **Defensive** | Attack enemies in range; do not pursue far |
| **Aggressive** | Pursue and engage hostiles |

Cycle stances with the **Stance** button on the Ship Control Bar, or press **H** (hold) / **V** (defensive) for quick overrides.

### Command modes

| Mode | Best for |
|------|----------|
| **Attack** (**T**) | Burst down a single priority target |
| **Attack-move** (**F** / **A**) | Advance while engaging along the path |
| **Patrol** (**P**) | Guard a lane between two points |
| **Stop** (**S** / **X**) | Cancel orders instantly |

### Formations

Select multiple ships and press **G** to cycle **Line → Wedge → Box → Column**. Formations keep escorts spaced while the group moves.

### Damage Formula

```
Final Damage = Base Damage × (100 / (100 + Armor)) − Shield Absorption
Minimum damage is always 1.
```

- **Shields** absorb damage first and regenerate slowly out of combat.
- **Armor** reduces incoming damage proportionally.

### Abilities

Abilities are activated with **1–4** (keyboard) or by tapping the HUD buttons.
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
| 04 | Station Defense | Hold the command station against three attack waves. |
| 05 | The Reckoning | Assault the enemy flagship; destroy four generators, then the dreadnought. |

### Objectives

- **Primary** objectives must all be completed for victory.
- **Secondary** objectives are optional but award bonus XP and resources.

### Victory & defeat overlay

When a mission ends, the **Mission Complete** (or **Mission Failed**) card shows:

| Field | Description |
|-------|-------------|
| Mission name | Display title from the briefing |
| Elapsed time | Minutes:seconds or seconds for short runs |
| XP earned | Victory only — from mission rewards |
| **Run stats** | Enemies destroyed, units lost, structures built/lost |
| Primary objectives | Checklist with ✓ / — markers |

Use **Replay Mission** to retry immediately or **Return to Menu** to exit.

### Triggers

Certain in-mission events spawn reinforcements, display dialogue, or pan the camera.
Pay attention to the **event log** on the left side of the HUD.

---

## 8. Settings & Accessibility

Open **Settings** from the main menu or pause screen. Options are grouped into four sections:

### Audio

| Control | Description |
|---------|-------------|
| Master / Music / SFX volume | Adjust each channel independently (± buttons) |

### Graphics

| Control | Description |
|---------|-------------|
| Quality Cycle | **High** / **Medium** / **Low** — effects and LOD budget |
| VSync Toggle | Reduce tearing on desktop displays |

### Controls

| Control | Description |
|---------|-------------|
| Pan / Zoom speed | Camera responsiveness multipliers |
| Edge Scroll | Enable pointer-at-screen-edge camera pan *(desktop)* |

### Accessibility

| Option | Description |
|--------|-------------|
| Font Scale | Increase UI text size (0.5 × – 3.0 ×) |
| Colorblind | None / Red-Green / Blue-Yellow / Monochrome — also adjusts team aura/insignia colors in gameplay |
| Visual Alerts | Screen-edge flash on critical events (no audio required) |
| Hi-Contrast | Thick white outlines on selected units |
| HUD Minimal | Hides non-essential HUD chrome for lower cognitive load |
| Skirmish difficulty | Default Easy / Normal / Hard for multiplayer setup |
| Key Rebind | Stub override counter — full remapping UI planned; overrides persist in settings JSON |

**HUD tooltips:** Hover over resource bar slots, minimap, build-map entries, and command buttons for contextual help (desktop). Touch layouts use tap-and-hold where supported.

### Graphics quality tiers

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
- **Attack-move into bases**: use **F** to advance through turret range while auto-engaging.
- **Use terrain**: asteroids and debris act as line-of-sight blockers; use them for ambushes.
- **Save often**: manual saves before high-risk engagements let you retry without replaying the whole mission.
- **Secondary objectives** are worth completing — the extra XP accelerates unit unlocks.
- **Report bugs**: open a [GitHub Issue](https://github.com/KyleWiering/SharpOpenGl/issues) with steps to reproduce.

---

*Last updated: 2026-07-14*