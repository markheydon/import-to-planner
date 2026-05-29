using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.Import.Presenters;

internal readonly record struct PlannerFailureMapping(
    string UserMessage,
    PlannerFailureCategory Category,
    string? DiagnosticCode,
    ConsentResolutionStatus ConsentStatus,
    bool IsUnsupportedAccount,
    bool IsAdminConsentRequired);

/// <summary>
/// Maps neutral planner failures to user-safe messages for web presentation.
/// </summary>
internal static class PlannerFailureMessageMapper
{
    private const string AdminConsentDiagnosticCode = "auth.admin_consent_required";
    private const string UnsupportedAccountDiagnosticCode = "auth.unsupported_account";
    private const string TenantContextUnresolvedDiagnosticCode = "tenant_context.unresolved";
    private const string PreviewStaleDiagnosticCode = "preview.stale";
    private const string UnhandledDiagnosticCode = "Unhandled";

    /// <summary>
    /// Converts a neutral planner failure into user-safe text.
    /// </summary>
    /// <param name="failure">The neutral failure metadata.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToUserSafeMessage(PlannerOperationFailure failure)
        => ToFailureMapping(failure, ConsentResolutionStatus.Unknown).UserMessage;

    /// <summary>
    /// Maps an exception to structured user-facing failure details.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <param name="currentConsentResolution">Optional current consent resolution state.</param>
    /// <returns>Structured user-facing failure details.</returns>
    public static PlannerFailureMapping FromException(Exception exception, ConsentResolution? currentConsentResolution = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var consentStatus = currentConsentResolution?.Status ?? ConsentResolutionStatus.Unknown;

        if (exception is ConsentBlockedException consentException)
        {
            var failure = new PlannerOperationFailure(
                PlannerFailureCategory.Authorisation,
                PlannerFailureTarget.Workflow,
                null,
                ToConsentBlockedMessage(consentException.Resolution),
                false,
                consentException.Resolution.DiagnosticCode ?? "consent.blocked");

            return ToFailureMapping(failure, consentException.Resolution.Status);
        }

        if (exception is PlannerOperationException plannerException)
        {
            return ToFailureMapping(plannerException.Failure, consentStatus);
        }

        if (exception is StaleImportPreviewException stalePreviewException)
        {
            return new PlannerFailureMapping(
                stalePreviewException.Message,
                PlannerFailureCategory.Conflict,
                PreviewStaleDiagnosticCode,
                consentStatus,
                false,
                false);
        }

        if (exception is GraphUnauthenticatedContextException)
        {
            return new PlannerFailureMapping(
                "Authentication expired. Sign in again and retry.",
                PlannerFailureCategory.Authentication,
                "Authentication",
                consentStatus,
                false,
                false);
        }

        if (exception is InvalidOperationException invalidOperationException)
        {
            return FromDiagnosticSignal(ResolveDiagnosticCode(invalidOperationException.Message), invalidOperationException.Message, consentStatus);
        }

        return new PlannerFailureMapping(
            "An unexpected planner error occurred. Retry and check logs if the issue continues.",
            PlannerFailureCategory.Unknown,
            UnhandledDiagnosticCode,
            consentStatus,
            false,
            false);
    }

    /// <summary>
    /// Maps diagnostic signal data to structured user-facing failure details.
    /// </summary>
    /// <param name="diagnosticCode">The optional diagnostic code.</param>
    /// <param name="message">The optional diagnostic message.</param>
    /// <param name="fallbackConsentStatus">The fallback consent status.</param>
    /// <param name="allowRawMessage">When true, preserves the original message for unknown diagnostics.</param>
    /// <returns>Structured user-facing failure details.</returns>
    public static PlannerFailureMapping FromDiagnosticSignal(
        string? diagnosticCode,
        string? message,
        ConsentResolutionStatus fallbackConsentStatus = ConsentResolutionStatus.Unknown,
        bool allowRawMessage = false)
    {
        if (IsUnsupportedAccountSignal(diagnosticCode, message))
        {
            return new PlannerFailureMapping(
                "Unsupported account type. Sign in with a supported work or school account.",
                PlannerFailureCategory.Authentication,
                diagnosticCode ?? UnsupportedAccountDiagnosticCode,
                ConsentResolutionStatus.Unknown,
                true,
                false);
        }

        if (IsAdminConsentSignal(diagnosticCode, message))
        {
            var userMessage = string.IsNullOrWhiteSpace(message)
                ? "Administrator consent is required for this tenant. Ask your Microsoft 365 administrator to approve access, then sign in again."
                : message;

            return new PlannerFailureMapping(
                userMessage,
                PlannerFailureCategory.Authorisation,
                diagnosticCode ?? AdminConsentDiagnosticCode,
                ConsentResolutionStatus.AdminConsentRequired,
                false,
                true);
        }

        if (!string.IsNullOrWhiteSpace(message)
            && message.Contains("tenant context", StringComparison.OrdinalIgnoreCase))
        {
            return new PlannerFailureMapping(
                "Tenant context could not be resolved for this hosted session. Sign in again and retry.",
                PlannerFailureCategory.Authentication,
                diagnosticCode ?? TenantContextUnresolvedDiagnosticCode,
                fallbackConsentStatus,
                false,
                false);
        }

        return new PlannerFailureMapping(
            allowRawMessage && !string.IsNullOrWhiteSpace(message)
                ? message
                : "An unexpected planner error occurred. Retry and check logs if the issue continues.",
            PlannerFailureCategory.Unknown,
            diagnosticCode,
            fallbackConsentStatus,
            false,
            false);
    }

