namespace SharpOpenGl.Engine.UI;

/// <summary>Describes the operation that caused a UI screen change.</summary>
public enum UIScreenAction
{
    /// <summary>A screen was pushed onto the stack.</summary>
    Pushed,

    /// <summary>The topmost screen was popped.</summary>
    Popped,

    /// <summary>The topmost screen was replaced with a new one.</summary>
    Replaced,
}

/// <summary>Fired by <see cref="UIManager"/> whenever the screen stack changes.</summary>
public record UIScreenChangedEvent(string Previous, string Current, UIScreenAction Action);
