using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var appRuntimeEnvironment = builder.Environment.IsProduction()
    ? "Production"
    : builder.Environment.IsStaging()
        ? "Staging"
        : "Development";
var minWebReplicas = builder.Environment.IsProduction() ? 1 : 0;

var azureAdTenantId = builder.AddParameter("azureAdTenantId");
var azureAdClientId = builder.AddParameter("azureAdClientId");
var graphClientCertificatePassword = builder.AddParameter("graphClientCertificatePassword", secret: true);
var graphClientCertificateBase64 = builder.AddParameter("graphClientCertificateBase64", secret: true);

builder.AddAzureContainerAppEnvironment("aca-env")
    .WithDashboard(!builder.Environment.IsProduction());

// Shared local storage emulator for blob and table resources.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

// Blob service resource used by client integration in the web host and
// by the ASP.NET Core Data Protection system for key-ring persistence.
var blobs = storage.AddBlobs("blobs");
var dataProtectionContainer = storage.AddBlobContainer("dataprotection", blobContainerName: "dataprotection");

// Table service used for tenant operational metadata.
var tables = storage.AddTables("tables");

builder.AddProject<Projects.ImportToPlanner_Web>("web")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", appRuntimeEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", appRuntimeEnvironment)
    .WithEnvironment("AzureAd__TenantId", azureAdTenantId)
    .WithEnvironment("AzureAd__ClientId", azureAdClientId)
    .WithEnvironment("AzureAd__ClientCertificates__0__SourceType", "Path")
    .WithEnvironment("AzureAd__ClientCertificates__0__CertificateDiskPath", "/tmp/import-to-planner-graph-client.pfx")
    .WithEnvironment("AzureAd__ClientCertificates__0__CertificatePassword", graphClientCertificatePassword)
    .WithEnvironment("AzureAd__ClientCertificates__0__CertificateBase64", graphClientCertificateBase64)
    .WithExternalHttpEndpoints()
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(dataProtectionContainer)
    .WaitFor(dataProtectionContainer)
    .WithReference(tables)
    .WaitFor(tables)
    .PublishAsAzureContainerApp((_, app) =>
    {
        app.Template.Scale.MinReplicas = minWebReplicas;
        app.Template.Scale.MaxReplicas = 1;
    });

builder.Build().Run();
