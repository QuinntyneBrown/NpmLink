# NpmLink - Detailed Design Documentation

## Purpose

This folder describes the architecture of the NpmLink CLI tool. Each document covers one area of the design and reflects the current implementation.

## Architecture

The project is a single .NET project (`NpmLink.Cli`) organized into logical layers by namespace and folder:

```text
┌────────────────────────────────────────────────────────────┐
│                      Program.cs                            │
│   Host.CreateApplicationBuilder, System.CommandLine,       │
│   DI registration, result rendering                        │
└──────────────────────────────┬─────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────┐
│                      Commands/                             │
│   LinkCommand, UnlinkCommand, VerifyCommand,               │
│   CommandOptions, CommandResultRenderer                     │
└──────────────────────────────┬─────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────┐
│                      Services/                             │
│   NpmLinkService (orchestration, validation)               │
│   NpmClient (typed npm invocation)                         │
│   TsConfigEditor (JSONC-safe tsconfig editing)             │
│   OperationResult (structured result model)                │
└────────────────────────────────────────────────────────────┘
```

## Design Principles

- Command parsing and console output live in `Commands/` and `Program.cs` only.
- `NpmLinkService` orchestrates workflows and validates inputs but does not write to the console or access the filesystem directly for tsconfig operations.
- `NpmClient` owns all npm process execution with structured arguments.
- `TsConfigEditor` owns all tsconfig.json file IO and JSONC parsing.
- All service methods return `OperationResult` (exit code + messages) rather than raw `int`.
- `tsconfig` parse/write failures on present files cause the operation to fail.
- `verify` validates actual tsconfig mapping values, not just key presence.

## Feature Design Documents

| # | Feature | Document |
|---|---------|----------|
| 1 | [CLI Command Parsing](01-cli-command-parsing.md) | Composition root, shared options, DI-based commands |
| 2 | [Application Orchestration](02-npm-link-service.md) | Link, unlink, and verify workflows with structured results |
| 3 | [Process Execution](03-process-execution.md) | `INpmClient`, typed process arguments, platform handling |
| 4 | [Validation](04-validation.md) | Input validation rules across commands |
| 5 | [TSConfig Editing](05-tsconfig-update.md) | JSONC-safe read/write and value verification |

## Diagrams

The PlantUML source and rendered PNG files in [diagrams/](diagrams/) represent an earlier implementation baseline. They should be refreshed to match the current architecture.

### Rendering Diagrams

```bash
cd docs/detailed-design/diagrams
python render.py
```

Requires the Python `plantuml` package (`pip install plantuml`).
