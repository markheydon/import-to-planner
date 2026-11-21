using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Web.Features.CommercialAccounts.Presenters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Pages;

public partial class Profile
{
    internal const string CreditCheckoutReturnQueryKey = "creditCheckout";

    internal const string CreditCheckoutResultQueryKey = "result";

    private bool isStartingCreditCheckout;

    private string? creditCheckoutUserMessage;

    private string? checkoutPackIdInProgress;

    private bool ShowCreditPackCatalog =>
        creditSummary is { ComplimentaryUnlimitedAccess: false }
        && string.IsNullOrWhiteSpace(creditBalanceErrorMessage)
        && !creditSummaryLoading
        && ServiceProvider.GetService<CreateCreditCheckoutUseCase>() is not null
        && ServiceProvider.GetService<IOptions<StripeOptions>>() is not null;

    private IReadOnlyList<ProfileCreditPackChoice> CreditPackCatalog
        => ProfileCreditPresenter.BuildPackCatalog(StripePackOptions);

    private IEnumerable<CreditPackOptions> StripePackOptions
        => ServiceProvider.GetService<IOptions<StripeOptions>>()?.Value.CreditPacks ?? [];

    private async Task StartCreditCheckoutForPackAsync(string packId)
    {
        if (isStartingCreditCheckout || !ShowCreditPackCatalog || string.IsNullOrWhiteSpace(packId))
        {
            return;
        }

        var checkoutUseCase = ServiceProvider.GetService<CreateCreditCheckoutUseCase>();
        var sessionIdentity = SessionIdentityContextAccessor.TryGetCurrent();
        if (checkoutUseCase is null
            || sessionIdentity is null
            || string.IsNullOrWhiteSpace(sessionIdentity.TenantId)
            || string.IsNullOrWhiteSpace(sessionIdentity.UserId))
        {
            creditCheckoutUserMessage = ProfileCreditPresenter.BuildCheckoutUnavailableMessage();
            return;
        }

        isStartingCreditCheckout = true;
        checkoutPackIdInProgress = packId;
        creditCheckoutUserMessage = null;
        try
        {
            var outcome = await checkoutUseCase.ExecuteAsync(
                new CreateCreditCheckoutRequest(
                    sessionIdentity.TenantId,
                    sessionIdentity.UserId,
                    packId,
                    CreditCheckoutReturnSurface.Profile,
                    BuildProfileCheckoutReturnUrl("success"),
                    BuildProfileCheckoutReturnUrl("cancel"),
                    DateTimeOffset.UtcNow),
                RequestCancellation).ConfigureAwait(false);

            if (outcome is CreateCreditCheckoutOutcome.Ready ready)
            {
                NavigationManager.NavigateTo(ready.RedirectUrl, forceLoad: true);
                return;
            }

            creditCheckoutUserMessage = ProfileCreditPresenter.BuildCheckoutUnavailableMessage();
        }
        catch (Exception)
        {
            creditCheckoutUserMessage = ProfileCreditPresenter.BuildCheckoutUnavailableMessage();
        }
        finally
        {
            isStartingCreditCheckout = false;
            checkoutPackIdInProgress = null;
        }
    }

    private string BuildProfileCheckoutReturnUrl(string result)
    {
        var baseUri = NavigationManager.BaseUri.TrimEnd('/');
        var returnUri = $"{baseUri}/profile";
        return QueryHelpers.AddQueryString(
            returnUri,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CreditCheckoutReturnQueryKey] = "return",
                [CreditCheckoutResultQueryKey] = result,
            });
    }
}
