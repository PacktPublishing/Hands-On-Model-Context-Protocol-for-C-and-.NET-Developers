# Chapter 2 Contract Tests

## Overview

The contract tests (`ch02_1_search_flights_contract_tests.cs` and `ch02_6_schema_compatibility_tests.cs`) are integration tests that verify MCP tool contracts and schema compatibility.

## Why They're Disabled

These tests are currently saved as `.example` files because they require:

1. **External Server**: A running MCP server (e.g., FlightsServer from Chapter 3)
2. **xunit Framework**: Test framework dependencies
3. **McpClient Access**: May require internal API access

## Running the Tests

### Option 1: Standalone Test Project

Create a separate test project:

```powershell
# From the Chapter02/code directory
cd ..
mkdir tests
cd tests

# Create test project
dotnet new xunit -n Chapter02.Tests -f net9.0

# Add references
dotnet add reference ..\..\..\..\src\ModelContextProtocol\ModelContextProtocol.csproj
dotnet add reference ..\..\..\..\src\ModelContextProtocol.Core\ModelContextProtocol.Core.csproj

# Copy test files
Copy-Item ..\code\ch02_1_search_flights_contract_tests.cs.example SearchFlightsContractTests.cs
Copy-Item ..\code\ch02_6_schema_compatibility_tests.cs.example SchemaCompatibilityTests.cs
```

### Option 2: Manual Verification

Instead of automated tests, manually verify using the demo program:

```powershell
# Terminal 1: Start a server (Chapter 3 or any MCP server)
cd ..\Chapter03\code
dotnet run

# Terminal 2: Run client demonstrations
cd ..\..\Chapter02\code
dotnet run
```

### Option 3: Convert to Demo Code

The tests can be converted to non-xunit demo code. See below for example.

## Test Requirements

### Prerequisites

- **Server Running**: FlightsServer or any MCP server with SearchFlights tool
- **.NET SDK 10.0.201**: Installed and configured
- **xunit 3.x**: For running as actual tests

### Test Scenarios Covered

#### ch02_1_search_flights_contract_tests.cs
- ✓ SearchFlights tool is registered
- ✓ Schema includes required fields (origin, destination, date)
- ✓ Tool returns results for valid input
- ✓ Schema has property descriptions

#### ch02_6_schema_compatibility_tests.cs
- ✓ Schema remains backward compatible
- ✓ No breaking changes in tool signatures
- ✓ Snapshot-based regression testing

## Converting to Runnable Demo

See `ContractTestsDemo.cs` for a non-xunit version that demonstrates the same concepts without requiring a test framework.

## Related Files

- **Chapter 3**: FlightsServer implementation (required for integration tests)
- **Program.cs**: Working demonstrations of tools without requiring external server
- **MockServices.cs**: In-memory implementations for standalone demos

## Further Reading

- [xunit Documentation](https://xunit.net/)
- [MCP Contract Testing Best Practices](https://modelcontextprotocol.io/docs/testing)
- [Integration Testing in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/)
