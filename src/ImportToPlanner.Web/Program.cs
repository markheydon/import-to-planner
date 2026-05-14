using ImportToPlanner.Application;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web;
using ImportToPlanner.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
builder.Services
    .AddWebHostServices(builder.Configuration)
    .AddApplication()
    .AddImportWorkflow()
    .AddInfrastructure(builder.Configuration.GetValue<bool>("PlannerGateway:UseGraph"));

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
app.MapDefaultEndpoints();

app.Run();
