using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NpmLink.Cli.Commands;
using NpmLink.Cli.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<INpmClient, NpmClient>();
builder.Services.AddSingleton<ITsConfigEditor, TsConfigEditor>();
builder.Services.AddSingleton<INpmLinkService, NpmLinkService>();

using var host = builder.Build();
var serviceProvider = host.Services;

var rootCommand = LinkCommand.CreateRoot(serviceProvider);
rootCommand.Add(UnlinkCommand.Create(serviceProvider));
rootCommand.Add(VerifyCommand.Create(serviceProvider));

// Disable response file handling so that scoped package names like @my-org/my-lib
// are not misinterpreted as response files.
var parserConfig = new ParserConfiguration { ResponseFileTokenReplacer = null };
var parseResult = rootCommand.Parse(args, parserConfig);
return await parseResult.InvokeAsync();
