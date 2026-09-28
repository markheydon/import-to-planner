using ImportToPlanner.Application.Import;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.Import.Workflows;

namespace ImportToPlanner.Web.Features.Import.Pages;

public partial class Home
{
    private const string DoNotImportOption = "__do_not_import__";

    private Dictionary<string, string?> MappingEditorAssignments { get; set; } = [];

    private ColumnMappingProposal? columnMappingProposal => WorkflowState.ColumnMappingProposal;

    private bool isColumnMappingConfirmed => WorkflowState.IsColumnMappingConfirmed;

    private bool showMappingEditor => WorkflowState.ShowMappingEditor;

    private bool showMappingPrivacyNote => WorkflowState.ShowMappingPrivacyNote;

    private bool showMappingSummary
        => columnMappingProposal is not null
           && !showMappingEditor
           && (columnMappingProposal.Status is ColumnMappingProposalStatus.Ready
               or ColumnMappingProposalStatus.NeedsConfirmation);

    private bool mappingBlocksPreview
        => columnMappingProposal is not null
           && (!isColumnMappingConfirmed
               || columnMappingProposal.Status is ColumnMappingProposalStatus.Conflict
                   or ColumnMappingProposalStatus.NeedsTaskName);

    private void OpenMappingEditor()
    {
        if (columnMappingProposal is null)
        {
            return;
        }

        MappingEditorAssignments = columnMappingProposal.SuggestedAssignments
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        WorkflowState.ShowMappingEditor = true;
    }

    private async Task ConfirmMappingFromEditorAsync()
    {
        if (isBusy || columnMappingProposal is null)
        {
            return;
        }

        isBusy = true;
        try
        {
            await WorkflowCoordinator.ConfirmColumnMappingAsync(
                WorkflowState,
                MappingEditorAssignments,
                CancellationToken.None);
            SetStatus("Column mapping confirmed.", WorkflowStatusLevel.Success);
            parseErrors.Clear();
            MaybeAdvanceViewedStep();
        }
        catch (InvalidOperationException exception)
        {
            parseErrors.Add(new ImportValidationError(0, "Mapping", exception.Message));
            SetStatus("Column mapping could not be confirmed.", WorkflowStatusLevel.Error);
        }
        finally
        {
            isBusy = false;
        }
    }

    private void DismissMappingPrivacyNote() => WorkflowState.ShowMappingPrivacyNote = false;

    private static string FormatImportFieldLabel(string fieldId)
        => ImportColumnFieldCatalog.FindByFieldId(fieldId)?.DisplayName ?? fieldId;

    private static IEnumerable<string> MappingSourceOptions(ColumnMappingProposal proposal)
    {
        yield return DoNotImportOption;
        foreach (var header in proposal.SourceHeaders)
        {
            yield return header;
        }
    }

    private static string FormatMappingSourceOption(string? value)
        => value == DoNotImportOption ? "Do not import" : value ?? string.Empty;

    private string? GetEditorAssignment(string fieldId)
    {
        if (MappingEditorAssignments.TryGetValue(fieldId, out var value))
        {
            return string.IsNullOrWhiteSpace(value) ? DoNotImportOption : value;
        }

        return DoNotImportOption;
    }

    private void SetEditorAssignment(string fieldId, string? selectedValue)
    {
        var assignments = MappingEditorAssignments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        assignments[fieldId] = selectedValue == DoNotImportOption ? null : selectedValue;
        MappingEditorAssignments = assignments;
    }
}
