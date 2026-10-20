using ZakBlazorTransition;
using ZakBlazorTransition.Demo.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddZakBT(DemoTransitions.Configure);

await builder.Build().RunAsync();
