using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Web.Features.Import.Presenters;
using ImportToPlanner.Web.Features.Import.Workflows;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    internal const string CreditCheckoutReturnQueryKey = "creditCheckout";

    internal const string CreditCheckoutResultQueryKey = "result";

    private string? selectedCreditPackId;

    private bool isStartingCreditCheckout;

    private string? creditCheckoutUserMessage;

    private bool ShowCreditPackPurchaseSection
        => CommercialModeOptions.Enabled
           && creditBalanceSnapshot is { InsufficientCredits: true, ComplimentaryUnlimitedAccess: false, LedgerUnavailable: false, RemainingCredits: not null };

    private IReadOnlyList<ImportCreditPackChoice> CreditPackChoices
        => ImportCreditPreviewPresenter.BuildPackChoices(
            StripePackOptions,
            creditBalanceSnapshot?.Shortfall ?? 0);

    private IEnumerable<CreditPackOptions> StripePackOptions
        => ServiceProvider.GetService<IOptions<StripeOptions>>()?.Value.CreditPacks ?? [];

    private bool EnsureCreditPackSelection()
    {
        var choices = CreditPackChoices;
        if (choices.Count == 0)
        {
            selectedCreditPackId = null;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selectedCreditPackId)
            && choices.Any(choice => string.Equals(choice.PackId, selectedCreditPackId, StringComparison.Ordinal)))
        {
            return true;
        }

        selectedCreditPackId = choices.FirstOrDefault(choice => choice.IsRecommended)?.PackId
            ?? choices[0].PackId;
        return true;
    }

    private async Task StartCreditCheckoutAsync()
    {
        if (isStartingCreditCheckout || !ShowCreditPackPurchaseSection)
        {
            return;
        }

        var checkoutUseCase = ServiceProvider.GetService<CreateCreditCheckoutUseCase>();
        var identity = SessionIdentityContextAccessor.TryGetCurrent();
        if (checkoutUseCase is null
            || identity is null
            || string.IsNullOrWhiteSpace(identity.TenantId)
            || string.IsNullOrWhiteSpace(identity.UserId)
            || string.IsNullOrWhiteSpace(selectedCreditPackId))
        {
            creditCheckoutUserMessage = ImportCreditPreviewPresenter.BuildCheckoutUnavailableMessage();
            return;
        }

        isStartingCreditCheckout = true;
        creditCheckoutUserMessage = null;
        try
        {
            var outcome = await checkoutUseCase.ExecuteAsync(
                new CreateCreditCheckoutRequest(
                    identity.TenantId,
                    identity.UserId,
                    selectedCreditPackId,
                    CreditCheckoutReturnSurface.Preview,
                    BuildCheckoutReturnUrl("success"),
                    BuildCheckoutReturnUrl("cancel"),
                    DateTimeOffset.UtcNow),
                WorkflowCancellation).ConfigureAwait(false);

            if (outcome is CreateCreditCheckoutOutcome.Ready ready)
            {
                NavigationManager.NavigateTo(ready.RedirectUrl, forceLoad: true);
                return;
            }

            creditCheckoutUserMessage = ImportCreditPreviewPresenter.BuildCheckoutUnavailableMessage();
        }
        catch (Exception ex)
        {
            HandleUserSafeFailure(ex, "workflow.credit_checkout", WorkflowStatusLevel.Warning);
            creditCheckoutUserMessage = ImportCreditPreviewPresenter.BuildCheckoutUnavailableMessage();
        }
        finally
        {
            isStartingCreditCheckout = false;
        }
    }

    private string BuildCheckoutReturnUrl(string result)
    {
        var baseUri = NavigationManager.BaseUri.TrimEnd('/');
        var returnUri = $"{baseUri}/";
        return QueryHelpers.AddQueryString(
            returnUri,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [CreditCheckoutReturnQueryKey] = "return",
                [CreditCheckoutResultQueryKey] = result,
            });
    }

    private async Task TryHandleCreditCheckoutReturnAsync(Dictionary<string, StringValues> parsedQuery)
    {
        if (!CommercialModeOptions.Enabled)
        {
            return;
        }

        if (!parsedQuery.TryGetValue(CreditCheckoutReturnQueryKey, out var checkoutValues)
            || !string.Equals(checkoutValues.ToString(), "return", StringComparison.Ordinal))
        {
            return;
        }

        RemoveCreditCheckoutQueryFromUrl();

        if (preview is null)
        {
            return;
        }

        await WorkflowCoordinator.RefreshCreditBalanceSnapshotAfterCheckoutReturnAsync(
            WorkflowState,
            WorkflowCancellation).ConfigureAwait(false);

        viewedStep = 4;
        await InvokeAsync(StateHasChanged);
    }

    private void RemoveCreditCheckoutQueryFromUrl()
    {
        var uri = new Uri(NavigationManager.Uri);
        var parsed = QueryHelpers.ParseQuery(uri.Query)
            .Where(pair => !string.Equals(pair.Key, CreditCheckoutReturnQueryKey, StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(pair.Key, CreditCheckoutResultQueryKey, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var path = uri.GetLeftPart(UriPartial.Path);
        var cleaned = parsed.Count == 0 ? path : QueryHelpers.AddQueryString(path, parsed);
        NavigationManager.NavigateTo(cleaned, replace: true);
    }
}
