var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

builder.AddProject<Projects.ImportToPlanner_Web>("web")
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(tables)
    .WaitFor(tables);

builder.Build().Run();
