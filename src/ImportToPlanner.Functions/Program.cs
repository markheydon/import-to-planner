using ImportToPlanner.Commercial;
using ImportToPlanner.Functions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
IHostApplicationBuilder hostBuilder = builder;

hostBuilder.AddServiceDefaults();

var commercialModeEnabled = bool.TryParse(
    builder.Configuration["Features:CommercialMode:Enabled"],
    out var parsedCommercialMode)
    && parsedCommercialMode;

if (commercialModeEnabled)
{
    hostBuilder.AddCommercialStorageClients();
    builder.Services.AddCommercial(builder.Configuration);
}

builder.Services.AddPaidCreditExpiryFunction();

builder.Build().Run();
