namespace ZakBlazorTransition.Core;

/// <summary>Where the engine is in a navigation.</summary>
public enum TransitionPhase
{
    /// <summary>No navigation is running: the page is interactive.</summary>
    Idle,

    /// <summary>The current page is being covered.</summary>
    Leaving,

    /// <summary>The page is covered; the destination is being prepared.</summary>
    Covered,

    /// <summary>The destination page is being revealed.</summary>
    Entering,
}
