using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Infrastructure.Graph.Import;

/// <summary>
/// Parses CSV files into normalised import rows.
/// </summary>
public sealed class CsvImportParser : ICsvImportParser
{
    private const string TaskNameHeader = "task name";
    private const string DescriptionHeader = "description";
    private const string PriorityHeader = "priority";
    private const string BucketHeader = "bucket";
    private const string GoalHeader = "goal";
    private const string DueDateHeader = "due date";
    private const int MaxDescriptionLength = 32_768;
    private const char Utf8Bom = '\uFEFF';

    private static readonly HashSet<string> SupportedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Task Name",
        "Description",
        "Priority",
        "Bucket",
        "Goal",
        "Due Date",
    };

    /// <inheritdoc/>
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalisedContent = StripLeadingBom(csvContent);

        if (string.IsNullOrWhiteSpace(normalisedContent))
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV file is empty.")]));
        }

        var headerLine = GetFirstHeaderLine(normalisedContent);
        var separatorDetection = DetectFieldSeparator(headerLine);

        if (separatorDetection == FieldSeparatorDetection.Ambiguous)
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(
                    0,
                    "File",
                    "The field separator could not be determined. Save the file as comma-separated UTF-8 and upload again.")]));
        }

        if (separatorDetection == FieldSeparatorDetection.Unsupported)
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(
                    0,
                    "File",
                    "This separator is not supported. Save the file as comma-separated UTF-8 and upload again.")]));
        }

        var delimiter = separatorDetection == FieldSeparatorDetection.Semicolon ? ";" : ",";

        using var reader = new StringReader(normalisedContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty,
        };

        using var csv = new CsvReader(reader, config);

        var errors = new List<ImportValidationError>();
        var rows = new List<CsvTaskRow>();

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV header row is missing.")]));
        }

        ValidateHeaders(csv.HeaderRecord, errors, ignoreExtraColumns);

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowNumber = csv.Parser.Row;
            var taskName = csv.GetField(TaskNameHeader)?.Trim();
            var description = Normalise(csv.GetField(DescriptionHeader));
            var priorityText = Normalise(csv.GetField(PriorityHeader));
            var bucket = Normalise(csv.GetField(BucketHeader));
            var goal = Normalise(csv.GetField(GoalHeader));
            var dueDateText = Normalise(csv.GetField(DueDateHeader));

            if (string.IsNullOrWhiteSpace(taskName))
            {
                errors.Add(new ImportValidationError(rowNumber, "Task Name", "Task Name is required."));
                continue;
            }

            if (description is not null && description.Length > MaxDescriptionLength)
            {
                errors.Add(new ImportValidationError(
                    rowNumber,
                    "Description",
                    $"Description must be {MaxDescriptionLength:N0} characters or fewer."));
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

            if (!TryParseDueDate(dueDateText, out var dueDate))
            {
                errors.Add(new ImportValidationError(
                    rowNumber,
                    "Due Date",
                    "Due Date must be empty or a recognised date (ISO yyyy-MM-dd or a UK day/month/year date)."));
                continue;
            }

            rows.Add(new CsvTaskRow(rowNumber, taskName, description, priority, bucket, goal, dueDate));
        }

        return Task.FromResult(new CsvParseResult(rows, errors));
    }

    /// <summary>
    /// Strips a leading UTF-8 BOM when present. Upload paths may already remove the BOM via
    /// <see cref="StreamReader"/>, but callers can still pass raw text containing U+FEFF.
    /// </summary>
    private static string StripLeadingBom(string? csvContent)
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            return string.Empty;
        }

        if (csvContent[0] == Utf8Bom)
        {
            return csvContent[1..];
        }

        return csvContent;
    }

    private static string GetFirstHeaderLine(string csvContent)
    {
        var inQuotes = false;

        for (var index = 0; index < csvContent.Length; index++)
        {
            var character = csvContent[index];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < csvContent.Length && csvContent[index + 1] == '"')
                    {
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }

                continue;
            }

            if (character == '"')
            {
                inQuotes = true;
                continue;
            }

            if (character == '\r' || character == '\n')
            {
                return csvContent[..index];
            }
        }

        return csvContent;
    }

    private static FieldSeparatorDetection DetectFieldSeparator(string headerLine)
    {
        var unquotedCommas = 0;
        var unquotedSemicolons = 0;
        var unquotedTabs = 0;
        var unquotedPipes = 0;
        var inQuotes = false;

        for (var index = 0; index < headerLine.Length; index++)
        {
            var character = headerLine[index];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < headerLine.Length && headerLine[index + 1] == '"')
                    {
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }

                continue;
            }

            if (character == '"')
            {
                inQuotes = true;
                continue;
            }

            switch (character)
            {
                case ',':
                    unquotedCommas++;
                    break;
                case ';':
                    unquotedSemicolons++;
                    break;
                case '\t':
                    unquotedTabs++;
                    break;
                case '|':
                    unquotedPipes++;
                    break;
            }
        }

        if (unquotedCommas > 0 && unquotedSemicolons > 0)
        {
            return FieldSeparatorDetection.Ambiguous;
        }

        if (unquotedSemicolons > 0 && unquotedCommas == 0)
        {
            return FieldSeparatorDetection.Semicolon;
        }

        if (unquotedCommas > 0 && unquotedSemicolons == 0)
        {
            return FieldSeparatorDetection.Comma;
        }

        if (unquotedTabs > 0 || unquotedPipes > 0)
        {
            return FieldSeparatorDetection.Unsupported;
        }

        return FieldSeparatorDetection.SingleColumn;
    }

    private static void ValidateHeaders(IEnumerable<string> headers, List<ImportValidationError> errors, bool ignoreExtraColumns = false)
    {
        var normalised = headers
            .Select(header => header.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!normalised.Contains("Task Name"))
        {
            errors.Add(new ImportValidationError(0, "Task Name", "Task Name column is required."));
        }

        if (!ignoreExtraColumns)
        {
            foreach (var header in normalised)
            {
                if (!SupportedHeaders.Contains(header))
                {
                    errors.Add(new ImportValidationError(0, header, "Unexpected column."));
                }
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

        var normalised = priorityText.Trim().ToLowerInvariant();
        priority = normalised switch
        {
            "urgent" => 1,
            "important" => 3,
            "medium" => 5,
            "low" => 9,
            _ => null,
        };

        return priority is not null;
    }

    private static readonly string[] DueDateFormats =
    [
        "yyyy-MM-dd",
        "d/M/yyyy",
        "dd/MM/yyyy",
        "d/M/yy",
        "dd/MM/yy",
        "d-M-yyyy",
        "dd-MM-yyyy",
        "d-M-yy",
        "dd-MM-yy",
    ];

    private static readonly CultureInfo DueDateCulture = CreateDueDateCulture();

    private static CultureInfo CreateDueDateCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.Calendar.TwoDigitYearMax = 2099;
        return culture;
    }

    private static bool TryParseDueDate(string? dueDateText, out DateOnly? dueDate)
    {
        dueDate = null;

        if (string.IsNullOrWhiteSpace(dueDateText))
        {
            return true;
        }

        if (DateOnly.TryParseExact(
                dueDateText.Trim(),
                DueDateFormats,
                DueDateCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            dueDate = parsed;
            return true;
        }

        return false;
    }

    private static string? Normalise(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private enum FieldSeparatorDetection
    {
        Comma,
        Semicolon,
        SingleColumn,
        Ambiguous,
        Unsupported,
    }
}
