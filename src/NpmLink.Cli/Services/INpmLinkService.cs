namespace NpmLink.Cli.Services;

public interface INpmLinkService
{
    Task<OperationResult> LinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
    Task<OperationResult> UnlinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
    Task<OperationResult> VerifyAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
}
