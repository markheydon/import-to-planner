var builder = DistributedApplication.CreateBuilder(args);

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
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(dataProtectionContainer)
    .WaitFor(dataProtectionContainer)
    .WithReference(tables)
    .WaitFor(tables);

builder.Build().Run();
