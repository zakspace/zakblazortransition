namespace ZakBlazorTransition.Core.Transitions;

/// <summary>Answers which transitions exist and which one plays when a link doesn't name one.</summary>
public interface ITransitionRegistry
{
    /// <summary>Every transition the engine ships with, in listing order.</summary>
    IReadOnlyList<IPageTransition> All { get; }

    /// <summary>Looks a transition up by name, or <c>null</c> when there's none.</summary>
    IPageTransition? Find(string? name);

    /// <summary>Whether a transition with this name can play (including the internal ones).</summary>
    bool Has(string name);

    /// <summary>What plays when a navigation doesn't ask for one by name.</summary>
    IPageTransition? Default { get; }
}

/// <summary>The built-in registry: the catalog, plus the internal transitions the engine keeps for itself.</summary>
public sealed class TransitionRegistry : ITransitionRegistry
{
    private readonly string _defaultName;

    public TransitionRegistry(string defaultTransition)
    {
        _defaultName = defaultTransition;
        Default = Resolve(defaultTransition);
    }

    public IReadOnlyList<IPageTransition> All => TransitionCatalog.All;

    public IPageTransition? Default { get; }

    public IPageTransition? Find(string? name) => TransitionCatalog.Find(name);

    /// <summary>
    /// <c>none</c> swaps the page instantly; <c>fade</c> is the internal cross-fade the
    /// reduced-motion setting can ask for. The rest are the catalog's.
    /// </summary>
    public bool Has(string name) =>
        name is TransitionNames.None or TransitionNames.Fade || TransitionCatalog.Contains(name);

    /// <summary>The transition a navigation plays: the named one, or the default, or nothing.</summary>
    public IPageTransition? Resolve(string? name)
    {
        if (name is null or TransitionNames.None)
        {
            return null;
        }

        return Find(name) ?? Default;
    }
}
