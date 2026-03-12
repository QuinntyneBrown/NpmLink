# NpmLink

A .NET global tool that links a local npm library into an Angular workspace for local development and debugging. It automates `npm link`, updates TypeScript path mappings in `tsconfig.json`, and provides a verify command to confirm everything is wired up correctly.

## Features

- **Link** a local library into an Angular workspace with a single command
- **Unlink** to cleanly reverse the process
- **Verify** that the symlink and tsconfig path mappings are correct
- JSONC-safe `tsconfig.json` handling (comments, trailing commas)
- Scoped package support (e.g. `@my-org/my-lib`)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) / npm

## Installation

Install or update the tool globally from source:

```bat
eng\scripts\install.bat
```

This packs the project and installs it as a global .NET tool named `npm-link`.

## Quick Start

```bash
# Link a library for local development
npm-link -w ./my-angular-app -l @my-org/my-lib -s ../my-lib

# Verify the link is set up correctly
npm-link verify -w ./my-angular-app -l @my-org/my-lib -s ../my-lib

# Unlink when done
npm-link unlink -w ./my-angular-app -l @my-org/my-lib -s ../my-lib
```

## Commands

### Link (default)

```bash
npm-link --workspace <path> --library <name> --source <path>
```

1. Runs `npm link` in the library source directory to register it globally.
2. Runs `npm link <library>` in the Angular workspace to create the symlink.
3. Updates `tsconfig.json` path mappings to resolve imports against the local source.

### Unlink

```bash
npm-link unlink --workspace <path> --library <name> --source <path>
```

1. Runs `npm unlink <library>` in the Angular workspace to remove the symlink.
2. Runs `npm unlink` in the library source directory to unregister it globally.
3. Removes the `tsconfig.json` path mappings added during linking.

### Verify

```bash
npm-link verify --workspace <path> --library <name> --source <path>
```

1. Checks that `node_modules/<library>` is a symlink pointing to the expected source.
2. Checks that `tsconfig.json` path mapping values are correct.

### Options

All commands accept the same options:

| Option | Alias | Description |
|---|---|---|
| `--workspace` | `-w` | Path to the Angular workspace (directory containing `angular.json`) |
| `--library` | `-l` | Library name as it appears in `package.json` (e.g. `@my-org/my-lib`) |
| `--source` | `-s` | Path to the library source directory (where its `package.json` lives) |

## Building from Source

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Project Structure

```
src/NpmLink.Cli/
  Program.cs              Composition root (DI, command wiring, output rendering)
  Commands/               One file per command + shared helpers
  Services/               NpmLinkService, NpmClient, TsConfigEditor, interfaces, OperationResult
tests/NpmLink.Cli.Tests/  Unit tests and fakes
docs/specs/               L1/L2 requirements
docs/detailed-design/     Architecture and design notes
eng/scripts/              Build and install scripts
```

## Documentation

- [L1 Requirements](docs/specs/L1.md)
- [L2 Detailed Requirements](docs/specs/L2.md)
- [Detailed Design Overview](docs/detailed-design/README.md)

## License

[MIT](LICENSE)
