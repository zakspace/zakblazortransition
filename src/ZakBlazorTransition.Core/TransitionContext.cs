using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core;

/// <summary>Where on screen a navigation started, in CSS pixels.</summary>
public readonly record struct TransitionOrigin(double X, double Y)
{
    public static TransitionOrigin FromPoint(double x, double y) => new(x, y);

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Everything a transition is given to work with. It is passed to the JavaScript
/// side, so no DOM references cross the boundary: the script resolves the layer
/// and the page content from the document.
/// </summary>
public sealed record TransitionContext
{
    /// <summary>The name of the transition in use, or <c>none</c>.</summary>
    public required string Transition { get; init; }

    /// <summary>The page being left.</summary>
    public required Uri From { get; init; }

    /// <summary>The page being revealed.</summary>
    public required Uri To { get; init; }

    /// <summary><c>back</c> for history navigation backwards, so a transition can play mirrored.</summary>
    public TransitionDirection Direction { get; init; } = TransitionDirection.Forward;

    /// <summary>Where the navigation started on screen: the pointer, or the trigger's centre. Null when the engine works it out itself.</summary>
    public TransitionOrigin? Origin { get; init; }

    /// <summary>The visitor prefers reduced motion, as of the navigation's start.</summary>
    public bool ReducedMotion { get; init; }

    /// <summary>True on the very first page load (there was no cover animation).</summary>
    public bool Initial { get; init; }

    /// <summary>
    /// When the page's own content may start coming in, in seconds from the start of
    /// the reveal. <c>null</c> when the transition brings the content in itself.
    /// </summary>
    public double? Entrance { get; init; }
}
