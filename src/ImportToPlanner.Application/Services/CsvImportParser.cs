using CsvHelper;
using CsvHelper.Configuration;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using System.Globalization;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Parses CSV files into normalized Planner import rows.
/// </summary>
public sealed class CsvImportParser : ICsvImportParser
{
    private static readonly HashSet<string> SupportedHeaders =
    [
        "Task Name",
        "Description",
        "Priority",
        "Bucket",
        "Goal",
    ];

    /// <inheritdoc/>
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV file is empty.")]));
        }

        using var reader = new StringReader(csvContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var csv = new CsvReader(reader, config);

        var errors = new List<ImportValidationError>();
        var rows = new List<CsvTaskRow>();

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV header row is missing.")]));
        }

        ValidateHeaders(csv.HeaderRecord, errors);

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowNumber = csv.Parser.Row;
            var taskName = csv.GetField("Task Name")?.Trim();
            var description = Normalize(csv.GetField("Description"));
            var priorityText = Normalize(csv.GetField("Priority"));
            var bucket = Normalize(csv.GetField("Bucket"));
            var goal = Normalize(csv.GetField("Goal"));

            if (string.IsNullOrWhiteSpace(taskName))
            {
                errors.Add(new ImportValidationError(rowNumber, "Task Name", "Task Name is required."));
                continue;
            }

            if (!TryParsePriority(priorityText, out var priority))
            {
                errors.Add(new ImportValidationError(
                    rowNumber,
                    "Priority",
                    "Priority must be empty, a value 0-10, or one of: Urgent, Important, Medium, Low."));
                continue;
            }

            rows.Add(new CsvTaskRow(rowNumber, taskName, description, priority, bucket, goal));
        }

        return Task.FromResult(new CsvParseResult(rows, errors));
    }

    private static void ValidateHeaders(IEnumerable<string> headers, List<ImportValidationError> errors)
    {
        var normalized = headers
            .Select(header => header.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!normalized.Contains("Task Name"))
        {
            errors.Add(new ImportValidationError(0, "Task Name", "Task Name column is required."));
        }

        foreach (var header in normalized)
        {
            if (!SupportedHeaders.Contains(header))
            {
                errors.Add(new ImportValidationError(0, header, "Unexpected column."));
            }
        }
    }

    private static bool TryParsePriority(string? priorityText, out int? priority)
    {
        priority = null;

        if (string.IsNullOrWhiteSpace(priorityText))
        {
            return true;
        }

        if (int.TryParse(priorityText, out var numeric))
        {
            if (numeric is >= 0 and <= 10)
            {
                priority = numeric;
                return true;
            }

            return false;
        }

        var normalized = priorityText.Trim().ToLowerInvariant();
        priority = normalized switch
        {
            "urgent" => 1,
            "important" => 3,
            "medium" => 5,
            "low" => 9,
            _ => null,
        };

        return priority is not null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
