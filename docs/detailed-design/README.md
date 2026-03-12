# NpmLink - Detailed Design Documentation

## Purpose

This folder describes the target architecture for the next round of NpmLink implementation work. It is intentionally aligned with the repository-level [implementation checklist](../implementation-fix-checklist.md) so an agent can move from design to code systematically.

## Target Architecture

```text
┌────────────────────────────────────────────────────────────┐
│                         NpmLink.Cli                        │
│   System.CommandLine, Generic Host, DI, result rendering  │
└──────────────────────────────┬─────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────┐
│                    NpmLink.Application                     │
│  Request models, validation, link/unlink/verify handlers  │
│  Structured results with diagnostics and exit semantics    │
└──────────────────────────────┬─────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────┐
│                   NpmLink.Infrastructure                   │
│  npm client, process runner, workspace inspection,        │
│  JSONC-safe tsconfig editor, filesystem interaction       │
└────────────────────────────────────────────────────────────┘
```

## Design Principles

- Keep command parsing and console output in the CLI layer only.
- Keep orchestration and policy decisions in the application layer.
- Keep process execution, filesystem access, and `tsconfig` mutation in infrastructure.
- Prefer typed requests and typed results over raw strings and raw `int` return values.
- Fail deterministically when a present `tsconfig` file cannot be parsed or updated.
- Verify actual `tsconfig` mapping values, not just key presence.

## Feature Design Documents

| # | Feature | Document |
|---|---------|----------|
| 1 | [CLI Command Parsing](01-cli-command-parsing.md) | Composition root, shared options, DI-based handlers |
| 2 | [Application Orchestration](02-npm-link-service.md) | Link, unlink, and verify handlers with structured results |
| 3 | [Process Execution](03-process-execution.md) | `INpmClient`, typed process requests, platform handling |
| 4 | [Validation](04-validation.md) | Shared validation rules and diagnostics across commands |
| 5 | [TSConfig Editing](05-tsconfig-update.md) | JSONC-safe read/write and exact mapping verification |

## Implementation Tracking

The design documents describe the end state. The implementation order, completion criteria, and verification steps are tracked in [implementation-fix-checklist.md](../implementation-fix-checklist.md).

## Diagrams

The PlantUML source and rendered PNG files in [diagrams/](diagrams/) represent the earlier implementation baseline. They should be refreshed after the code has been refactored to match the target architecture described in these documents.

### Rendering Diagrams

To re-render the PlantUML diagrams after updates:

```bash
cd docs/detailed-design/diagrams
python render.py
```

Requires the Python `plantuml` package (`pip install plantuml`). Diagrams are rendered via the PlantUML web service.
