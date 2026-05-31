# Implementation Plan

This plan covers the next set of features and improvements for the SharpOpenGl space RTS project. Work items are ordered by dependency — earlier items unblock later ones.

---

## 1. Repository Cleanup

The codebase has accumulated scaffolding and placeholder code across many phases. This step brings it to a maintainable baseline.

### Tasks

- [ ] Remove dead code, unused files, and empty placeholder classes that have no implementation
- [ ] Consolidate duplicated logic (e.g. redundant component definitions)
- [ ] Ensure the solution builds cleanly with `dotnet build` and all tests pass
- [ ] Enforce the 300-line file limit — split oversized files
- [ ] Remove or update stale comments and TODO markers
- [ ] Verify `GameData/` JSON files are valid and match their `_template.json` schemas
- [ ] Update `readme.md` to reflect current project state
- [ ] Clean up `.gitignore` and remove any committed build artifacts

### Acceptance Criteria

- `dotnet build` succeeds with no warnings
- All existing tests pass
- No file exceeds 300 lines
- No dead/unreachable code remains

---

## 2. Unexplored Map Areas (Fog of War)

Players should see unexplored portions of the map rendered distinctly (darkened/hidden) until a ship visits the area.

### Tasks

- [ ] Verify `FogOfWar` system (`SharpOpenGl.Engine/Grid/FogOfWar.cs`) correctly tracks cell visibility states (Unexplored, Explored, Visible)
- [ ] Implement rendering differentiation: unexplored cells are blacked out, explored-but-not-visible cells are dimmed, visible cells are fully lit
- [ ] Tie fog state updates to ship positions — when a ship moves, reveal cells within its sight radius
- [ ] Add a `SightRadiusComponent` to entities that can reveal the map
- [ ] Ensure fog state persists across save/load
- [ ] Write unit tests for fog reveal logic

### Acceptance Criteria

- New game starts with entire map unexplored (black)
- Moving a ship reveals cells within its sight radius
- Previously-visited cells remain in "explored" state (dimmed but visible terrain)
- Fog state survives save/load

---

## 3. Ship Movement to Destinations

Ships should be commandable to move to a specific map location.

### Tasks

- [ ] Implement a `DestinationComponent` that holds target grid coordinates
- [ ] Extend `MovementSystem` to consume `DestinationComponent` — move entity toward destination each frame using speed/acceleration from `MovementComponent`
- [ ] Stop movement and remove `DestinationComponent` when entity arrives (within threshold)
- [ ] Support click-to-move: player right-clicks a location, selected ship gets a `DestinationComponent`
- [ ] Handle obstacle avoidance using the pathfinding system (see item 7)
- [ ] Write unit tests for movement toward destination and arrival detection

### Acceptance Criteria

- Player can command a ship to move to any walkable cell
- Ship follows a path and stops upon arrival
- Movement respects speed/acceleration values from JSON definition

---

## 4. Map Generation

Procedurally generated maps with varied terrain, resource placement, and spawn points.

### Tasks

- [ ] Review and complete `MapGenerator` (`SharpOpenGl.Engine/Grid/MapGenerator.cs`)
- [ ] Implement configurable generation parameters: grid size, terrain density, resource node count, spawn point count
- [ ] Implement terrain region placement: asteroid fields, nebulae, debris — with randomized shapes and positions
- [ ] Place resource nodes with minimum distance constraints
- [ ] Place player spawn points far from each other
- [ ] Generate a `MapData` JSON output that conforms to the existing map schema
- [ ] Add a seed parameter for reproducible generation
- [ ] Write tests verifying generated maps meet constraints (valid terrain, reachable spawns, correct resource count)

### Acceptance Criteria

- `MapGenerator.Generate(config)` produces a valid `MapData`
- Generated maps have varied layouts across different seeds
- Spawns are reachable from each other (no walled-off areas)
- Resource nodes are distributed, not clustered

---

## 5. Example Scenario on a Map

A playable example scenario that demonstrates ships, a map, objectives, and basic gameplay.

### Tasks

- [ ] Create `GameData/Missions/example_scenario.json` defining a small scenario
- [ ] Scenario uses a generated or hand-crafted map (can reference `sector_alpha` or a new map)
- [ ] Place 2–3 player ships and 2–3 enemy ships at defined spawn points
- [ ] Define a simple objective (e.g., "Move your fleet to the waypoint" or "Destroy enemy scouts")
- [ ] Include at least one trigger event (e.g., reinforcements spawn after 60 seconds)
- [ ] Verify the scenario loads and runs through the `MissionLoader` → `MissionState` → `ObjectiveSystem` pipeline
- [ ] Document how to run the example scenario in the readme

### Acceptance Criteria

- Scenario loads without errors
- Objectives display and can be completed
- Triggers fire at specified conditions
- Serves as a template for creating future scenarios

---

## 6. Example Ships

A set of distinct, playable ship definitions demonstrating the data-driven ship system.

### Tasks

- [ ] Create `GameData/Ships/scout_light.json` — fast, low HP, large sight radius
- [ ] Create `GameData/Ships/cruiser_heavy.json` — slow, high HP, heavy weapons
- [ ] Create `GameData/Ships/transport_cargo.json` — medium speed, high cargo, no weapons
- [ ] Ensure all ship JSON files conform to `_template.json` schema
- [ ] Verify each ship loads correctly through `ShipFactory`
- [ ] Assign distinct procedural mesh parameters or fallback meshes to each ship type
- [ ] Write tests that load each ship definition and validate component values

