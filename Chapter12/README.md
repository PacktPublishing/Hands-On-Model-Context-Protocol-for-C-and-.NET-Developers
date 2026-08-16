# Chapter 12: Design UX with Blazor and .NET MAUI: Background Work, and Offline

## Overview

This directory contains the companion code for Chapter 12 of
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

`Demos.cs` and `Program.cs` provide runnable orientation examples, while all
`ch12_*.cs` files remain in `code/` as verbatim chapter listings.

## Code samples

See the `code/` directory. Files are named `ch12_N_description.cs`:

- `ch12_1_host_configuration.cs`
- `ch12_1_mcp_metrics_instruments.cs`
- `ch12_2_streaming_search_component.cs`
- `ch12_2_structured_logging_correlation.cs`
- `ch12_3_cancellation_component.cs`
- `ch12_3_otel_exporter_config.cs`
- `ch12_4_distributed_tracing_activity.cs`
- `ch12_4_mcp_error_handler.cs`
- `ch12_5_auto_instrumentation_middleware.cs`
- `ch12_5_mcp_job_queue.cs`
- `ch12_6_job_processor_service.cs`
- `ch12_6_token_usage_tracker.cs`
- `ch12_7_budget_cap_enforcer.cs`
- `ch12_7_job_status_display.cs`
- `ch12_8_llm_response_cache.cs`
- `ch12_8_retry_processor.cs`
- `ch12_9_caching_mcp_client.cs`
- `ch12_9_consistent_hash_router.cs`
- `ch12_10_connectivity_service.cs`
- `ch12_10_load_shedding_middleware.cs`
- `ch12_11_mcp_gateway.cs`
- `ch12_11_offline_retry_queue.cs`
- `ch12_12_aspire_app_host.cs`
- `ch12_12_ux_metrics.cs`
- `ch12_13_safe_remediation.cs`
- `ch12_13_traced_streaming_component.cs`
- `ch12_14_native_approval_provider.cs`

## Reuse from earlier chapters

The full runnable MCP infrastructure already lives in earlier chapters:

- `../Chapter05/code` -- runnable ASP.NET Core MCP server (Flight tools,
  `Shared.cs`, `HealthChecks.cs`)
- `../Chapter06/code` -- validation and error sanitisation
  (`DomainModels.cs`, `Validators.cs`, `Sanitisation.cs`) plus the
  xUnit v3 test project in `../Chapter06/tests`

When a Chapter 12 snippet references a service such as
`IFlightSearchService`, `IIdempotencyStore`, validators, or the
sanitisation filter, prefer the implementations already in Chapters 5 and 6
rather than re-introducing local copies.

## Solutions

Quiz questions are in `solutions/solution-quiz.md`, and answers are in
`solutions/solution-quiz-answers.md`.
