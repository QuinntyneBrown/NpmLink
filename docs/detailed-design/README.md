# NpmLink - Detailed Design Documentation

## Overview

NpmLink is a .NET 9 CLI tool that automates the process of linking a local npm library into an Angular workspace for local development and debugging. It eliminates manual steps by orchestrating three operations: registering the library globally via `npm link`, linking it into the workspace via `npm link <library>`, and updating `tsconfig.json` path mappings for TypeScript resolution.

## Architecture

```
┌─────────────────────────────────────────────────┐
│                   Program.cs                     │
│              (System.CommandLine)                │
│         Parses CLI args, creates services        │
└─────────────────┬───────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────┐
│              NpmLinkService                      │
│           (INpmLinkService)                      │
│                                                  │
│  ┌──────────┐  ┌──────────┐  ┌───────────────┐  │
│  │Validation│→ │npm link  │→ │tsconfig update│  │
│  │  (4 checks) │(2 steps) │  │  (path maps)  │  │
│  └──────────┘  └──────────┘  └───────────────┘  │
└─────────────────┬───────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────┐
│             ProcessRunner                        │
│           (IProcessRunner)                       │
│     Executes npm commands cross-platform         │
└─────────────────────────────────────────────────┘
```

## Feature Design Documents

| # | Feature | Document |
|---|---------|----------|
| 1 | [CLI Command Parsing](01-cli-command-parsing.md) | Entry point, argument parsing with System.CommandLine |
| 2 | [Npm Link Service](02-npm-link-service.md) | Core orchestration of the linking workflow |
| 3 | [Process Execution](03-process-execution.md) | External process management and cross-platform support |
| 4 | [Validation](04-validation.md) | Input validation and fail-fast checks |
| 5 | [TSConfig Path Update](05-tsconfig-update.md) | TypeScript configuration path mapping updates |

## Diagrams

All PlantUML source files and rendered PNGs are in the [diagrams/](diagrams/) directory.

| Diagram | Class | Sequence |
|---------|-------|----------|
| CLI Command Parsing | [class](diagrams/cli-class.png) | [sequence](diagrams/cli-sequence.png) |
| Npm Link Service | [class](diagrams/npm-link-service-class.png) | [sequence](diagrams/npm-link-service-sequence.png) |
| Process Execution | [class](diagrams/process-runner-class.png) | [sequence](diagrams/process-runner-sequence.png) |
| Validation | [class](diagrams/validation-class.png) | [sequence](diagrams/validation-sequence.png) |
| TSConfig Update | [class](diagrams/tsconfig-class.png) | [sequence](diagrams/tsconfig-sequence.png) |

### Rendering Diagrams

To re-render the PlantUML diagrams after changes:

```bash
cd docs/detailed-design/diagrams
python render.py
```

Requires the Python `plantuml` package (`pip install plantuml`). Diagrams are rendered via the PlantUML web service.

## Key Design Patterns

- **Dependency Injection** - `IProcessRunner` abstraction enables testing without real process execution.
- **Fail-Fast Validation** - All inputs validated before any side effects occur.
- **Strategy Pattern** - Cross-platform command handling (Windows `cmd /c` vs Unix direct).
- **Graceful Degradation** - tsconfig update failure is non-fatal.
- **Exit Code Propagation** - Process exit codes bubble up from npm through the service to the CLI.
