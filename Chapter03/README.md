# Chapter 3: Tooling and setup

This chapter builds a working MCP server from scratch using the .NET C# SDK, connects to it with the MCP Inspector, and scales it into the full four-server Travel Booking topology using Docker Compose. By the end you will have a running local stack you can use as the foundation for every subsequent chapter.

Topics covered:

- Installing the SDK and scaffolding a server project
- Registering tools and resources with the `[McpServerToolType]` and `[McpServerTool]` attributes
- Connecting the MCP Inspector to a stdio server and to an HTTP server
- Reading raw protocol messages to understand the JSON-RPC handshake
- Configuring VS Code tasks, launch profiles, and Visual Studio 2026 launchSettings
- Enforcing schema compatibility with a pre-commit Git hook
- Scaffolding the full Travel Booking solution (four servers, contracts library, test project)
- Running the complete stack with Docker Compose

---

## Folder structure

```
Chapter03/
├── assets/
│   ├── compose.yaml          # Docker Compose for all four Travel Booking servers
│   └── Dockerfile            # Multi-stage Dockerfile for FlightsServer (template for others)
├── code/
│   ├── Chapter03.csproj      # Standalone project for running the chapter examples
│   ├── Program.cs            # HTTP + stdio entry point with --mode and --example switches
│   ├── FlightTools.cs        # SearchFlightsTool implementation (transport-agnostic)
│   ├── Shared.cs             # Domain models and mock flight search service
│   ├── Properties/
│   │   └── launchSettings.json   # Visual Studio launch profiles (4 modes)
│   ├── ch03_1_flights_server_stdio.cs    # Section 3.2 — stdio transport server
│   ├── ch03_2_flights_server_http.cs     # Section 3.5.4 — HTTP transport server
│   ├── ch03_3_minimal_server_program.cs  # Section 3.2.1 — minimal Program.cs walkthrough
│   ├── ch03_4_flight_tools_first_tool.cs # Section 3.2.3 — first tool registration
│   └── ch03_5_flight_resources_first_resource.cs  # Section 3.2.4 — first resource
├── .githooks/
│   └── pre-commit            # Schema compatibility gate (section 3.4.4)
├── .vscode/
│   ├── extensions.json       # Recommended extensions (section 3.4.5)
│   ├── launch.json           # FlightsServer debugger config (section 3.4.1)
│   └── tasks.json            # Build task (section 3.4.1)
├── Directory.Build.props     # Solution-wide MSBuild settings (section 3.5.3)
├── global.json               # SDK pin — minimum 9.0.100, rollForward latestMajor
└── solutions/
    └── solution-quiz.md
```

The `code/` files numbered `ch03_3` through `ch03_5` are reference snippets excluded from compilation. They illustrate incremental steps described in the chapter text. `Program.cs`, `FlightTools.cs`, and `Shared.cs` are the complete runnable server.

---

## Prerequisites

The hardware and general software prerequisites from Chapters 1 and 2 apply here. This chapter upgrades the target runtime to .NET 9 and adds the following tools and packages:

- **.NET 9 SDK or later** — this chapter targets `net9.0` (verify with `dotnet --version`)
- **Docker Desktop** with Compose support — for the multi-server stack later in this chapter
- **Node.js 18 or later** — required to run the MCP Inspector via `npx`
- **`ModelContextProtocol.AspNetCore`** — for `app.MapMcp()` and HTTP transport
- **`Microsoft.Extensions.Hosting`** — for generic host lifetime management on console-hosted servers

> **Note:** If you have both .NET 9 and .NET 10 installed, the build automatically uses the latest version (per `global.json` `rollForward: latestMajor`). The project still produces `net9.0` output.

### Verify your environment

```powershell
dotnet --version          # 9.0.x or 10.0.x
node --version            # v18+
docker --version          # Docker engine
docker compose version    # Compose v2+
```

---

## Running the FlightsServer (4 modes)

The `code/` project supports **four launch modes** through command-line arguments. All four are pre-configured as Visual Studio launch profiles in `code/Properties/launchSettings.json`.

| Profile | Command | Purpose |
|---------|---------|---------|
| **Stdio Mode** _(default)_ | `dotnet run` | Stdio transport for in-process MCP Inspector |
| **HTTP Mode** | `dotnet run -- --mode http` | HTTP transport on port 5001 |
| **Example: Stdio** | `dotnet run -- --example stdio` | Run the section 3.2 stdio sample |
| **Example: HTTP** | `dotnet run -- --example http` | Run the section 3.5.4 HTTP sample |

### From Visual Studio

1. Set `Chapter03.csproj` as Startup Project
2. Pick a profile from the launch dropdown (next to ▶)
3. Press **F5** to start with debugger, or **Ctrl+F5** to start without

### From the command line

```powershell
cd HandsOnMCPCSharp\Chapter03\code
dotnet run -- --mode http
```

The server is ready when you see:

```
✓ HTTP server ready!
  Endpoint: http://localhost:5001/mcp
  CORS: Enabled (any origin)
  Press Ctrl+C to stop
```

Verify the server is listening:

```powershell
curl -i http://localhost:5001/mcp
```

A `400 Bad Request` response confirms it's listening — a plain `GET` isn't a valid JSON-RPC message; the error is expected.

---

## Connecting the MCP Inspector

In a **second terminal**, launch the Inspector:

```powershell
npx @modelcontextprotocol/inspector
```

Open the printed URL (typically `http://localhost:6274`) and configure:

