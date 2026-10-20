using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;

namespace ZakBlazorTransition.Core.Tests;

public class TransitionOptionsTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        new TransitionOptions().Validate();
    }

    [Fact]
    public void Default_transition_none_is_valid()
    {
        new TransitionOptions { DefaultTransition = TransitionNames.None }.Validate();
    }

    [Fact]
    public void Unknown_default_transition_throws()
    {
        var options = new TransitionOptions { DefaultTransition = "origami" };
        var error = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("origami", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_default_transition_throws(string name)
    {
        var options = new TransitionOptions { DefaultTransition = name };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Negative_min_cover_time_throws()
    {
        var options = new TransitionOptions { MinCoverTimeMs = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Zero_media_timeout_throws()
    {
        var options = new TransitionOptions { MediaTimeoutMs = 0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Negative_render_timeout_throws()
    {
        var options = new TransitionOptions { RenderTimeoutMs = -5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Unknown_reveal_on_load_transition_throws()
    {
        var options = new TransitionOptions
        {
            RevealOnLoad = new RevealOnLoadOptions { Transition = "origami" },
        };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Known_reveal_on_load_transition_is_valid()
    {
        var options = new TransitionOptions
        {
            RevealOnLoad = new RevealOnLoadOptions { Transition = TransitionNames.Curtain },
        };
        options.Validate();
    }
}

public class TransitionCatalogTests
{
    [Fact]
    public void Catalog_holds_twenty_one_transitions()
    {
        Assert.Equal(21, TransitionCatalog.All.Count);
        Assert.Equal(21, TransitionNames.All.Count);
        Assert.All(TransitionNames.All, name => Assert.True(TransitionCatalog.Contains(name), name));
    }

    [Theory]
    [InlineData(TransitionGroup.Overlay, 5)]
    [InlineData(TransitionGroup.PageComposition, 9)]
    [InlineData(TransitionGroup.WebGl, 7)]
    public void Groups_hold_the_right_counts(TransitionGroup group, int count)
    {
        Assert.Equal(count, TransitionCatalog.InGroup(group).Count());
    }

    [Fact]
    public void Orders_are_unique_and_listing_is_sorted()
    {
        var orders = TransitionCatalog.All.Select(t => t.Order).ToList();
        Assert.Equal(orders.Count, orders.Distinct().Count());
        Assert.Equal(orders, orders.Order());
    }

    [Fact]
    public void Find_returns_the_descriptor_by_name()
    {
        var wipe = TransitionCatalog.Find(TransitionNames.Wipe);
        Assert.NotNull(wipe);
        Assert.Equal(TransitionGroup.Overlay, wipe.Group);
    }

    [Fact]
    public void Find_is_case_sensitive_and_null_for_nothing()
    {
        Assert.Null(TransitionCatalog.Find("Wipe"));
        Assert.Null(TransitionCatalog.Find(null));
        Assert.Null(TransitionCatalog.Find("origami"));
    }

    [Fact]
    public void Only_webgl_transitions_flag_webgl()
    {
        Assert.All(TransitionCatalog.InGroup(TransitionGroup.WebGl), t => Assert.True(t.UsesWebGl));
        Assert.All(
            TransitionCatalog.All.Where(t => t.Group != TransitionGroup.WebGl),
            t => Assert.False(t.UsesWebGl));
    }
}

public class TransitionRegistryTests
{
    [Fact]
    public void Registry_knows_the_internal_names()
    {
        var registry = new TransitionRegistry(TransitionNames.Peel);
        Assert.True(registry.Has(TransitionNames.None));
        Assert.True(registry.Has(TransitionNames.Fade));
        Assert.False(registry.Has("origami"));
    }

    [Fact]
    public void Default_resolves_from_the_given_name()
    {
        var registry = new TransitionRegistry(TransitionNames.Wipe);
        Assert.Equal(TransitionNames.Wipe, registry.Default?.Name);
    }

    [Fact]
    public void Resolve_returns_null_for_none_and_unknown_falls_back_to_default()
    {
        var registry = new TransitionRegistry(TransitionNames.Peel);
        Assert.Null(registry.Resolve(null));
        Assert.Null(registry.Resolve(TransitionNames.None));
        Assert.Equal(TransitionNames.Peel, registry.Resolve("origami")?.Name);
        Assert.Equal(TransitionNames.Cube, registry.Resolve(TransitionNames.Cube)?.Name);
    }

    [Fact]
    public void All_exposes_the_whole_catalog()
    {
        var registry = new TransitionRegistry(TransitionNames.Peel);
        Assert.Equal(TransitionCatalog.All.Count, registry.All.Count);
    }
}
