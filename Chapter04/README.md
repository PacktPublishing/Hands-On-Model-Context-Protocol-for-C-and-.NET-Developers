# Chapter 4: Designing capabilities and resource distribution

This chapter establishes the architectural blueprint for the Travel Booking domain. You derive a twelve-capability inventory from business events using event storming, validate the four-server topology using cohesion and independent deployability tests, and attach SLA metadata to capability descriptors backed by xUnit acceptance tests. No new server is built here; the blueprint produced in this chapter is the fixed starting point that Chapter 5 implements.

Topics covered:

- Deriving a typed capability inventory from business events using event storming
- Applying cohesion, coupling, and independent deployability tests to validate server boundaries
- Selecting distribution patterns: fan-out, resource subscriptions, boundary-local caching, the outbox pattern
- Embedding p50, p95, and p99 latency targets in capability descriptors
- Enforcing SLA targets with xUnit acceptance tests that fail CI on regression

---

## Folder structure

```
Chapter04/
├── code/
│   ├── Chapter04.csproj       # Standalone project — reference snippets excluded from compilation
│   ├── Program.cs             # Entry point (prints orientation message only)
│   ├── Shared.cs              # Domain models: Flight, Hotel, Payment, Itinerary aggregates
│   ├── ch04_1_fan_out_parallel_invocation.cs      # Section 4.3.2 — Task.WhenAll fan-out (reference)
│   ├── ch04_2_flight_tools_sla_metadata.cs        # Section 4.4.3 — SLA metadata in descriptors (reference)
│   └── ch04_3_flight_search_sla_acceptance_test.cs # Section 4.4.4 — p95 latency test (reference)
├── tests/
│   ├── Chapter04.Tests.csproj # Compilable xUnit v3 test project
│   └── FlightSearchSlaTests.cs # p95 latency acceptance test (runnable)
├── .githooks/
│   └── pre-commit             # Schema compatibility gate
├── .vscode/
│   ├── extensions.json        # Recommended extensions
│   ├── launch.json            # Run SLA acceptance tests
│   └── tasks.json             # Build task
├── Directory.Build.props      # Solution-wide MSBuild settings
├── global.json                # SDK version pin — 9.0.100
└── solutions/
    └── solution-quiz.md
```

The `ch04_1` through `ch04_3` files in `code/` are reference snippets excluded from compilation — they show the patterns as they appear in the chapter text. The `tests/` folder contains the compilable, runnable version of the acceptance test.

---

## Prerequisites

- .NET SDK 9.0.100 or later (`dotnet --version`)
- FlightsServer from Chapter 5 must be built before running the SLA acceptance test.

---

## Building the reference project

```bash
cd Chapter04/code
dotnet build
```

The build verifies that `Shared.cs` compiles cleanly. The three `ch04_*` reference snippet files are excluded and do not affect the build.

---

## Running the SLA acceptance test

The `tests/` project contains a compilable xUnit v3 test that invokes `SearchFlightsTool` 1000 times and asserts the p95 latency is below the configured target. It requires FlightsServer to be available via stdio transport.

1. Build FlightsServer from Chapter 5:

```bash
dotnet build ../../Chapter05/src/FlightsServer
```

2. Run the acceptance test:

```bash
dotnet test Chapter04/tests/Chapter04.Tests.csproj
```

Run this test in a dedicated CI stage separate from unit tests. It starts a real server process and measures latency under realistic conditions; sharing the stage with unit tests produces noisy measurements and slows every build unnecessarily.

---

## Reference snippets

### ch04_1_fan_out_parallel_invocation.cs — Fan-out with Task.WhenAll

Demonstrates dispatching `SearchFlightsTool` and `SearchHotelsTool` concurrently so that combined search latency equals the maximum of the two durations rather than their sum. The key pattern is calling `.AsTask()` on each `CallToolAsync` return value before passing both tasks to `Task.WhenAll`.

This snippet requires an `IMcpClient` instance for each server. The full implementation with client setup is part of the Chapter 7 host application.

### ch04_2_flight_tools_sla_metadata.cs — SLA metadata in descriptors

Shows the `FlightTools` class with all three flight capabilities (`SearchFlightsTool`, `BookFlightTool`, `CancelFlightTool`) annotated with p95 latency, availability, and retry guidance in their `Description` fields. An LLM agent reads this metadata during capability discovery and can apply the retry and idempotency instructions without additional configuration.

The method bodies throw `NotImplementedException`; the full implementation is in Chapter 5.

### ch04_3_flight_search_sla_acceptance_test.cs — p95 latency acceptance test

An xUnit test that invokes `SearchFlightsTool` 1000 times, records each latency observation, sorts the distribution, and asserts the p95 value is below the configured target. If the assertion fails, the CI build fails with a message showing actual p95, p50, and p99 values.

The `CreateFlightsMcpClientAsync` helper connects via `StdioClientTransport` to a FlightsServer process. Run this test in a dedicated CI stage separate from unit tests, pointing at a fully built FlightsServer binary.

---

## One-time setup: pre-commit hook

```bash
git config core.hooksPath .githooks
```

On Linux and macOS also make the script executable:

```bash
chmod +x .githooks/pre-commit
```

---

## VS Code setup

Press `Ctrl+Shift+B` to run the build task. The workspace recommends the C# Dev Kit extension for full IntelliSense on the reference snippet files even though they are excluded from compilation.

---

## Further reading

- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP getting started: https://modelcontextprotocol.io/docs/getting-started/intro
