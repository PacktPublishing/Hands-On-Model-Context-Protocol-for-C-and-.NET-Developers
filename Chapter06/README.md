# Chapter 6: Validate, Profile, and Harden Your MCP Server

This chapter is the testing-and-hardening companion to Chapter 5: how you validate that the FlightsServer keeps its contract, how you measure its hot path, how you inject faults to verify resilience policies actually fire, and how you sanitise errors so internal detail never leaks to the LLM host.

Topics covered:

- Integration testing tools end-to-end with `McpClient` over stdio and HTTP transports
- Contract testing against committed JSON schema snapshots so breaking removals fail CI
- Micro-benchmarks with BenchmarkDotNet (`[MemoryDiagnoser]`, `[Benchmark(Baseline = true)]`)
- Fault injection via a `DelegatingHandler` to inject delays and HTTP 503 errors
- Chaos tests that verify Polly timeout, retry, circuit breaker, and partial-failure isolation
- FluentValidation rules that enforce business invariants beyond JSON Schema
- `McpErrorSanitisationFilter` — pass-through for `McpException`, safe message + correlation ID otherwise

---

## Folder structure

```
Chapter06/
├── code/
│   ├── Chapter06.csproj                          # Class-of-snippets project; reference snippets excluded
│   ├── Program.cs                                # Entry point (orientation message)
│   ├── DomainModels.cs                           # Tool input/output records, services, FlightNotAvailableException
│   ├── Validators.cs                             # Section 6.4.2 — runnable FluentValidation validators
│   ├── Sanitisation.cs                           # Section 6.4.4 — runnable McpErrorSanitisationFilter
│   ├── ch06_1_search_flights_integration_tests.cs  # Section 6.1.3 — stdio integration tests (reference)
│   ├── ch06_2_contract_test_snapshot.cs            # Section 6.1.4 — snapshot contract tests (reference)
│   ├── ch06_3_search_flights_benchmarks.cs         # Section 6.2.2 — BenchmarkDotNet micro-benchmarks (reference)
│   ├── ch06_4_fault_injection_handler.cs           # Section 6.3.2 — DelegatingHandler chaos hook (reference)
│   ├── ch06_5_chaos_tests.cs                       # Section 6.3.3 — Polly chaos tests (reference)
│   ├── ch06_6_search_flights_validator.cs          # Section 6.4.2 — FluentValidation validators (reference)
│   └── ch06_7_error_sanitisation_filter.cs         # Section 6.4.4 — error sanitisation (reference)
├── tests/
│   ├── Chapter06.Tests.csproj                    # Compilable xUnit v3 test project (45 unit + 3 manual)
│   ├── ValidationTests.cs                        # Tests for SearchFlights/BookFlight/CancelFlight validators
│   ├── ErrorSanitisationTests.cs                 # Tests for McpErrorSanitisationFilter & SanitisedToolInvoker
│   └── FlightsServerIntegrationTests.cs          # [Trait("Execution","Manual")] — spawns Chapter 5 over HTTP
├── .githooks/
│   └── pre-commit                                # Schema compatibility gate
├── .vscode/
│   ├── extensions.json                           # Recommended extensions
│   ├── launch.json                               # Run unit tests (skip Manual)
│   └── tasks.json                                # Build + test tasks
├── Directory.Build.props                         # Solution-wide MSBuild settings
├── global.json                                   # SDK version pin — 9.0.100
└── solutions/
    └── solution-quiz.md
```

The seven `ch06_*.cs` files are **verbatim chapter listings** — they reproduce each section's code as it appears in the book. They are excluded from compilation in `Chapter06.csproj` because they target separate projects (test, benchmarks, chaos packages) with overlapping symbol definitions.

`Validators.cs` and `Sanitisation.cs` in `code/` are the runnable consolidation of sections 6.4.2 and 6.4.4. The xUnit test project in `tests/` exercises them directly, plus the integration tests in `FlightsServerIntegrationTests.cs` exercise the live Chapter 5 FlightsServer over HTTP.

---

## Prerequisites

- .NET SDK 9.0.100 or later (`dotnet --version`)
- For the Manual integration tests only: the Chapter 5 project must be built
  (`dotnet build ../../Chapter05/code/Chapter05.csproj`)

---

## Building

```bash
cd Chapter06
dotnet build code/Chapter06.csproj
dotnet build tests/Chapter06.Tests.csproj
```

The build produces zero warnings under `TreatWarningsAsErrors` in Release.

---

## Running the tests

### Default unit-test run (no external dependencies — fast, deterministic)

```bash
cd tests
dotnet test --filter "Execution!=Manual"
```

