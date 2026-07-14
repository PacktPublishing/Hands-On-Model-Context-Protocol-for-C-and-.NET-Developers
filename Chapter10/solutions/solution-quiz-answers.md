# Chapter 10 — Quiz answers

1. The most likely causes are missing or incorrect tool scope authorization, or tenant/account claims that do not map to the expected capability. Diagnose by checking the token's scopes and the server's scope mapping first, then verify whether the `process_payment` handler requires an additional claim, policy, or tenant filter that the token lacks.

2. The root cause is that authentication/session state is not being shared across replicas, so a request after login can land on a different instance that does not recognize the session. The fix is to externalize the session or token state, or otherwise use stateless auth such as validated bearer tokens and shared token protection across instances.

3. The tool is denied by default because it is not present in `ToolScopeMap`, so the server treats it as unauthorized even though the token is valid. That is correct because unknown capabilities must be blocked until an explicit scope mapping is added.

4. If the new credential is not verified before revoking the old one, the system can lock itself out during cutover and cause authentication failures for all callers. Detect this by testing the new credential against the live dependency before rotation, and recover by re-enabling the old credential or restoring the previous secret immediately if the new one fails.

5. Three hypotheses are an authorization policy regression for the Payments server, a bad or expired token issuer/audience configuration, or a backend dependency failure causing requests to be rejected. First check server logs and APIM policy traces for denied scopes, then inspect token validation results and downstream dependency health.

6. Update `TenantContext` so it can resolve the tenant from `tenant_id` as an alternate claim source instead of only `tid`. Add a test that issues a token with `tenant_id`, performs cross-tenant requests, and verifies that EF Core global query filters still prevent data from one tenant being visible to another.
