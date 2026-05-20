#:sdk Aspire.AppHost.Sdk@13.3.4

#pragma warning disable ASPIRECSHARPAPPS001
var builder = DistributedApplication.CreateBuilder(args);

_ = builder
	.AddContainer("hostedstorage", "mcr.microsoft.com/azure-storage/azurite");

var web = builder.AddCSharpApp("web", "./src/ImportToPlanner.Web/ImportToPlanner.Web.csproj", options =>
{
	options.LaunchProfileName = "https";
});

web.WithEnvironment("HostedStorage__Enabled", "true")
	.WithEnvironment("HostedStorage__ConnectionString", "UseDevelopmentStorage=true")
	.WithEnvironment("HostedStorage__TenantMetadataTable", "TenantOperationalMetadata")
	.WithEnvironment("HostedStorage__DataProtectionContainer", "dataprotection")
	.WithEnvironment("HostedStorage__DataProtectionBlob", "keys.xml");

builder.Build().Run();