    /// <summary>
    /// Resolves a stable diagnostic code from a diagnostic message.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>A stable diagnostic code, when determinable.</returns>
    public static string? ResolveDiagnosticCode(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        if (message.Contains("unsupported account", StringComparison.OrdinalIgnoreCase))
        {
            return UnsupportedAccountDiagnosticCode;
        }

        if (message.Contains("administrator consent", StringComparison.OrdinalIgnoreCase))
        {
            return AdminConsentDiagnosticCode;
        }

        if (message.Contains("tenant context", StringComparison.OrdinalIgnoreCase))
        {
            return TenantContextUnresolvedDiagnosticCode;
        }

        return null;
    }

    private static PlannerFailureMapping ToFailureMapping(PlannerOperationFailure failure, ConsentResolutionStatus fallbackConsentStatus)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (IsConsentBlockedFailure(failure))
        {
            return new PlannerFailureMapping(
                failure.Message,
                failure.Category,
                failure.DiagnosticCode,
                ResolveConsentStatus(failure.DiagnosticCode, failure.Message, fallbackConsentStatus),
                IsUnsupportedAccountSignal(failure.DiagnosticCode, failure.Message),
                IsAdminConsentSignal(failure.DiagnosticCode, failure.Message));
        }

        var userMessage = failure.Category switch
        {
            PlannerFailureCategory.Authorisation when IsAdminConsentSignal(failure.DiagnosticCode, failure.Message)
                => "Administrator consent is required for this tenant. Ask your Microsoft 365 administrator to approve access, then sign in again.",
            PlannerFailureCategory.Authentication when IsUnsupportedAccountSignal(failure.DiagnosticCode, failure.Message)
                => "Unsupported account type. Sign in with a supported work or school account.",
            PlannerFailureCategory.Authentication => "Authentication expired. Sign in again and retry.",
            PlannerFailureCategory.Authorisation => "Permission denied. Confirm the required Planner permissions and try again.",
            PlannerFailureCategory.Conflict => "Planner data changed during processing. Run a fresh preview and retry.",
            PlannerFailureCategory.Unavailable => "Planner is temporarily busy. Wait and retry the import.",
            PlannerFailureCategory.Validation => "The request is not valid. Review the input and try again.",
            _ => "An unexpected planner error occurred. Retry and check logs if the issue continues.",
        };

        return new PlannerFailureMapping(
            userMessage,
            failure.Category,
            failure.DiagnosticCode,
            ResolveConsentStatus(failure.DiagnosticCode, failure.Message, fallbackConsentStatus),
            IsUnsupportedAccountSignal(failure.DiagnosticCode, failure.Message),
            IsAdminConsentSignal(failure.DiagnosticCode, failure.Message));
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

    private static ConsentResolutionStatus ResolveConsentStatus(
        string? diagnosticCode,
        string? message,
        ConsentResolutionStatus fallbackConsentStatus)
        => IsAdminConsentSignal(diagnosticCode, message)
            ? ConsentResolutionStatus.AdminConsentRequired
            : fallbackConsentStatus;

    private static bool IsUnsupportedAccountSignal(string? diagnosticCode, string? message)
        => string.Equals(diagnosticCode, UnsupportedAccountDiagnosticCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticCode, "tenant_context.unsupported_account", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(message)
                && message.Contains("unsupported account", StringComparison.OrdinalIgnoreCase));

    private static bool IsAdminConsentSignal(string? diagnosticCode, string? message)
        => string.Equals(diagnosticCode, AdminConsentDiagnosticCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticCode, "consent.blocked", StringComparison.OrdinalIgnoreCase)
            || string.Equals(diagnosticCode, nameof(ConsentResolutionStatus.AdminConsentRequired), StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(message)
                && message.Contains("administrator consent", StringComparison.OrdinalIgnoreCase));

    private static bool IsConsentBlockedFailure(PlannerOperationFailure failure)
        => failure.Category == PlannerFailureCategory.Authorisation
            && failure.Target == PlannerFailureTarget.Workflow
            && !string.IsNullOrWhiteSpace(failure.Message)
            && (string.Equals(failure.DiagnosticCode, "consent.blocked", StringComparison.OrdinalIgnoreCase)
                || string.Equals(failure.DiagnosticCode, AdminConsentDiagnosticCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(failure.DiagnosticCode, nameof(ConsentResolutionStatus.AdminConsentRequired), StringComparison.OrdinalIgnoreCase)
                || string.Equals(failure.DiagnosticCode, nameof(ConsentResolutionStatus.Declined), StringComparison.OrdinalIgnoreCase)
                || string.Equals(failure.DiagnosticCode, nameof(ConsentResolutionStatus.Unavailable), StringComparison.OrdinalIgnoreCase));
}
