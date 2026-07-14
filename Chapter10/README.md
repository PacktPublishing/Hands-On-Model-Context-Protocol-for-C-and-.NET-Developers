# Chapter 10: Multi-agent coordination with MCP

## Overview

Code samples for Chapter 10.
See the chapter text in
`d:/gitbook/ModelContextProtocol/chapters/Ch10.md` for full explanations.

This chapter follows the same standards used by the rest of the
`HandsOnMCPCSharp` solution:

- Pinned .NET SDK via `global.json` (9.0.100, `rollForward: latestMinor`)
- Shared MSBuild settings via `Directory.Build.props`
- Stable MCP SDK package `ModelContextProtocol` 1.2.0
- `ManagePackageVersionsCentrally=false` so the project is self-contained
- Reference snippet files (`ch10_*.cs`) excluded from compilation

## Running the orientation entry point

```bash
cd code
dotnet build
dotnet run
```

The console host prints the chapter title and a pointer to the reference
snippets. The numbered `ch10_*.cs` files are the verbatim chapter
listings and are not built; they exist as reading material.

## Code samples

See the `code/` directory. Files are named `ch10_N_description.cs`.
Each file maps to a section of the chapter and is intended to be read
alongside the book, then copied into a dedicated project (with the
packages called out by the chapter) for hands-on experimentation.

## Reuse from earlier chapters

The full runnable MCP infrastructure already lives in earlier chapters:

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the
  xUnit v3 test project in `../Chapter06/tests`
- `../Chapter09/code` -- workflow state machine, transition guard,
  workflow budget, sustained-error guard, workflow resumer

When a Chapter 10 snippet references a service such as
`IFlightSearchService`, `IIdempotencyStore`, `WorkflowState`, validators,
or the sanitisation filter, prefer the implementations already in
Chapters 5, 6, and 9 rather than re-introducing local copies.

## Solutions

Quiz answers are in `solutions/solution-quiz-answers.md`.
