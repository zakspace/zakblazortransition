using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core.Tests;

public class TransitionServiceTests
{
    private static TransitionService NewService() =>
        new(new TransitionOptions(), new TransitionRegistry(TransitionNames.Peel));

    private static NavigationRequest Request(string to) => new()
    {
        From = new Uri("http://localhost/"),
        To = new Uri($"http://localhost/{to}"),
        RequestedTransition = TransitionNames.Wipe,
    };

    private static TransitionContext Context(string name = TransitionNames.Wipe) => new()
    {
        Transition = name,
        From = new Uri("http://localhost/"),
        To = new Uri("http://localhost/next"),
    };

    [Fact]
    public void BeginNavigation_increments_the_sequence_and_records_the_request()
    {
        var service = NewService();
        var first = service.BeginNavigation(Request("one"), Context());
        var second = service.BeginNavigation(Request("two"), Context());

        Assert.Equal(first + 1, second);
        Assert.True(service.IsCurrent(second));
        Assert.Equal(new Uri("http://localhost/two"), service.CurrentRequest!.To);
    }

    [Fact]
    public void A_newer_navigation_cancels_the_previous_token()
    {
        var service = NewService();
        service.BeginNavigation(Request("one"), Context());
        var before = service.NavigationToken;
        service.BeginNavigation(Request("two"), Context());

        Assert.True(before.IsCancellationRequested);
        Assert.False(service.NavigationToken.IsCancellationRequested);
    }

    [Fact]
    public void EndNavigation_clears_state_and_returns_to_idle()
    {
        var service = NewService();
        var sequence = service.BeginNavigation(Request("one"), Context());
        service.SetPhase(TransitionPhase.Covered, Context());

        service.EndNavigation(sequence);

        Assert.Equal(TransitionPhase.Idle, service.Phase);
        Assert.False(service.IsTransitioning);
        Assert.Null(service.CurrentRequest);
        Assert.Null(service.CurrentContext);
    }

    [Fact]
    public void An_old_navigation_ending_does_not_disturb_the_current_one()
    {
        var service = NewService();
        var first = service.BeginNavigation(Request("one"), Context());
        var second = service.BeginNavigation(Request("two"), Context());
        service.SetPhase(TransitionPhase.Covered, Context());

        service.EndNavigation(first);

        Assert.True(service.IsCurrent(second));
        Assert.NotNull(service.CurrentRequest);
        Assert.Equal(TransitionPhase.Covered, service.Phase); // untouched by the stale end
    }

    [Fact]
    public void SetPhase_raises_PhaseChanged_with_the_context()
    {
        var service = NewService();
        var seen = new List<(TransitionPhase Phase, TransitionContext? Context)>();
        service.PhaseChanged += (_, e) => seen.Add((e.Phase, e.Context));

        var context = Context(TransitionNames.Cube);
        service.SetPhase(TransitionPhase.Leaving, context);
        service.SetPhase(TransitionPhase.Idle);

        Assert.Equal(
            [(TransitionPhase.Leaving, context), (TransitionPhase.Idle, (TransitionContext?)null)],
            seen);
    }

    [Fact]
    public async Task WaitForPageRender_resolves_when_the_page_reports_in()
    {
        var service = NewService();
        service.BeginNavigation(Request("one"), Context());
        service.SetPhase(TransitionPhase.Covered, Context());

        var wait = service.WaitForPageRenderAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        service.NotifyPageRendered();

        Assert.True(await wait);
    }

    [Fact]
    public async Task WaitForPageRender_gives_up_at_the_timeout()
    {
        var service = NewService();
        service.BeginNavigation(Request("one"), Context());

        var rendered = await service.WaitForPageRenderAsync(TimeSpan.FromMilliseconds(40), CancellationToken.None);

        Assert.False(rendered);
    }

    [Fact]
    public async Task Going_back_to_leaving_forgets_an_earlier_rendered_signal()
    {
        var service = NewService();
        service.BeginNavigation(Request("one"), Context());
        service.SetPhase(TransitionPhase.Covered, Context());

        var first = service.WaitForPageRenderAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        service.NotifyPageRendered();
        Assert.True(await first);

        // The destination failed to arrive and the engine retries the cover:
        // the old page's signal must not satisfy the new wait.
        service.SetPhase(TransitionPhase.Leaving, Context());
        var second = service.WaitForPageRenderAsync(TimeSpan.FromMilliseconds(40), CancellationToken.None);
        Assert.False(await second);
    }

    [Fact]
    public async Task A_newer_navigation_releases_the_waiting_page()
    {
        var service = NewService();
        service.BeginNavigation(Request("one"), Context());
        var token = service.NavigationToken;
        var wait = service.WaitForPageRenderAsync(TimeSpan.FromSeconds(30), token);

        service.BeginNavigation(Request("two"), Context());

        // The old navigation's token was cancelled: its wait gives up instead of
        // holding the engine hostage.
        Assert.False(await wait);
    }
}