| Field | Value |
|-------|-------|
| **Transport Type** | `Streamable HTTP` |
| **URL** | `http://localhost:5001/mcp` |
| **Connection Type** | `Direct` |

Click **Connect**. The **Tools** tab lists `search_flights` with its full JSON Schema (origin, destination, date — all required strings).

> **Tip:** Want to test stdio instead? Use `npx @modelcontextprotocol/inspector dotnet run --project HandsOnMCPCSharp/Chapter03/code` to launch the server as a subprocess over stdio.

---

## ⚠️ Important: HTTP transport requirements for MCP Inspector

The MCP Inspector is a **browser-based web app**. To connect to a local HTTP MCP server, the server must be configured to accept browser-originated requests **and** operate in stateless mode. The reference `Program.cs` in this chapter already includes both fixes — they are described here so you understand why.

### 1. Bind to all interfaces on a fixed port

ASP.NET Core's default development URL (`localhost:5000`) collides with other tools and isn't predictable across environments. Force port `5001` and bind to all interfaces so the Inspector (running on a different port) can reach it:

```csharp
builder.WebHost.UseUrls("http://0.0.0.0:5001");
```

### 2. Enable CORS so the browser can call the server

Browsers block cross-origin requests by default. The Inspector at `localhost:6274` is a different origin from the server at `localhost:5001`, so the browser silently rejects every request unless the server returns the right CORS headers. Without this, the Inspector reports a vague `Connection Error` and you'll see no requests reach the server.

```csharp
// Register CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Apply CORS — must be BEFORE MapMcp
var app = builder.Build();
app.UseCors();
app.MapMcp("/mcp");
```

> **Production note:** `AllowAnyOrigin` is fine for local development with the Inspector. In production, replace it with `WithOrigins("https://your-known-client.com")` to restrict access.

### 3. Enable stateless mode for the HTTP transport

The MCP Inspector's Streamable HTTP client does **not** track session IDs across requests. The default Streamable HTTP transport in the SDK requires session continuity, so without this option you get:

```
Bad Request: A new session can only be created by an initialize request.
Include a valid Mcp-Session-Id header for non-initialize requests, or
enable stateless mode by setting HttpServerTransportOptions.Stateless = true
```

Enable stateless mode when registering the transport:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<FlightTools>();
```

Stateless mode treats every request independently — no session affinity, no `Mcp-Session-Id` header required. This is the right mode for browser-based clients and stateless tool calls. Use the default session-based mode only when you have streaming or stateful subscriptions.

### Putting it together

The complete `RunHttpServerAsync` method in `Program.cs` combines all three:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5001");

builder.Services.AddSingleton<IFlightSearchService, MockFlightSearchService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<FlightTools>();

var app = builder.Build();
app.UseCors();
app.MapMcp("/mcp");
await app.RunAsync();
```

### stdio transport (section 3.2)

The stdio variant is in `code/ch03_1_flights_server_stdio.cs` and runs via `dotnet run -- --example stdio`. Stdio doesn't need CORS or stateless mode because there's no browser and no HTTP — communication is direct over the process's standard streams.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Inspector shows `Connection Error` | Server not running, wrong port, or CORS missing | Confirm `✓ HTTP server ready!` in console; verify `app.UseCors()` is called before `MapMcp` |
| `Bad Request: A new session can only be created by an initialize request` | Session-based transport with stateless client | Add `options => options.Stateless = true` to `WithHttpTransport()` |
| Server logs `Now listening on: http://localhost:5000` | `UseUrls()` not called or env var override | Add `builder.WebHost.UseUrls("http://0.0.0.0:5001")` |
| Build error: `MSB3027 ... file is locked by: TravelBooking.Chapter03` | Previous server instance still running | `Stop-Process -Name TravelBooking.Chapter03 -Force`, then rebuild |
| Build error: `Could not resolve SDK 'Microsoft.NET.Sdk.Web'` | `MSBuildSDKsPath` env var pointing to wrong SDK | Restart Visual Studio, or `$env:MSBuildSDKsPath = 'C:\Program Files\dotnet\sdk\10.0.203\Sdks'` |

---

## Running the full Travel Booking stack

The `assets/` folder contains the Docker Compose file and a reference Dockerfile for the chapter's four-server topology. Before running the stack, scaffold the full solution as described in section 3.5.1 of the book, then copy `assets/compose.yaml` to the solution root and replicate `assets/Dockerfile` for each server project.

Start the stack from the solution root:

```bash
docker compose up --build
```

Stop and remove containers:

```bash
docker compose down
```

The four servers listen on ports 5001 (Flights), 5002 (Hotels), 5003 (Payments), and 5004 (Itinerary). Connect the Inspector to any of them using the HTTP URL pattern above.

---

## One-time setup: pre-commit hook

The `.githooks/pre-commit` script runs schema compatibility tests before every commit. Register it once per clone:

```bash
git config core.hooksPath .githooks
```

On Linux and macOS also make the script executable:

```bash
chmod +x .githooks/pre-commit
```

---

## VS Code setup

Open the `Chapter03` folder in VS Code. The workspace detects `.vscode/extensions.json` and prompts you to install the recommended extensions (C# Dev Kit, Docker, REST Client).

Press `Ctrl+Shift+B` to run the build task defined in `.vscode/tasks.json`. Press `F5` to launch FlightsServer under the debugger using the configuration in `.vscode/launch.json`.

---

## Further reading

- MCP Inspector: https://github.com/modelcontextprotocol/inspector
- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP getting started: https://modelcontextprotocol.io/docs/getting-started/intro
