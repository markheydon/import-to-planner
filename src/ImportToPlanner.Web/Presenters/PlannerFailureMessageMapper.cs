using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Presenters;

/// <summary>
/// Maps neutral planner failures to user-safe messages for web presentation.
/// </summary>
internal static class PlannerFailureMessageMapper
{
    /// <summary>
    /// Converts a neutral planner failure into user-safe text.
    /// </summary>
    /// <param name="failure">The neutral failure metadata.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToUserSafeMessage(PlannerOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (IsConsentBlockedFailure(failure))
        {
            return failure.Message;
        }

        if (failure.Category == PlannerFailureCategory.Authorisation
            && !string.IsNullOrWhiteSpace(failure.Message)
            && failure.Message.Contains("administrator consent", StringComparison.OrdinalIgnoreCase))
        {
            return "Administrator consent is required for this tenant. Ask your Microsoft 365 administrator to approve access, then sign in again.";
        }

        if (failure.Category == PlannerFailureCategory.Authentication
            && !string.IsNullOrWhiteSpace(failure.Message)
            && failure.Message.Contains("unsupported account", StringComparison.OrdinalIgnoreCase))
        {
            return "Unsupported account type. Sign in with a supported work or school account.";
        }

        return failure.Category switch
        {
            PlannerFailureCategory.Authentication => "Authentication expired. Sign in again and retry.",
            PlannerFailureCategory.Authorisation => "Permission denied. Confirm the required Planner permissions and try again.",
            PlannerFailureCategory.Conflict => "Planner data changed during processing. Run a fresh preview and retry.",
            PlannerFailureCategory.Unavailable => "Planner is temporarily busy. Wait and retry the import.",
            PlannerFailureCategory.Validation => "The request is not valid. Review the input and try again.",
            _ => "An unexpected planner error occurred. Retry and check logs if the issue continues.",
        };
    }

    /// <summary>
    /// Converts a blocked consent resolution into a user-safe message.
    /// </summary>
    /// <param name="resolution">The structured consent resolution.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToConsentBlockedMessage(ConsentResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return resolution.Status switch
        {
            ConsentResolutionStatus.AdminConsentRequired => resolution.AdminConsentUri is null
                ? "Administrator consent is required before this hosted tenant can continue."
                : $"Administrator consent is required before this hosted tenant can continue. Ask your administrator to approve access: {resolution.AdminConsentUri}",
            ConsentResolutionStatus.Declined => "Consent was declined. Sign in again and complete consent, or contact your administrator.",
            _ => "Hosted consent cannot be validated right now. Retry shortly or contact your administrator.",
        };
    }

    private static bool IsConsentBlockedFailure(PlannerOperationFailure failure)
        => failure.Category == PlannerFailureCategory.Authorisation
            && failure.Target == PlannerFailureTarget.Workflow
            && !string.IsNullOrWhiteSpace(failure.Message)
            && failure.DiagnosticCode is "consent.blocked"
                or "auth.admin_consent_required"
                or nameof(ConsentResolutionStatus.AdminConsentRequired)
                or nameof(ConsentResolutionStatus.Declined)
                or nameof(ConsentResolutionStatus.Unavailable);
}
