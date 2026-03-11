namespace NpmLink.Services;

public interface IProcessRunner
{
    Task<int> RunAsync(string command, string arguments, string workingDirectory, CancellationToken cancellationToken = default);
}
