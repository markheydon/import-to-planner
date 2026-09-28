using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Import;

/// <summary>
/// Single source of canonical CSV column definitions and aliases for import mapping.
/// </summary>
public static class ImportColumnFieldCatalog
{
    private static readonly ImportColumnFieldDefinition[] Fields =
    [
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.TaskName,
            DisplayName = "Task Name",
            CanonicalHeaders = ["Task Name"],
            Aliases = ["Title", "Subject", "Task", "Name", "TaskName", "Task title"],
            IsRequired = true,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.Description,
            DisplayName = "Description",
            CanonicalHeaders = ["Description"],
            Aliases = ["Notes", "Body", "Details", "Comments"],
            IsRequired = false,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.Priority,
            DisplayName = "Priority",
            CanonicalHeaders = ["Priority"],
            Aliases = ["Pri", "Importance"],
            IsRequired = false,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.Bucket,
            DisplayName = "Bucket",
            CanonicalHeaders = ["Bucket"],
            Aliases = ["Bucket Name", "BucketName"],
            IsRequired = false,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.Goal,
            DisplayName = "Goal",
            CanonicalHeaders = ["Goal"],
            Aliases = ["Objective", "Theme"],
            IsRequired = false,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.DueDate,
            DisplayName = "Due Date",
            CanonicalHeaders = ["Due Date"],
            Aliases = ["Due date", "DueDate", "Deadline"],
            IsRequired = false,
            IsEnabled = true,
        },
        new ImportColumnFieldDefinition
        {
            FieldId = ImportColumnFieldIds.AssignedTo,
            DisplayName = "Assigned To",
            CanonicalHeaders = ["Assigned To"],
            Aliases = ["Assignee", "Assignees", "Email"],
            IsRequired = false,
            IsEnabled = true,
        },
    ];

    /// <summary>
    /// Returns enabled field definitions in catalog order.
    /// </summary>
    public static IReadOnlyList<ImportColumnFieldDefinition> EnabledFields
        => Fields.Where(definition => definition.IsEnabled).ToArray();

    /// <summary>
    /// Finds a field definition by id.
    /// </summary>
    public static ImportColumnFieldDefinition? FindByFieldId(string fieldId)
        => Fields.FirstOrDefault(field => string.Equals(field.FieldId, fieldId, StringComparison.Ordinal));

    /// <summary>
    /// Matches a raw header to a field using canonical exact match, then alias normalised match.
    /// </summary>
    public static ImportColumnFieldDefinition? MatchHeader(string rawHeader)
    {
        if (string.IsNullOrWhiteSpace(rawHeader))
        {
            return null;
        }

        var trimmed = rawHeader.Trim();

        foreach (var field in EnabledFields)
        {
            if (field.CanonicalHeaders.Any(canonical =>
                    string.Equals(canonical.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return field;
            }
        }

        var normalisedHeader = ImportColumnHeaderNormalisation.NormaliseForMatch(trimmed);
        if (normalisedHeader.Length == 0)
        {
            return null;
        }

        foreach (var field in EnabledFields)
        {
            foreach (var alias in field.Aliases)
            {
                if (ImportColumnHeaderNormalisation.HeadersMatch(alias, trimmed))
                {
                    return field;
                }
            }

            foreach (var canonical in field.CanonicalHeaders)
            {
                if (ImportColumnHeaderNormalisation.HeadersMatch(canonical, trimmed))
                {
                    return field;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns whether the raw header is an exact canonical heading for the field (case-insensitive trim).
    /// </summary>
    public static bool IsExactCanonicalHeader(string fieldId, string rawHeader)
    {
        var field = FindByFieldId(fieldId);
        if (field is null || string.IsNullOrWhiteSpace(rawHeader))
        {
            return false;
        }

        var trimmed = rawHeader.Trim();
        return field.CanonicalHeaders.Any(canonical =>
            string.Equals(canonical.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
