using ImportToPlanner.Web.Features.CommercialAccounts.Backend;

namespace ImportToPlanner.Web.Features.Authentication;

/// <summary>
/// Maps session identity context into user-facing summary text.
/// </summary>
internal sealed class SessionIdentityPresenter
{
    private readonly string defaultSignedInLabel = "Signed in";

    public SessionIdentityPresentation Present(SessionIdentityContext? sessionIdentity)
    {
        if (sessionIdentity is null)
        {
            return new SessionIdentityPresentation(defaultSignedInLabel, null);
        }

        var emailAddress = string.IsNullOrWhiteSpace(sessionIdentity.EmailAddress)
            ? defaultSignedInLabel
            : sessionIdentity.EmailAddress;
        var tenantName = string.IsNullOrWhiteSpace(sessionIdentity.TenantName)
            ? null
            : sessionIdentity.TenantName;

        return new SessionIdentityPresentation(emailAddress, tenantName);
    }
}

internal sealed record SessionIdentityPresentation(string EmailAddress, string? TenantName);
