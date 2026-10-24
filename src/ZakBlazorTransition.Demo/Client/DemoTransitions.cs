using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Demo.Client;

/// <summary>
/// The demo's engine settings, in one place so the server (prerendering, which
/// decides how the first page is painted) and the client (which runs the engine)
/// can never disagree about them.
/// </summary>
public static class DemoTransitions
{
    public static void Configure(TransitionOptions options)
    {
        // Used by links that don't name a transition with data-transition.
        options.DefaultTransition = TransitionNames.Peel;

        // A visitor who asked for less motion still gets the page changed, gently.
        options.ReducedMotion = ReducedMotionMode.Fade;

        // Cover the very first load and reveal it, like a preloader.
        options.RevealOnLoad = new RevealOnLoadOptions
        {
            Transition = TransitionNames.Curtain,
            MinCoverTimeMs = 700,
        };
    }
}
