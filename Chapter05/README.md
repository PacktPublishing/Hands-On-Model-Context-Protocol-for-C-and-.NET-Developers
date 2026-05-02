# Chapter 5: Building the Travel Booking Server in ASP.NET Core

This chapter builds out the FlightsServer in ASP.NET Core: minimal hosting, dependency injection, typed configuration, health checks, attribute-based tool registration, streaming, cancellation, idempotency, error handling, OpenTelemetry tracing, and Polly v8 resilience pipelines. The seventeen reference snippets in `code/` reproduce the exact code listings from the chapter text.

Topics covered:

- ASP.NET Core minimal hosting with `AddMcpServer().WithHttpTransport().WithTools<T>()`
- Environment-conditional DI registration for the idempotency store (in-memory vs Redis)
- Typed options binding for airline and resilience configuration
- Liveness and readiness health checks for the airline partner API
- Attribute-based tool registration with `[McpServerToolType]` and `[McpServerTool]`
- Domain records with `[Description]` and data-annotation constraints
- Capability versioning by adding `_v1` suffixes to deprecated methods
- Streaming results with `IAsyncEnumerable<T>` and `[EnumeratorCancellation]`
- Propagating `CancellationToken` through every downstream async call
- Idempotency guards keyed on a caller-supplied UUID
- Throwing `McpException` and `McpProtocolException` with valid `McpErrorCode` values
- OpenTelemetry tracing, metrics, and logging with the OTLP exporter
- Named Polly v8 resilience pipelines for search and booking
- xUnit test verifying a circuit breaker opens after threshold failures

---

## Folder structure

```
Chapter05/
├── code/
│   ├── Chapter05.csproj                        # ASP.NET Core MCP server project
│   ├── Program.cs                              # WebApplication host: DI, health checks, MapMcp
│   ├── Shared.cs                               # Domain records, service interfaces, mock implementations
│   ├── FlightTools.cs                          # [McpServerToolType] — search, stream, book, cancel
│   ├── HealthChecks.cs                         # AirlineApiHealthCheck + CapabilityValidationService
│   ├── ch05_1_program_minimal_setup.cs         # Section 5.1.1 — minimal Program.cs (reference)
│   ├── ch05_2_di_env_idempotency_store.cs      # Section 5.1.2 — environment-conditional DI (reference)
│   ├── ch05_3_configuration_options_binding.cs # Section 5.1.3 — typed options binding (reference)
│   ├── ch05_4_airline_api_health_check.cs      # Section 5.1.4 — readiness probe (reference)
│   ├── ch05_5_flight_tools_registration.cs     # Section 5.2.1 — FlightTools registration (reference)
│   ├── ch05_6_flight_models.cs                 # Section 5.2.2 — domain records (reference)
│   ├── ch05_7_passenger_input_validation.cs    # Section 5.2.2 — data-annotation constraints (reference)
│   ├── ch05_8_capability_validation_service.cs # Section 5.2.3 — startup validation (reference)
│   ├── ch05_9_flight_tools_versioned.cs        # Section 5.2.4 — capability versioning (reference)
│   ├── ch05_10_search_flights_streaming.cs     # Section 5.3.1 — IAsyncEnumerable streaming (reference)
│   ├── ch05_11_book_flight_cancellation.cs     # Section 5.3.2 — CancellationToken propagation (reference)
│   ├── ch05_12_book_flight_idempotency.cs      # Section 5.3.3 — idempotency guard (reference)
│   ├── ch05_13_mcp_tool_base_errors.cs         # Section 5.3.4 — McpException / McpProtocolException (reference)
│   ├── ch05_14_opentelemetry_setup.cs          # Section 5.3.5 — OpenTelemetry pipeline (reference)
│   ├── ch05_15_search_flights_tracing.cs       # Section 5.3.5 — explicit ActivitySource span (reference)
│   ├── ch05_16_polly_resilience_pipelines.cs   # Section 5.4.1 — named Polly pipelines (reference)
│   └── ch05_17_circuit_breaker_test.cs         # Section 5.4.5 — circuit breaker xUnit test (reference)
├── .githooks/
│   └── pre-commit                              # Schema compatibility gate
├── .vscode/
│   ├── extensions.json                         # Recommended extensions
│   ├── launch.json                             # Run Chapter 05 project
│   └── tasks.json                              # Build task
├── Directory.Build.props                       # Solution-wide MSBuild settings
├── global.json                                 # SDK version pin — 9.0.100
└── solutions/
    └── solution-quiz.md
```

