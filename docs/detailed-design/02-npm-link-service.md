# Application Orchestration - Detailed Design

## Overview

The current single-service shape should be refactored into application-level handlers that orchestrate link, unlink, and verify workflows through narrow abstractions. The application layer should own business flow and failure policy, but not console output, file IO, or process execution details.

## Core Use Cases

### `LinkLibraryHandler`

Responsibilities:

- Validate the request.
- Register the library globally with npm.
- Link the library into the Angular workspace.
- Update `tsconfig` path mappings.
- Return a structured result with success/failure diagnostics.

### `UnlinkLibraryHandler`

Responsibilities:

- Validate the request, including the source path if it is required for npm unlink.
- Remove the workspace link.
- Remove the global library link.
- Remove `tsconfig` path mappings.
- Return a structured result with success/failure diagnostics.

### `VerifyLinkHandler`

Responsibilities:

- Validate the request.
- Verify that `node_modules/<library>` is a symlink to the expected source path.
- Verify that `tsconfig` contains the expected exact and wildcard mappings, including value correctness.
- Return a structured result that lists all failures rather than stopping after the first verification problem.

## Suggested Contracts

| Type | Responsibility |
|------|----------------|
| `ILinkLibraryHandler` | Executes the link workflow |
| `IUnlinkLibraryHandler` | Executes the unlink workflow |
| `IVerifyLinkHandler` | Executes the verify workflow |
| `IRequestValidator<TRequest>` | Validates requests before orchestration |
| `INpmClient` | npm-specific operations |
| `ITsConfigEditor` | Upsert, remove, and inspect path mappings |
| `IWorkspaceInspector` | Checks workspace structure and symlink state |

## Result Model

Application handlers should return a structured result rather than a raw exit code, for example:

- `Succeeded`
- `ExitCode`
- `Diagnostics`
- `Warnings`

This keeps orchestration testable and leaves rendering decisions to the CLI layer.

## Behaviour

### Link

1. Validate workspace path, source path, Angular workspace shape, and package identity.
2. Invoke `INpmClient.LinkGlobalAsync`.
3. Invoke `INpmClient.LinkIntoWorkspaceAsync`.
4. Invoke `ITsConfigEditor.UpsertMappingsAsync`.
5. Return success only if all required steps succeed.

### Unlink

1. Validate workspace path and source path consistently with the link workflow.
2. Invoke `INpmClient.UnlinkFromWorkspaceAsync`.
3. Invoke `INpmClient.UnlinkGlobalAsync`.
4. Invoke `ITsConfigEditor.RemoveMappingsAsync`.
5. Return success only if all required steps succeed.

### Verify

1. Validate the request.
2. Inspect the symlink in `node_modules`.
3. Inspect `tsconfig` mappings and compare both keys and values.
4. Aggregate failures into a single result.

## Failure Policy

- Validation failures should prevent any side effects.
- npm failures should stop the workflow and propagate the relevant non-zero result.
- If a `tsconfig` file is present and expected to be updated, parse/write failures should be treated as operation failures.
- Verification should report all detected mismatches, not just the first one.

## Design Decisions

- **No console writes in application code**: Console output belongs to the CLI layer.
- **Use-case handlers over one large service**: Separate handlers make the workflows easier to reason about and test.
- **Structured results over raw `int`**: The application layer should communicate intent and diagnostics, not just exit codes.
- **Explicit failure policy**: `tsconfig` mutation is part of correctness, not a best-effort side effect.

## Diagram Note

The existing service diagrams in `diagrams/` reflect the earlier implementation and should be regenerated after the refactor is complete.
