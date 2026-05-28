using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ImportToPlanner.Web.Components.Pages;

/// <summary>
/// Displays the current commercial account details and lifecycle actions.
/// </summary>
public partial class Profile
{
    [Inject]
    internal ISessionIdentityContextAccessor SessionIdentityContextAccessor { get; set; } = default!;

    [Inject]
    internal ICommercialProfileUseCase CommercialProfileUseCase { get; set; } = default!;

    [Inject]
    internal CommercialModeOptions CommercialModeOptions { get; set; } = default!;

    [Inject]
    internal NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    internal AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private CommercialAccount? account;
    private bool isBusy;
    private bool isDeletingAccount;
    private bool showSignInPrompt;
    private bool showDeleteConfirmation;
    private string? statusMessage;
    private Severity statusSeverity = Severity.Info;

    protected override async Task OnInitializedAsync()
    {
        if (!CommercialModeOptions.Enabled)
        {
            NavigationManager.NavigateTo("/", forceLoad: false);
            return;
        }

        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            showSignInPrompt = true;
            return;
        }

        var sessionIdentity = SessionIdentityContextAccessor.TryGetCurrent();
        if (sessionIdentity is null)
        {
            statusMessage = "We could not resolve your signed-in identity. Sign out and sign in again.";
            statusSeverity = Severity.Warning;
            return;
        }

        isBusy = true;
        try
        {
            account = await CommercialProfileUseCase.GetProfileAsync(sessionIdentity, CancellationToken.None);
        }
        finally
        {
            isBusy = false;
        }
    }

    protected void OnSignInClicked()
    {
        NavigationManager.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
    }

    protected void OnDeleteRequested()
    {
        showDeleteConfirmation = true;
    }

    protected void OnDeleteCancelled()
    {
        showDeleteConfirmation = false;
    }

    protected async Task OnDeleteConfirmedAsync()
    {
        if (isDeletingAccount)
        {
            return;
        }

        var sessionIdentity = SessionIdentityContextAccessor.TryGetCurrent();
        if (sessionIdentity is null)
        {
            statusMessage = "We could not resolve your signed-in identity. Sign out and sign in again.";
            statusSeverity = Severity.Warning;
            showDeleteConfirmation = false;
            return;
        }

        isDeletingAccount = true;
        try
        {
            await CommercialProfileUseCase.DeleteAccountAsync(sessionIdentity, DateTimeOffset.UtcNow, CancellationToken.None);
        }
        finally
        {
            isDeletingAccount = false;
        }

        showDeleteConfirmation = false;
        statusMessage = "Your account has been marked for deletion. Sign in again if you need to restore it during retention.";
        statusSeverity = Severity.Success;

        var returnUrl = Uri.EscapeDataString("/");
        NavigationManager.NavigateTo($"MicrosoftIdentity/Account/SignOut?returnUrl={returnUrl}", forceLoad: true);
    }
}
