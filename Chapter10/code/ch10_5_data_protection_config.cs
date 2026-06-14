// Chapter 10 — Section 10.3.3
// ASP.NET Core Data Protection configured for multi-instance deployments.
// PersistKeysToAzureBlobStorage shares the key ring across all replicas.
// ProtectKeysWithAzureKeyVault encrypts the key ring at rest so that
// blob storage access alone is insufficient to decrypt protected data.
// SetApplicationName scopes the key ring: only this application can use it.

using Azure.AspNetCore.DataProtection.Blobs;
using Azure.AspNetCore.DataProtection.Keys;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var credential = new DefaultAzureCredential();

// BlobUri points to a specific blob within the storage account.
// Example: https://storageaccount.blob.core.windows.net/dataprotection/keys.xml
var blobUri = new Uri(
    builder.Configuration["DataProtection:BlobUri"]
    ?? throw new InvalidOperationException("DataProtection:BlobUri is required."));

// KeyVaultKeyUri points to a specific Key Vault key (not a secret).
// Example: https://keyvault.vault.azure.net/keys/dataprotection/version
var keyVaultKeyUri = new Uri(
    builder.Configuration["DataProtection:KeyVaultKeyUri"]
    ?? throw new InvalidOperationException("DataProtection:KeyVaultKeyUri is required."));

builder.Services
    .AddDataProtection()
    // All replicas read and write the same key ring XML blob.
    .PersistKeysToAzureBlobStorage(blobUri, credential)
    // The key ring blob is encrypted with a Key Vault key — blob access alone
    // is insufficient to read or tamper with the ring.
    .ProtectKeysWithAzureKeyVault(keyVaultKeyUri, credential)
    // Scope the key ring to this application. A different app sharing the same
    // storage account cannot read or use these keys.
    .SetApplicationName("TravelBooking.MCP")
    // Rotate keys every 90 days; keep old keys for 14 days after rotation
    // so in-flight tokens encrypted with the old key remain valid.
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Verify multi-instance interoperability: protect data on instance A,
// then create a second IDataProtector and confirm it can unprotect the same payload.
// This test should run as part of the deployment validation pipeline.

var app = builder.Build();
app.Run();
