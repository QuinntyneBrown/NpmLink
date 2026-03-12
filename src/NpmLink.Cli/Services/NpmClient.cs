using System.Diagnostics;

namespace NpmLink.Cli.Services;

public class NpmClient : INpmClient
{
    private static string NpmExecutable => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

    public Task<int> LinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        return RunNpmAsync(new[] { "link" }, workingDirectory, cancellationToken);
    }

    public Task<int> LinkIntoWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default)
    {
        return RunNpmAsync(new[] { "link", libraryName }, workingDirectory, cancellationToken);
    }

    public Task<int> UnlinkFromWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default)
    {
        return RunNpmAsync(new[] { "unlink", libraryName }, workingDirectory, cancellationToken);
    }

    public Task<int> UnlinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        return RunNpmAsync(new[] { "unlink" }, workingDirectory, cancellationToken);
    }

    private static async Task<int> RunNpmAsync(string[] arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = NpmExecutable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.WriteLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.Error.WriteLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }
}
