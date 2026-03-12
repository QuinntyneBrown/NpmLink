# NpmLink Implementation Fix Checklist

This document turns the current review findings into an ordered implementation plan. The goal is to move the tool toward a more robust and idiomatic C#/.NET architecture while fixing the highest-risk behavior gaps first.

## Execution Order

1. Fix correctness gaps that can produce false success.
2. Fix resilience gaps around file parsing and process execution.
3. Refactor toward a layered architecture with dependency injection.
4. Harden the test suite so each fix has durable coverage.

## Item 1: Validate `tsconfig` Mapping Values During `verify`

Problem:
`verify` currently checks only whether the `library` and `library/*` keys exist. It does not validate that the values point to the expected library source path.

Implementation:
- Extract the expected relative path calculation into one shared method.
- Validate the exact mapping for `libraryName` against the expected relative path.
- Validate the wildcard mapping for `libraryName/*` against the expected wildcard path.
- Return explicit diagnostics showing expected and actual values.
- Keep path normalization consistent by converting separators to `/` before comparison.

Recommended code shape:
- Add a small result model such as `PathMappingValidationResult`.
- Move the comparison logic into a dedicated validator or `ITsConfigEditor`/`ITsConfigInspector` abstraction.

Completion criteria:
- `verify` fails when the exact mapping points to the wrong location.
- `verify` fails when the wildcard mapping points to the wrong location.
- `verify` succeeds only when both keys exist and both values match the expected paths.

Verification:
- Add a test where the exact mapping exists but points to a different folder.
- Add a test where the wildcard mapping exists but points to a different folder.
- Add a test where both mappings are correct and `verify` returns success.

## Item 2: Make `tsconfig` Parsing and Mutation JSONC-Safe

Problem:
`tsconfig.json` is commonly JSONC. The current code skips comments but does not allow trailing commas. It also logs a warning and still returns success when `link` or `unlink` cannot update the file.

Implementation:
- Centralize all `tsconfig` read/write behavior in one component.
- Parse with `JsonCommentHandling.Skip` and `AllowTrailingCommas = true`.
- Preserve the current formatting policy consistently when writing.
- Treat parse and write failures as operation failures for `link` and `unlink` when a `tsconfig` file is present and expected to be updated.
- Keep the missing-file behavior explicit and documented. If the intended behavior remains "skip when missing", test that behavior directly.

Recommended code shape:
- Introduce an abstraction such as `ITsConfigEditor`.
- Return a typed result instead of writing warnings directly to the console.

Completion criteria:
- `link` does not report success if a present `tsconfig` file cannot be parsed or written.
- `unlink` does not report success if a present `tsconfig` file cannot be parsed or written.
- A `tsconfig` file with comments and trailing commas can be read and updated successfully.

Verification:
- Add a `link` test using JSONC with comments and trailing commas.
- Add an `unlink` test using JSONC with comments and trailing commas.
- Add a test where `tsconfig` contains invalid content and the command returns a non-zero exit code.

## Item 3: Validate `--source` Consistently in `unlink`

Problem:
`LinkAsync` validates the library source directory before invoking npm. `UnlinkAsync` does not, so a bad source path can surface as a process startup failure instead of a controlled validation error.

Implementation:
- Resolve and validate `librarySourcePath` in `UnlinkAsync` before any process call.
- Reuse the same validation rules as `LinkAsync`.
- Return a deterministic validation result and message when the path does not exist.

Recommended code shape:
- Extract shared request validation into a single validator or request object factory.

Completion criteria:
- `unlink` returns a non-zero validation result when the source path does not exist.
- No process is started when source path validation fails.

Verification:
- Add an `UnlinkAsync_MissingLibrarySourcePath_ReturnsOne` test.
- Add an assertion that the fake process runner recorded no invocations.

## Item 4: Replace Shell String Commands with Typed npm Invocation

Problem:
Process execution is currently string-based and shell-based on Windows (`cmd /c npm ...`). That is brittle for quoting, harder to test, and less idiomatic than passing structured arguments.

Implementation:
- Introduce an `INpmClient` abstraction with explicit methods for `LinkGlobalAsync`, `LinkIntoWorkspaceAsync`, `UnlinkFromWorkspaceAsync`, and `UnlinkGlobalAsync`.
- Use `ProcessStartInfo.ArgumentList` instead of building a single argument string.
- Invoke `npm.cmd` on Windows and `npm` on non-Windows systems instead of `cmd /c`.
- Keep process output capture in infrastructure, not in application orchestration code.

