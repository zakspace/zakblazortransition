using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZakBlazorTransition;
using ZakBlazorTransition.Demo.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The app's root: the persistent transition layer and the router live under
// Routes.razor, mounted into <div id="app"> in wwwroot/index.html.
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The engine's settings. With no server prerender in a standalone WebAssembly app,
// this is the single place the options are configured; RevealOnLoad still covers and
// reveals the first load, driven entirely on the client.
builder.Services.AddZakBT(DemoTransitions.Configure);

await builder.Build().RunAsync();
