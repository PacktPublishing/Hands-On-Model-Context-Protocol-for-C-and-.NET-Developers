// Chapter 12 — Section 12.5.1
// .NET Aspire app host for the Travel Booking distributed system.
// DistributedApplication.CreateBuilder is the entry point for the AppHost project.
// AddProject<T> registers each service; WithReference injects connection strings
// and service discovery endpoints so services find each other by name, not URL.
// AddRedis and AddPostgres start managed containers locally; azd maps them to
// Azure Cache for Redis and Azure Database for PostgreSQL in production.

var builder = DistributedApplication.CreateBuilder(args);

// Shared infrastructure: Redis for session/state cache, PostgreSQL for bookings.
var redis    = builder.AddRedis("cache");
var postgres = builder.AddPostgres("pgserver").AddDatabase("bookingdb");

// MCP servers — each gets its own Container App in Azure via azd.
var flightsMcp = builder.AddProject<Projects.FlightsMcpServer>("flights-mcp")
                        .WithReference(postgres)
                        .WithReference(redis);

var hotelsMcp  = builder.AddProject<Projects.HotelsMcpServer>("hotels-mcp")
                        .WithReference(postgres);

var paymentsMcp = builder.AddProject<Projects.PaymentsMcpServer>("payments-mcp")
                         .WithReference(postgres);

// Orchestrator discovers MCP servers by name through Aspire service discovery.
// No hardcoded URLs — Aspire injects the endpoint as an environment variable.
var orchestrator = builder.AddProject<Projects.BookingOrchestrator>("orchestrator")
                          .WithReference(flightsMcp)
                          .WithReference(hotelsMcp)
                          .WithReference(paymentsMcp)
                          .WithReference(redis);

// Blazor UI is the only externally reachable endpoint.
builder.AddProject<Projects.BlazorFrontEnd>("web-frontend")
       .WithReference(orchestrator)
       .WithExternalHttpEndpoints();

builder.Build().Run();

// Deployment commands (from the repository root):
//   dotnet workload install aspire
//   azd auth login
//   azd provision   # Creates Azure resource group, Container Apps environment, Redis, PostgreSQL
//   azd deploy      # Builds images, pushes to Container Registry, updates Container Apps
