# Process Execution - Detailed Design

## Overview

npm process execution is handled by `NpmClient`, which implements `INpmClient`. It translates business-level npm operations into process invocations using `ProcessStartInfo.ArgumentList` for safe argument passing.

## Abstraction

### `INpmClient`

The application layer depends on npm-specific operations:

```csharp
public interface INpmClient
{
    Task<int> LinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> LinkIntoWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> UnlinkFromWorkspaceAsync(string libraryName, string workingDirectory, CancellationToken cancellationToken = default);
    Task<int> UnlinkGlobalAsync(string workingDirectory, CancellationToken cancellationToken = default);
}
```

Each method returns the process exit code. The application layer interprets non-zero as failure.

## Implementation

`NpmClient` manages its own process execution directly — there is no separate `IProcessRunner` or `ProcessRequest` type in the execution path.

Each method calls a private `RunNpmAsync` helper that:

1. Creates a `ProcessStartInfo` with `FileName` set to the platform-appropriate npm executable.
2. Adds each argument individually via `ArgumentList.Add()`.
3. Redirects stdout and stderr.
4. Starts the process and waits for exit.
5. Returns the exit code.

### Platform Handling

```csharp
private static string NpmExecutable => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
```

- Windows: invokes `npm.cmd` directly.
- Linux/macOS: invokes `npm`.
- No `cmd /c` wrapping.

Platform resolution is encapsulated inside `NpmClient` so the application layer is platform-agnostic.

### Console Output

`NpmClient` currently forwards npm's stdout to `Console.WriteLine` and stderr to `Console.Error.WriteLine` via `OutputDataReceived` and `ErrorDataReceived` event handlers. This is the one exception to the "no console writes outside the CLI layer" principle — npm process output is streamed in real time rather than buffered.

## Test Strategy

Tests use a `FakeNpmClient` that implements `INpmClient`, records invocations (method name, arguments, working directory), and returns configurable exit codes.

Tests cover:
- Scoped package names (e.g. `@my-org/my-lib`).
- Paths containing spaces.
- Invocation order and short-circuiting on failure.

## Design Decisions

- **No separate `IProcessRunner`/`ProcessRequest`**: `NpmClient` owns its process execution directly. The legacy `IProcessRunner` and `ProcessRunner` still exist in the codebase but are not used by the current architecture. `FakeNpmClient` replaces `FakeProcessRunner` for testing.
- **`ArgumentList` over string concatenation**: Prevents quoting bugs with scoped packages and paths containing spaces.
- **npm-specific abstraction**: The application layer expresses intent (`LinkGlobalAsync`) rather than process mechanics (`RunAsync("npm", "link")`).
