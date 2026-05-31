using ImportToPlanner.ApiService.Commercial;
using ImportToPlanner.Infrastructure.Graph;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructureStorageClients();

StartupConfigurationValidator.Validate(builder.Configuration);

builder.Services.AddCommercialApiServices(builder.Configuration);

var app = builder.Build();

app.MapCommercialApiEndpoints();
app.MapDefaultEndpoints();

app.Run();
