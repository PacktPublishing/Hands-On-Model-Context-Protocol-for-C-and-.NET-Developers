# Chapter 9: Building agentic workflows with MCP and .NET

## Overview

Code samples for Chapter 9. See the chapter text in
`d:/gitbook/ModelContextProtocol/chapters/Ch09.md` for full explanations.

This chapter follows the same standards used by the rest of the
`HandsOnMCPCSharp` solution:

- Pinned .NET SDK via `global.json` (9.0.100, `rollForward: latestMinor`)
- Shared MSBuild settings via `Directory.Build.props`
- Stable MCP SDK package `ModelContextProtocol` 1.2.0
- `ManagePackageVersionsCentrally=false` so the project is self-contained
- Reference snippet files (`ch09_*.cs`) excluded from compilation

## Running the demos

```bash
cd code
dotnet build
dotnet run
```

`Demos.cs` distils the twelve `ch09_*.cs` snippets (workflow state hierarchy,
transition guard, workflow budget, sustained-error guard, in-memory workflow
state store, and resumer) into a self-contained implementation that compiles
against only the BCL. `Program.cs` walks a `TravelPlan` from `IdleState`
through `ConfirmedState`, then demonstrates:

1. TransitionGuard rejecting a tool that is not valid in the current state.
2. WorkflowBudget stopping a runaway loop.
3. SustainedErrorGuard triggering an emergency stop after N consecutive
   failures.
4. WorkflowResumer choosing the correct recovery action for
   `AwaitingApprovalState`, `FailedState`, and `ConfirmedState` workflows.

## Code samples

The `code/` directory contains twelve `ch09_N_description.cs` files. Each maps
to a section of the chapter and is intended to be read alongside the book,
then copied into a dedicated project (with the packages called out by the
chapter -- `Microsoft.Extensions.Caching.Distributed`,
`Microsoft.Extensions.Logging`, and an MCP client) for hands-on experimentation.

## Reuse from earlier chapters

The runnable MCP infrastructure lives in earlier chapters:

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the xUnit v3
  test project in `../Chapter06/tests`

When a Chapter 9 snippet references `IFlightSearchService`, `IIdempotencyStore`,
validators, or the sanitisation filter, prefer the implementations already in
Chapters 5 and 6 rather than re-introducing local copies.

## Solutions

Quiz answers are in `solutions/solution-quiz.md`.
