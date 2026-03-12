# Validation - Detailed Design

## Overview

Input validation is performed inline at the start of each `NpmLinkService` method. Each method resolves paths, checks preconditions, and returns an `OperationResult.Failure` immediately if any check fails — preventing side effects.

## Validation Rules

### Link

1. Resolve `workspacePath` and `librarySourcePath` to absolute paths via `Path.GetFullPath`.
2. Workspace directory must exist.
3. Library source directory must exist.
4. Workspace must contain `angular.json`.
5. Library source must contain `package.json` with a `name` field matching `libraryName`.

### Unlink

1. Resolve `workspacePath` and `librarySourcePath` to absolute paths.
2. Workspace directory must exist.
3. Workspace must contain `angular.json`.
4. Library source directory must exist.

### Verify

1. Resolve `workspacePath` and `librarySourcePath` to absolute paths.
2. Workspace directory must exist.
3. Workspace must contain `angular.json`.

## Shared Helpers

Two private static methods in `NpmLinkService` handle reusable checks:

- `ValidateAngularWorkspace(path)` — checks for `angular.json`.
- `ValidateLibraryPackageJson(path, name)` — checks for `package.json` with matching name.

## Error Reporting

Each validation failure returns `OperationResult.Failure(message)` with a descriptive error message prefixed with `"Error:"`. The CLI layer routes these to stderr via `CommandResultRenderer`.

## Design Decisions

- **Inline validation**: Validation is performed directly in each service method rather than through a separate `IRequestValidator<T>` abstraction. The rules are straightforward and command-specific, so a separate validator layer would add indirection without meaningful benefit.
- **No `ValidationResult` type**: Validation failures use the same `OperationResult` type as all other failures, keeping the result model simple.
- **Early return**: Each validation check returns immediately on failure, preventing any npm or tsconfig side effects.
- **Consistent source path validation**: Both `LinkAsync` and `UnlinkAsync` validate that the library source path exists before proceeding.
