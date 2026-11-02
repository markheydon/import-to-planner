using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Web.Features.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ImportToPlanner.Web.Features.Billing;

/// <summary>
/// Authenticated minimal API for starting credit pack checkout.
/// </summary>
internal static class CreditCheckoutEndpoint
{
    /// <summary>
    /// Maps <c>POST /api/billing/credit-checkout</c> for signed-in commercial users.
    /// </summary>
    public static IEndpointRouteBuilder MapCreditCheckout(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/billing/credit-checkout",
            async (
                [FromBody] CreditCheckoutHttpRequest body,
                ISessionIdentityContextAccessor sessionIdentityContextAccessor,
                CreateCreditCheckoutUseCase createCreditCheckoutUseCase,
                CancellationToken cancellationToken) =>
            {
                var identity = sessionIdentityContextAccessor.TryGetCurrent();
                if (identity is null
                    || string.IsNullOrWhiteSpace(identity.TenantId)
                    || string.IsNullOrWhiteSpace(identity.UserId))
                {
                    return Results.Unauthorized();
                }

                if (body is null
                    || string.IsNullOrWhiteSpace(body.PackId)
                    || string.IsNullOrWhiteSpace(body.SuccessUrl)
                    || string.IsNullOrWhiteSpace(body.CancelUrl))
                {
                    return Results.BadRequest();
                }

                var outcome = await createCreditCheckoutUseCase.ExecuteAsync(
                    new CreateCreditCheckoutRequest(
                        identity.TenantId,
                        identity.UserId,
                        body.PackId,
                        body.ReturnSurface,
                        body.SuccessUrl,
                        body.CancelUrl,
                        DateTimeOffset.UtcNow),
                    cancellationToken).ConfigureAwait(false);

                return outcome switch
                {
                    CreateCreditCheckoutOutcome.Ready ready => Results.Ok(new CreditCheckoutHttpResponse(ready.RedirectUrl)),
                    CreateCreditCheckoutOutcome.Rejected rejected => Results.BadRequest(new { code = rejected.DiagnosticCode }),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
                };
            })
            .RequireAuthorization();

        return endpoints;
    }

    internal sealed record CreditCheckoutHttpRequest(
        string PackId,
        CreditCheckoutReturnSurface ReturnSurface,
        string SuccessUrl,
        string CancelUrl);

    internal sealed record CreditCheckoutHttpResponse(string RedirectUrl);
}
