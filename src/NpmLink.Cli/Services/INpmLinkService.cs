namespace NpmLink.Cli.Services;

public interface INpmLinkService
{
    Task<int> LinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
}
