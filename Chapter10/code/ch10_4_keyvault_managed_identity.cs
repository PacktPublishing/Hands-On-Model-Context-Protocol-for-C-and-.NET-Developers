// Chapter 10 — Section 10.3.2
// Azure Key Vault integration using DefaultAzureCredential (Managed Identity in Azure,
// Azure CLI / Visual Studio credentials locally). AddAzureKeyVault maps Key Vault
// secrets to IConfiguration keys using "--" as the hierarchy separator, so a secret
// named "ConnectionStrings--BookingDb" maps to "ConnectionStrings:BookingDb".
// All downstream IConfiguration reads are identical to appsettings.json reads.

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DefaultAzureCredential tries: Managed Identity → Workload Identity →
// Azure CLI → Visual Studio → environment variables, in that order.
// In production with Managed Identity enabled, the first probe succeeds.
var credential = new DefaultAzureCredential();

// Key Vault URI is the only non-secret configuration value.
// All other secrets are resolved through IConfiguration after this call.
var keyVaultUri = new Uri(
    builder.Configuration["KeyVault:Uri"]
    ?? throw new InvalidOperationException("KeyVault:Uri is required."));

builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    credential,
    new AzureKeyVaultConfigurationOptions
    {
        // Poll Key Vault for changes every 30 seconds.
        // Shorter intervals allow faster secret rotation propagation.
        ReloadInterval = TimeSpan.FromSeconds(30),
    });

// All IConfiguration reads below are now resolved from Key Vault.
// Secret names use "--" separators: "ConnectionStrings--BookingDb",
// "AirlineApi--Key", "PaymentGateway--ApiKey".

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration["ConnectionStrings:BookingDb"]));

builder.Services.AddHttpClient<AirlineApiClient>(client =>
{
    var baseUrl = builder.Configuration["AirlineApi:BaseUrl"]!;
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add(
        "X-Api-Key",
        builder.Configuration["AirlineApi:Key"]);
});

builder.Services.AddHttpClient<PaymentGatewayClient>(client =>
    client.DefaultRequestHeaders.Add(
        "Authorization",
        $"Bearer {builder.Configuration["PaymentGateway:ApiKey"]}"));

var app = builder.Build();
app.Run();

// Placeholder types referenced above.
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> o) : DbContext(o);
public sealed class AirlineApiClient(HttpClient http) { }
public sealed class PaymentGatewayClient(HttpClient http) { }
