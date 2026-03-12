# TSConfig Editing - Detailed Design

## Overview

`tsconfig` editing should be handled by a dedicated infrastructure component that can upsert, remove, and verify path mappings in a JSONC-safe way. This component is part of the correctness boundary of the tool, not an optional convenience layer.

## Target Abstraction

### `ITsConfigEditor`

Suggested responsibilities:

- Load `tsconfig` content.
- Parse JSONC with comment support and trailing comma support.
- Upsert exact and wildcard mappings.
- Remove exact and wildcard mappings.
- Verify that exact and wildcard mappings match the expected values.
- Return typed results instead of throwing raw exceptions into the application layer.

## Required Behaviour

### Upsert

When linking:

1. Read the workspace `tsconfig`.
2. Parse JSONC safely.
3. Ensure `compilerOptions.paths` exists.
4. Compute the relative path from workspace to library source.
5. Normalize separators to `/`.
6. Write both:
   - `libraryName` -> `[relativePath]`
   - `libraryName/*` -> `[relativePath/*]`

### Remove

When unlinking:

1. Read the workspace `tsconfig`.
2. Parse JSONC safely.
3. Remove both keys if present.
4. Persist the updated content.

### Verify

When verifying:

1. Read the workspace `tsconfig`.
2. Parse JSONC safely.
3. Compute the expected exact and wildcard values.
4. Confirm that both keys exist.
5. Confirm that both values match the expected normalized values.
6. Return all mismatches in a typed result.

## Failure Policy

- If a `tsconfig` file is present and cannot be parsed, the operation should fail.
- If a present `tsconfig` file cannot be written, the operation should fail.
- The policy for a missing `tsconfig` file should be explicit and documented in the application layer. If the current skip behavior is preserved, it must be deliberate and covered by tests.

## Data Rules

- Treat `tsconfig` as JSONC, not strict JSON.
- Support both comments and trailing commas.
- Normalize path separators to `/`.
- Verify actual values, not just key presence.

## Example Mapping

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

## Test Strategy

- Add tests for JSONC comments.
- Add tests for trailing commas.
- Add tests for wrong exact mapping values.
- Add tests for wrong wildcard mapping values.
- Add tests for invalid `tsconfig` content that should produce a non-zero result.

## Design Decisions

- **Dedicated `tsconfig` abstraction**: Keeps JSONC and path logic out of application orchestration.
- **Value-based verification**: `verify` must prove correctness, not just presence.
- **Explicit failure semantics**: A present but unreadable or unwritable `tsconfig` is a failed operation.

## Diagram Note

The current tsconfig diagrams in `diagrams/` reflect the earlier implementation and should be regenerated after the refactor is complete.
