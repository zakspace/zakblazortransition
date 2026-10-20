using Microsoft.Playwright;
using Xunit;

namespace ZakBlazorTransition.PlaywrightTests;

[Collection("Demo web")]
public class TransitionEngineTests : IAsyncLifetime
{
    private readonly DemoWebFixture _fixture;
    private readonly List<IPage> _pages = [];

    public TransitionEngineTests(DemoWebFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var page in _pages)
        {
            await page.CloseAsync();
        }
    }

    /// <summary>Opens the demo and waits until the engine is up (the layer reports idle).</summary>
    private async Task<(IPage Page, List<string> Errors)> OpenAsync(
        string path = "/", IBrowser? browser = null, BrowserNewContextOptions? options = null)
    {
        var context = await (browser ?? _fixture.Browser).NewContextAsync(options ?? new());
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, message) => errors.Add(message);
        await page.GotoAsync(DemoWebFixture.Url + path);
        await WaitForEngineAsync(page);
        _pages.Add(page);
        return (page, errors);
    }

    private static Task WaitForEngineAsync(IPage page) =>
        page.WaitForFunctionAsync(
            "document.querySelector('[data-zakbt]')?.getAttribute('data-state') === 'idle'",
            null, new() { Timeout = 60_000 });

    /// <summary>Records every data-state the layer goes through, deduplicated.</summary>
    private static async Task InstallStateSamplerAsync(IPage page) =>
        await page.EvaluateAsync(
            """
            () => {
              window.__states = [];
              const layer = document.querySelector('[data-zakbt]');
              const record = () => {
                const s = layer.getAttribute('data-state');
                if (s && window.__states.at(-1) !== s) window.__states.push(s);
              };
              new MutationObserver(record)
                  .observe(layer, { attributes: true, attributeFilter: ['data-state'] });
              record();
              window.__leaves = [];
              document.addEventListener('zakbt:leave',
                  (e) => window.__leaves.push({ t: e.detail?.transition, d: e.detail?.direction }));
            }
            """);

    private static Task WaitForIdleAsync(IPage page) =>
        page.WaitForFunctionAsync(
            "document.querySelector('[data-zakbt]').getAttribute('data-state') === 'idle'",
            null, new() { Timeout = 30_000 });

    private static async Task ClickTransitionAsync(IPage page, string name) =>
        await page.Locator($".index a[data-transition=\"{name}\"]").First.ClickAsync();

    [Fact]
    public async Task Index_lists_all_twenty_one_transitions()
    {
        var (page, errors) = await OpenAsync();

        var links = page.Locator(".index a[data-transition]");
        Assert.Equal(21, await links.CountAsync());
        Assert.Equal("/transition/curtain", await links.First.GetAttributeAsync("href"));
        Assert.Contains("particles", await page.ContentAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Deep_link_opens_a_transition_page()
    {
        var (page, errors) = await OpenAsync("/transition/tear");

        Assert.Equal("tear", await page.Locator(".detail-title").InnerTextAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Wipe_navigation_animates_the_layer_and_swaps_the_page()
    {
        var (page, errors) = await OpenAsync();
        await InstallStateSamplerAsync(page);

        await ClickTransitionAsync(page, "wipe");
        await page.WaitForURLAsync("**/transition/wipe");
        await WaitForIdleAsync(page);

        var states = await page.EvaluateAsync<string[]>("window.__states");
        Assert.Contains("leaving", states);
        Assert.Contains("covered", states);
        Assert.Contains("entering", states);
        Assert.Equal("idle", states[^1]);
        Assert.Equal("wipe", await page.Locator(".detail-title").InnerTextAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Back_button_replays_the_transition_in_reverse()
    {
        var (page, errors) = await OpenAsync();
        await InstallStateSamplerAsync(page);

        await ClickTransitionAsync(page, "wipe");
        await page.WaitForURLAsync("**/transition/wipe");
        await WaitForIdleAsync(page);
        await page.WaitForFunctionAsync(
            "window.__leaves.some(l => l.t === 'wipe' && l.d === 'forward')");

        await page.GoBackAsync();
        await page.WaitForURLAsync(DemoWebFixture.Url + "/");
        await WaitForIdleAsync(page);
        await page.WaitForFunctionAsync(
            "window.__leaves.some(l => l.t === 'wipe' && l.d === 'back')");

        Assert.Empty(errors);
    }

    [Fact]
    public async Task The_page_is_inert_during_a_transition_and_free_after()
    {
        var (page, errors) = await OpenAsync();

        await ClickTransitionAsync(page, "wipe");
        await page.WaitForFunctionAsync(
            "document.querySelector('[data-zakbt]').getAttribute('data-state') === 'covered'");

        Assert.NotNull(await page.Locator("main").GetAttributeAsync("inert"));

        await WaitForIdleAsync(page);

        Assert.Null(await page.Locator("main").GetAttributeAsync("inert"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Focus_lands_on_main_after_the_reveal()
    {
        var (page, errors) = await OpenAsync();

        await ClickTransitionAsync(page, "cube");
        await page.WaitForURLAsync("**/transition/cube");
        await WaitForIdleAsync(page);

        Assert.Equal("MAIN", await page.EvaluateAsync<string>("document.activeElement?.tagName"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Reduced_motion_swaps_pages_with_the_fade_instead_of_the_effect()
    {
        // The demo configures ReducedMotion = Fade: a visitor who prefers less
        // motion still gets a gentle cross-fade, never the clicked effect.
        var (page, errors) = await OpenAsync(options: new BrowserNewContextOptions
        {
            ReducedMotion = ReducedMotion.Reduce,
        });
        await InstallStateSamplerAsync(page);

        await ClickTransitionAsync(page, "wipe");
        await page.WaitForURLAsync("**/transition/wipe");
        await WaitForIdleAsync(page);
        await page.WaitForFunctionAsync(
            "window.__leaves.some(l => l.t === 'fade' && l.d === 'forward')");

        var played = await page.EvaluateAsync<string[]>(
            "window.__leaves.map(l => l.t)");
        Assert.DoesNotContain("wipe", played);
        Assert.Equal("wipe", await page.Locator(".detail-title").InnerTextAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task WebGL_pages_fall_back_to_a_fade_without_a_GPU()
    {
        var browser = await _fixture.LaunchWithoutGpuAsync();
        try
        {
            var (page, errors) = await OpenAsync(browser: browser);

            await ClickTransitionAsync(page, "dither");
            await page.WaitForURLAsync("**/transition/dither");
            await WaitForIdleAsync(page);

            Assert.Equal("dither", await page.Locator(".detail-title").InnerTextAsync());
            // A GPU canvas must not have been mounted: the fallback is a plain fade.
            Assert.Equal(0, await page.Locator("[data-zakbt] canvas").CountAsync());
            Assert.Empty(errors);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    [Fact]
    public async Task Unknown_transition_route_renders_the_custom_404()
    {
        var (page, errors) = await OpenAsync("/transition/origami");

        Assert.Contains("Lost between pages", await page.Locator(".lost__title").InnerTextAsync());
        Assert.Equal("grey", await page.EvaluateAsync<string?>("document.body.dataset.ground"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Keyboard_navigation_triggers_the_transition()
    {
        // Tab to a link, press Enter: the same flow as a click (focusin preloads
        // the module, the click carries the intent). No focus trap: Enter works
        // while the page is not covered, and lands with focus back on main.
        var (page, errors) = await OpenAsync();

        await page.Locator(".index a[data-transition=\"wipe\"]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForURLAsync("**/transition/wipe");
        await WaitForIdleAsync(page);

        Assert.Equal("wipe", await page.Locator(".detail-title").InnerTextAsync());
        Assert.Equal("MAIN", await page.EvaluateAsync<string>("document.activeElement?.tagName"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task External_links_are_left_to_the_browser()
    {
        var (page, _) = await OpenAsync();

        var link = page.Locator("footer a[href^='https://']");
        Assert.Equal("_blank", await link.GetAttributeAsync("target"));
        Assert.Null(await link.GetAttributeAsync("data-transition"));
    }

    [Fact]
    public async Task Docs_page_renders_and_navigation_reaches_it()
    {
        // Nav link plays its transition…
        var (page, errors) = await OpenAsync();
        await InstallStateSamplerAsync(page);

        await page.Locator(".header__nav a[href='/docs']").ClickAsync();
        await page.WaitForURLAsync("**/docs");
        await WaitForIdleAsync(page);
        await page.WaitForFunctionAsync(
            "window.__leaves.some(l => l.t === 'wipe' && l.d === 'forward')");

        Assert.Equal("Documentation", await page.Locator(".detail-title").InnerTextAsync());
        Assert.True(await page.Locator(".gs-table tbody tr").CountAsync() >= 8);
        Assert.Contains("AddZakBT", await page.ContentAsync());
        Assert.Empty(errors);

        // …and the route also works as a cold deep link.
        var (fresh, freshErrors) = await OpenAsync("/docs");
        Assert.Equal("Documentation", await fresh.Locator(".detail-title").InnerTextAsync());
        Assert.Contains("AddZakBT", await fresh.ContentAsync());
        Assert.Empty(freshErrors);
    }
}
