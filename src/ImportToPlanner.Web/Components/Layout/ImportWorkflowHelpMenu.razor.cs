using ImportToPlanner.Web.Features.Demo;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Components.Layout;

/// <summary>
/// Help menu for import workflow pages (documentation, support, and About).
/// </summary>
public partial class ImportWorkflowHelpMenu
{
    [Inject]
    private IOptions<DocsExternalLinksOptions> DocsLinks { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private string DocsBaseUrl => DocsLinks.Value.DocsBaseUrl.TrimEnd('/');

    private string DocsHomeUrl => $"{DocsBaseUrl}/";

    private string SupportUrl => $"{DocsBaseUrl}/support";

    private async Task OnAboutClicked()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Uri.EscapeDataString("/about");
            NavigationManager.NavigateTo($"MicrosoftIdentity/Account/SignIn?redirectUri={returnUrl}", forceLoad: true);
            return;
        }

        NavigationManager.NavigateTo("/about");
    }
}
