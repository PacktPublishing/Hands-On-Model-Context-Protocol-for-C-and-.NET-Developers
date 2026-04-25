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
│   ├── Program.cs            # HTTP transport entry point (the runnable server)
│   ├── FlightTools.cs        # SearchFlightsTool implementation
│   ├── Shared.cs             # Domain models and mock flight search service
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
├── Properties/
│   └── launchSettings.json   # http profile on port 5001 (section 3.4.2)
├── Directory.Build.props     # Solution-wide MSBuild settings (section 3.5.3)
├── global.json               # SDK version pin — 9.0.100 (section 3.5.3)
└── solutions/
    └── solution-quiz.md
```

The `code/` files numbered `ch03_3` through `ch03_5` are reference snippets excluded from compilation. They illustrate incremental steps described in the chapter text. `Program.cs`, `FlightTools.cs`, and `Shared.cs` are the complete runnable server.

---

## Prerequisites

- .NET SDK 9.0.100 or later (`dotnet --version`)
- Node.js 18 or later (`node --version`) — required for `npx @modelcontextprotocol/inspector`
- Docker Desktop — required for the Docker Compose stack

---

## Running the single FlightsServer

The `code/` project implements an HTTP transport server on port 5001.

```bash
cd Chapter03/code
dotnet run
```

The server is ready when the console shows `Application started`. Verify with:

```bash
curl -i http://localhost:5001/mcp
```

A `400 Bad Request` response confirms the server is listening. A plain `GET` is not a valid JSON-RPC message; the error is expected.

### Connecting the MCP Inspector

```bash
npx @modelcontextprotocol/inspector
```

In the Inspector UI, select **Streamable HTTP**, set the URL to `http://localhost:5001/mcp`, and click **Connect**. The **Tools** tab lists `search_flights` and the **Resources** tab lists the `flight://status/{flightId}` template.

### stdio transport (section 3.2)

The stdio variant is in `code/ch03_1_flights_server_stdio.cs`. To run it directly, replace the contents of `Program.cs` with that file's `Program.cs` entry point, or follow the scaffold steps in section 3.2 of the book to create a separate console project.

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
