# Multiplayer Architecture

## Overview

SharpOpenGL's multiplayer layer is designed around three principles:

1. **Determinism** — identical inputs must produce identical game states on every peer.
2. **Command authority** — all game-state mutations are expressed as serialisable commands.
3. **Replay-first** — the recording/playback system doubles as the network synchronisation mechanism.

---

## Command Pattern

Every player action that mutates game state is represented as an `IGameCommand`:

| Command         | Purpose                                      |
|-----------------|----------------------------------------------|
| `MoveCommand`   | Move one or more units to a world position   |
| `AttackCommand` | Order units to attack a target entity        |
| `BuildCommand`  | Queue a production order at a building       |
| `StopCommand`   | Halt all current actions                     |
| `UseAbilityCommand` | Activate a unit ability                 |

Commands carry a `PlayerId`, a simulation `Tick`, and their type-specific payload. All commands are JSON-serialisable via `CommandSerializer` so they can be transmitted over the network or written to a replay file.

```
Player input
    │
    ▼
IGameCommand (created, tick assigned)
    │
    ├─── CommandSerializer.Serialize() ──► JSON string ──► Network / Replay file
    │
    └─── CommandQueue.Enqueue() ──► awaits execution at correct tick
```

---

## Deterministic Game Loop

The `DeterministicClock` drives game logic at a fixed simulation rate (default: 20 ticks/second), independent of the render frame rate. This eliminates floating-point drift caused by variable `deltaTime`.

```
Real time elapsed
    │
    ▼
DeterministicClock.Advance(realDelta)
    │
    ▼  (each render frame)
while HasPendingTick:
    tick = ConsumeTick()
    commands = CommandQueue.DrainUpTo(tick)
    SimulationStep(tick, commands)          ← deterministic
    Renderer.Draw(lerp alpha = RenderAlpha) ← smooth at any frame rate
```

Key properties:
- Game logic runs at exactly `TicksPerSecond` Hz — never more, never less.
- `TickDuration` is a constant `1.0 / TicksPerSecond` seconds.
- `RenderAlpha` provides a [0, 1) interpolation value for smooth visual rendering between ticks.
- Floating-point accumulation is avoided by tracking total elapsed time rather than repeatedly subtracting.

### Sources of Non-Determinism (Eliminated)

| Source | Mitigation |
|--------|-----------|
| Variable `deltaTime` | Fixed-tick simulation; render delta used only for interpolation |
| `float` vs `double` precision | All simulation math uses `float` consistently (OpenTK `Vector3`); no mixed-precision operations |
| Dictionary/HashSet iteration order | ECS `ComponentPool<T>` uses indexed arrays; iteration is always in insertion order |
| Random number generation | `LocalGameSession.Seed` / `ReplayData.Seed` initialise PRNG at session start |
| System time calls | No `DateTime.Now` or `Environment.TickCount` in simulation code |

---

## Replay System

A game session can be recorded with `ReplayRecorder` and played back with `ReplayPlayer`.

```
Live session                            Replay
────────────────────────────────        ───────────────────────────────
recorder.Start(tick: 0)                 player = new ReplayPlayer(data)
  │                                       │
  ├─ recorder.Record(cmd) ◄── issued      ├─ player.FeedTick(tick, queue)
  │                                       │   └─ enqueues commands at tick
  ├─ recorder.Stop(endTick)             while !player.IsFinished:
  │                                         clock.Advance(...)
  └─ data = recorder.Export()              while clock.HasPendingTick:
                                               tick = clock.ConsumeTick()
                                               player.FeedTick(tick, queue)
                                               cmds = queue.DrainUpTo(tick)
                                               SimulationStep(tick, cmds)
```

`ReplayData` is a plain data record containing the seed and an array of `ReplayEntry` (tick + JSON). It can be serialised to disk or transmitted to a spectator.

---

## Network Message Protocol

`NetworkMessage` is the wire envelope for all peer-to-peer communication:

```json
{
  "messageType": "gameCommand",
  "senderId": 0,
  "sequenceNumber": 42,
  "payload": "{ \"type\": \"move\", \"playerId\": 0, \"tick\": 100, ... }"
}
```

Message types:

| Type | Direction | Description |
|------|-----------|-------------|
| `JoinRequest` | Client → Host | Request to join a lobby room |
| `JoinAck` | Host → Client | Confirms slot assignment |
| `LobbyState` | Host → All | Broadcast updated player list |
| `GameStart` | Host → All | Signals session start with seed |
| `GameCommand` | Any → All | A serialised `IGameCommand` |
| `SyncHeartbeat` | Any → All | Tick + state checksum for desync detection |
| `PlayerLeft` | Host → All | A peer disconnected |
| `GamePause` | Host → All | Pause (e.g. for lag) |
| `GameResume` | Host → All | Resume after pause |
| `Chat` | Any → All | In-session chat message |

`SequenceNumber` is monotonically increasing per sender and used for message ordering and deduplication.

### Desync Detection

Each peer computes a `uint` checksum of their simulation state after every N ticks and broadcasts a `SyncHeartbeat`. If checksums diverge between peers, the session is flagged as desynced. Recommended: checksum entity count + total health across all entities as a lightweight sanity check.

---

## Lobby / Room Architecture

```
LobbyRoom
    │
    ├─ Join(displayName) ──► LobbyPlayer (slot assigned)
    ├─ SetReady(slotIndex, true)
    ├─ TryStart(seed) ──► transitions to LobbyPhase.Starting
    ├─ ConfirmGameStarted() ──► LobbyPhase.InGame
    └─ ConfirmGameEnded() ──► LobbyPhase.PostGame
```

Phase transitions:

```
Waiting ──TryStart()──► Starting ──ConfirmGameStarted()──► InGame ──ConfirmGameEnded()──► PostGame
```

`TryStart` only succeeds when:
- The lobby is in `Waiting` phase
- At least one player is present
- All connected players have status `Ready`

Up to 8 players are supported per room. The first player to join is designated host.

---

## Local Split-Test Mode

`LocalGameSession` provides a no-network harness for testing the full command + replay pipeline locally with 1 or 2 players. Both players share a single `DeterministicClock` and separate `CommandQueue` instances.

```csharp
var session = new LocalGameSession(ticksPerSecond: 20, seed: 42, playerCount: 2);
session.Start();

// Issue commands
session.IssueCommand(new MoveCommand { PlayerId = 0, Tick = session.CurrentTick + 1, ... });
session.IssueCommand(new MoveCommand { PlayerId = 1, Tick = session.CurrentTick + 1, ... });

// Per render frame
session.Advance(deltaSeconds);
while (session.HasPendingTick)
{
    long tick = session.ConsumeTick();
    var p0Cmds = session.DrainCommandsForPlayer(0, tick);
    var p1Cmds = session.DrainCommandsForPlayer(1, tick);
    SimulatePlayer0(tick, p0Cmds);
    SimulatePlayer1(tick, p1Cmds);
}

session.Stop();
ReplayData p0Replay = session.ExportReplay(0);
```

---

## Future Implementation Notes

This phase delivers the **architecture and plumbing**. The following items are deferred to a dedicated networking phase:

- Real transport layer (TCP/UDP sockets for desktop; WebSocket/WebRTC for browser)
- Authoritative server vs. peer-to-peer evaluation
- Lag compensation and input prediction
- Full lobby matchmaking / room discovery
- Spectator stream delivery
- Replay file format versioning and schema migration
