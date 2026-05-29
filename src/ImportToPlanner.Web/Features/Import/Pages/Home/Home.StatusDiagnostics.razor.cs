using ImportToPlanner.Web.Diagnostics;
using ImportToPlanner.Web.Features.Import.Presenters;
using ImportToPlanner.Web.Features.Import.Workflows;
using MudBlazor;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private void SetStatus(
        string? message,
        WorkflowStatusLevel level,
        string? referenceId = null,
        bool preserveGuidanceFlags = false,
        PlannerFailureMapping? failureSignals = null)
    {
        statusMessage = message;
        statusMessageLevel = level;
        statusReferenceId = string.IsNullOrWhiteSpace(message) ? null : referenceId;

        if (!preserveGuidanceFlags)
        {
            WorkflowState.IsUnsupportedAccount = false;
            WorkflowState.IsAdminConsentRequired = false;
            return;
        }

        if (failureSignals is { } mappedFailure)
        {
            ApplyWorkflowFailureSignals(mappedFailure);
        }
    }

    private FailurePresentation HandleUserSafeFailure(Exception exception, string operation, WorkflowStatusLevel severity)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var mappedFailure = PlannerFailureMessageMapper.FromException(exception, WorkflowState.ConsentResolution);
        var failurePresentation = FailureDiagnostics.RecordHandledFailure(
            Logger,
            exception,
            operation,
            mappedFailure.Category.ToString(),
            mappedFailure.UserMessage,
            severity == WorkflowStatusLevel.Error ? LogLevel.Error : LogLevel.Warning,
            tenantContext: WorkflowState.ActiveTenantContext,
            consentStatus: mappedFailure.ConsentStatus,
            failureCode: mappedFailure.DiagnosticCode);

        SetStatus(
            failurePresentation.UserMessage,
            severity,
            failurePresentation.ReferenceId,
            preserveGuidanceFlags: true,
            failureSignals: mappedFailure);
        return failurePresentation;
    }

    private void ApplyWorkflowFailureSignals(PlannerFailureMapping mappedFailure)
    {
        WorkflowState.IsUnsupportedAccount = mappedFailure.IsUnsupportedAccount;
        WorkflowState.IsAdminConsentRequired = mappedFailure.IsAdminConsentRequired;
    }

    private static Severity ToMudSeverity(WorkflowStatusLevel level)
    {
        return level switch
        {
            WorkflowStatusLevel.Info => Severity.Info,
            WorkflowStatusLevel.Success => Severity.Success,
            WorkflowStatusLevel.Warning => Severity.Warning,
            WorkflowStatusLevel.Error => Severity.Error,
            _ => Severity.Info,
        };
    }
}
