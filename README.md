# NpmLink

A .NET CLI tool that links a local npm library into an Angular workspace for local development and debugging.

## What it does

NpmLink automates the process of symlinking a local library into an Angular workspace:

1. Runs `npm link` in the library source directory to register it globally
2. Runs `npm link <library>` in the Angular workspace to create the symlink
3. Updates `tsconfig.json` path mappings to point to the local library source

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Node.js / npm

## Build

```bash
dotnet build
```

## Usage

```bash
NpmLink --workspace <path> --library <name> --source <path>
```

### Options

| Option | Alias | Description |
|---|---|---|
| `--workspace` | `-w` | Path to the Angular workspace (directory containing `angular.json`) |
| `--library` | `-l` | Name of the library as it appears in `package.json` (e.g. `@my-org/my-lib`) |
| `--source` | `-s` | Path to the library source project directory (where its `package.json` lives) |

### Example

```bash
NpmLink -w ./my-angular-app -l @my-org/my-lib -s ../my-lib
```

## Tests

```bash
dotnet test
```
