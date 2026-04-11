using ImportToPlanner.Web.Components;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

var certificatePathOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
foreach (var certificateSection in builder.Configuration.GetSection("AzureAd:ClientCertificates").GetChildren())
{
    var certificateIndex = certificateSection.Key;
    var certificateDiskPath = certificateSection["CertificateDiskPath"];
    var legacyCertificatePath = certificateSection["CertificatePath"];

    if (string.IsNullOrWhiteSpace(certificateDiskPath) && !string.IsNullOrWhiteSpace(legacyCertificatePath))
    {
        certificatePathOverrides[$"AzureAd:ClientCertificates:{certificateIndex}:CertificateDiskPath"] = legacyCertificatePath;
    }
}

if (certificatePathOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(certificatePathOverrides);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph"))
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

builder.Services.AddScoped<ICsvImportParser, CsvImportParser>();
builder.Services.AddScoped<IImportPlannerOrchestrator, ImportPlannerOrchestrator>();
builder.Services.AddScoped<IPlannerGateway, InMemoryPlannerGateway>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
