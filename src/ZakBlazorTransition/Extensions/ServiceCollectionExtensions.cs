using ZakBlazorTransition.Core;
using ZakBlazorTransition.Core.Configuration;
using ZakBlazorTransition.Core.Transitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ZakBlazorTransition;

/// <summary>Registration for the page-transition engine.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ZakBlazorTransition page-transition engine: <see cref="ITransitionService"/> and the
    /// <see cref="TransitionOptions"/> it runs with. Call it on the client (WebAssembly) side.
    /// </summary>
    public static IServiceCollection AddZakBT(this IServiceCollection services, Action<TransitionOptions>? configure = null)
    {
        var options = new TransitionOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<ITransitionRegistry>(_ => new TransitionRegistry(options.DefaultTransition));
        services.AddSingleton<TransitionService>();
        services.AddSingleton<ITransitionService>(sp => sp.GetRequiredService<TransitionService>());
        services.TryAddSingleton<TransitionRuntime>();
        return services;
    }
}
