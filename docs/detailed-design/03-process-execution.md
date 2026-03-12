# Process Execution - Detailed Design

## Overview

Process execution should be modeled as infrastructure, not embedded in application orchestration. The goal is to make npm invocation explicit, testable, and cross-platform without relying on shell string composition.

## Target Abstractions

### `INpmClient`

The application layer should depend on npm-specific operations, not raw processes:

- `LinkGlobalAsync`
- `LinkIntoWorkspaceAsync`
- `UnlinkFromWorkspaceAsync`
- `UnlinkGlobalAsync`

### `IProcessRunner`

`IProcessRunner` should accept a structured process request instead of separate command and argument strings.

Suggested request model:

- `FileName`
- `WorkingDirectory`
- `Arguments` as a collection
- `CancellationToken`

### `ProcessRunner`

`ProcessRunner` should:

- Create `ProcessStartInfo`.
- Populate `ArgumentList` rather than a single concatenated argument string.
- Redirect stdout and stderr.
- Return a typed execution result or exit code.
- Avoid writing to the console directly unless that output is routed through a dedicated output sink owned by the CLI.

## Platform Handling

- On Windows, invoke `npm.cmd`.
- On Linux and macOS, invoke `npm`.
- Do not wrap npm invocations in `cmd /c`.
- Keep platform resolution inside infrastructure so the application layer stays platform-agnostic.

## Behaviour

1. The application layer requests an npm operation through `INpmClient`.
2. `NpmClient` translates that intent into a `ProcessRequest`.
3. `ProcessRunner` launches the process with structured arguments.
4. The runner waits for completion and captures output.
5. The result is returned to `NpmClient`, then to the application layer.

## Test Strategy

- Keep a fake process runner for unit tests.
- Record the executable name, working directory, and argument list.
- Add tests for scoped package names such as `@my-org/my-lib`.
- Add tests for paths containing spaces.
- Add tests that verify no shell wrapper is used.

## Design Decisions

- **Typed arguments over command strings**: Prevents quoting bugs and reduces platform-specific branching.
- **npm-specific abstraction**: The application layer should express intent in business terms, not process terms.
- **Platform logic stays in infrastructure**: The handler code should not care whether the current platform uses `npm` or `npm.cmd`.
- **Console output stays out of process infrastructure**: Process execution should report data, not decide how it is presented.

## Diagram Note

The current process diagrams in `diagrams/` reflect the earlier implementation and should be regenerated after the refactor is complete.
