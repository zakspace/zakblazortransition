using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Demo.Client;

/// <summary>Where a demo page is set: the editorial grounds, as <c>data-ground</c> on the body.</summary>
public static class Grounds
{
    public const string White = "white";
    public const string Grey = "grey";
    public const string Night = "night";
}

/// <summary>
/// The demo's own words about each transition: what it does, and the ground it
/// looks best on. This is demo content, not engine data — the engine works
/// without it.
/// </summary>
public static class DemoCatalog
{
    /// <summary>A short line for the index, shown beside each name.</summary>
    public static string Talk(string name) => name switch
    {
        TransitionNames.Curtain => "a split panel slides across, and the page comes in behind it",
        TransitionNames.Wipe => "the cover wipes the screen, from the edge you'd expect",
        TransitionNames.Circle => "a circle grows from where you clicked to the farthest corner",
        TransitionNames.Columns => "columns cover one after another, like a stage wall",
        TransitionNames.Typewriter => "a grid of cells types the page away, letter by letter",
        TransitionNames.Stack => "the page lifts like a card and stacks behind the next",
        TransitionNames.SlideOver => "the new page slides over the old one, which holds its ground",
        TransitionNames.Peel => "a sheet of the page curls away, fold and all",
        TransitionNames.Slices => "the page breaks into columns that push each other out",
        TransitionNames.Frame => "a red frame grows, with the next page opening inside it",
        TransitionNames.Carousel => "the page zooms out into a row of the site's pages",
        TransitionNames.Cube => "the page is one face of a box that turns to the next",
        TransitionNames.Corner => "red, black, then the page itself grow from the corner",
        TransitionNames.Tear => "the page tears in two, and the halves pull apart",
        TransitionNames.Dither => "dots fill squares of every size, big to small",
        TransitionNames.Dissolve => "blotches burn across the screen with a glowing rim",
        TransitionNames.Ink => "a red blot spreads from your click, ragged-edged",
        TransitionNames.Spiral => "a whirlpool with a burning rim closes over the page",
        TransitionNames.Velvet => "a stage curtain draws, hem trailing, lights dimming",
        TransitionNames.Particles => "drifting points gather into a word, and burst",
        TransitionNames.Channel => "the signal goes: bands, scanlines, and the page jumping",
        _ => "a page transition",
    };

    /// <summary>The ground (page background) the demo page for a transition uses.</summary>
    public static string Ground(string name) => name switch
    {
        TransitionNames.Columns or TransitionNames.Stack or TransitionNames.Slices
            or TransitionNames.Frame or TransitionNames.Carousel or TransitionNames.Cube
            or TransitionNames.Corner or TransitionNames.Dither or TransitionNames.Ink
            or TransitionNames.Spiral or TransitionNames.Velvet
            => Grounds.Grey,
        TransitionNames.Dissolve or TransitionNames.Particles or TransitionNames.Channel
            => Grounds.Night,
        _ => Grounds.White,
    };
}
