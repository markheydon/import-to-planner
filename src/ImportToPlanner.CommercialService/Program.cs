using ImportToPlanner.CommercialService;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableServiceClient(connectionName: "tables");

// Register our services.
builder.Services.AddSingleton<ICommercialAccountsService, CommercialAccountsService>();
builder.Services.AddSingleton<ICommercialAuditService, CommercialAuditService>();
builder.Services.AddSingleton<ITenantMetadataService, TenantMetadataService>();
builder.Services.AddSingleton<CommercialProfileService>();
builder.Services.AddSingleton<CommercialAccessService>();
builder.Services.AddHostedService<CommercialAccountRetentionHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Add our endpoints.
app.MapCommercialApiEndpoints();

app.Run();
