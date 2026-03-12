using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NpmLink.Cli.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<INpmClient, NpmClient>();
builder.Services.AddSingleton<ITsConfigEditor, TsConfigEditor>();
builder.Services.AddSingleton<INpmLinkService, NpmLinkService>();

var host = builder.Build();
var serviceProvider = host.Services;

var workspaceOption = new Option<string>("--workspace", "-w")
{
    Description = "Path to the Angular workspace (directory containing angular.json).",
    Required = true,
};

var libraryNameOption = new Option<string>("--library", "-l")
{
    Description = "Name of the library as it appears in package.json (e.g. @my-org/my-lib).",
    Required = true,
};

var librarySourceOption = new Option<string>("--source", "-s")
{
    Description = "Path to the library source project directory (where its package.json lives).",
    Required = true,
};

var rootCommand = new RootCommand("NpmLink — links a local library into an Angular workspace for local development and debugging.");
rootCommand.Add(workspaceOption);
rootCommand.Add(libraryNameOption);
rootCommand.Add(librarySourceOption);

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var workspace = parseResult.GetValue(workspaceOption)!;
    var library = parseResult.GetValue(libraryNameOption)!;
    var source = parseResult.GetValue(librarySourceOption)!;

    var service = serviceProvider.GetRequiredService<INpmLinkService>();
    var result = await service.LinkAsync(workspace, library, source, cancellationToken);
    RenderResult(result);
    return result.ExitCode;
});

var unlinkCommand = new Command("unlink", "Unlinks a previously linked local library from an Angular workspace.");
unlinkCommand.Add(workspaceOption);
unlinkCommand.Add(libraryNameOption);
unlinkCommand.Add(librarySourceOption);

unlinkCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var workspace = parseResult.GetValue(workspaceOption)!;
    var library = parseResult.GetValue(libraryNameOption)!;
    var source = parseResult.GetValue(librarySourceOption)!;

    var service = serviceProvider.GetRequiredService<INpmLinkService>();
    var result = await service.UnlinkAsync(workspace, library, source, cancellationToken);
    RenderResult(result);
    return result.ExitCode;
});

var verifyCommand = new Command("verify", "Verifies that a library is correctly linked in an Angular workspace.");
verifyCommand.Add(workspaceOption);
verifyCommand.Add(libraryNameOption);
verifyCommand.Add(librarySourceOption);

verifyCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var workspace = parseResult.GetValue(workspaceOption)!;
    var library = parseResult.GetValue(libraryNameOption)!;
    var source = parseResult.GetValue(librarySourceOption)!;

    var service = serviceProvider.GetRequiredService<INpmLinkService>();
    var result = await service.VerifyAsync(workspace, library, source, cancellationToken);
    RenderResult(result);
    return result.ExitCode;
});

rootCommand.Add(unlinkCommand);
rootCommand.Add(verifyCommand);

// Disable response file handling so that scoped package names like @my-org/my-lib
// are not misinterpreted as response files.
var parserConfig = new ParserConfiguration { ResponseFileTokenReplacer = null };
var parseResultFinal = rootCommand.Parse(args, parserConfig);
return await parseResultFinal.InvokeAsync();

static void RenderResult(OperationResult result)
{
    foreach (var message in result.Messages)
    {
        if (message.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("FAIL:", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
