<h1 align="center">Hands-On MCP for C# and .NET Developers, First Edition</h1>
<p align="center">
  This is the companion code repository for
  <strong>Hands-On MCP for C# and .NET Developers</strong>, published by Packt.
</p>

<h2 align="center">Build distributed, AI-ready apps with the Model Context Protocol</h2>
<p align="center">Deepak Kamboj</p>

---

## About the book

*Hands-On MCP for C# and .NET Developers* teaches you to design, build, validate, and deploy Model Context Protocol servers and clients using the official C# SDK and .NET 9. Through a single Travel Booking reference application — covering flights, hotels, payments, and itineraries — you progress from first-principles MCP concepts through production ASP.NET Core servers, LLM-orchestrated clients, Azure deployment, and end-to-end observability.

Every chapter builds on the previous one. By the end you will have a complete, running system and patterns you can apply to your own domains immediately.

---

## Key learnings

- Design MCP capability contracts and distribute tools across multi-server topologies
- Build production-ready MCP servers in ASP.NET Core with streaming, idempotency, and resilience patterns
- Validate, profile, and harden MCP servers using Inspector scenarios and fault injection
- Build .NET MCP clients with and without LLM orchestration
- Secure MCP endpoints with authentication, authorization, and Azure API Management
- Deploy MCP workloads to Azure Functions and Container Apps with CI/CD
- Observe and scale MCP systems end to end with distributed tracing and .NET Aspire

---

## Chapters

| # | Title | Pages |
|---|-------|-------|
| 1 | Adopt MCP on .NET: Problems, Patterns, and Payoff | 15–20 |
| 2 | MCP Fundamentals: Protocol, Roles, and Capabilities | 15–20 |
| 3 | Setup Your .NET MCP Workspace: SDKs, Inspector, and Dev Environment | 20–25 |
| 4 | Designing Capabilities and Resource Distribution | 20–25 |
| 5 | Building the Travel Booking Server in ASP.NET Core | 20–25 |
| 6 | Validate, Profile, and Harden Your MCP Server | 20–25 |
| 7 | MCP Clients without LLMs: Streams, State, and Resilience | 20–25 |
| 8 | LLM-Integrated Clients: Orchestration, Tool Use, and Safety | 20–25 |
| 9 | UX Integration: Blazor and .NET MAUI, Background Work, and Offline | 20–25 |
| 10 | Security and Governance: AuthZ, Secrets, Azure API Management | 15–20 |
| 11 | Cloud Deployment: Azure Functions and Container Apps with CI/CD | 15–20 |
| 12 | Observability and Scale: Metrics, Tracing, Costs, and Sharding | 15–20 |

---

## Requirements

- .NET 9 SDK — https://dotnet.microsoft.com/download
- Visual Studio 2022 / VS Code / Rider
- Docker Desktop (Chapters 3, 11)
- An MCP-compatible host such as Claude Desktop or VS Code Copilot (Chapter 3 onward)
- Azure subscription (Chapters 10, 11, 12)

All code samples target **.NET 9** and use the **official `ModelContextProtocol` NuGet package**.

---

## Repository structure

```
HandsOnMCPCSharp/
├── README.md                  ← this file
├── Chapter01/
│   ├── README.md              ← chapter overview and running instructions
│   ├── code/                  ← runnable .NET projects
│   └── solutions/             ← quiz answers
├── Chapter02/ … Chapter12/    ← same structure
└── TravelBooking.sln          ← solution file linking all chapter projects
```

Each `code/` folder contains a self-contained .NET project with its own `.csproj` and `Program.cs`.

---

## Getting started

```bash
git clone https://github.com/PacktPublishing/Hands-On-MCP-CSharp.git
cd Hands-On-MCP-CSharp
dotnet restore TravelBooking.sln
```

To run a specific chapter project:

```bash
cd Chapter01/code
dotnet run
```

---

## About the author

**Deepak Kamboj** is a Senior Software Engineer at Microsoft and a software architect with 24 years of experience, including eight years in architectural roles. He specializes in .NET, distributed systems, AI strategy, and agent-based workflows. At Microsoft he leads AI agent orchestration and test automation infrastructure initiatives. He contributes to open-source .NET projects and speaks on cloud-native architecture and AI tooling at industry events and the University of Washington.
