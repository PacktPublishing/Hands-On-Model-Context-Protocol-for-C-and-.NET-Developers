# Chapter 2 — Quiz questions

1. A host maintains three concurrent MCP server connections. How many client instances are active, and what does each one own?

2. A client sends an `initialize` request with `"protocolVersion": "2025-03-26"`. The server responds with `"protocolVersion": "2024-11-05"`. What should the client do next, and why?

3. A `tools/call` response carries `isError: true` in the `result` field. A colleague triggers a transport-level retry. Explain what is wrong with this approach.

4. You need to add a `maxStops` filter to an existing `SearchFlights` tool. Classify this change as breaking or non-breaking, and describe what you must do to keep existing clients working.

5. A `resources/read` response for URI `itinerary://booking/B-042` takes 12 seconds on average. A colleague suggests remodeling it as a `tools/call` to support progress notifications. Evaluate this suggestion against the tool and resource selection rules.
