using OpenTK.Mathematics;

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