Expected output:

```
Passed!  - Failed: 0, Passed: 45, Skipped: 0, Total: 45
```

### Including the Manual integration tests (spawns Chapter 5)

The integration tests reuse a Chapter 5 server already listening on `http://localhost:5002`, or spawn one via `dotnet run --no-build --project ../../Chapter05/code/Chapter05.csproj` and tear it down after the test class completes.

```bash
# Build Chapter 5 once
dotnet build ../../Chapter05/code/Chapter05.csproj

# Run only the Manual tests
dotnet test --filter "Execution=Manual"

# Or run everything (45 unit + 3 manual = 48 total)
dotnet test
```

Expected output:

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

If the Chapter 5 project is missing or fails to start, the Manual tests skip cleanly with an actionable message rather than failing the run.

---

## Reference snippets

### ch06_1_search_flights_integration_tests.cs — Stdio integration tests (Section 6.1.3)

Uses `McpClient.CreateAsync` with `StdioClientTransport` to launch the FlightsServer as a subprocess. Each `[Fact]` exercises the full JSON-RPC serialisation, schema validation, and handler dispatch path. Key invariant: `result.IsError` is `bool?` — `null` means success, `true` means tool-level error; never assert `False`. The runnable HTTP-based version of these tests lives in `tests/FlightsServerIntegrationTests.cs`.

### ch06_2_contract_test_snapshot.cs — Snapshot contract tests (Section 6.1.4)

Compares the live tool descriptor returned by `ListToolsAsync` against a committed JSON snapshot. The snapshot's `required` set must remain a subset of the live `required` set — additive changes pass, removals fail. `SnapshotGenerator.GenerateAsync` is a one-shot helper to create the initial snapshot files.

### ch06_3_search_flights_benchmarks.cs — BenchmarkDotNet micro-benchmarks (Section 6.2.2)

`[MemoryDiagnoser]` adds Gen0/Gen1/Gen2 and allocated columns. `[Benchmark(Baseline = true)]` marks the reference point so other benchmarks report a Ratio column relative to it. `CachedFlightSearchService` decorator illustrates the allocation reduction visible in the benchmark output. Run with `dotnet run -c Release` from a dedicated benchmarks project.

### ch06_4_fault_injection_handler.cs — Chaos hook (Section 6.3.2)

`FaultInjectionHandler` is a `DelegatingHandler` that injects configurable delays and HTTP 503 errors before forwarding. Registered exclusively in `Staging` via the `AddFaultInjection` extension to prevent accidental production registration. The `cancellationToken` parameter on `Task.Delay` matters — it lets a Polly timeout cancel an in-progress delay rather than wait it out.

### ch06_5_chaos_tests.cs — Polly chaos tests (Sections 6.3.3 / 6.3.4)

xUnit tests that mutate `FaultOptions` to activate timeout, retry, circuit-breaker, and partial-failure-isolation experiments, then assert on the observable side effects (error response, elapsed time inside the deadline, healthy sibling tool, fast-fail under 1 s after circuit opens).

### ch06_6_search_flights_validator.cs — FluentValidation validators (Section 6.4.2)

`SearchFlightsValidator`, `BookFlightValidator`, `CancelFlightValidator` enforce business rules JSON Schema cannot express (IATA format, future-date requirement, UUID v4 idempotency keys, `B-YYYYMMDD-NNN` booking references). The runnable copies live in `code/Validators.cs`; tests in `tests/ValidationTests.cs`.

### ch06_7_error_sanitisation_filter.cs — Error sanitisation (Section 6.4.4)

`McpErrorSanitisationFilter` decides which client-facing message to produce for each escaping exception type. `McpException` messages are authored for client consumption and pass through verbatim. Domain exceptions get a safe generic message + correlation ID. All other exceptions hit the default branch — `Exception.Message` is never returned to the client. The runnable copy lives in `code/Sanitisation.cs`; tests in `tests/ErrorSanitisationTests.cs`.

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

- `Ctrl+Shift+B` — runs the build task (`dotnet build code/Chapter06.csproj`).
- The default test task runs `dotnet test tests/Chapter06.Tests.csproj --filter "Execution!=Manual"`.
- The launch configuration "Run Chapter 06 unit tests (skip Manual)" runs the same filter under the debugger.

---

## Solutions

Quiz answers are in `solutions/solution-quiz.md`.

---

## Further reading

- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- xUnit v3 docs: https://xunit.net/docs/getting-started/v3/cmdline
- FluentValidation docs: https://docs.fluentvalidation.net/
- BenchmarkDotNet docs: https://benchmarkdotnet.org/
