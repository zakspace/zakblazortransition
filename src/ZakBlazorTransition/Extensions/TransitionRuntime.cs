using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace ZakBlazorTransition;

/// <summary>
/// The browser half of the engine, reached through a typed wrapper over the
/// <c>wwwroot/js/zakbt/index.js</c> module. The host component drives it; nothing
/// else should call into it.
/// </summary>
public sealed class TransitionRuntime : IAsyncDisposable
{
    /// <summary>Camel-case names for the objects the .NET side sends to JavaScript.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly IServiceProvider _services;
    private readonly Lazy<Task<IJSObjectReference>> _module;

    /// <summary>
    /// The JavaScript runtime is resolved lazily, on first use: during server
    /// prerendering the engine only renders markup, and <see cref="IJSRuntime"/>
    /// is a scoped service there.
    /// </summary>
    public TransitionRuntime(IServiceProvider services)
    {
        _services = services;
        _module = new Lazy<Task<IJSObjectReference>>(() =>
        {
            var js = _services.GetRequiredService<IJSRuntime>();
            return js.InvokeAsync<IJSObjectReference>("import", "./_content/ZakBlazorTransition/js/zakbt/index.js").AsTask();
        });
    }

    /// <summary>The JavaScript module, loaded on first use.</summary>
    public Task<IJSObjectReference> GetModuleAsync() => _module.Value;

    /// <summary>Pushes the engine settings to JavaScript so both halves agree.</summary>
    public async Task ConfigureAsync(Core.Configuration.TransitionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        await (await GetModuleAsync()).InvokeVoidAsync("configure", new EngineConfig
        {
            DefaultTransition = options.DefaultTransition,
            MinCoverTime = options.MinCoverTimeMs,
            MediaTimeout = options.MediaTimeoutMs,
            ReducedMotion = options.ReducedMotion == Core.Configuration.ReducedMotionMode.Fade ? "fade" : "none",
            Entrance = options.EntranceEnabled,
            Color = options.CoverColor,
            RevealOnLoad = options.RevealOnLoad is { } reveal
                ? new RevealConfig { Transition = reveal.Transition, MinCoverTime = reveal.MinCoverTimeMs }
                : null,
        });
    }

    /// <summary>Starts the engine on the layer element the host rendered.</summary>
    public async Task InitAsync(ElementReference layer) =>
        await (await GetModuleAsync()).InvokeVoidAsync("init", layer);

    /// <summary>Stops the engine and clears the layer.</summary>
    public async Task DestroyAsync() =>
        await (await GetModuleAsync()).InvokeVoidAsync("destroy");

    /// <summary>What the click or history traversal that started a navigation to <paramref name="to"/> asked for.</summary>
    public async Task<NavigationIntent> ConsumeIntentAsync(string? to = null) =>
        await (await GetModuleAsync()).InvokeAsync<NavigationIntent>("consumeIntent", to) ?? new NavigationIntent();

    /// <summary>Covers the page, awaiting the cover animation. The swap waits on this.</summary>
    public async Task<BeginNavigationResult> BeginNavigationAsync(NavigationRequestInfo request) =>
        await (await GetModuleAsync()).InvokeAsync<BeginNavigationResult>("beginNavigation", request)
        ?? new BeginNavigationResult();

    /// <summary>The destination is live: give its history entry an index.</summary>
    public async Task RecordNavigationAsync() =>
        await (await GetModuleAsync()).InvokeVoidAsync("recordNavigation");

    public async Task RememberLeaveAsync(string? name) =>
        await (await GetModuleAsync()).InvokeVoidAsync("rememberLeave", name);

    public async Task RememberArriveAsync(string? name) =>
        await (await GetModuleAsync()).InvokeVoidAsync("rememberArrive", name);

    /// <summary>The new page is in the DOM but hasn't painted: set it up.</summary>
    public async Task PrepareAsync(long? scroll = null) =>
        await (await GetModuleAsync()).InvokeVoidAsync("prepare", new PrepareInfo { Scroll = scroll });

    /// <summary>Waits for what the page needs, reveals it and gives the page back.</summary>
    public async Task RevealAsync(bool waitReady = false, int? minCoverTime = null) =>
        await (await GetModuleAsync()).InvokeVoidAsync("reveal", new RevealInfo { WaitReady = waitReady, MinCoverTime = minCoverTime });

