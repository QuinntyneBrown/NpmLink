namespace NpmLink.Cli.Services;

public interface INpmClient
{
    Task<int> LinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> LinkIntoWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> UnlinkFromWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> UnlinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default);
}
