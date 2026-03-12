# TSConfig Editing - Detailed Design

## Overview

`TsConfigEditor` implements `ITsConfigEditor` and handles all `tsconfig.json` file IO, JSONC parsing, and path mapping operations. It is part of the correctness boundary — tsconfig mutations are not best-effort side effects.

## Interface

```csharp
public interface ITsConfigEditor
{
    bool AddPaths(string workspacePath, string libraryName, string librarySourcePath);
    bool RemovePaths(string workspacePath, string libraryName);
    (bool exists, bool exactKeyMatch, bool wildcardKeyMatch, bool exactValueMatch, bool wildcardValueMatch) VerifyPaths(
        string workspacePath, string libraryName, string librarySourcePath);
}
```

- `AddPaths` and `RemovePaths` return `bool` — `true` on success, `false` on parse/write failure.
- `VerifyPaths` returns a tuple describing the state of each check.

## JSONC Support

All parsing uses these `JsonDocumentOptions`:

```csharp
CommentHandling = JsonCommentHandling.Skip
AllowTrailingCommas = true
```

This handles real-world `tsconfig.json` files that commonly contain `//` comments and trailing commas.

## Operations

### AddPaths (link)

1. Read `tsconfig.json` from the workspace root.
2. If the file does not exist, return `true` (skip silently — the application layer treats missing tsconfig as non-fatal).
3. Parse as JSONC.
4. Ensure `compilerOptions.paths` exists (create if needed).
5. Compute the relative path from workspace to library source via `Path.GetRelativePath`, normalized to `/`.
6. Set `libraryName` → `[relativePath]` and `libraryName/*` → `[relativePath/*]`.
7. Write back with `WriteIndented = true`.
8. Return `false` if any step throws (parse error, IO error).

### RemovePaths (unlink)

1. Read `tsconfig.json` from the workspace root.
2. If the file does not exist, return `true` (skip silently).
3. Parse as JSONC.
4. Remove both `libraryName` and `libraryName/*` keys from `compilerOptions.paths` if present.
5. Write back with `WriteIndented = true`.
6. Return `false` if any step throws.

### VerifyPaths (verify)

1. Check if `tsconfig.json` exists. If not, return `(false, false, false, false, false)`.
2. Parse as JSONC.
3. Compute the expected relative path and wildcard path.
4. Check whether both keys exist in `compilerOptions.paths`.
5. For each key that exists, compare the first array element against the expected value (normalized to `/`).
6. Return the full tuple so the caller can report specific failures.

## Failure Policy

- **Present file, parse failure** → return `false` (or tuple with `exists = true` but no matches). The application layer treats this as an operation failure.
- **Present file, write failure** → return `false`.
- **Missing file** → `AddPaths` and `RemovePaths` return `true` (success, nothing to do). `VerifyPaths` returns `exists = false`.

## Path Normalization

- Relative paths are computed via `Path.GetRelativePath(workspacePath, librarySourcePath)`.
- All backslashes are replaced with forward slashes before writing or comparing.
- Comparison in `VerifyPaths` normalizes both expected and actual values to `/` before checking equality.

## Example

Given:

- Workspace: `C:\projects\my-app`
- Library: `@my-org/my-lib`
- Source: `C:\projects\my-lib`

Expected mappings:

```json
{
  "compilerOptions": {
    "paths": {
      "@my-org/my-lib": ["../my-lib"],
      "@my-org/my-lib/*": ["../my-lib/*"]
    }
  }
}
```

## Design Decisions

- **`bool` return over typed result**: `AddPaths` and `RemovePaths` only need to communicate success/failure. A richer result type was not needed since the error details are logged internally and the caller just needs to know whether to proceed.
- **Tuple return for verify**: The 5-field tuple gives the caller enough detail to produce specific PASS/FAIL messages for each check without requiring a dedicated result class.
- **No async**: All operations are synchronous file reads/writes, so the interface methods are not async. The service layer calls them synchronously.
