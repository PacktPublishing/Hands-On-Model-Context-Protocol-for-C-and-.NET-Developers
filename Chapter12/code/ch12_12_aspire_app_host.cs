// Chapter 15 — Section 15.6.1
// .NET Aspire 9.3 app host for the Travel Booking distributed system.
// AddProject<T> registers each service; WithReference injects connection strings
// and service discovery endpoints so services find each other by name, not URL.
// aspire publish generates Bicep and deploys to Azure Container Apps.

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("pgserver").AddDatabase("traveldb");
var redis    = builder.AddRedis("cache");

// MCP servers — each becomes a Container App via aspire publish.
var flightsMcp = builder.AddProject<Projects.FlightsMcpServer>("flights-mcp")
    .WithReference(postgres);

var hotelsMcp = builder.AddProject<Projects.HotelsMcpServer>("hotels-mcp")
    .WithReference(redis);

var paymentsMcp = builder.AddProject<Projects.PaymentsMcpServer>("payments-mcp")
    .WithReference(postgres);

// Gateway mediates all MCP traffic (see ch15_11_mcp_gateway.cs).
var gateway = builder.AddProject<Projects.McpGateway>("mcp-gateway")
    .WithReference(flightsMcp)
    .WithReference(hotelsMcp)
    .WithReference(paymentsMcp);

// Orchestrator talks to the gateway, not directly to MCP servers.
var orchestrator = builder.AddProject<Projects.TravelOrchestrator>("orchestrator")
    .WithReference(gateway)
    .WithReference(redis);

builder.AddProject<Projects.TravelFrontend>("web")
    .WithReference(orchestrator)
    .WithExternalHttpEndpoints();

builder.Build().Run();

// Deployment (from the repository root):
//   dotnet workload install aspire
//   aspire publish --publisher azure --subscription <your-subscription-id>
