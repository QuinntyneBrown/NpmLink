using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using NpmLink.Cli.Services;

namespace NpmLink.Cli.Commands;

public static class VerifyCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var libraryNameOption = CommandOptions.CreateLibraryNameOption();
        var librarySourceOption = CommandOptions.CreateLibrarySourceOption();

        var command = new Command("verify", "Verifies that a library is correctly linked in an Angular workspace.");
        command.Add(workspaceOption);
        command.Add(libraryNameOption);
        command.Add(librarySourceOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var workspace = parseResult.GetValue(workspaceOption)!;
            var library = parseResult.GetValue(libraryNameOption)!;
            var source = parseResult.GetValue(librarySourceOption)!;

            var service = serviceProvider.GetRequiredService<INpmLinkService>();
            var result = await service.VerifyAsync(workspace, library, source, cancellationToken);
            CommandResultRenderer.Render(result);
            return result.ExitCode;
        });

        return command;
    }
}
