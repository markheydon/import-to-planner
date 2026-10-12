using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Web.Features.Authentication;
using ImportToPlanner.Web.Features.CommercialAccounts.Abstractions;
using ImportToPlanner.Web.Features.CommercialAccounts.Presenters;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Pages;

/// <summary>
/// Displays the current commercial account details and lifecycle actions.
/// </summary>
public partial class Profile
{
    [Inject]
    internal ISessionIdentityContextAccessor SessionIdentityContextAccessor { get; set; } = default!;

    [Inject]
    internal IServiceProvider ServiceProvider { get; set; } = default!;

    private ICommercialProfileUseCase? CommercialProfileUseCase =>
        ServiceProvider.GetService<ICommercialProfileUseCase>();

    private IGetProfileCreditSummaryUseCase? ProfileCreditSummaryUseCase =>
        ServiceProvider.GetService<IGetProfileCreditSummaryUseCase>();

    private IGetProfileCreditActivityUseCase? ProfileCreditActivityUseCase =>
        ServiceProvider.GetService<IGetProfileCreditActivityUseCase>();

    [Inject]
    internal CommercialModeOptions CommercialModeOptions { get; set; } = default!;

    [Inject]
    internal NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    internal AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    internal IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    private CancellationToken RequestCancellation
        => HttpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

    private CommercialAccount? account;
    private bool isBusy;
    private bool isDeletingAccount;
    private bool showSignInPrompt;
    private bool showDeleteConfirmation;
    private string? statusMessage;
    private Severity statusSeverity = Severity.Info;

    private bool showCreditsSection;
    private bool creditSummaryLoading;
    private bool creditActivityLoading;
    private ProfileCreditSummaryResult? creditSummary;
    private ProfileCreditActivityResult? creditActivity;
    private string? creditBalanceErrorMessage;
    private string? creditActivityErrorMessage;

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

        if (CommercialProfileUseCase is null)
        {
            statusMessage = "Commercial profile services are not available in this deployment.";
            statusSeverity = Severity.Error;
            return;
        }

        isBusy = true;
        try
        {
            account = await CommercialProfileUseCase.GetProfileAsync(sessionIdentity, RequestCancellation);
            if (account is not null)
            {
                await LoadCreditsAsync(sessionIdentity.TenantId, sessionIdentity.UserId);
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task LoadCreditsAsync(string tenantId, string actorUserId)
    {
        showCreditsSection = ProfileCreditSummaryUseCase is not null
            && ProfileCreditActivityUseCase is not null;

        if (!showCreditsSection)
        {
            return;
        }

        var occurredUtc = DateTimeOffset.UtcNow;
        var summaryRequest = new ProfileCreditSummaryRequest(tenantId, actorUserId, occurredUtc);
        var activityRequest = new ProfileCreditActivityRequest(tenantId, actorUserId, occurredUtc);

        creditSummaryLoading = true;
        creditActivityLoading = true;
        creditBalanceErrorMessage = null;
        creditActivityErrorMessage = null;
        creditSummary = null;
        creditActivity = null;

        await LoadCreditSummaryAsync(summaryRequest);
        await LoadCreditActivityAsync(activityRequest);
    }

    private async Task LoadCreditSummaryAsync(ProfileCreditSummaryRequest request)
    {
        if (ProfileCreditSummaryUseCase is null)
        {
            creditSummaryLoading = false;
            return;
        }

        try
        {
            var outcome = await ProfileCreditSummaryUseCase.GetAsync(request, RequestCancellation);
            if (outcome is GetProfileCreditSummaryOutcome.Succeeded succeeded)
            {
                creditSummary = succeeded.Result;
            }
            else
            {
                creditBalanceErrorMessage = ProfileCreditPresenter.BuildBalanceUnavailableMessage();
            }
        }
        catch (Exception)
        {
            creditBalanceErrorMessage = ProfileCreditPresenter.BuildBalanceUnavailableMessage();
        }
        finally
        {
            creditSummaryLoading = false;
        }
    }

    private async Task LoadCreditActivityAsync(ProfileCreditActivityRequest request)
    {
        if (ProfileCreditActivityUseCase is null)
        {
            creditActivityLoading = false;
            return;
        }

        try
        {
            var outcome = await ProfileCreditActivityUseCase.GetAsync(request, RequestCancellation);
            if (outcome is GetProfileCreditActivityOutcome.Succeeded succeeded)
            {
                creditActivity = succeeded.Result;
            }
            else
            {
                creditActivityErrorMessage = ProfileCreditPresenter.BuildActivityUnavailableMessage();
            }
        }
        catch (Exception)
        {
            creditActivityErrorMessage = ProfileCreditPresenter.BuildActivityUnavailableMessage();
        }
        finally
        {
            creditActivityLoading = false;
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

        if (CommercialProfileUseCase is null)
        {
            statusMessage = "Commercial profile services are not available in this deployment.";
            statusSeverity = Severity.Warning;
            showDeleteConfirmation = false;
            return;
        }

        isDeletingAccount = true;
        try
        {
            await CommercialProfileUseCase.DeleteAccountAsync(sessionIdentity, DateTimeOffset.UtcNow, RequestCancellation);
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

    private bool ShowBuyCreditsButton =>
        creditSummary is { ComplimentaryUnlimitedAccess: false }
        && ServiceProvider.GetService<IBuyCreditsProfileNavigation>() is not null;

    private void OnBuyCreditsClicked()
    {
        var navigation = ServiceProvider.GetService<IBuyCreditsProfileNavigation>();
        if (navigation is null || creditSummary?.ComplimentaryUnlimitedAccess == true)
        {
            return;
        }

        NavigationManager.NavigateTo(navigation.BuyCreditsRelativeUrl);
    }
}
