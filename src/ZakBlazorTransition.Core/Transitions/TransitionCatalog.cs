namespace ZakBlazorTransition.Core.Transitions;

/// <summary>The names of the transitions the engine ships with.</summary>
public static class TransitionNames
{
    public const string Curtain = "curtain";
    public const string Wipe = "wipe";
    public const string Circle = "circle";
    public const string Columns = "columns";
    public const string Typewriter = "typewriter";
    public const string Stack = "stack";
    public const string SlideOver = "slide-over";
    public const string Peel = "peel";
    public const string Slices = "slices";
    public const string Frame = "frame";
    public const string Carousel = "carousel";
    public const string Cube = "cube";
    public const string Corner = "corner";
    public const string Tear = "tear";
    public const string Dither = "dither";
    public const string Dissolve = "dissolve";
    public const string Ink = "ink";
    public const string Spiral = "spiral";
    public const string Velvet = "velvet";
    public const string Particles = "particles";
    public const string Channel = "channel";

    /// <summary>Reserved name: swap the page with no animation.</summary>
    public const string None = "none";

    /// <summary>A plain cross-fade, used when <see cref="Configuration.ReducedMotionMode.Fade"/> asks for it.</summary>
    public const string Fade = "fade";

    /// <summary>All twenty-one transitions, in the order the demo lists them.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Curtain, Wipe, Circle, Columns, Typewriter,
        Stack, SlideOver, Peel, Slices, Frame, Carousel, Cube, Corner, Tear,
        Dither, Dissolve, Ink, Spiral, Velvet, Particles, Channel,
    ];
}

/// <summary>Every transition the engine knows, with the settings the .NET side needs.</summary>
public static class TransitionCatalog
{
    /// <summary>The twenty-one shipped transitions, grouped from the simplest kind to the most involved.</summary>
    public static readonly IReadOnlyList<TransitionDescriptor> All =
    [
        new() { Name = TransitionNames.Curtain, Group = TransitionGroup.Overlay, Entrance = 0.2, Order = 1 },
        new() { Name = TransitionNames.Wipe, Group = TransitionGroup.Overlay, Entrance = null, Order = 2 },
        new() { Name = TransitionNames.Circle, Group = TransitionGroup.Overlay, Entrance = null, Order = 3 },
        new() { Name = TransitionNames.Columns, Group = TransitionGroup.Overlay, Entrance = 0.2, Order = 4 },
        new() { Name = TransitionNames.Typewriter, Group = TransitionGroup.Overlay, Entrance = null, SolidCover = false, Order = 5 },

        new() { Name = TransitionNames.Stack, Group = TransitionGroup.PageComposition, Entrance = 0.2, SolidCover = false, Order = 6 },
        new() { Name = TransitionNames.SlideOver, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 7 },
        new() { Name = TransitionNames.Peel, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 8 },
        new() { Name = TransitionNames.Slices, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 9 },
        new() { Name = TransitionNames.Frame, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 10 },
        new() { Name = TransitionNames.Carousel, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 11 },
        new() { Name = TransitionNames.Cube, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 12 },
        new() { Name = TransitionNames.Corner, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 13 },
        new() { Name = TransitionNames.Tear, Group = TransitionGroup.PageComposition, Entrance = null, SolidCover = false, Order = 14 },

        new() { Name = TransitionNames.Dither, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 15 },
        new() { Name = TransitionNames.Dissolve, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 16 },
        new() { Name = TransitionNames.Ink, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 17 },
        new() { Name = TransitionNames.Spiral, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 18 },
        new() { Name = TransitionNames.Velvet, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 19 },
        new() { Name = TransitionNames.Particles, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 20 },
        new() { Name = TransitionNames.Channel, Group = TransitionGroup.WebGl, Entrance = null, SolidCover = false, UsesWebGl = true, Order = 21 },
    ];

    private static readonly IReadOnlyDictionary<string, TransitionDescriptor> ByName =
        All.ToDictionary(t => t.Name, StringComparer.Ordinal);

    /// <summary>Looks a transition up by name, or <c>null</c> when there's none with that name.</summary>
    public static TransitionDescriptor? Find(string? name) =>
        name is not null && ByName.TryGetValue(name, out var transition) ? transition : null;

    /// <summary>Whether a transition with this name ships with the engine.</summary>
    public static bool Contains(string name) => ByName.ContainsKey(name);

    /// <summary>The transitions of one group, in listing order.</summary>
    public static IEnumerable<TransitionDescriptor> InGroup(TransitionGroup group) =>
        All.Where(t => t.Group == group).OrderBy(t => t.Order);
}
