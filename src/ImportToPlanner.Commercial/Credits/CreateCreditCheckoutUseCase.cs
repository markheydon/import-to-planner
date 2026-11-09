using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Models;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Validates pack and tenant policy, resolves billing customer metadata, and starts external checkout.
/// </summary>
public sealed class CreateCreditCheckoutUseCase(
    IOptions<StripeOptions> stripeOptions,
    ITenantBillingStore billingStore,
    ICreditCheckoutPaymentPort checkoutPaymentPort,
    IComplimentaryTenantPolicy complimentaryTenantPolicy)
{
    /// <summary>
    /// Creates a checkout session for a published credit pack.
    /// </summary>
    public async Task<CreateCreditCheckoutOutcome> ExecuteAsync(
        CreateCreditCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.ActorUserId)
            || string.IsNullOrWhiteSpace(request.PackId)
            || string.IsNullOrWhiteSpace(request.SuccessUrl)
            || string.IsNullOrWhiteSpace(request.CancelUrl))
        {
            return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.PackUnavailable);
        }

        if (complimentaryTenantPolicy.IsComplimentary(request.TenantId))
        {
            return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.ComplimentaryTenant);
        }

        var pack = stripeOptions.Value.CreditPacks
            .FirstOrDefault(candidate =>
                candidate.Published
                && string.Equals(candidate.PackId, request.PackId, StringComparison.Ordinal));
        if (pack is null
            || pack.CreditQuantity <= 0
            || string.IsNullOrWhiteSpace(pack.StripePriceId))
        {
            return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.PackUnavailable);
        }

        TenantBillingMetadata? billingMetadata;
        try
        {
            billingMetadata = await billingStore
                .GetAsync(request.TenantId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.BillingStoreUnavailable);
        }

        var stripeCustomerId = billingMetadata?.StripeCustomerId;
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            try
            {
                stripeCustomerId = await checkoutPaymentPort
                    .CreateCustomerAsync(request.TenantId, cancellationToken)
                    .ConfigureAwait(false);
                await billingStore.UpsertAsync(
                    new TenantBillingMetadata(
                        request.TenantId,
                        stripeCustomerId,
                        billingMetadata?.LastInvoiceOrReceiptId,
                        request.OccurredUtc),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.PaymentProviderUnavailable);
            }
        }

        try
        {
            var redirectUrl = await checkoutPaymentPort.CreateCheckoutSessionAsync(
                new CreditCheckoutSessionRequest(
                    request.TenantId,
                    request.ActorUserId,
                    pack.PackId,
                    pack.StripePriceId,
                    stripeCustomerId,
                    request.SuccessUrl,
                    request.CancelUrl,
                    request.OccurredUtc),
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.PaymentProviderUnavailable);
            }

            return new CreateCreditCheckoutOutcome.Ready(redirectUrl);
        }
        catch (Exception)
        {
            return new CreateCreditCheckoutOutcome.Rejected(PaidCreditPurchaseFailureCodes.PaymentProviderUnavailable);
        }
    }
}
