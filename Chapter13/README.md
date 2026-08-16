# Chapter 13: Secure and Govern MCP - AuthN/Z, Secrets, Azure API Management

## Overview

This directory contains the companion code for Chapter 13 of
*Hands-On MCP for C# and .NET Developers*. The book chapter provides the
concepts, walkthroughs, and explanations that accompany these samples.

The samples follow the repository's shared chapter conventions:

- Pinned .NET SDK via `global.json` (10.0.100, `rollForward: latestMinor`)
- Shared MSBuild settings via `Directory.Build.props`
- Stable MCP SDK package `ModelContextProtocol` 1.2.0
- `ManagePackageVersionsCentrally=false` so the project is self-contained
- Reference snippet files (`ch13_*.cs`) excluded from compilation

## Running the demos

```bash
cd code
dotnet build
dotnet run
```

`Program.cs` provides an orientation entry point for the chapter. The verbatim
`ch13_*.cs` snippet files stay in `code/` as reading material next to the book
and are excluded from compilation.

## Code samples

See the `code/` directory. Files are named `ch13_N_description.cs`:

- `ch13_1_entra_jwt_auth_config.cs`
- `ch13_2_capability_authorization.cs`
- `ch13_3_tenant_isolation.cs`
- `ch13_4_keyvault_managed_identity.cs`
- `ch13_5_data_protection_config.cs`
- `ch13_6_secret_rotation_options.cs`
- `ch13_7_correlation_id_middleware.cs`

## Reuse from earlier chapters

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the
  xUnit v3 test project in `../Chapter06/tests`
- `../Chapter09/code` -- workflow state machine, transition guard,
  workflow budget, sustained-error guard, workflow resumer

When a Chapter 13 snippet references shared contracts, middleware, or
validation primitives, prefer reusing implementations from earlier chapters.

## Solutions

Quiz questions are in `solutions/solution-quiz.md`, and answers are in
`solutions/solution-quiz-answers.md`.
