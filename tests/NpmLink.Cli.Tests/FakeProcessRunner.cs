using NpmLink.Cli.Services;

namespace NpmLink.Cli.Tests;

/// <summary>
/// Fake implementation of IProcessRunner that records calls and returns configurable exit codes.
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Queue<int> _exitCodes;
    public List<(string Command, string Arguments, string WorkingDirectory)> Invocations { get; } = new();

    public FakeProcessRunner(params int[] exitCodes)
    {
        _exitCodes = new Queue<int>(exitCodes.Length > 0 ? exitCodes : new[] { 0, 0 });
    }

    public Task<int> RunAsync(string command, string arguments, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Invocations.Add((command, arguments, workingDirectory));
        var code = _exitCodes.Count > 0 ? _exitCodes.Dequeue() : 0;
        return Task.FromResult(code);
    }
}
