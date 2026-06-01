using ImportToPlanner.CommercialService;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableServiceClient(connectionName: "tables");

// Register our services.
builder.Services.AddSingleton<CommercialAccountsService>();
builder.Services.AddSingleton<CommercialAuditService>();
builder.Services.AddSingleton<TenantMetadataService>();
builder.Services.AddSingleton<CommercialProfileService>();
builder.Services.AddSingleton<CommercialAccessService>();
builder.Services.AddHostedService<CommercialAccountRetentionHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Add our endpoints.
app.MapCommercialApiEndpoints();

app.Run();
