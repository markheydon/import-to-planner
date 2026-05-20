#:sdk Aspire.AppHost.Sdk@13.3.4

#pragma warning disable ASPIRECSHARPAPPS001
var builder = DistributedApplication.CreateBuilder(args);

var deploymentMode = GetEnvironmentOrDefault("DeploymentMode__Mode", "SelfHostedSingleTenant");
var plannerGatewayUseGraph = GetEnvironmentOrDefault("PlannerGateway__UseGraph", "false");
var hostedStorageEnabled = GetEnvironmentOrDefault("HostedStorage__Enabled", "false");
var hostedStorageConnectionString = GetEnvironmentOrDefault("HostedStorage__ConnectionString", "UseDevelopmentStorage=true");
var hostedStorageTenantMetadataTable = GetEnvironmentOrDefault("HostedStorage__TenantMetadataTable", "TenantOperationalMetadata");
var hostedStorageDataProtectionContainer = GetEnvironmentOrDefault("HostedStorage__DataProtectionContainer", "dataprotection");
var hostedStorageDataProtectionBlob = GetEnvironmentOrDefault("HostedStorage__DataProtectionBlob", "keys.xml");
var deploymentAuthorityTenant = GetOptionalEnvironment("DeploymentMode__AuthorityTenant");
var azureAdTenantId = GetOptionalEnvironment("AzureAd__TenantId");
var isHostedStorageEnabled = string.Equals(hostedStorageEnabled, "true", StringComparison.OrdinalIgnoreCase);

if (isHostedStorageEnabled)
{
	_ = builder
		.AddContainer("hostedstorage", "mcr.microsoft.com/azure-storage/azurite");
}

var web = builder.AddCSharpApp("web", "./src/ImportToPlanner.Web/ImportToPlanner.Web.csproj", options =>
{
	options.LaunchProfileName = "https";
});

web.WithEnvironment("DeploymentMode__Mode", deploymentMode)
	.WithEnvironment("PlannerGateway__UseGraph", plannerGatewayUseGraph)
	.WithEnvironment("HostedStorage__Enabled", hostedStorageEnabled);

if (!string.IsNullOrWhiteSpace(deploymentAuthorityTenant))
{
	web.WithEnvironment("DeploymentMode__AuthorityTenant", deploymentAuthorityTenant);
}

if (!string.IsNullOrWhiteSpace(azureAdTenantId))
{
	web.WithEnvironment("AzureAd__TenantId", azureAdTenantId);
}

if (isHostedStorageEnabled)
{
	web.WithEnvironment("HostedStorage__ConnectionString", hostedStorageConnectionString)
		.WithEnvironment("HostedStorage__TenantMetadataTable", hostedStorageTenantMetadataTable)
		.WithEnvironment("HostedStorage__DataProtectionContainer", hostedStorageDataProtectionContainer)
		.WithEnvironment("HostedStorage__DataProtectionBlob", hostedStorageDataProtectionBlob);
}

builder.Build().Run();

static string GetEnvironmentOrDefault(string name, string defaultValue)
{
	var value = Environment.GetEnvironmentVariable(name);
	return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}

static string? GetOptionalEnvironment(string name)
{
	var value = Environment.GetEnvironmentVariable(name);
	return string.IsNullOrWhiteSpace(value) ? null : value;
}
