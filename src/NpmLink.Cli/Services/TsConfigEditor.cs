using System.Text.Json;
using System.Text.Json.Nodes;

namespace NpmLink.Cli.Services;

public class TsConfigEditor : ITsConfigEditor
{
    private static readonly JsonDocumentOptions JsoncDocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonNodeOptions JsoncNodeOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public bool AddPaths(string workspacePath, string libraryName, string librarySourcePath)
    {
        var tsconfigPath = Path.Combine(workspacePath, "tsconfig.json");
        if (!File.Exists(tsconfigPath))
        {
            // No tsconfig.json found; not an error, just nothing to update
            return true;
        }

        try
        {
            var content = File.ReadAllText(tsconfigPath);
            var tsconfig = JsonNode.Parse(content, JsoncNodeOptions, JsoncDocumentOptions);

            if (tsconfig is null)
                return false;

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
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RemovePaths(string workspacePath, string libraryName)
    {
        var tsconfigPath = Path.Combine(workspacePath, "tsconfig.json");
        if (!File.Exists(tsconfigPath))
        {
            // No tsconfig.json found; not an error, just nothing to remove
            return true;
        }

        try
        {
            var content = File.ReadAllText(tsconfigPath);
            var tsconfig = JsonNode.Parse(content, JsoncNodeOptions, JsoncDocumentOptions);

            if (tsconfig is null)
                return false;

            var paths = tsconfig["compilerOptions"]?["paths"]?.AsObject();
            if (paths is null)
                return true;

            paths.Remove(libraryName);
            paths.Remove($"{libraryName}/*");

            var updatedContent = tsconfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tsconfigPath, updatedContent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public (bool exists, bool exactKeyMatch, bool wildcardKeyMatch, bool exactValueMatch, bool wildcardValueMatch) VerifyPaths(
        string workspacePath, string libraryName, string librarySourcePath)
    {
        var tsconfigPath = Path.Combine(workspacePath, "tsconfig.json");
        if (!File.Exists(tsconfigPath))
        {
            return (false, false, false, false, false);
        }

        try
        {
            var content = File.ReadAllText(tsconfigPath);
            var tsconfig = JsonNode.Parse(content, JsoncNodeOptions, JsoncDocumentOptions);
            var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();

            var exactKeyMatch = paths?.ContainsKey(libraryName) ?? false;
            var wildcardKeyMatch = paths?.ContainsKey($"{libraryName}/*") ?? false;

            var expectedRelativePath = Path.GetRelativePath(workspacePath, librarySourcePath).Replace('\\', '/');
            var expectedExactValue = expectedRelativePath;
            var expectedWildcardValue = $"{expectedRelativePath}/*";

            var exactValueMatch = false;
            var wildcardValueMatch = false;

            if (exactKeyMatch)
            {
                var exactArray = paths![libraryName]?.AsArray();
                if (exactArray is not null && exactArray.Count > 0)
                {
                    var actualValue = exactArray[0]?.GetValue<string>()?.Replace('\\', '/');
                    exactValueMatch = string.Equals(actualValue, expectedExactValue, StringComparison.Ordinal);
                }
            }

            if (wildcardKeyMatch)
            {
                var wildcardArray = paths![$"{libraryName}/*"]?.AsArray();
                if (wildcardArray is not null && wildcardArray.Count > 0)
                {
                    var actualValue = wildcardArray[0]?.GetValue<string>()?.Replace('\\', '/');
                    wildcardValueMatch = string.Equals(actualValue, expectedWildcardValue, StringComparison.Ordinal);
                }
            }

            return (true, exactKeyMatch, wildcardKeyMatch, exactValueMatch, wildcardValueMatch);
        }
        catch
        {
            return (true, false, false, false, false);
        }
    }
}
