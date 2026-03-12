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

## Architecture

The project follows a layered .NET architecture with dependency injection:

- **CLI layer** (`Program.cs`): composition root using `Host.CreateApplicationBuilder`, command parsing via System.CommandLine, output rendering, exit code mapping.
- **Application layer** (`NpmLinkService`): link, unlink, and verify orchestration. Returns structured `OperationResult` objects — no direct console IO.
- **Infrastructure layer**: `NpmClient` (typed npm process invocation via `ProcessStartInfo.ArgumentList`), `TsConfigEditor` (JSONC-safe tsconfig parsing and mutation).

All services are registered in DI and resolved by command handlers. The `ITsConfigEditor` and `INpmClient` abstractions are fully testable via fakes.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Node.js / npm

## Build

```bash
dotnet build
```

## Install

To install or update the `npm-link` global tool from source:

```bash
eng\scripts\install.bat
```

This packs the CLI project and installs (or updates) it as a global dotnet tool.

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
| `--library` | `-l` | Name of the library as it appears in `package.json` (e.g. `@my-org/my-lib`) |
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
- [Implementation Fix Checklist](docs/implementation-fix-checklist.md)
- [Detailed Design Overview](docs/detailed-design/README.md)

## Tests

```bash
dotnet test
```
