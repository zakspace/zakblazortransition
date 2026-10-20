namespace ZakBlazorTransition.Core.Transitions;

/// <summary>Which way a navigation goes, so a transition can mirror itself.</summary>
public enum TransitionDirection
{
    /// <summary>A link was followed, or the history went forward.</summary>
    Forward,

    /// <summary>The history went back.</summary>
    Back,
}
