using System.Diagnostics;

namespace KaraokeList.E2E;

public sealed class E2eServerFixture : IAsyncLifetime
{
    private readonly List<Process> startedProcesses = [];
    private string? skipReason;

    public bool IsReady => skipReason is null;

    public string SkipReason => skipReason ?? "E2E servers are not ready.";

    public string? WarmUpEmail { get; private set; }

    public string? WarmUpToken { get; private set; }

    public async Task InitializeAsync()
    {
        if (E2eConfiguration.ManualServers)
        {
            skipReason = await VerifyManualServersAsync();
            if (skipReason is null)
            {
                skipReason = await WarmUpApiAsync();
            }

            return;
        }

        if (!E2eConfiguration.AutoStartServers)
        {
            skipReason = "E2E auto-start disabled. Set KARAOKE_E2E_MANUAL=true and run Api + Web yourself.";
            return;
        }

        var repoRoot = FindRepoRoot();
        await BuildProjectsAsync(repoRoot);

        startedProcesses.Add(StartDotnetProcess(
            repoRoot,
            "KaraokeList.Api/KaraokeList.Api.csproj",
            "--launch-profile e2e"));

        startedProcesses.Add(StartDotnetProcess(
            repoRoot,
            "KaraokeList.Web/KaraokeList.Web.csproj",
            "--launch-profile e2e"));

        skipReason = await WaitForServersAsync();
        if (skipReason is null)
        {
            skipReason = await WarmUpApiAsync();
        }
    }

    public Task DisposeAsync()
    {
        foreach (var process in startedProcesses)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort cleanup for local test runs.
            }
            finally
            {
                process.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<string?> VerifyManualServersAsync()
    {
        if (!await IsReachableAsync($"{E2eConfiguration.ApiBaseUrl}/api/auth/registration"))
        {
            return $"API not reachable at {E2eConfiguration.ApiBaseUrl}. Start KaraokeList.Api (http profile).";
        }

        if (!await IsReachableAsync(E2eConfiguration.WebBaseUrl))
        {
            return $"Web app not reachable at {E2eConfiguration.WebBaseUrl}. Start KaraokeList.Web (e2e profile).";
        }

        return null;
    }

    private static async Task BuildProjectsAsync(string repoRoot)
    {
        await RunDotnetAsync(repoRoot, "build KaraokeList.Api/KaraokeList.Api.csproj -c Debug");
        await RunDotnetAsync(
            repoRoot,
            "build KaraokeList.Web/KaraokeList.Web.csproj -c Debug /p:SyncfusionKey=\"\" /p:WasmApplicationEnvironmentName=E2E");
    }

    private static Process StartDotnetProcess(string repoRoot, string projectPath, string extraArgs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project {projectPath} {extraArgs} --no-build",
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not start dotnet for {projectPath}.");

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[{projectPath}] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[{projectPath} ERR] {e.Data}");
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task<string?> WaitForServersAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            var apiReady = await IsReachableAsync($"{E2eConfiguration.ApiBaseUrl}/api/auth/registration");
            var webReady = apiReady && await IsReachableAsync(E2eConfiguration.WebBaseUrl);
            if (webReady)
            {
                return null;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        return "Timed out waiting for KaraokeList.Api and KaraokeList.Web to start.";
    }

    private async Task<string?> WarmUpApiAsync()
    {
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
            var (email, token) = await E2eAuthHelper.WarmUpApiAsync(client);
            WarmUpEmail = email;
            WarmUpToken = token;
            return null;
        }
        catch (Exception ex)
        {
            return $"API warm-up failed: {ex.Message}";
        }
    }

    private static async Task<bool> IsReachableAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task RunDotnetAsync(string repoRoot, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not run dotnet {arguments}.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {arguments} failed ({process.ExitCode}).{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KaraokeList.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate KaraokeList.sln from test output directory.");
    }
}

[CollectionDefinition(Name)]
public sealed class E2eCollection : ICollectionFixture<E2eServerFixture>
{
    public const string Name = nameof(E2eCollection);
}
