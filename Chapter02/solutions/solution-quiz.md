# Chapter 2 — Quiz answers

1. **What are the three MCP roles and what does each one do?**
   The host initiates sessions, manages the LLM, and decides which servers to connect. The client is a protocol component embedded in the host that opens and maintains connections to servers. The server exposes tools, resources, and prompts that clients discover and invoke.

2. **What transport should you use for a server embedded in the same process as the host?**
   stdio transport. Stdin and stdout are already shared when running in the same process, so no network binding is needed and there is no port to manage.

3. **What is the difference between a tool and a resource in MCP?**
   A tool is a callable action — it accepts parameters, executes logic, and returns a result. A resource is a readable data source identified by a URI; the client fetches it rather than invoking it.

4. **Why does adding a required parameter to an existing tool constitute a breaking change?**
   Existing clients have already been trained or configured to invoke the tool without that parameter. The server will reject their call because the required field is absent, breaking all existing callers without any indication that the tool signature changed.

5. **What pattern keeps an old tool available while a new version is introduced?**
   Register both the current tool (no suffix) and the deprecated version with a `_v1` suffix. The deprecated version maps old parameter names to new ones and delegates to the current implementation. Remove the deprecated version only after confirming no active clients invoke it.
