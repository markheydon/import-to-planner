using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Import;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Application policy for CSV column mapping proposals and confirmation.
/// </summary>
public sealed class CsvColumnMappingService : ICsvColumnMappingService
{
    /// <inheritdoc />
    public ColumnMappingProposal BuildProposal(IReadOnlyList<string> rawHeaders, SavedLayoutMapping? savedForLayout)
    {
        ArgumentNullException.ThrowIfNull(rawHeaders);

        var headers = rawHeaders.Select(header => header.Trim()).ToArray();
        var layoutSignature = ImportColumnHeaderNormalisation.BuildLayoutSignature(headers);
        var fieldToSource = new Dictionary<string, string>(StringComparer.Ordinal);
        var skippedSources = new HashSet<string>(StringComparer.Ordinal);
        var fieldCompetitors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (savedForLayout is not null
            && string.Equals(savedForLayout.LayoutSignature, layoutSignature, StringComparison.Ordinal))
        {
            foreach (var (sourceHeader, fieldId) in savedForLayout.Assignments)
            {
                if (!headers.Contains(sourceHeader, StringComparer.Ordinal))
                {
                    continue;
                }

                if (fieldId is null)
                {
                    skippedSources.Add(sourceHeader);
                    continue;
                }

                TryAssign(fieldToSource, fieldCompetitors, fieldId, sourceHeader);
            }
        }

        foreach (var header in headers)
        {
            if (skippedSources.Contains(header))
            {
                continue;
            }

            if (fieldToSource.Values.Contains(header, StringComparer.Ordinal))
            {
                continue;
            }

            var matchedField = ImportColumnFieldCatalog.MatchHeader(header);
            if (matchedField is null)
            {
                continue;
            }

            TryAssign(fieldToSource, fieldCompetitors, matchedField.FieldId, header);
        }

        var conflicts = fieldCompetitors
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => new ColumnMappingConflict(pair.Key, pair.Value.OrderBy(source => source, StringComparer.Ordinal).ToArray()))
            .ToArray();

        var suggested = BuildSuggestedAssignments(fieldToSource);
        var status = ResolveStatus(suggested, conflicts, fieldToSource);

        return new ColumnMappingProposal
        {
            LayoutSignature = layoutSignature,
            SourceHeaders = headers,
            SuggestedAssignments = suggested,
            Status = status,
            Conflicts = conflicts,
        };
    }

    /// <inheritdoc />
    public CsvColumnMapping ToConfirmedMapping(
        ColumnMappingProposal proposal,
        IReadOnlyDictionary<string, string?> userAssignments)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(userAssignments);

        var assignments = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var field in ImportColumnFieldCatalog.EnabledFields)
        {
            if (!userAssignments.TryGetValue(field.FieldId, out var sourceHeader))
            {
                if (proposal.SuggestedAssignments.TryGetValue(field.FieldId, out var suggested)
                    && !string.IsNullOrWhiteSpace(suggested))
                {
                    sourceHeader = suggested;
                }
            }

            if (string.IsNullOrWhiteSpace(sourceHeader))
            {
                continue;
            }

            if (!proposal.SourceHeaders.Contains(sourceHeader, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Source header '{sourceHeader}' is not present in the uploaded file.");
            }

            assignments[field.FieldId] = sourceHeader;
        }

        if (!assignments.ContainsKey(ImportColumnFieldIds.TaskName))
        {
            throw new InvalidOperationException("Task Name must be mapped before import.");
        }

        var sourceToField = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (fieldId, sourceHeader) in assignments)
        {
            if (sourceToField.TryGetValue(sourceHeader, out var existingFieldId))
            {
                var existingLabel = ImportColumnFieldCatalog.FindByFieldId(existingFieldId)?.DisplayName ?? existingFieldId;
                var duplicateLabel = ImportColumnFieldCatalog.FindByFieldId(fieldId)?.DisplayName ?? fieldId;
                throw new InvalidOperationException(
                    $"Source column '{sourceHeader}' cannot map to both {existingLabel} and {duplicateLabel}. Choose one import field per column.");
            }

            sourceToField[sourceHeader] = fieldId;
        }

        return new CsvColumnMapping
        {
            LayoutSignature = proposal.LayoutSignature,
            Assignments = assignments,
        };
    }

    private static void TryAssign(
        Dictionary<string, string> fieldToSource,
        Dictionary<string, HashSet<string>> fieldCompetitors,
        string fieldId,
        string sourceHeader)
    {
        if (!fieldCompetitors.TryGetValue(fieldId, out var competitors))
        {
            competitors = new HashSet<string>(StringComparer.Ordinal);
            fieldCompetitors[fieldId] = competitors;
        }

        competitors.Add(sourceHeader);

        if (!fieldToSource.ContainsKey(fieldId))
        {
            fieldToSource[fieldId] = sourceHeader;
        }
    }

    private static Dictionary<string, string?> BuildSuggestedAssignments(Dictionary<string, string> fieldToSource)
    {
        var suggested = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var field in ImportColumnFieldCatalog.EnabledFields)
        {
            suggested[field.FieldId] = fieldToSource.TryGetValue(field.FieldId, out var source)
                ? source
                : null;
        }

        return suggested;
    }

    private static ColumnMappingProposalStatus ResolveStatus(
        IReadOnlyDictionary<string, string?> suggested,
        ColumnMappingConflict[] conflicts,
        Dictionary<string, string> fieldToSource)
    {
        if (conflicts.Length > 0)
        {
            return ColumnMappingProposalStatus.Conflict;
        }

        if (!fieldToSource.ContainsKey(ImportColumnFieldIds.TaskName)
            && string.IsNullOrWhiteSpace(suggested.GetValueOrDefault(ImportColumnFieldIds.TaskName)))
        {
            return ColumnMappingProposalStatus.NeedsTaskName;
        }

        var usedAlias = fieldToSource.Any(pair =>
            !ImportColumnFieldCatalog.IsExactCanonicalHeader(pair.Key, pair.Value));

        return usedAlias
            ? ColumnMappingProposalStatus.NeedsConfirmation
            : ColumnMappingProposalStatus.Ready;
    }
}
