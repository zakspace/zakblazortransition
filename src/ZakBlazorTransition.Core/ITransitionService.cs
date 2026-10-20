using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core;

/// <summary>Raised when the engine moves between the phases of a navigation.</summary>
public sealed class TransitionPhaseChangedEventArgs(TransitionPhase phase, TransitionContext? context) : EventArgs
{
    public TransitionPhase Phase { get; } = phase;

    /// <summary>The navigation the phase belongs to, or <c>null</c> when a transition ended.</summary>
    public TransitionContext? Context { get; } = context;
}

/// <summary>
/// The engine's public surface: what a site reads to know whether a transition is
/// running, and what page components signal so the engine knows its content is on screen.
/// </summary>
public interface ITransitionService
{
    /// <summary>The settings the engine runs with.</summary>
    TransitionOptions Options { get; }

    /// <summary>The transitions that can play.</summary>
    ITransitionRegistry Transitions { get; }

    /// <summary>Where the engine is right now.</summary>
    TransitionPhase Phase { get; }

    /// <summary>True while a page is covered or being covered or revealed.</summary>
    bool IsTransitioning { get; }

    /// <summary>Raised on the renderer's thread whenever <see cref="Phase"/> changes.</summary>
    event EventHandler<TransitionPhaseChangedEventArgs>? PhaseChanged;

    /// <summary>
    /// Called by a component inside the routed page after every render, so the engine
    /// knows the destination content is on screen and can be revealed.
    /// </summary>
    void NotifyPageRendered();
}
