using System.CommandLine;

namespace NpmLink.Cli.Commands;

internal static class CommandOptions
{
    public static Option<string> CreateWorkspaceOption() =>
        new("--workspace", "-w")
        {
            Description = "Path to the Angular workspace (directory containing angular.json).",
            Required = true,
        };

    public static Option<string> CreateLibraryNameOption() =>
        new("--library", "-l")
        {
            Description = "Name of the library as it appears in package.json (e.g. @my-org/my-lib).",
            Required = true,
        };

    public static Option<string> CreateLibrarySourceOption() =>
        new("--source", "-s")
        {
            Description = "Path to the library source project directory (where its package.json lives).",
            Required = true,
        };
}
