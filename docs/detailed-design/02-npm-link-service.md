# Application Orchestration - Detailed Design

## Overview

`NpmLinkService` is the single orchestration service that implements `INpmLinkService`. It coordinates validation, npm operations, and tsconfig editing for the link, unlink, and verify workflows. It depends on `INpmClient` and `ITsConfigEditor` and returns `OperationResult` from every public method.

## Interface

```csharp
public interface INpmLinkService
{
    Task<OperationResult> LinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
    Task<OperationResult> UnlinkAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
    Task<OperationResult> VerifyAsync(string workspacePath, string libraryName, string librarySourcePath, CancellationToken cancellationToken = default);
}
```

## Result Model

All methods return `OperationResult`:

```csharp
public record OperationResult(int ExitCode, List<string> Messages);
```

`OperationResult` includes static factory methods (`Success`, `Failure`) and an `AddMessage` helper. Messages are rendered by the CLI layer — the service never writes to the console.

## Workflows

### Link

1. Resolve and validate workspace path (exists, contains `angular.json`).
2. Resolve and validate library source path (exists, contains `package.json` with matching name).
3. Call `INpmClient.LinkGlobalAsync` in the library source directory.
4. Call `INpmClient.LinkIntoWorkspaceAsync` in the workspace directory.
5. Call `ITsConfigEditor.AddPaths` to update tsconfig mappings.
6. Return success only if all steps succeed.

### Unlink

1. Resolve and validate workspace path (exists, contains `angular.json`).
2. Resolve and validate library source path (exists).
3. Call `INpmClient.UnlinkFromWorkspaceAsync` in the workspace directory.
4. Call `INpmClient.UnlinkGlobalAsync` in the library source directory.
5. Call `ITsConfigEditor.RemovePaths` to clean up tsconfig mappings.
6. Return success only if all steps succeed.

### Verify

1. Resolve and validate workspace path (exists, contains `angular.json`).
2. Check `node_modules/<library>` symlink existence, type, and target.
3. Call `ITsConfigEditor.VerifyPaths` to check both key presence and value correctness.
4. Aggregate all check results — report every failure, not just the first one.
5. Return exit code 0 only if all checks pass.

## Failure Policy

- **Validation failures** return immediately with exit code 1 and prevent any side effects.
- **npm failures** stop the workflow and propagate the non-zero exit code.
- **tsconfig failures** (present file cannot be parsed or written) are treated as operation failures (exit code 1).
- **Missing tsconfig** is not an error — the operation succeeds without updating paths.
- **Verify** reports all detected mismatches rather than short-circuiting on the first failure.

## Dependencies

| Abstraction | Implementation | Purpose |
|---|---|---|
| `INpmClient` | `NpmClient` | npm link/unlink process execution |
| `ITsConfigEditor` | `TsConfigEditor` | tsconfig.json reading, writing, and verification |

## Design Decisions

- **Single service, not separate handlers**: The three workflows share validation logic and have similar structure, so a single `NpmLinkService` class is simpler than three separate handler classes.
- **Inline validation**: Validation is performed directly in each method rather than through a separate `IRequestValidator<T>` abstraction, since the rules are straightforward and command-specific.
- **No `IWorkspaceInspector`**: Symlink verification is done inline in `VerifyAsync` using `DirectoryInfo` and `Directory.ResolveLinkTarget`. A separate abstraction was not needed given the limited scope.
- **No console writes**: All output is returned via `OperationResult.Messages`.
