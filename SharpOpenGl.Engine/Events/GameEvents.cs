using OpenTK.Mathematics;
using SharpOpenGl.Engine.Audio;

namespace SharpOpenGl.Engine.Events;

// ── Engine-level events ────────────────────────────────────────────────────

/// <summary>Fired when the application window is resized.</summary>
public record WindowResizedEvent(int Width, int Height);

/// <summary>Fired once per frame with elapsed time in seconds.</summary>
public record FrameUpdateEvent(float DeltaTime);

// ── Input events ───────────────────────────────────────────────────────────

/// <summary>Fired when a keyboard key is pressed.</summary>
public record KeyPressedEvent(int Key);

/// <summary>Fired when a keyboard key is released.</summary>
public record KeyReleasedEvent(int Key);

/// <summary>Fired on a touch tap or mouse click, with normalized screen coords.</summary>
public record PointerTappedEvent(Vector2 ScreenPosition, int Button);

// ── Game events ────────────────────────────────────────────────────────────

/// <summary>Fired when an entity's health reaches zero.</summary>
public record EntityDestroyedEvent(uint EntityId);

/// <summary>Fired when a resource amount changes for a player.</summary>
public record ResourceChangedEvent(int PlayerId, string ResourceType, float NewAmount);

/// <summary>Fired when a mission objective changes state.</summary>
public record ObjectiveChangedEvent(string MissionId, string ObjectiveId, bool Completed);

/// <summary>Fired when a game scene transition is requested.</summary>
public record SceneTransitionEvent(string FromScene, string ToScene);

// ── Combat events ──────────────────────────────────────────────────────────

/// <summary>Fired when a projectile hits a target entity.</summary>
public record ProjectileHitEvent(uint ProjectileId, uint TargetId, float Damage);

/// <summary>Fired when a unit deals damage to another entity after armor/shield reduction.</summary>
public record DamageDealtEvent(uint AttackerId, uint TargetId, float RawDamage, float FinalDamage);

/// <summary>Fired each repair tick for optional VFX hooks.</summary>
public record RepairTickEvent(uint RepairerId, uint TargetId);

/// <summary>Fired when a hero ability is activated.</summary>
public record AbilityActivatedEvent(uint CasterId, int Slot, string AbilityId);

/// <summary>Fired when an entity dies during combat (HP drops to zero).</summary>
public record UnitDiedEvent(uint VictimId, uint KillerId, int XpAwarded);

/// <summary>Visual scale for an explosion particle burst.</summary>
public enum ExplosionVfxKind
{
    /// <summary>Small spark on projectile or beam impact.</summary>
    Impact,

    /// <summary>Medium burst when a ship is destroyed.</summary>
    ShipDeath,

    /// <summary>Large burst when a station or base is destroyed.</summary>
    StationDeath,
}

/// <summary>Fired when a combat impact or destruction should spawn particle VFX.</summary>
public record ExplosionVfxEvent(Vector3 Position, ExplosionVfxKind Kind, float Scale = 1f);

/// <summary>Short-lived combat ring overlay kinds (line primitive, no mesh upload).</summary>
public enum CombatRingVfxKind
{
    /// <summary>Brief ring flash when shields deplete to zero.</summary>
    ShieldBreak,

    /// <summary>Expanding ring on ship destruction.</summary>
    DeathExpand,

    /// <summary>Race ultimate cast pulse around the hero.</summary>
    UltimateCast,
}

/// <summary>Fired when a brief line-ring overlay should expand at a world position.</summary>
public record CombatRingVfxEvent(
    Vector3 Position,
    CombatRingVfxKind Kind,
    float Radius = 7f,
    Vector4? Tint = null);

// ── Mission events ─────────────────────────────────────────────────────────

/// <summary>Fired when a mission transitions to the InProgress phase.</summary>
public record MissionStartedEvent(string MissionId);

/// <summary>Fired when all primary objectives are complete (victory).</summary>
public record MissionVictoryEvent(string MissionId);

/// <summary>Fired when a defeat condition is met.</summary>
public record MissionDefeatEvent(string MissionId, string Reason);

/// <summary>Fired when a trigger fires its actions.</summary>
public record TriggerFiredEvent(string MissionId, string TriggerId);

/// <summary>Fired by scripted events to display in-game dialog.</summary>
public record DialogEvent(string Speaker, string Text);

/// <summary>Fired when the player requests a mission restart.</summary>
public record MissionReplayRequestedEvent(string MissionId);

// ── Audio events ───────────────────────────────────────────────────────────

/// <summary>
/// Fired when a sound effect should play at a world-space position.
/// Listeners (e.g. <see cref="IAudioManager"/>) convert this to an AL call.
/// </summary>
public record SoundRequestedEvent(AudioEventType EventType, Vector3 WorldPosition);

/// <summary>Fired to request a music track by logical ID.</summary>
public record MusicRequestedEvent(string TrackId, bool Loop = true, float CrossfadeSeconds = 1.0f);

/// <summary>Fired to request music fade-out and stop.</summary>
public record MusicStopRequestedEvent(float FadeOutSeconds = 1.0f);

/// <summary>Fired when the player changes a volume setting.</summary>
public record VolumeChangedEvent(string Channel, float NewValue);
