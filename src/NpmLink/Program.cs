using System.CommandLine;
using System.CommandLine.Parsing;
using NpmLink.Services;

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

    var service = new NpmLinkService(new ProcessRunner());
    return await service.LinkAsync(workspace, library, source, cancellationToken);
});

// Disable response file handling so that scoped package names like @my-org/my-lib
// are not misinterpreted as response files.
var parserConfig = new ParserConfiguration { ResponseFileTokenReplacer = null };
var parseResult = rootCommand.Parse(args, parserConfig);
return await parseResult.InvokeAsync();
