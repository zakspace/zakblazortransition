using System.Diagnostics;
using Microsoft.Playwright;

namespace ZakBlazorTransition.PlaywrightTests;

/// <summary>
/// Runs the Demo app on its own Kestrel port for the length of the test class,
/// and provides a shared headless Chromium. Build the solution before running
/// (the fixture starts the already-built app with `dotnet run --no-build`).
/// </summary>
public sealed class DemoWebFixture : IAsyncLifetime
{
    public const string Url = "http://127.0.0.1:5310";

    private Process? _server;
    private IPlaywright _playwright = default!;

    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        StartServer();
        await WaitForServerAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_server is { HasExited: false })
        {
            try { _server.Kill(entireProcessTree: true); } catch(InvalidOperationException) { }
        }
        _server?.Dispose();
    }

    /// <summary>A browser with no GPU at all: the engine should fall back and still navigate.</summary>
    public async Task<IBrowser> LaunchWithoutGpuAsync() =>
        await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-gpu", "--disable-software-rasterizer", "--disable-blink-features=WebGL,WebGL2"],
        });

    private static string DemoProject
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ZakBlazorTransition.slnx")))
            {
                dir = dir.Parent;
            }

            return Path.Combine(
                dir?.FullName ?? throw new InvalidOperationException("Solution root not found."),
                "src", "ZakBlazorTransition.Demo", "ZakBlazorTransition.Demo.csproj");
        }
    }

    private void StartServer()
    {
        _server = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{DemoProject}\" --no-build --no-launch-profile",
            WorkingDirectory = Path.GetDirectoryName(DemoProject)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            // A Debug build serves its WebAssembly assets in Development; Production
            // wants a publish, so run the fixture the same way the app is verified.
            EnvironmentVariables =
            {
                ["ASPNETCORE_URLS"] = Url,
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DOTNET_NOLOGO"] = "1",
            },
        }) ?? throw new InvalidOperationException("Could not start the Demo server.");
    }

    private async Task WaitForServerAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            if (_server!.HasExited)
            {
                throw new InvalidOperationException(
                    "The Demo server exited before it was ready:\n" + await _server.StandardOutput.ReadToEndAsync());
            }

            try
            {
                var response = await http.GetAsync($"{Url}/transition/wipe");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception)
            {
                // Not up yet: keep asking until the deadline.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"The Demo server never answered at {Url}.");
    }
}

[CollectionDefinition("Demo web")]
public class DemoWebCollection : ICollectionFixture<DemoWebFixture>
{
    // Groups the tests onto the one server and browser the fixture starts.
}
