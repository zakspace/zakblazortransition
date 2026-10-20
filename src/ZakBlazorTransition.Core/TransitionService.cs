using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core;

/// <summary>
/// Keeps the engine's shared state: the phase, the navigation in flight, and the
/// signal the destination page sends when it has rendered. Registered once and
/// shared by the host and the pages.
/// </summary>
public sealed class TransitionService : ITransitionService
{
    private readonly object _gate = new();
    private TaskCompletionSource<bool>? _rendered;
    private CancellationTokenSource? _navigationCts;
    private int _sequence;

    public TransitionService(TransitionOptions options, ITransitionRegistry transitions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(transitions);
        Options = options;
        Transitions = transitions;
    }

    public TransitionOptions Options { get; }

    public ITransitionRegistry Transitions { get; }

    public TransitionPhase Phase { get; private set; } = TransitionPhase.Idle;

    public bool IsTransitioning => Phase is not TransitionPhase.Idle;

    public event EventHandler<TransitionPhaseChangedEventArgs>? PhaseChanged;

    /// <summary>The navigation currently in flight, if any. Read by the host.</summary>
    public NavigationRequest? CurrentRequest { get; private set; }

    /// <summary>The context of the navigation currently in flight, if any.</summary>
    public TransitionContext? CurrentContext { get; private set; }

    /// <summary>
    /// Cancelled when a newer navigation takes over, so the steps of an old one
    /// can stop waiting instead of holding the engine up.
    /// </summary>
    public CancellationToken NavigationToken
    {
        get
        {
            lock (_gate)
            {
                return _navigationCts?.Token ?? CancellationToken.None;
            }
        }
    }

    /// <summary>Starts tracking a navigation. Cancels any earlier one still in flight.</summary>
    public int BeginNavigation(NavigationRequest request, TransitionContext context)
    {
        lock (_gate)
        {
            _navigationCts?.Cancel();
            _navigationCts?.Dispose();
            _navigationCts = new CancellationTokenSource();
            _rendered = null;
            CurrentRequest = request;
            CurrentContext = context;
            return ++_sequence;
        }
    }

    /// <summary>Moves the engine to a new phase and raises <see cref="PhaseChanged"/>.</summary>
    public void SetPhase(TransitionPhase phase, TransitionContext? context = null)
    {
        Phase = phase;
        if (phase is TransitionPhase.Leaving or TransitionPhase.Covered)
        {
            lock (_gate)
            {
                _rendered = null; // any earlier signal belongs to the page we left
            }
        }

        PhaseChanged?.Invoke(this, new TransitionPhaseChangedEventArgs(phase, context));
    }

    /// <summary>True when the given navigation is still the one in flight.</summary>
    public bool IsCurrent(int sequence)
    {
        lock (_gate)
        {
            return sequence == _sequence;
        }
    }

    /// <summary>Stops tracking the navigation (it finished, failed or was superseded).</summary>
    public void EndNavigation(int sequence)
    {
        lock (_gate)
        {
            if (sequence == _sequence)
            {
                CurrentRequest = null;
                CurrentContext = null;
                _navigationCts?.Cancel();
                _navigationCts?.Dispose();
                _navigationCts = null;
            }
        }

        if (sequence == _sequence)
        {
            SetPhase(TransitionPhase.Idle);
        }
    }

    public void NotifyPageRendered()
    {
        TaskCompletionSource<bool>? pending;
        lock (_gate)
        {
            pending = _rendered;
        }

        pending?.TrySetResult(true);
    }

    /// <summary>
    /// Waits until <see cref="NotifyPageRendered"/> is called, or the timeout passes.
    /// Returns whether the page rendered in time, so the engine can reveal anyway.
    /// </summary>
    public async Task<bool> WaitForPageRenderAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool> pending;
        lock (_gate)
        {
            pending = _rendered ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, stop.Token);
        var completed = await Task.WhenAny(pending.Task, timeoutTask).ConfigureAwait(false);

        if (completed == pending.Task)
        {
            return true;
        }

        // Timed out or cancelled: stop the delay and let the caller reveal anyway.
        stop.Cancel();
        return false;
    }
}
