using System.Text.Json;
using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core.Tests;

/// <summary>
/// The shape crossing the .NET ↔ JavaScript boundary. The engine's modules read
/// camelCase properties and write them back, so these names are a contract.
/// </summary>
public class RuntimeDtoJsonTests
{
    private static readonly JsonSerializerOptions Options = TransitionRuntime.JsonOptions;

    private static JsonDocument Serialize(object value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value, Options));

    [Fact]
    public void NavigationRequestInfo_serializes_camel_case()
    {
        var json = Serialize(new NavigationRequestInfo
        {
            From = "http://localhost/",
            To = "http://localhost/transition/wipe",
            Direction = "forward",
            Transition = "wipe",
            Origin = new OriginInfo { X = 12, Y = 34 },
            Traverse = false,
            Initial = false,
        });

        var root = json.RootElement;
        foreach (var name in new[] { "from", "to", "direction", "transition", "origin", "traverse", "initial" })
        {
            Assert.True(root.TryGetProperty(name, out _), name);
        }

        Assert.Equal(12, root.GetProperty("origin").GetProperty("x").GetDouble());
        Assert.Equal(34, root.GetProperty("origin").GetProperty("y").GetDouble());
    }

    [Fact]
    public void EngineConfig_serializes_camel_case()
    {
        var root = Serialize(new EngineConfig
        {
            DefaultTransition = "peel",
            MinCoverTime = 0,
            MediaTimeout = 1500,
            ReducedMotion = "none",
            Entrance = true,
            Color = "#111111",
            RevealOnLoad = new RevealConfig { Transition = "curtain", MinCoverTime = 800 },
        }).RootElement;

        Assert.Equal("peel", root.GetProperty("defaultTransition").GetString());
        Assert.Equal(1500, root.GetProperty("mediaTimeout").GetInt32());
        Assert.Equal("curtain", root.GetProperty("revealOnLoad").GetProperty("transition").GetString());
    }

    [Fact]
    public void Enums_cross_as_camelCase_strings()
    {
        Assert.Equal("\"idle\"", JsonSerializer.Serialize(TransitionPhase.Idle, Options));
        Assert.Equal("\"leaving\"", JsonSerializer.Serialize(TransitionPhase.Leaving, Options));
        Assert.Equal("\"forward\"", JsonSerializer.Serialize(TransitionDirection.Forward, Options));
        Assert.Equal("\"back\"", JsonSerializer.Serialize(TransitionDirection.Back, Options));
        Assert.Equal("\"fade\"", JsonSerializer.Serialize(ReducedMotionMode.Fade, Options));
    }

    [Fact]
    public void Enums_deserialize_from_camelCase_strings()
    {
        Assert.Equal(
            TransitionPhase.Covered,
            JsonSerializer.Deserialize<TransitionPhase>("\"covered\"", Options));
        Assert.Equal(
            TransitionDirection.Back,
            JsonSerializer.Deserialize<TransitionDirection>("\"back\"", Options));
    }

    [Fact]
    public void NavigationIntent_reads_what_the_engine_writes()
    {
        // The payload consumeIntent() returns in JavaScript.
        var payload = """
            {
              "click": {
                "url": "http://localhost/transition/wipe",
                "transition": "wipe",
                "origin": { "x": 100.5, "y": 200.25 },
                "time": 1234.5
              },
              "traverse": null
            }
            """;

        var intent = JsonSerializer.Deserialize<NavigationIntent>(payload, Options);

        Assert.NotNull(intent);
        Assert.Null(intent.Traverse);
        Assert.Equal("http://localhost/transition/wipe", intent.Click!.Url);
        Assert.Equal("wipe", intent.Click.Transition);
        Assert.Equal(100.5, intent.Click.Origin!.X);
        Assert.Equal(200.25, intent.Click.Origin.Y);
        Assert.Equal(1234.5, intent.Click.Time);
    }

    [Fact]
    public void TraverseIntent_reads_what_the_engine_writes()
    {
        var payload = """
            {
              "click": null,
              "traverse": { "direction": "back", "transition": "tear", "scroll": 640 }
            }
            """;

        var intent = JsonSerializer.Deserialize<NavigationIntent>(payload, Options);

        Assert.NotNull(intent);
        Assert.Null(intent.Click);
        Assert.Equal("back", intent.Traverse!.Direction);
        Assert.Equal("tear", intent.Traverse.Transition);
        Assert.Equal(640, intent.Traverse.Scroll);
    }

    [Fact]
    public void An_empty_object_is_a_valid_intent()
    {
        var intent = JsonSerializer.Deserialize<NavigationIntent>("{}", Options);
        Assert.NotNull(intent);
        Assert.Null(intent.Click);
        Assert.Null(intent.Traverse);
    }

    [Fact]
    public void BeginNavigationResult_defaults_to_no_transition()
    {
        var result = JsonSerializer.Deserialize<BeginNavigationResult>("{}", Options);
        Assert.Equal(TransitionNames.None, result!.Transition);
    }
}
