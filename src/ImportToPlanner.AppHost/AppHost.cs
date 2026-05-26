var builder = DistributedApplication.CreateBuilder(args);

// Shared local storage emulator for blob and table resources.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

// Dedicated container used by ASP.NET Core Data Protection key-ring persistence.
var dataProtectionContainer = storage.AddBlobContainer("dataprotection", blobContainerName: "dataprotection");

// Table service used for tenant operational metadata.
var tables = storage.AddTables("tables");

builder.AddProject<Projects.ImportToPlanner_Web>("web")
    .WithReference(dataProtectionContainer)
    .WaitFor(dataProtectionContainer)
    .WithReference(tables)
    .WaitFor(tables);

builder.Build().Run();
