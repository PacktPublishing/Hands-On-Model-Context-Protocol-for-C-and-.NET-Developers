# Chapter 15: Observe and Scale MCP - Metrics, Tracing, Costs, and Sharding

## Overview

This directory contains the companion code for Chapter 15 of
*Hands-On MCP for C# and .NET Developers*. The book chapter provides the
concepts, walkthroughs, and explanations that accompany these samples.

The samples follow the repository's shared chapter conventions:

- Pinned .NET SDK via `global.json` (10.0.100, `rollForward: latestMinor`)
- Shared MSBuild settings via `Directory.Build.props`
- Stable MCP SDK package `ModelContextProtocol` 1.2.0
- `ManagePackageVersionsCentrally=false` so the project is self-contained
- Reference snippet files (`ch12_*.cs`) excluded from compilation

## Running the demos

```bash
cd code
dotnet build
dotnet run
```

`Demos.cs` distils the `ch12_*.cs` snippets into a self-contained
implementation that compiles against only the BCL, and `Program.cs` walks
through each concept end-to-end. The verbatim snippet files stay in `code/`
as reading material next to the book.

The runnable console host exercises:

- `TokenUsageTracker` (`ch12_6`) -- prompt/completion/cached token counters
- `BudgetCapEnforcer` (`ch12_7`) -- per-workflow and per-tenant token caps
- `LlmResponseKeyBuilder` (`ch12_8`) -- normalised prompt hashing + scope TTLs
- `ConsistentHashRouter` (`ch12_9`) -- SHA-256 hash-ring routing with vnodes
- `LoadShedder` (`ch12_10`) -- priority-based load shedding

## Code samples

See the `code/` directory. Files are named `ch12_N_description.cs`.
Each file maps to a section of the chapter and is intended to be read
alongside the book, then copied into a dedicated project (with the
packages called out by the chapter) for hands-on experimentation:

- `ch12_1_mcp_metrics_instruments.cs` -- metrics instruments
- `ch12_2_structured_logging_correlation.cs` -- structured logging + correlation
- `ch12_3_otel_exporter_config.cs` -- OpenTelemetry exporter configuration
- `ch12_4_distributed_tracing_activity.cs` -- distributed tracing with Activity
- `ch12_5_auto_instrumentation_middleware.cs` -- auto-instrumentation middleware
- `ch12_6_token_usage_tracker.cs` -- token usage tracker
- `ch12_7_budget_cap_enforcer.cs` -- budget cap enforcer
- `ch12_8_llm_response_cache.cs` -- LLM response cache
- `ch12_9_consistent_hash_router.cs` -- consistent hash router
- `ch12_10_load_shedding_middleware.cs` -- load-shedding middleware
- `ch12_11_mcp_gateway.cs` -- MCP gateway middleware
- `ch12_12_aspire_app_host.cs` -- .NET Aspire app host
- `ch12_13_safe_remediation.cs` -- safe auto-remediation framework

## Reuse from earlier chapters

The full runnable MCP infrastructure already lives in earlier chapters:

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the
  xUnit v3 test project in `../Chapter06/tests`

When a Chapter 15 snippet references a service such as
`IFlightSearchService`, `IIdempotencyStore`, validators, or the
sanitisation filter, prefer the implementations already in Chapters 5 and 6
rather than re-introducing local copies.
