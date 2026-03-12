using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using NpmLink.Cli.Services;

namespace NpmLink.Cli.Commands;

public static class LinkCommand
{
    public static RootCommand CreateRoot(IServiceProvider serviceProvider)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var libraryNameOption = CommandOptions.CreateLibraryNameOption();
        var librarySourceOption = CommandOptions.CreateLibrarySourceOption();

        var command = new RootCommand("NpmLink — links a local library into an Angular workspace for local development and debugging.");
        command.Add(workspaceOption);
        command.Add(libraryNameOption);
        command.Add(librarySourceOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var workspace = parseResult.GetValue(workspaceOption)!;
            var library = parseResult.GetValue(libraryNameOption)!;
            var source = parseResult.GetValue(librarySourceOption)!;

            var service = serviceProvider.GetRequiredService<INpmLinkService>();
            var result = await service.LinkAsync(workspace, library, source, cancellationToken);
            CommandResultRenderer.Render(result);
            return result.ExitCode;
        });

        return command;
    }
}
