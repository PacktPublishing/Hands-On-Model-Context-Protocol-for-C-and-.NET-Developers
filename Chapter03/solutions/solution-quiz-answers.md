# Chapter 3 — Quiz answers

1. **What NuGet package provides the MCP server and client API for ASP.NET Core hosting?**
   `ModelContextProtocol.AspNetCore`. The base protocol and stdio transport are in `ModelContextProtocol`; the ASP.NET Core integration (HTTP transport and `MapMcp`) is in the `.AspNetCore` package.

2. **Why does the stdio server log to stderr instead of stdout?**
   The stdio transport uses stdout as the protocol stream for JSON-RPC messages. Writing log lines to stdout would corrupt the protocol framing and make the server unreadable to any MCP client. Stderr is a separate stream that the host typically discards or captures independently.

3. **What changes between the stdio configuration and the HTTP configuration?**
   Only `Program.cs` changes. `WithStdioServerTransport()` is replaced by `WithHttpTransport()` and `app.MapMcp()` is added. The tool classes, DI registrations, and capability declarations are identical; transport is orthogonal to capability registration.

4. **What does MCP Inspector let you do that a plain HTTP client cannot?**
   Inspector understands the MCP protocol: it performs the initialization handshake, discovers tools and resources via `tools/list` and `resources/list`, and invokes them via `tools/call` with schema-validated arguments. A plain HTTP client would need to construct all JSON-RPC envelopes manually.
