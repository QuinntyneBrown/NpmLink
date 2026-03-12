# NpmLink

NpmLink is a .NET CLI tool for linking a local npm library into an Angular workspace for local development and debugging.

## What it does

NpmLink provides three commands:

**Link**

1. Runs `npm link` in the library source directory to register it globally.
2. Runs `npm link <library>` in the Angular workspace to create the symlink.
3. Updates TypeScript path mappings so the workspace resolves imports against the local library source.

**Unlink**

1. Runs `npm unlink <library>` in the Angular workspace to remove the symlink.
2. Runs `npm unlink` in the library source directory to unregister it globally.
3. Removes the TypeScript path mappings that were added during linking.

**Verify**

1. Checks that `node_modules/<library>` is a symlink pointing to the expected library source.
2. Checks that the workspace contains the expected TypeScript path mappings for that library.

## Current Code Organization

The repository currently contains a single source project, `src/NpmLink.Cli`, organized into logical layers:

- `Program.cs`: composition root using `Host.CreateApplicationBuilder`, command parsing, and process exit handling.
- `Commands/`: one command per file plus shared command helpers.
- `Services/`: orchestration, npm execution, `tsconfig` editing, service contracts, and result models.

This is a layered design inside one project rather than separate physical projects.

## Repository Layout

| Path | Purpose |
|---|---|
| `src/NpmLink.Cli/` | Main CLI project |
| `src/NpmLink.Cli/Commands/` | `Link`, `Unlink`, and `Verify` command definitions plus shared command helpers |
| `src/NpmLink.Cli/Services/` | `NpmLinkService`, `NpmClient`, `TsConfigEditor`, interfaces, and `OperationResult` |
| `tests/NpmLink.Cli.Tests/` | Unit tests and fakes such as `FakeNpmClient` |
| `docs/specs/` | L1 and L2 requirements/specification documents |
| `docs/detailed-design/` | Design notes and PlantUML-based architecture diagrams |
| `eng/scripts/` | Helper scripts, including `install.bat` for local tool installation |
| `NpmLink.slnx` | Solution entry point |

## Architecture Notes

The current implementation uses dependency injection and keeps responsibilities separated by concern:

- Command files resolve `INpmLinkService` from DI and render `OperationResult` messages.
- `NpmLinkService` orchestrates link, unlink, and verify workflows.
- `NpmClient` handles typed npm invocation with `ProcessStartInfo.ArgumentList`.
- `TsConfigEditor` handles JSONC-safe `tsconfig.json` parsing and mutation.

The design documents under `docs/detailed-design/` describe the intended architecture direction and may be slightly ahead of the current physical project layout.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Node.js / npm

## Build

```bash
dotnet build
```

## Install

To install or update the `npm-link` global tool from source on Windows:

```bat
eng\scripts\install.bat
```

This packs `src/NpmLink.Cli` and installs or updates it as a global .NET tool.

## Usage

### Link (default command)

```bash
npm-link --workspace <path> --library <name> --source <path>
```

### Unlink

```bash
npm-link unlink --workspace <path> --library <name> --source <path>
```

### Verify

```bash
npm-link verify --workspace <path> --library <name> --source <path>
```

### Options

All commands accept the same options:

| Option | Alias | Description |
|---|---|---|
| `--workspace` | `-w` | Path to the Angular workspace (directory containing `angular.json`) |
| `--library` | `-l` | Name of the library as it appears in `package.json` (for example `@my-org/my-lib`) |
| `--source` | `-s` | Path to the library source project directory (where its `package.json` lives) |

### Examples

```bash
# Link a library for local development
npm-link -w ./my-angular-app -l @my-org/my-lib -s ../my-lib

# Verify the link is set up correctly
npm-link verify -w ./my-angular-app -l @my-org/my-lib -s ../my-lib

# Unlink when done
npm-link unlink -w ./my-angular-app -l @my-org/my-lib -s ../my-lib
```

## Documentation

- [L1 Requirements](docs/specs/L1.md)
- [L2 Detailed Requirements](docs/specs/L2.md)
- [Detailed Design Overview](docs/detailed-design/README.md)
- [CLI Command Parsing Design](docs/detailed-design/01-cli-command-parsing.md)
- [Application Orchestration Design](docs/detailed-design/02-npm-link-service.md)
- [Process Execution Design](docs/detailed-design/03-process-execution.md)
- [Validation Design](docs/detailed-design/04-validation.md)
- [TSConfig Editing Design](docs/detailed-design/05-tsconfig-update.md)

## Tests

```bash
dotnet test
```