    /// <summary>Shows a page that had no transition.</summary>
    public async Task AppearAsync(NavigationRequestInfo? request = null) =>
        await (await GetModuleAsync()).InvokeVoidAsync("appear", request);

    /// <summary>Scrolls to a fragment of the current page, for an in-page link.</summary>
    public async Task ScrollToHashAsync(string fragment) =>
        await (await GetModuleAsync()).InvokeVoidAsync("scrollToHash", fragment);

    /// <summary>The first page was rendered covered: draws the transition's own cover.</summary>
    public async Task<BeginNavigationResult> CoverOnLoadAsync(string name) =>
        await (await GetModuleAsync()).InvokeAsync<BeginNavigationResult>("coverOnLoad", name)
        ?? new BeginNavigationResult();

    public async Task<bool> PrefersReducedMotionAsync() =>
        await (await GetModuleAsync()).InvokeAsync<bool>("prefersReducedMotion");

    public async Task<bool> HasTransitionAsync(string name) =>
        await (await GetModuleAsync()).InvokeAsync<bool>("has", name);

    /// <summary>The settings a transition declares, once it's loaded.</summary>
    public async Task<TransitionInfo?> DescribeAsync(string name) =>
        await (await GetModuleAsync()).InvokeAsync<TransitionInfo>("describe", name);

    public async Task PreloadAsync(string name) =>
        await (await GetModuleAsync()).InvokeVoidAsync("preload", name);

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module.IsValueCreated)
            {
                var module = await _module.Value;
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is going away with the circuit: nothing to release.
        }
    }
}

/// <summary>The engine settings pushed to JavaScript by <c>configure()</c>.</summary>
public sealed class EngineConfig
{
    public string? DefaultTransition { get; init; }

    public int MinCoverTime { get; init; }

    public int MediaTimeout { get; init; }

    public string? ReducedMotion { get; init; }

    public bool Entrance { get; init; }

    public string? Color { get; init; }

    public RevealConfig? RevealOnLoad { get; init; }
}

/// <summary>The <c>revealOnLoad</c> settings, as JavaScript reads them.</summary>
public sealed class RevealConfig
{
    public string? Transition { get; init; }

    public int MinCoverTime { get; init; }
}

/// <summary>What a click or a history traversal asked for, read from JavaScript.</summary>
public sealed class NavigationIntent
{
    public ClickIntent? Click { get; init; }

    public TraverseIntent? Traverse { get; init; }
}

/// <summary>A link the visitor clicked: where it goes, which transition it named, where on screen it was.</summary>
public sealed class ClickIntent
{
    public string? Url { get; init; }

    public string? Transition { get; init; }

    public OriginInfo? Origin { get; init; }

    [JsonPropertyName("time")]
    public double Time { get; init; }
}

/// <summary>A history traversal: which way it goes, which transition to replay, where the page was scrolled.</summary>
public sealed class TraverseIntent
{
    public string? Direction { get; init; }

    public string? Transition { get; init; }

    public double Scroll { get; init; }
}

/// <summary>Where on screen a navigation started, in pixels.</summary>
public sealed class OriginInfo
{
    public double X { get; init; }

    public double Y { get; init; }
}

/// <summary>The navigation the engine is about to cover the page for.</summary>
public sealed class NavigationRequestInfo
{
    public required string From { get; init; }

    public required string To { get; init; }

    public required string Direction { get; init; }

    public required string Transition { get; init; }

    public OriginInfo? Origin { get; init; }

    public bool Traverse { get; init; }

    public bool Initial { get; init; }
}

/// <summary>The transition the engine resolved for a navigation.</summary>
public sealed class BeginNavigationResult
{
    public string Transition { get; init; } = "none";
}

/// <summary>Setup for the new page, before it paints.</summary>
public sealed class PrepareInfo
{
    public long? Scroll { get; init; }
}

/// <summary>What the reveal waits for.</summary>
public sealed class RevealInfo
{
    public bool WaitReady { get; init; }

    public int? MinCoverTime { get; init; }
}

/// <summary>A transition's own settings, once it's loaded.</summary>
public sealed class TransitionInfo
{
    public string? Name { get; init; }

    /// <summary>Seconds into <c>enter</c> when the page's content starts coming in; <c>null</c> when the transition brings it in itself.</summary>
    public double? Entrance { get; init; }

    public bool SolidCover { get; init; } = true;
}
