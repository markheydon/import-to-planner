using System.Diagnostics;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Authentication;
using ImportToPlanner.Web.Infrastructure;

namespace ImportToPlanner.Web.Diagnostics;

internal sealed class UserFacingFailureDiagnostics(
    IHttpContextAccessor httpContextAccessor,
    TenantAuthorityConfiguration tenantAuthorityConfiguration)
{
    private const string HandledFailureEventName = "import_to_planner.handled_failure";

    public FailurePresentation RecordHandledFailure(
        ILogger logger,
        Exception? exception,
        string operation,
        string failureCategory,
        string userSafeMessage,
        LogLevel logLevel,
        TenantContext? tenantContext = null,
        ConsentResolutionStatus consentStatus = ConsentResolutionStatus.Unknown,
        string? failureCode = null,
        string? tenantKeyOverride = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(userSafeMessage);

        var referenceId = ResolveReferenceId();
        if (logger.IsEnabled(logLevel))
        {
            var scopeState = BuildScopeState(referenceId, operation, failureCategory, userSafeMessage, tenantContext, consentStatus, failureCode, tenantKeyOverride);

            using var _ = logger.BeginScope(scopeState);
            logger.Log(logLevel, exception, "Handled failure in {Operation}. A user-safe response was returned.", operation);
        }

        RecordActivity(referenceId, operation, failureCategory, logLevel, failureCode, userSafeMessage, tenantContext, consentStatus, tenantKeyOverride, exception);

        return new FailurePresentation(userSafeMessage, referenceId);
    }

    public string? ResolveReferenceId()
    {
        var traceId = Activity.Current?.TraceId;
        if (traceId is { } currentTraceId && currentTraceId != default)
        {
            return currentTraceId.ToString();
        }

        return string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.TraceIdentifier)
            ? null
            : httpContextAccessor.HttpContext.TraceIdentifier;
    }

    private Dictionary<string, object?> BuildScopeState(
        string? referenceId,
        string operation,
        string failureCategory,
        string userSafeMessage,
        TenantContext? tenantContext,
        ConsentResolutionStatus consentStatus,
        string? failureCode,
        string? tenantKeyOverride)
    {
        var state = HostedTelemetryHelper
            .BuildHostedTelemetryDimensions(tenantAuthorityConfiguration, tenantContext, consentStatus, failureCategory)
            .ToDictionary(static pair => pair.Key, static pair => (object?)pair.Value, StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(tenantKeyOverride))
        {
            state["tenant.key"] = tenantKeyOverride;
        }

        state["failure.handled"] = true;
        state["failure.operation"] = operation;
        state["failure.user_message_present"] = true;
        state["failure.reference_id"] = referenceId ?? "none";

        if (!string.IsNullOrWhiteSpace(failureCode))
        {
            state["failure.code"] = failureCode;
        }

        return state;
    }

    private void RecordActivity(
        string? referenceId,
        string operation,
        string failureCategory,
        LogLevel logLevel,
        string? failureCode,
        string userSafeMessage,
        TenantContext? tenantContext,
        ConsentResolutionStatus consentStatus,
        string? tenantKeyOverride,
        Exception? exception)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            ["failure.handled"] = true,
            ["failure.operation"] = operation,
            ["failure.category"] = failureCategory,
            ["failure.reference_id"] = referenceId ?? "none",
            ["failure.user_message_present"] = true,
            ["consent.status"] = consentStatus.ToString(),
            ["authority.kind"] = tenantAuthorityConfiguration.AuthorityKind.ToString(),
            ["tenant.key"] = tenantKeyOverride ?? tenantContext?.TenantKey ?? "none",
        };

        if (!string.IsNullOrWhiteSpace(failureCode))
        {
            tags.Add("failure.code", failureCode);
        }

        if (exception is not null)
        {
            tags.Add("exception.type", exception.GetType().FullName);
        }

        activity.AddEvent(new ActivityEvent(HandledFailureEventName, tags: tags));

        if (logLevel >= LogLevel.Error)
        {
            activity.SetStatus(ActivityStatusCode.Error, userSafeMessage);
        }
    }
}

internal readonly record struct FailurePresentation(string UserMessage, string? ReferenceId);
