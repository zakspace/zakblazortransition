namespace ZakBlazorTransition.Core.Configuration;

/// <summary>What a visitor who prefers reduced motion gets instead of a transition.</summary>
public enum ReducedMotionMode
{
    /// <summary>Pages swap with no animation at all.</summary>
    None,

    /// <summary>Pages swap with a short cross-fade (the internal <c>fade</c> transition).</summary>
    Fade,
}
