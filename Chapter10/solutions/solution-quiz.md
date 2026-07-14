# Chapter 10 — Quiz questions

1. A new MCP client authenticates successfully with a valid JWT token but receives a 403 Forbidden response when invoking `process_payment`. Describe the two most likely causes and the diagnostic steps you would take for each.

2. The Travel Booking system is deployed to two Container App replicas behind a load balancer. Users report that after logging in they are sometimes immediately logged out on the next request. Identify the root cause and explain the fix.

3. You add a new tool called `cancel_flight` to the Flights MCP server. The tool does not appear in `ToolScopeMap`. Describe what happens when a client with a valid token attempts to invoke it and explain why this behavior is the correct security default.

4. Your team rotates the Airline API key using the two-phase sequence but skips step 4 (verifying the new credential is active before revoking the old). Describe the failure mode and how you would detect and recover from it.

5. An APIM traffic dashboard shows that the 4xx error rate for the Payments MCP server has risen from 2% to 35% over the past hour while the Flights and Hotels servers remain normal. List three distinct hypotheses for the cause and the first diagnostic action for each.

6. You need to add a tenant whose JWT tokens carry a `tenant_id` custom claim rather than the standard Entra ID `tid` claim. Describe the changes required to `TenantContext` and the test you would add to confirm that cross-tenant isolation still holds after the change.
