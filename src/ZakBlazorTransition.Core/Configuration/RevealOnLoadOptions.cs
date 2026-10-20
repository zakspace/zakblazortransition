namespace ZakBlazorTransition.Core.Configuration;

/// <summary>Settings for the transition that reveals the very first page load.</summary>
public sealed class RevealOnLoadOptions
{
    /// <summary>The transition that reveals the first page. <c>null</c> uses the default transition.</summary>
    public string? Transition { get; set; }

    /// <summary>
    /// Keep the first page covered at least this long (ms), counted from when the
    /// engine starts: for an intro, or a preloader of the site's own.
    /// </summary>
    public int MinCoverTimeMs { get; set; }
}
