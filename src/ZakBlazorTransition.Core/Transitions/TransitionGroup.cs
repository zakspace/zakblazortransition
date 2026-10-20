namespace ZakBlazorTransition.Core.Transitions;

/// <summary>How a transition draws: over the page, with the page, or in WebGL.</summary>
public enum TransitionGroup
{
    /// <summary>A cover is drawn above the page (curtain, wipe, circle, columns, typewriter).</summary>
    Overlay,

    /// <summary>The page itself is copied, moved, cut or turned (stack, peel, cube, tear…).</summary>
    PageComposition,

    /// <summary>A WebGL shader or scene draws the cover (dither, ink, particles…).</summary>
    WebGl,
}
