using System.Text.Json;
using System.Text.Json.Nodes;

namespace NpmLink.Cli.Services;

public class NpmLinkService : INpmLinkService
{
    private readonly INpmClient _npmClient;
    private readonly ITsConfigEditor _tsConfigEditor;

    public NpmLinkService(INpmClient npmClient, ITsConfigEditor tsConfigEditor)
    {
        _npmClient = npmClient;
        _tsConfigEditor = tsConfigEditor;
    }

    public async Task<OperationResult> LinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default)
    {
        var resolvedWorkspacePath = Path.GetFullPath(workspacePath);
        var resolvedLibrarySourcePath = Path.GetFullPath(librarySourcePath);
        var messages = new List<string>();

        if (!Directory.Exists(resolvedWorkspacePath))
            return OperationResult.Failure($"Error: Angular workspace path does not exist: {resolvedWorkspacePath}");

        if (!Directory.Exists(resolvedLibrarySourcePath))
            return OperationResult.Failure($"Error: Library source path does not exist: {resolvedLibrarySourcePath}");

        if (!ValidateAngularWorkspace(resolvedWorkspacePath))
            return OperationResult.Failure($"Error: No angular.json found in workspace path: {resolvedWorkspacePath}");

        if (!ValidateLibraryPackageJson(resolvedLibrarySourcePath, libraryName))
            return OperationResult.Failure($"Error: No package.json with name '{libraryName}' found in library source path: {resolvedLibrarySourcePath}");

        messages.Add($"Step 1: Running 'npm link' in library source: {resolvedLibrarySourcePath}");
        var linkExitCode = await _npmClient.LinkGlobalAsync(resolvedLibrarySourcePath, cancellationToken);
        if (linkExitCode != 0)
        {
            messages.Add("Error: 'npm link' in library source failed.");
            return new OperationResult(linkExitCode, messages);
        }

        messages.Add($"Step 2: Running 'npm link {libraryName}' in Angular workspace: {resolvedWorkspacePath}");
        var linkInWorkspaceExitCode = await _npmClient.LinkIntoWorkspaceAsync(libraryName, resolvedWorkspacePath, cancellationToken);
        if (linkInWorkspaceExitCode != 0)
        {
            messages.Add($"Error: 'npm link {libraryName}' in Angular workspace failed.");
            return new OperationResult(linkInWorkspaceExitCode, messages);
        }

        messages.Add("Step 3: Updating tsconfig.json paths for local development...");
        var tsconfigResult = _tsConfigEditor.AddPaths(resolvedWorkspacePath, libraryName, resolvedLibrarySourcePath);
        if (!tsconfigResult)
        {
            messages.Add("Error: Could not update tsconfig.json paths.");
            return new OperationResult(1, messages);
        }

        messages.Add($"Updated tsconfig.json with path mapping for '{libraryName}'.");
        messages.Add($"Successfully linked '{libraryName}' to {resolvedLibrarySourcePath}");
        return new OperationResult(0, messages);
    }

    public async Task<OperationResult> UnlinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default)
    {
        var resolvedWorkspacePath = Path.GetFullPath(workspacePath);
        var resolvedLibrarySourcePath = Path.GetFullPath(librarySourcePath);
        var messages = new List<string>();

        if (!Directory.Exists(resolvedWorkspacePath))
            return OperationResult.Failure($"Error: Angular workspace path does not exist: {resolvedWorkspacePath}");

        if (!ValidateAngularWorkspace(resolvedWorkspacePath))
            return OperationResult.Failure($"Error: No angular.json found in workspace path: {resolvedWorkspacePath}");

        // Item 3: Validate --source in UnlinkAsync
        if (!Directory.Exists(resolvedLibrarySourcePath))
            return OperationResult.Failure($"Error: Library source path does not exist: {resolvedLibrarySourcePath}");

        messages.Add($"Step 1: Running 'npm unlink {libraryName}' in Angular workspace: {resolvedWorkspacePath}");
        var unlinkInWorkspaceExitCode = await _npmClient.UnlinkFromWorkspaceAsync(libraryName, resolvedWorkspacePath, cancellationToken);
        if (unlinkInWorkspaceExitCode != 0)
        {
            messages.Add($"Error: 'npm unlink {libraryName}' in Angular workspace failed.");
            return new OperationResult(unlinkInWorkspaceExitCode, messages);
        }

        messages.Add($"Step 2: Running 'npm unlink' in library source: {resolvedLibrarySourcePath}");
        var unlinkExitCode = await _npmClient.UnlinkGlobalAsync(resolvedLibrarySourcePath, cancellationToken);
        if (unlinkExitCode != 0)
        {
            messages.Add("Error: 'npm unlink' in library source failed.");
            return new OperationResult(unlinkExitCode, messages);
        }

        messages.Add("Step 3: Removing tsconfig.json path mappings...");
        var tsconfigResult = _tsConfigEditor.RemovePaths(resolvedWorkspacePath, libraryName);
        if (!tsconfigResult)
        {
            messages.Add("Error: Could not update tsconfig.json paths.");
            return new OperationResult(1, messages);
        }

        messages.Add($"Removed tsconfig.json path mappings for '{libraryName}'.");
        messages.Add($"Successfully unlinked '{libraryName}'");
        return new OperationResult(0, messages);
    }

    public Task<OperationResult> VerifyAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default)
    {
        var resolvedWorkspacePath = Path.GetFullPath(workspacePath);
        var resolvedLibrarySourcePath = Path.GetFullPath(librarySourcePath);
        var messages = new List<string>();

        if (!Directory.Exists(resolvedWorkspacePath))
            return Task.FromResult(OperationResult.Failure($"Error: Angular workspace path does not exist: {resolvedWorkspacePath}"));

        if (!ValidateAngularWorkspace(resolvedWorkspacePath))
            return Task.FromResult(OperationResult.Failure($"Error: No angular.json found in workspace path: {resolvedWorkspacePath}"));

        var allPassed = true;

        // Symlink check
        var nodeModulesLibPath = Path.Combine(resolvedWorkspacePath, "node_modules", libraryName);
        if (!Directory.Exists(nodeModulesLibPath))
        {
            messages.Add($"FAIL: Symlink not found at {nodeModulesLibPath}");
            allPassed = false;
        }
        else
        {
            var dirInfo = new DirectoryInfo(nodeModulesLibPath);
            if (dirInfo.LinkTarget is null)
            {
                messages.Add($"FAIL: {nodeModulesLibPath} exists but is not a symbolic link");
                allPassed = false;
            }
            else
            {
                var resolvedTarget = Directory.ResolveLinkTarget(nodeModulesLibPath, returnFinalTarget: true);
                var resolvedTargetPath = resolvedTarget is not null ? Path.GetFullPath(resolvedTarget.FullName) : null;
                if (!string.Equals(resolvedTargetPath, resolvedLibrarySourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add($"FAIL: Symlink target is '{resolvedTargetPath}' but expected '{resolvedLibrarySourcePath}'");
                    allPassed = false;
                }
                else
                {
                    messages.Add($"PASS: Symlink exists and points to {resolvedLibrarySourcePath}");
                }
            }
        }

        // tsconfig check (Item 1: validate values too)
        var (tsconfigExists, exactKeyMatch, wildcardKeyMatch, exactValueMatch, wildcardValueMatch) =
            _tsConfigEditor.VerifyPaths(resolvedWorkspacePath, libraryName, resolvedLibrarySourcePath);

        if (!tsconfigExists)
        {
            messages.Add("FAIL: tsconfig.json not found in workspace");
            allPassed = false;
        }
        else
        {
            if (!exactKeyMatch)
            {
                messages.Add($"FAIL: tsconfig.json missing path mapping for '{libraryName}'");
                allPassed = false;
            }
            else if (!exactValueMatch)
            {
                messages.Add($"FAIL: tsconfig.json path mapping for '{libraryName}' has wrong value");
                allPassed = false;
            }

            if (!wildcardKeyMatch)
            {
                messages.Add($"FAIL: tsconfig.json missing path mapping for '{libraryName}/*'");
                allPassed = false;
            }
            else if (!wildcardValueMatch)
            {
                messages.Add($"FAIL: tsconfig.json path mapping for '{libraryName}/*' has wrong value");
                allPassed = false;
            }

            if (exactKeyMatch && exactValueMatch && wildcardKeyMatch && wildcardValueMatch)
            {
                messages.Add($"PASS: tsconfig.json contains correct path mappings for '{libraryName}'");
            }
        }

        return Task.FromResult(new OperationResult(allPassed ? 0 : 1, messages));
    }

    private static bool ValidateAngularWorkspace(string workspacePath)
    {
        return File.Exists(Path.Combine(workspacePath, "angular.json"));
    }

    private static bool ValidateLibraryPackageJson(string librarySourcePath, string libraryName)
    {
        var packageJsonPath = Path.Combine(librarySourcePath, "package.json");
        if (!File.Exists(packageJsonPath))
            return false;

        try
        {
            var content = File.ReadAllText(packageJsonPath);
            var json = JsonNode.Parse(content);
            var name = json?["name"]?.GetValue<string>();
            return string.Equals(name, libraryName, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
