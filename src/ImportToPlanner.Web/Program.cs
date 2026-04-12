using ImportToPlanner.Web.Components;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
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
builder.Services.AddHttpContextAccessor();

var graphScopes = builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>() ?? ["User.Read"];

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(graphScopes)
    .AddInMemoryTokenCaches();

builder.Services.AddScoped<GraphServiceClient>(serviceProvider =>
{
    var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var accessTokenProvider = new MicrosoftIdentityAccessTokenProvider(tokenAcquisition, httpContextAccessor, graphScopes);
    var authenticationProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
    return new GraphServiceClient(authenticationProvider);
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

builder.Services.AddScoped<ICsvImportParser, CsvImportParser>();
builder.Services.AddScoped<IImportPlannerOrchestrator, ImportPlannerOrchestrator>();
if (builder.Configuration.GetValue<bool>("PlannerGateway:UseGraph"))
{
    builder.Services.AddScoped<IPlannerGateway, GraphPlannerGateway>();
}
else
{
    builder.Services.AddScoped<IPlannerGateway, InMemoryPlannerGateway>();
}

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

internal sealed class MicrosoftIdentityAccessTokenProvider : IAccessTokenProvider
{
    private readonly ITokenAcquisition tokenAcquisition;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IReadOnlyCollection<string> scopes;

    public MicrosoftIdentityAccessTokenProvider(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        IReadOnlyCollection<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(tokenAcquisition);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(scopes);

        this.tokenAcquisition = tokenAcquisition;
        this.httpContextAccessor = httpContextAccessor;
        this.scopes = scopes;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator(["graph.microsoft.com"]);

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();

        if (!AllowedHostsValidator.IsUrlHostValid(uri))
        {
            return string.Empty;
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("An authenticated user context is required to acquire a Graph access token.");
        }

        return await tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: user).ConfigureAwait(false);
    }
}