`Program.cs`, `Shared.cs`, `FlightTools.cs`, and `HealthChecks.cs` together form the runnable server: a working consolidation of the patterns shown across the seventeen `ch05_*.cs` reference snippets.

The seventeen `ch05_*.cs` files are the **verbatim chapter listings** — they reproduce each section's code as it appears in the book. They are excluded from compilation in `Chapter05.csproj` because they contain partial classes, top-level builder fragments, and overlapping record definitions that are not intended to compile together as a single project.

---

## Prerequisites

- .NET SDK 9.0.100 or later (`dotnet --version`)

---

## Building and running

```bash
cd Chapter05/code
dotnet build
dotnet run
```

Once running you should see:

```
✓ Chapter 5 Travel Booking server ready
  MCP endpoint:  http://localhost:5002/mcp
  Liveness:      http://localhost:5002/health/live
  Readiness:     http://localhost:5002/health/ready
```

### Smoke testing the endpoints

```bash
# Liveness / readiness probes
curl http://localhost:5002/health/live
curl http://localhost:5002/health/ready

# MCP initialize
curl -X POST http://localhost:5002/mcp \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"smoke","version":"1.0"}}}'

# List tools — search_flights, search_flights_streaming, book_flight, cancel_flight
curl -X POST http://localhost:5002/mcp \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Call search_flights
curl -X POST http://localhost:5002/mcp \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"search_flights","arguments":{"origin":"LHR","destination":"JFK","departureDate":"2026-06-15","passengerCount":2}}}'
```

### Connecting MCP Inspector

```bash
npx @modelcontextprotocol/inspector
```

Point Inspector at `http://localhost:5002/mcp`. The Tools panel will list all four registered tools with their JSON Schemas, including the `StringLength` and `Range` constraints declared on `PassengerInput` and `FlightSearchRequest`.

---

## Reference snippets

### ch05_1_program_minimal_setup.cs — Minimal `Program.cs`

`AddMcpServer().WithHttpTransport().WithTools<FlightTools>()` plus an OpenTelemetry pipeline, a typed `HttpClient` for the airline partner API, and `MapMcp()` to expose the MCP endpoint. Health-check endpoints are bound to `/health/live` and `/health/ready`.

### ch05_2_di_env_idempotency_store.cs — Environment-conditional DI

`builder.Environment.IsDevelopment()` selects `InMemoryIdempotencyStore`; all other environments select `RedisIdempotencyStore`. Domain services are registered scoped so each HTTP request gets its own instance. `CapabilityValidationService` runs as a hosted service and validates tool attributes at startup.

### ch05_3_configuration_options_binding.cs — Typed options

`AirlineOptions` and `ResilienceOptions` are bound from `appsettings.json` and `Resilience` sections. Secrets stay out of source control via `dotnet user-secrets` in development and environment variables or Key Vault in deployed environments.

### ch05_4_airline_api_health_check.cs — Readiness probe

`AirlineApiHealthCheck` implements `IHealthCheck` and is registered with the `ready` tag so it only participates in `/health/ready`. Returns `Healthy`, `Degraded`, or `Unhealthy` based on the response status.

### ch05_5_flight_tools_registration.cs — Attribute-based tool registration

