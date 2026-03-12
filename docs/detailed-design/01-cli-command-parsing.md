# CLI Command Parsing - Detailed Design

## Overview

The CLI layer should be a thin shell around the application layer. Its job is to parse arguments, resolve handlers through dependency injection, translate structured results into user-facing output, and return the correct process exit code.

This design replaces direct construction of concrete services inside command actions with a real composition root based on the .NET Generic Host.

## Responsibilities

### `Program.cs`

`Program.cs` should become the composition root and own:

- `Host.CreateApplicationBuilder()` setup.
- DI registration for application and infrastructure services.
- Construction of the `RootCommand` and subcommands.
- Parser configuration that disables response file handling for scoped package names such as `@my-org/my-lib`.
- Invocation of the parser and propagation of the final exit code.

### Command Definitions

Command definitions should:

- Define shared options once and reuse them across `link`, `unlink`, and `verify`.
- Bind parsed values into request records rather than passing raw strings through multiple layers.
- Resolve handler classes from DI rather than constructing services directly.

### Command Handlers

Each command should delegate to a dedicated handler, for example:

- `LinkCommandHandler`
- `UnlinkCommandHandler`
- `VerifyCommandHandler`

Each handler should:

- Accept a typed request record.
- Call the corresponding application service or use-case handler.
- Render structured diagnostics returned by the application layer.
- Map the final result to an exit code.

## Suggested Types

| Type | Responsibility |
|------|----------------|
| `LinkLibraryRequest` | Parsed arguments for the link workflow |
| `UnlinkLibraryRequest` | Parsed arguments for the unlink workflow |
| `VerifyLinkRequest` | Parsed arguments for the verify workflow |
| `OperationResult` | Success/failure state, exit code, and diagnostics |
| `IResultRenderer` or CLI-local renderer | Converts structured results to console output |

## Behaviour

1. The user invokes `npm-link`, `npm-link unlink`, or `npm-link verify`.
2. `Program.cs` builds the host and registers dependencies.
3. The root command and subcommands are created using shared option definitions.
4. Response file handling is disabled so scoped package names are parsed correctly.
5. `System.CommandLine` parses the arguments.
6. The selected command binds the arguments into a request record.
7. The command resolves its handler from DI.
8. The handler calls the application layer and receives an `OperationResult`.
9. The CLI renders diagnostics and returns the mapped exit code.

## Design Decisions

- **Thin CLI layer**: The CLI should not perform orchestration, validation, or infrastructure setup beyond dependency registration.
- **DI-first command execution**: Commands should not construct concrete services like `NpmLinkService` or `ProcessRunner` directly.
- **Shared option model**: Common options must be defined once so the command surface stays consistent across commands.
- **Structured output boundary**: The CLI is the only layer that should write to the console.

## Diagram Note

The existing CLI diagrams in `diagrams/` reflect the earlier implementation and should be regenerated after the refactor is complete.
