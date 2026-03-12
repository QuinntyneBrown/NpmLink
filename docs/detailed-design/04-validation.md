# Validation - Detailed Design

## Overview

Validation should be centralized and reusable across `link`, `unlink`, and `verify`. The goal is to keep validation rules consistent, produce deterministic diagnostics, and prevent side effects when required preconditions are not met.

## Validation Scope

The validation layer should cover:

- Workspace path resolution and existence.
- Source path resolution and existence.
- Angular workspace identification through `angular.json`.
- Package identity validation through `package.json`.
- Command-specific prerequisites where needed.

## Suggested Abstractions

| Type | Responsibility |
|------|----------------|
| `IRequestValidator<TRequest>` | Validates a request and returns diagnostics |
| `LinkLibraryRequestValidator` | Validation rules for link |
| `UnlinkLibraryRequestValidator` | Validation rules for unlink |
| `VerifyLinkRequestValidator` | Validation rules for verify |
| `ValidationResult` | Success/failure plus validation messages |

## Command Rules

### Link

- Workspace directory must exist.
- Source directory must exist.
- Workspace must contain `angular.json`.
- Source must contain `package.json`.
- `package.json` name must match the requested library name.

### Unlink

- Workspace directory must exist.
- Source directory must exist when the command needs to run `npm unlink` there.
- Workspace must contain `angular.json`.
- Package identity validation should be applied consistently if the source path is part of the unlink workflow.

### Verify

- Workspace directory must exist.
- Source directory should exist so the expected symlink target can be validated meaningfully.
- Workspace must contain `angular.json`.
- The validator should prepare normalized absolute paths for downstream comparison logic.

## Behaviour

Validation should run before any side effects and return all actionable diagnostics required to explain why the command cannot proceed.

Recommended flow:

1. Normalize incoming paths.
2. Validate directory existence.
3. Validate workspace shape.
4. Validate package identity where applicable.
5. Return a `ValidationResult` to the application handler.

## Test Strategy

- Unit test each validator independently.
- Assert that failed validation prevents any npm invocation.
- Add explicit unlink validation coverage for missing source path.
- Add verification coverage for missing source path if verify depends on it.

## Design Decisions

- **Shared policy, command-specific rules**: Common validation should be reused, but command-specific requirements still need explicit validators.
- **Deterministic diagnostics**: Validation should explain the real precondition failure instead of leaking lower-level process exceptions.
- **No inline validation in orchestrators**: Handlers should consume validator results, not embed all validation logic themselves.

## Diagram Note

The current validation diagrams in `diagrams/` reflect the earlier implementation and should be regenerated after the refactor is complete.
