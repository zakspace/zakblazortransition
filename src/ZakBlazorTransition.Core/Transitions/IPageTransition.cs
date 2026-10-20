namespace ZakBlazorTransition.Core.Transitions;

/// <summary>
/// A transition the engine can play: its identity and the settings the .NET side
/// needs. The drawing itself lives in JavaScript (see <c>wwwroot/js/zakbt/transitions</c>),
/// which never leaks into this contract.
/// </summary>
public interface IPageTransition
{
    /// <summary>Unique name, used as <c>data-transition="name"</c> on links.</summary>
    string Name { get; }

    /// <summary>Which family the transition belongs to; the index groups by it.</summary>
    TransitionGroup Group { get; }

    /// <summary>
    /// Seconds into <c>enter</c> when the page's own content may start coming in,
    /// or <c>null</c> when the transition brings the content in itself.
    /// </summary>
    double? Entrance { get; }

    /// <summary>True when the transition needs three.js (loaded only then).</summary>
    bool UsesWebGl { get; }

    /// <summary>
    /// False when the transition hides the page some other way (its own canvas or a
    /// copy of the page) instead of painting a solid cover.
    /// </summary>
    bool SolidCover { get; }
}
