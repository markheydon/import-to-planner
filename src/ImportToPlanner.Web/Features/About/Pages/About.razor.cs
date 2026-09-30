using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Demo;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.About.Pages;

/// <summary>
/// About page showing product and release information for signed-in users.
/// </summary>
public partial class About
{
    [Inject]
    internal IReleaseInformationQuery ReleaseInformationQuery { get; set; } = default!;

    [Inject]
    internal IOptions<DocsExternalLinksOptions> DocsLinks { get; set; } = default!;

    [Inject]
    internal NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    internal AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private ReleaseInformation? releaseInformation;
    private bool isBusy;
    private bool showSignInPrompt;
    private string? loadErrorMessage;

    private string docsBaseUrl => DocsLinks.Value.DocsBaseUrl.TrimEnd('/');

    private string docsHomeUrl => $"{docsBaseUrl}/";

    private string termsUrl => $"{docsBaseUrl}/terms";

    private string privacyUrl => $"{docsBaseUrl}/privacy-and-security";

    private string supportUrl => $"{docsBaseUrl}/support";

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            showSignInPrompt = true;
            return;
        }

        isBusy = true;
        try
        {
            releaseInformation = await ReleaseInformationQuery.GetAsync();
        }
        catch (Exception)
        {
            loadErrorMessage = "Release information is temporarily unavailable. Try again later.";
        }
        finally
        {
            isBusy = false;
        }
    }

    protected void OnSignInClicked()
    {
        var returnUrl = Uri.EscapeDataString("/about");
        NavigationManager.NavigateTo($"MicrosoftIdentity/Account/SignIn?redirectUri={returnUrl}", forceLoad: true);
    }
}
