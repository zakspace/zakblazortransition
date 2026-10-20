using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core.Configuration;

/// <summary>Engine settings. The transitions themselves are in <c>wwwroot/js/zakbt/transitions</c>.</summary>
public sealed class TransitionOptions
{
    /// <summary>Used when a link doesn't name one with <c>data-transition</c>.</summary>
    public string DefaultTransition { get; set; } = TransitionNames.Peel;

    /// <summary>
    /// On each navigation, keep the page covered at least this long (ms) before the
    /// next one is revealed, even if it is ready sooner. Counted from the end of the
    /// cover animation. 0 reveals the page as soon as it is ready.
    /// </summary>
    public int MinCoverTimeMs { get; set; }

    /// <summary>Cover the very first page load and reveal it, like a preloader. Off by default.</summary>
    public RevealOnLoadOptions? RevealOnLoad { get; set; }

    /// <summary>What a visitor who prefers reduced motion gets.</summary>
    public ReducedMotionMode ReducedMotion { get; set; } = ReducedMotionMode.None;

    /// <summary>
    /// Before revealing a page, wait for the images near the top to be decoded and
    /// the fonts to be ready, but never longer than this (ms).
    /// </summary>
    public int MediaTimeoutMs { get; set; } = 1500;

    /// <summary>
    /// Never wait longer than this (ms) for the destination page to render before
    /// revealing anyway, so a lost notification can't leave the page covered.
    /// </summary>
    public int RenderTimeoutMs { get; set; } = 4000;

    /// <summary>Run the page's content entrance animations as the page is revealed.</summary>
    public bool EntranceEnabled { get; set; } = true;

    /// <summary>The colour the engine paints the cover with, before a transition draws its own.</summary>
    public string CoverColor { get; set; } = "#111111";

    /// <summary>Validates the options and throws on anything the engine can't honour.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultTransition))
        {
            throw new ArgumentException($"{nameof(DefaultTransition)} must name a transition.", nameof(DefaultTransition));
        }

        if (DefaultTransition != TransitionNames.None && !TransitionCatalog.Contains(DefaultTransition))
        {
            throw new ArgumentException(
                $"No transition named \"{DefaultTransition}\". Known: {string.Join(", ", TransitionNames.All)}.",
                nameof(DefaultTransition));
        }

        if (MinCoverTimeMs < 0 || MediaTimeoutMs <= 0 || RenderTimeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinCoverTimeMs), "Timeouts and cover times must be positive.");
        }

        var reveal = RevealOnLoad?.Transition;
        if (reveal is not null && reveal != TransitionNames.None && !TransitionCatalog.Contains(reveal))
        {
            throw new ArgumentException($"No reveal transition named \"{reveal}\".", nameof(RevealOnLoad));
        }
    }
}