Recommended code shape:
- `ProcessRunner` should accept a structured command object such as `ProcessRequest`.
- `NpmClient` should translate application intent into process requests.

Completion criteria:
- No command path relies on `cmd /c`.
- Scoped package names and paths with spaces are passed correctly.
- Application code no longer knows how npm is launched on each platform.

Verification:
- Add tests for scoped package names.
- Add tests for workspace and source paths containing spaces.
- Run the full test suite after replacing the process abstraction.

## Item 5: Introduce a Real Composition Root and Dependency Injection

Problem:
Each command currently constructs concrete services directly. That prevents clean separation between command parsing, application logic, infrastructure, and output concerns.

Implementation:
- Use `Host.CreateApplicationBuilder()` in `Program.cs`.
- Register application services, infrastructure services, and command handlers in DI.
- Resolve handlers from DI rather than constructing `new NpmLinkService(new ProcessRunner())` inside command code.
- Consolidate shared option definitions so command wiring is not duplicated across `link`, `unlink`, and `verify`.

Recommended code shape:
- Keep `Program.cs` as the composition root.
- Move command action logic into handler classes.
- Use records for request models such as `LinkLibraryRequest`, `UnlinkLibraryRequest`, and `VerifyLinkRequest`.

Completion criteria:
- `Program.cs` owns service registration and command wiring.
- Command classes no longer construct infrastructure directly.
- Shared options are defined once and reused.

Verification:
- Build the solution.
- Run all tests.
- Add at least one command-level test or thin integration test that exercises parsing plus handler resolution.

## Item 6: Separate Application Logic from IO and Console Output

Problem:
`NpmLinkService` currently performs orchestration, validation, filesystem access, JSON parsing, process execution, and user-facing console output.

Implementation:
- Split the current service into focused components.
- Keep orchestration in an application handler/service.
- Move filesystem and JSON operations into infrastructure services.
- Replace direct `Console.WriteLine` and `Console.Error.WriteLine` calls with structured result objects that the CLI layer renders.
- Prefer returning a typed `OperationResult` with exit code, status, and messages rather than returning raw `int`.

Recommended code shape:
- `NpmLink.Application`
- `NpmLink.Infrastructure`
- `NpmLink.Cli`

If project splitting feels too large for one pass, start by introducing namespaces and abstractions inside the existing project, then split projects once the boundaries are stable.

Completion criteria:
- Application code can be unit tested without touching the console or filesystem.
- Infrastructure code can be swapped with fakes in tests.
- Command handlers map structured results to process exit codes and user output.

Verification:
- Existing unit tests continue to pass after the refactor.
- New tests exercise application handlers without using real file IO or console output.

## Item 7: Expand Test Coverage Around Failure Modes

Problem:
The current suite is decent for the happy path and some validation cases, but it misses the defects above.

Implementation:
- Add tests for wrong `tsconfig` mapping values.
- Add tests for JSONC comments and trailing commas.
- Add tests for invalid `tsconfig` content.
- Add tests for missing library source path during `unlink`.
- Add tests for paths containing spaces.
- Add tests for any new result types or handlers introduced by the refactor.

Completion criteria:
- Every item in this checklist has at least one test that would fail before the fix and pass after it.
- The suite covers both success and failure outcomes for each command.

Verification:
- Run `dotnet test`.
- Confirm the new tests fail before the corresponding fix and pass after it.

## Item 8: Clean Up Tooling Warnings

Problem:
`dotnet test` currently restores `xunit.runner.visualstudio` `3.0.0` because `2.9.2` is unavailable, which produces a restore warning.

Implementation:
- Align the test package versions to published versions that restore cleanly.
- Recheck whether the runner package is still needed in the current SDK and test workflow.

Completion criteria:
- `dotnet test` runs without the `NU1603` warning.

Verification:
- Run `dotnet restore`.
- Run `dotnet test`.

## Definition of Done

The improvement effort is complete when all of the following are true:

- `verify` validates both presence and correctness of `tsconfig` path mappings.
- `link` and `unlink` fail deterministically when a present `tsconfig` file cannot be updated.
- `unlink` validates the source path before invoking npm.
- npm execution is modeled through a dedicated abstraction with structured arguments.
- DI is used as the composition mechanism for commands and services.
- Application logic is testable without direct console, process, or filesystem dependencies.
- The test suite contains coverage for each defect and each architectural seam introduced.
- `dotnet test` passes cleanly.
