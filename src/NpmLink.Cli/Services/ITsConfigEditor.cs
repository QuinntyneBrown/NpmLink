namespace NpmLink.Cli.Services;

public interface ITsConfigEditor
{
    bool AddPaths(string workspacePath, string libraryName, string librarySourcePath);
    bool RemovePaths(string workspacePath, string libraryName);
    (bool exists, bool exactKeyMatch, bool wildcardKeyMatch, bool exactValueMatch, bool wildcardValueMatch) VerifyPaths(
        string workspacePath, string libraryName, string librarySourcePath);
}
