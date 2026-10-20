namespace ZakBlazorTransition.Core.Transitions;

/// <summary>The registry's description of one transition.</summary>
public sealed class TransitionDescriptor : IPageTransition
{
    public required string Name { get; init; }

    public TransitionGroup Group { get; init; }

    public double? Entrance { get; init; }

    public bool UsesWebGl { get; init; }

    public bool SolidCover { get; init; } = true;

    /// <summary>Order inside its group, as the index lists them.</summary>
    public int Order { get; init; }

    public override string ToString() => Name;
}
