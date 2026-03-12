# CLI Command Parsing - Detailed Design

## Overview

The CLI layer is a thin shell around the service layer. It parses arguments, resolves services through dependency injection, translates structured results into console output, and returns the process exit code.

## Structure

### `Program.cs`

`Program.cs` is the composition root. It:

- Calls `Host.CreateApplicationBuilder()` to set up the DI container.
- Registers `INpmClient`, `ITsConfigEditor`, and `INpmLinkService` as singletons.
- Delegates command construction to the static methods in `Commands/`.
- Configures the parser to disable response file handling (so scoped package names like `@my-org/my-lib` are not misinterpreted).
- Invokes the parser and propagates the exit code.

### `Commands/`

Each command is defined in its own file as a static class:

| File | Responsibility |
|------|----------------|
| `LinkCommand.cs` | Creates the `RootCommand` with link action |
| `UnlinkCommand.cs` | Creates the `unlink` subcommand |
| `VerifyCommand.cs` | Creates the `verify` subcommand |
| `CommandOptions.cs` | Factory methods for the shared `--workspace`, `--library`, `--source` options |
| `CommandResultRenderer.cs` | Renders `OperationResult` messages to stdout/stderr |

Each command file follows the same pattern:

1. Create option instances via `CommandOptions`.
2. Build the `Command` (or `RootCommand`).
3. Set an action that resolves `INpmLinkService` from the `IServiceProvider`, calls the appropriate method, renders the result via `CommandResultRenderer.Render()`, and returns the exit code.

### `CommandResultRenderer`

Routes messages to `Console.Error` when they start with `"Error:"` or `"FAIL:"`, and to `Console.WriteLine` otherwise. This is the only place in the codebase that writes to the console.

## Shared Options

Options are defined once in `CommandOptions` and instantiated per command (System.CommandLine requires separate option instances per command). The three shared options are:

- `--workspace` (`-w`) — path to the Angular workspace
- `--library` (`-l`) — library name as it appears in `package.json`
- `--source` (`-s`) — path to the library source directory

## Design Decisions

- **No request records**: Arguments are passed as strings directly to `INpmLinkService` methods rather than through typed request objects. This keeps the layer count minimal for a CLI tool of this size.
- **No separate handler classes**: Command actions resolve `INpmLinkService` directly — there are no intermediate `LinkCommandHandler`-style classes.
- **DI-first**: Commands receive `IServiceProvider` and resolve services at invocation time, not at construction time.
- **Structured output boundary**: The CLI layer is the only layer that writes to the console.
