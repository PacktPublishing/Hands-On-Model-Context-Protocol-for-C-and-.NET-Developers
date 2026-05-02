// Chapter 6 — Validate, Profile, and Harden Your MCP Server.
//
// This chapter is testing-focused. The runnable artefacts are:
//
//   1. The supporting types in this folder (DomainModels.cs, Validators.cs,
//      Sanitisation.cs) — a working consolidation of the patterns shown across
//      the seven ch06_*.cs reference snippets.
//
//   2. The xUnit v3 test project in ../tests/ — exercises the validators and the
//      error-sanitisation filter end-to-end.
//
// The seven ch06_*.cs files are the verbatim chapter listings (integration
// tests, contract tests, benchmarks, fault injection, chaos tests, validators,
// error sanitisation). They are excluded from compilation in Chapter06.csproj.
// See README.md for a per-section description.
//
// Run the unit tests:
//   dotnet test ../tests/Chapter06.Tests.csproj --filter "Execution!=Manual"

Console.WriteLine("Chapter 6 — Validate, Profile, and Harden Your MCP Server");
Console.WriteLine("Reference snippets are in the code/ folder.");
Console.WriteLine("Run the test suite with: dotnet test ../tests/Chapter06.Tests.csproj");
