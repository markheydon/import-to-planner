using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var appRuntimeEnvironment = builder.Environment.IsProduction()
    ? "Production"
    : builder.Environment.IsStaging()
        ? "Staging"
        : "Development";
var minWebReplicas = builder.Environment.IsProduction() ? 1 : 0;

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
