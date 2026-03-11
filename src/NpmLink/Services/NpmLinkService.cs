using System.Text.Json;
using System.Text.Json.Nodes;

namespace NpmLink.Services;

public class NpmLinkService : INpmLinkService
{
    private readonly IProcessRunner _processRunner;

    public NpmLinkService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<int> LinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default)
    {
        var resolvedWorkspacePath = Path.GetFullPath(workspacePath);
        var resolvedLibrarySourcePath = Path.GetFullPath(librarySourcePath);

        if (!Directory.Exists(resolvedWorkspacePath))
        {
            Console.Error.WriteLine($"Error: Angular workspace path does not exist: {resolvedWorkspacePath}");
            return 1;
        }

        if (!Directory.Exists(resolvedLibrarySourcePath))
        {
            Console.Error.WriteLine($"Error: Library source path does not exist: {resolvedLibrarySourcePath}");
            return 1;
        }

        if (!ValidateAngularWorkspace(resolvedWorkspacePath))
        {
            Console.Error.WriteLine($"Error: No angular.json found in workspace path: {resolvedWorkspacePath}");
            return 1;
        }

        if (!ValidateLibraryPackageJson(resolvedLibrarySourcePath, libraryName))
        {
            Console.Error.WriteLine($"Error: No package.json with name '{libraryName}' found in library source path: {resolvedLibrarySourcePath}");
            return 1;
        }

        Console.WriteLine($"Step 1: Running 'npm link' in library source: {resolvedLibrarySourcePath}");
        var (npmCommand, linkArgs, linkInWorkspaceArgs) = GetNpmCommands(libraryName);
        var linkExitCode = await _processRunner.RunAsync(npmCommand, linkArgs, resolvedLibrarySourcePath, cancellationToken);
        if (linkExitCode != 0)
        {
            Console.Error.WriteLine("Error: 'npm link' in library source failed.");
            return linkExitCode;
        }

        Console.WriteLine($"Step 2: Running 'npm link {libraryName}' in Angular workspace: {resolvedWorkspacePath}");
        var linkInWorkspaceExitCode = await _processRunner.RunAsync(npmCommand, linkInWorkspaceArgs, resolvedWorkspacePath, cancellationToken);
        if (linkInWorkspaceExitCode != 0)
        {
            Console.Error.WriteLine($"Error: 'npm link {libraryName}' in Angular workspace failed.");
            return linkInWorkspaceExitCode;
        }

        Console.WriteLine("Step 3: Updating tsconfig.json paths for local development...");
        UpdateTsconfigPaths(resolvedWorkspacePath, libraryName, resolvedLibrarySourcePath);

        Console.WriteLine($"Successfully linked '{libraryName}' to {resolvedLibrarySourcePath}");
        return 0;
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

    private static (string npmCommand, string linkArgs, string linkInWorkspaceArgs) GetNpmCommands(string libraryName)
    {
        var isWindows = OperatingSystem.IsWindows();
        var npmCommand = isWindows ? "cmd" : "npm";
        var linkArgs = isWindows ? "/c npm link" : "link";
        var linkInWorkspaceArgs = isWindows ? $"/c npm link {libraryName}" : $"link {libraryName}";
        return (npmCommand, linkArgs, linkInWorkspaceArgs);
    }

    private static void UpdateTsconfigPaths(string workspacePath, string libraryName, string librarySourcePath)
    {
        var tsconfigPath = Path.Combine(workspacePath, "tsconfig.json");
        if (!File.Exists(tsconfigPath))
        {
            Console.WriteLine("No tsconfig.json found in workspace root; skipping path mapping update.");
            return;
        }

        try
        {
            var content = File.ReadAllText(tsconfigPath);
            var tsconfig = JsonNode.Parse(content, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

            if (tsconfig is null)
                return;

            tsconfig["compilerOptions"] ??= new JsonObject();
            var compilerOptions = tsconfig["compilerOptions"]!.AsObject();

            compilerOptions["paths"] ??= new JsonObject();
            var paths = compilerOptions["paths"]!.AsObject();

            var relativeLibPath = Path.GetRelativePath(workspacePath, librarySourcePath).Replace('\\', '/');

            var exactPathArray = new JsonArray();
            exactPathArray.Add(JsonValue.Create(relativeLibPath));
            paths[libraryName] = exactPathArray;

            var wildcardPathArray = new JsonArray();
            wildcardPathArray.Add(JsonValue.Create($"{relativeLibPath}/*"));
            paths[$"{libraryName}/*"] = wildcardPathArray;

            var updatedContent = tsconfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tsconfigPath, updatedContent);
            Console.WriteLine($"Updated tsconfig.json with path mapping for '{libraryName}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not update tsconfig.json paths: {ex.Message}");
        }
    }
}
