using ImportToPlanner.Application.Import;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Tests;

public sealed class CsvColumnMappingServiceTests
{
    private readonly CsvColumnMappingService service = new();

    [Fact]
    public void NormaliseForMatch_TreatsSpacingAndPunctuationAsEquivalent()
    {
        var left = ImportColumnHeaderNormalisation.NormaliseForMatch("Task Name");
        var right = ImportColumnHeaderNormalisation.NormaliseForMatch("TaskName");

        Assert.Equal(left, right);
    }

    [Fact]
    public void BuildLayoutSignature_TaskNameAndTaskNameVariant_Match()
    {
        var signatureA = ImportColumnHeaderNormalisation.BuildLayoutSignature(["Task Name", "Description"]);
        var signatureB = ImportColumnHeaderNormalisation.BuildLayoutSignature(["TaskName", "Description"]);

        Assert.Equal(signatureA, signatureB);
    }

    [Fact]
    public void BuildProposal_WithAliasHeaders_SuggestsMappedFields()
    {
        var proposal = service.BuildProposal(["Title", "Notes"], savedForLayout: null);

        Assert.Equal("Title", proposal.SuggestedAssignments[ImportColumnFieldIds.TaskName]);
        Assert.Equal("Notes", proposal.SuggestedAssignments[ImportColumnFieldIds.Description]);
        Assert.Equal(ColumnMappingProposalStatus.NeedsConfirmation, proposal.Status);
    }

    [Fact]
    public void BuildProposal_WithoutTaskName_ReturnsNeedsTaskName()
    {
        var proposal = service.BuildProposal(["Description", "Priority"], savedForLayout: null);

        Assert.Equal(ColumnMappingProposalStatus.NeedsTaskName, proposal.Status);
        Assert.Null(proposal.SuggestedAssignments[ImportColumnFieldIds.TaskName]);
    }

    [Fact]
    public void MatchHeader_Status_DoesNotMapToBucket()
    {
        var matched = ImportColumnFieldCatalog.MatchHeader("Status");

        Assert.Null(matched);
    }

    [Fact]
    public void BuildProposal_WithStatusColumn_DoesNotAssignBucket()
    {
        var proposal = service.BuildProposal(["Task Name", "Status"], savedForLayout: null);

        Assert.Null(proposal.SuggestedAssignments[ImportColumnFieldIds.Bucket]);
    }

    [Fact]
    public void BuildProposal_WithCompetingPriorityAliases_ReturnsConflict()
    {
        var proposal = service.BuildProposal(["Task Name", "Pri", "Importance"], savedForLayout: null);

        Assert.Equal(ColumnMappingProposalStatus.Conflict, proposal.Status);
        Assert.Contains(proposal.Conflicts, conflict => conflict.FieldId == ImportColumnFieldIds.Priority);
    }

    [Fact]
    public void BuildProposal_WithCompetingTaskNameAliases_ReturnsConflict()
    {
        var proposal = service.BuildProposal(["Title", "Task"], savedForLayout: null);

        Assert.Equal(ColumnMappingProposalStatus.Conflict, proposal.Status);
        Assert.Contains(proposal.Conflicts, conflict => conflict.FieldId == ImportColumnFieldIds.TaskName);
    }

    [Fact]
    public void BuildProposal_WithSavedLayout_AppliesSavedAssignmentsFirst()
    {
        var headers = new[] { "Task Name", "Notes", "Extra" };
        var signature = ImportColumnHeaderNormalisation.BuildLayoutSignature(headers);
        var saved = new SavedLayoutMapping
        {
            LayoutSignature = signature,
            Assignments = new Dictionary<string, string?>
            {
                ["Notes"] = ImportColumnFieldIds.Description,
                ["Extra"] = null,
            },
            UpdatedUtc = DateTimeOffset.UtcNow,
        };

        var proposal = service.BuildProposal(headers, saved);

        Assert.Equal("Notes", proposal.SuggestedAssignments[ImportColumnFieldIds.Description]);
        Assert.Equal(ColumnMappingProposalStatus.NeedsConfirmation, proposal.Status);
    }

    [Fact]
    public void ToConfirmedMapping_WithoutTaskName_Throws()
    {
        var proposal = service.BuildProposal(["Description"], savedForLayout: null);
        var assignments = proposal.SuggestedAssignments.ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.Throws<InvalidOperationException>(() => service.ToConfirmedMapping(proposal, assignments));
    }

    [Fact]
    public void ToConfirmedMapping_WithTaskName_ReturnsMapping()
    {
        var proposal = service.BuildProposal(["Task Name"], savedForLayout: null);
        var mapping = service.ToConfirmedMapping(proposal, proposal.SuggestedAssignments);

        Assert.Equal("Task Name", mapping.Assignments[ImportColumnFieldIds.TaskName]);
    }

    [Fact]
    public void ToConfirmedMapping_WithDuplicateSourceColumn_Throws()
    {
        var proposal = service.BuildProposal(["Task Name", "Notes"], savedForLayout: null);
        var assignments = proposal.SuggestedAssignments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        assignments[ImportColumnFieldIds.Description] = "Task Name";

        var exception = Assert.Throws<InvalidOperationException>(() => service.ToConfirmedMapping(proposal, assignments));

        Assert.Contains("Task Name", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cannot map", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
