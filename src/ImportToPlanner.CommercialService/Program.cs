using ImportToPlanner.CommercialService;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableServiceClient(connectionName: "tables");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register our services.
builder.Services.AddSingleton<ICommercialAccountsService, CommercialAccountsService>();
builder.Services.AddSingleton<ICommercialAuditService, CommercialAuditService>();
builder.Services.AddSingleton<ITenantMetadataService, TenantMetadataService>();
builder.Services.AddSingleton<CommercialProfileService>();
builder.Services.AddSingleton<CommercialAccessService>();
builder.Services.AddHostedService<CommercialAccountRetentionHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add our endpoints.
app.MapCommercialApiEndpoints();

app.MapDefaultEndpoints();

app.Run();
