# Chapter 11: Testing and evaluating MCP agents

## Overview

Code samples for Chapter 11. See the chapter text in
`d:/gitbook/ModelContextProtocol/chapters/Ch11.md` for full explanations.

This chapter follows the same standards used by the rest of the
`HandsOnMCPCSharp` solution:

- Pinned .NET SDK via `global.json` (10.0.100, `rollForward: latestMinor`)
- Shared MSBuild settings via `Directory.Build.props`
- Stable MCP SDK package `ModelContextProtocol` 1.2.0
- `ManagePackageVersionsCentrally=false` so the project is self-contained
- Reference snippet files (`ch11_*.cs`) excluded from compilation

## Running the demos

```bash
cd code
dotnet build
dotnet run
```

`Demos.cs` distils the `ch11_*.cs` snippets into a self-contained
implementation that compiles against only the BCL, and `Program.cs` walks
through each concept end-to-end. The verbatim snippet files stay in `code/`
as reading material next to the book.

## Reuse from earlier chapters

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the
  xUnit v3 test project in `../Chapter06/tests`
- `../Chapter09/code` -- workflow state machine, transition guard,
  workflow budget, sustained-error guard, workflow resumer

When a Chapter 11 snippet references `IFlightSearchService`,
`WorkflowState`, validators, or the sanitisation filter, prefer the
implementations already in Chapters 5, 6, and 9.

## Solutions

Quiz answers are in `solutions/solution-quiz.md`.
