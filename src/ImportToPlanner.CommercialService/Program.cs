using ImportToPlanner.CommercialService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableServiceClient(connectionName: "tables");

StartupConfigurationValidator.Validate(builder.Configuration);

CommercialServiceComposition.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.MapCommercialApiEndpoints();
app.MapDefaultEndpoints();

app.Run();
