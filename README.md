# NpmLink

A .NET CLI tool that links a local npm library into an Angular workspace for local development and debugging.

## What it does

NpmLink provides two commands:

**Link** — symlinks a local library into an Angular workspace:

1. Runs `npm link` in the library source directory to register it globally
2. Runs `npm link <library>` in the Angular workspace to create the symlink
3. Updates `tsconfig.json` path mappings to point to the local library source

**Unlink** — reverses the linking process:

1. Runs `npm unlink <library>` in the Angular workspace to remove the symlink
2. Runs `npm unlink` in the library source directory to unregister it globally
3. Removes the `tsconfig.json` path mappings that were added during linking

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Node.js / npm

## Build

```bash
dotnet build
```

## Usage

### Link (default command)

```bash
npm-link --workspace <path> --library <name> --source <path>
```

### Unlink

```bash
npm-link unlink --workspace <path> --library <name> --source <path>
```

### Options

Both commands accept the same options:

| Option | Alias | Description |
|---|---|---|
| `--workspace` | `-w` | Path to the Angular workspace (directory containing `angular.json`) |
| `--library` | `-l` | Name of the library as it appears in `package.json` (e.g. `@my-org/my-lib`) |
| `--source` | `-s` | Path to the library source project directory (where its `package.json` lives) |

### Examples

```bash
# Link a library for local development
npm-link -w ./my-angular-app -l @my-org/my-lib -s ../my-lib

# Unlink when done
npm-link unlink -w ./my-angular-app -l @my-org/my-lib -s ../my-lib
```

## Tests

```bash
dotnet test
```