`FlightTools` is decorated with `[McpServerToolType]`. Each handler method carries `[McpServerTool]` and `[Description]`; `[Description]` on parameters flows into the JSON Schema exposed to LLM clients.

### ch05_6_flight_models.cs — Domain records

`PassengerInput`, `FlightOption`, `FlightSearchResult`, `BookingConfirmation`, `CancellationResult`, and `Money` are positional records — immutable by default with structural equality. `[property: Description]` flows into the schema.

### ch05_7_passenger_input_validation.cs — Data-annotation constraints

`[StringLength]` and `[Range]` tighten the JSON Schema generated by the SDK, reducing invalid inputs before they reach the handler. `FlightSearchRequest` shows the same pattern at the request boundary.

### ch05_8_capability_validation_service.cs — Startup validation

A hosted service that walks every `[McpServerToolType]` class via reflection and throws `InvalidOperationException` if any `[McpServerTool]` method is missing a non-empty `[Description]`. Misconfiguration is caught before any client connects.

### ch05_9_flight_tools_versioned.cs — Capability versioning

The current `SearchFlights` adds a `cabinClass` filter; `SearchFlightsV1` carries a deprecation notice and delegates to the current method. The `_v1` suffix and the deprecation message in the description signal the migration path to clients.

### ch05_10_search_flights_streaming.cs — Streaming with `IAsyncEnumerable`

`SearchFlightsStreaming` returns `IAsyncEnumerable<FlightOption>`. The SDK serialises each yielded item as a streamed tool response chunk. `[EnumeratorCancellation]` propagates client disconnection through `await foreach`.

### ch05_11_book_flight_cancellation.cs — CancellationToken propagation

`BookFlight` propagates `cancellationToken` through every downstream async call. Passing `CancellationToken.None` on any call breaks the cancellation chain and allows downstream work to continue after the client disconnects.

### ch05_12_book_flight_idempotency.cs — Idempotency guard

`BookFlightIdempotent` checks `IIdempotencyStore` at the top of the handler. A cache hit returns the original `BookingConfirmation` without contacting the airline; results are stored for 24 hours.

### ch05_13_mcp_tool_base_errors.cs — `McpException` and `McpProtocolException`

Business rule violations throw `McpException`. Protocol-level failures throw `McpProtocolException` with a valid `McpErrorCode` (`InvalidParams`, `ResourceNotFound`, etc.). Invalid codes such as `RequestTimeout` or `Conflict` are documented as not-to-use.

### ch05_14_opentelemetry_setup.cs — OpenTelemetry pipeline

`AddSource("*")` and `AddMeter("*")` capture spans and metrics from any `ActivitySource`/`Meter`, including the SDK's own. `UseOtlpExporter()` ships traces, metrics, and logs to the configured OTLP endpoint.

### ch05_15_search_flights_tracing.cs — Explicit ActivitySource span

`SearchFlights` opens a custom span via a static `ActivitySource` and tags it with travel-domain attributes (`flight.origin`, `flight.destination`, `flight.passenger_count`, `flight.options_returned`). The span appears as a child of the incoming HTTP request span in Jaeger or Tempo.

### ch05_16_polly_resilience_pipelines.cs — Named Polly v8 pipelines

Two named pipelines: `flights-search` (concurrency 20, 2 retries, 50% breaker threshold) and `flights-booking` (concurrency 10, 1 retry, 30% breaker threshold). Inject via `ResiliencePipelineProvider<string>` in tool handlers.

### ch05_17_circuit_breaker_test.cs — Circuit breaker xUnit test

Drives a fake airline client into failure mode until the breaker opens, then asserts the next call throws `BrokenCircuitException` without reaching the client. Verifies that the breaker short-circuits requests under sustained downstream failure.

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

## Solutions

Quiz answers are in `solutions/solution-quiz.md`.

---

## Further reading

- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP getting started: https://modelcontextprotocol.io/docs/getting-started/intro
