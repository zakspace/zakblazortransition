using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core;

/// <summary>A navigation the engine has been asked to animate.</summary>
public sealed record NavigationRequest
{
    /// <summary>The absolute URI to go to.</summary>
    public required Uri To { get; init; }

    /// <summary>The page being left.</summary>
    public required Uri From { get; init; }

    /// <summary>The transition the link asked for, before reduced motion overrides it.</summary>
    public required string RequestedTransition { get; init; }

    /// <summary>How the navigation started.</summary>
    public TransitionDirection Direction { get; init; } = TransitionDirection.Forward;

    /// <summary>True when the browser's history moved (back/forward), rather than a link.</summary>
    public bool IsTraverse { get; init; }

    /// <summary>Where on screen the navigation started, in CSS pixels. Null when the engine works it out itself.</summary>
    public TransitionOrigin? Origin { get; init; }

    /// <summary>True when nothing should be animated: an instant swap with the same hooks.</summary>
    public bool ForceInstant { get; init; }
}
