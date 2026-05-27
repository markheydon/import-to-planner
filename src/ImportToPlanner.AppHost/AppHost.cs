using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Determine runtime behavior from the host environment.
// We set both ASPNETCORE_ENVIRONMENT and DOTNET_ENVIRONMENT on the web project
// to keep hosting and app-level environment resolution aligned.
var appRuntimeEnvironment = builder.Environment.IsProduction()
    ? "Production"
    : builder.Environment.IsStaging()
        ? "Staging"
        : "Development";

// Keep at least one web replica in production for availability;
// allow scale-to-zero in non-production to reduce cost.
var minWebReplicas = builder.Environment.IsProduction() ? 1 : 0;

// Azure AD application parameters used by the web host to authenticate to Microsoft Graph.
// Certificate values are secrets because they contain sensitive credential material.
var azureAdTenantId = builder.AddParameter("azureAdTenantId");
var azureAdClientId = builder.AddParameter("azureAdClientId");
var graphClientCertificatePassword = builder.AddParameter("graphClientCertificatePassword", secret: true);
var graphClientCertificateBase64 = builder.AddParameter("graphClientCertificateBase64", secret: true);

// Optional custom-domain settings for hosted deployments.
// Leave customDomainCertificateName empty on first deployment; set it after the managed certificate exists.
var hasCustomDomainConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Parameters:customDomain"]);
IResourceBuilder<ParameterResource>? customDomain = null;
IResourceBuilder<ParameterResource>? customDomainCertificateName = null;

if (hasCustomDomainConfigured)
{
    customDomain = builder.AddParameter("customDomain", secret: false);
    customDomainCertificateName = builder.AddParameter("customDomainCertificateName", secret: false);
}

// Azure Container Apps environment that will host published services.
// The Aspire dashboard is enabled outside production for diagnostics.
builder.AddAzureContainerAppEnvironment("aca-env")
    .WithDashboard(!builder.Environment.IsProduction());

// Shared storage account (emulated locally) used by the web service.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

// Blob service is used by app features and Data Protection key-ring persistence.
var blobs = storage.AddBlobs("blobs");
var dataProtectionContainer = storage.AddBlobContainer("dataprotection", blobContainerName: "dataprotection");

// Table service stores tenant operational metadata.
var tables = storage.AddTables("tables");

// Configure the web service and wire all dependencies.
builder.AddProject<Projects.ImportToPlanner_Web>("web")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", appRuntimeEnvironment)
    .WithEnvironment("DOTNET_ENVIRONMENT", appRuntimeEnvironment)
    .WithEnvironment("AzureAd__TenantId", azureAdTenantId)
    .WithEnvironment("AzureAd__ClientId", azureAdClientId)
    // The app currently expects a certificate path; the base64 value is provided
    // so deployment/runtime can materialize the certificate at that location.
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

        if (hasCustomDomainConfigured && customDomain is not null && customDomainCertificateName is not null)
        {
            #pragma warning disable ASPIREACADOMAINS001
            app.ConfigureCustomDomain(customDomain, customDomainCertificateName);
            #pragma warning restore ASPIREACADOMAINS001
        }
    });

builder.Build().Run();
