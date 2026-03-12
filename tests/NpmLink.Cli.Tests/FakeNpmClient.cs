using NpmLink.Cli.Services;

namespace NpmLink.Cli.Tests;

/// <summary>
/// Fake implementation of INpmClient that records calls and returns configurable exit codes.
/// </summary>
internal sealed class FakeNpmClient : INpmClient
{
    private readonly Queue<int> _exitCodes;
    public List<(string Method, string? LibraryName, string WorkingDirectory)> Invocations { get; } = new();

    public FakeNpmClient(params int[] exitCodes)
    {
        _exitCodes = new Queue<int>(exitCodes.Length > 0 ? exitCodes : new[] { 0, 0 });
    }

    private int NextExitCode() => _exitCodes.Count > 0 ? _exitCodes.Dequeue() : 0;

    public Task<int> LinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        Invocations.Add(("LinkGlobal", null, workingDirectory));
        return Task.FromResult(NextExitCode());
    }

    public Task<int> LinkIntoWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Invocations.Add(("LinkIntoWorkspace", libraryName, workingDirectory));
        return Task.FromResult(NextExitCode());
    }

    public Task<int> UnlinkFromWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Invocations.Add(("UnlinkFromWorkspace", libraryName, workingDirectory));
        return Task.FromResult(NextExitCode());
    }

    public Task<int> UnlinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default)
    {
        Invocations.Add(("UnlinkGlobal", null, workingDirectory));
        return Task.FromResult(NextExitCode());
    }
}
