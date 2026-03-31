# Chapter 1 — Quiz answers

1. **Name three recurring pain points in .NET distributed architectures that MCP addresses.**
   Resource distribution bottlenecks (each consumer integrates the same upstream API independently), capability standardization (no shared discovery mechanism across hosts), and client integration friction (bespoke adapters per consumer that break when the upstream changes).

2. **What is a capability in MCP terms?**
   A named, schema-described unit of functionality — a tool, resource, or prompt — that a server declares and any MCP-compliant client or host can discover and invoke without writing custom integration code.

3. **What are the four success criteria introduced in the Travel Booking reference application?**
   Latency (p95 search response under 500 ms), throughput (concurrent bookings handled without degradation), reliability (SLA targets with circuit breaker protection), and cost (LLM token spend within budget for planner-driven flows).

4. **What is the key difference between `ch01_1_without_mcp_integration.cs` and `ch01_2_with_mcp_search_flights.cs`?**
   The pre-MCP version requires every consumer to know the upstream URL convention, auth header, and response shape. The MCP version declares a tool once; the SDK generates the schema, and any compliant host discovers and invokes it without any per-consumer adapter.