### Acceptance Criteria

- At least 3 new distinct ship types defined in JSON
- All ships spawn correctly in-game via `ShipFactory`
- Ships have visually distinct representations (different shapes/colors)
- Ship stats (speed, HP, weapons) vary meaningfully across types

---

## 7. Pathing

Ships navigate around obstacles using the A* pathfinding system.

### Tasks

- [ ] Verify `Pathfinding.cs` A* implementation handles all terrain types (walkable, blocked, slow)
- [ ] Integrate pathfinding with `MovementSystem`: when a `DestinationComponent` is set, compute a path and follow waypoints
- [ ] Implement path recalculation when obstacles change (e.g., new buildings placed)
- [ ] Add terrain cost modifiers: nebula cells slow movement, asteroid cells are impassable
- [ ] Implement smooth path following (interpolation between waypoints, not teleporting cell-to-cell)
- [ ] Handle unreachable destinations gracefully (move as close as possible, notify player)
- [ ] Write tests for pathfinding around obstacles, through varied terrain costs, and unreachable targets

### Acceptance Criteria

- Ships route around impassable terrain
- Ships slow down in nebula/difficult terrain
- Path updates when map changes
- Unreachable destinations do not crash the game

---

## 8. Automatic Ship Movement to Destinations

Ships should autonomously move to assigned destinations without continuous player input.

### Tasks

- [ ] Implement an `AutoMoveSystem` that processes entities with `DestinationComponent` each frame without player interaction
- [ ] Support waypoint queues: ships can have multiple sequential destinations (`WaypointQueueComponent`)
- [ ] On arrival at each waypoint, advance to the next; on final arrival, mark complete
- [ ] Integrate with fog of war: auto-moving ships reveal cells as they travel
- [ ] Support patrol behavior: loop through waypoints continuously
- [ ] Add events for arrival (`ShipArrivedEvent`) so other systems can react
- [ ] Write tests for waypoint progression, patrol looping, and arrival events

### Acceptance Criteria

- Ships move to destinations without further player commands
- Waypoint queues process in order
- Patrol mode loops indefinitely
- Arrival events fire correctly for each waypoint

---

## 9. Control Bar with Buttons for Each Ship

A UI panel showing action buttons for the currently selected ship.

### Tasks

- [ ] Create a `ShipControlBar` widget (extends `Panel`) that appears when a ship is selected
- [ ] Display buttons for: Move, Stop, Patrol, Attack-Move, and any ship-specific abilities
- [ ] Buttons reflect ship capabilities (e.g., transport has no Attack button)
- [ ] Highlight active command mode (e.g., Move mode highlighted while awaiting click)
- [ ] Support keyboard shortcuts mapped to each button
- [ ] Integrate with existing `SelectionComponent` and `UnitInfoPanel`
- [ ] Position control bar at bottom-center of screen, responsive to screen size
- [ ] Write tests for button visibility logic based on ship type

### Acceptance Criteria

- Selecting a ship shows the control bar
- Buttons correspond to the ship's available actions
- Clicking a button activates the corresponding command mode
- Deselecting hides the control bar
- Works on both desktop (click) and mobile (tap)

---

## 10. Ship Stance System (Neutral, Defensive, Aggressive)

Ships have a configurable combat stance that governs their automatic behavior.

### Tasks

- [ ] Create a `StanceComponent` with enum values: `Neutral`, `Defensive`, `Aggressive`
- [ ] Define stance behaviors:
  - **Neutral**: Ship does not auto-engage enemies; only responds to explicit player commands
  - **Defensive**: Ship auto-attacks enemies that come within a defensive radius or attack it first
  - **Aggressive**: Ship auto-acquires and pursues any enemy within its weapon range
- [ ] Integrate stance with `CombatSystem` targeting logic
- [ ] Add a stance toggle button to the `ShipControlBar` (cycles N → D → A)
- [ ] Persist stance in save data
- [ ] Default stance for new ships defined in ship JSON (`"defaultStance": "defensive"`)
- [ ] Write tests for each stance behavior (ignore enemy, react to attack, pursue enemy)

### Acceptance Criteria

- Ships respect their stance setting at all times
- Stance is visible in the UI (icon or label on control bar)
- Stance persists across save/load
- Each stance produces distinct observable behavior in combat scenarios

---

## Dependency Graph

```
1. Cleanup
   └─► 4. Map Generation
        └─► 2. Fog of War (unexplored areas)
             └─► 5. Example Scenario
   └─► 6. Example Ships
        └─► 5. Example Scenario
   └─► 7. Pathing
        └─► 3. Ship Movement
             └─► 8. Automatic Movement
                  └─► 5. Example Scenario
   └─► 9. Control Bar
        └─► 10. Stance System
```

## Suggested Implementation Order

1. Repository Cleanup
2. Map Generation
3. Pathing
4. Ship Movement to Destinations
5. Automatic Ship Movement
6. Fog of War (Unexplored Areas)
7. Example Ships
8. Control Bar UI
9. Ship Stance System
10. Example Scenario (ties everything together)
