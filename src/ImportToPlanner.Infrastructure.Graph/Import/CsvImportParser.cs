using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Import;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Infrastructure.Graph.Import;

/// <summary>
/// Parses CSV files into normalised import rows.
/// </summary>
public sealed class CsvImportParser : ICsvImportParser
{
    private const int MaxDescriptionLength = 32_768;
    private const char Utf8Bom = '\uFEFF';

    private readonly ICsvColumnMappingService columnMappingService;

    /// <summary>
    /// Creates a parser using the default in-process mapping service for legacy parse calls.
    /// </summary>
    public CsvImportParser()
        : this(new CsvColumnMappingService())
    {
    }

    /// <summary>
    /// Creates a parser with the supplied mapping service.
    /// </summary>
    public CsvImportParser(ICsvColumnMappingService columnMappingService)
    {
        ArgumentNullException.ThrowIfNull(columnMappingService);
        this.columnMappingService = columnMappingService;
    }

    /// <inheritdoc />
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var peek = PeekHeadersInternal(csvContent);
        if (peek.HasErrors)
        {
            return Task.FromResult(new CsvParseResult([], peek.ValidationErrors));
        }

        var proposal = columnMappingService.BuildProposal(peek.Headers, savedForLayout: null);
        if (proposal.Status is ColumnMappingProposalStatus.NeedsTaskName)
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(0, "Task Name", "Task Name column is required.")]));
        }

        if (proposal.Status is ColumnMappingProposalStatus.Conflict)
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(0, "Mapping", "Column mapping conflict. Map each import field to a single source column.")]));
        }

        try
        {
            var mapping = columnMappingService.ToConfirmedMapping(proposal, proposal.SuggestedAssignments);
            return ParseAsync(csvContent, mapping, ignoreExtraColumns, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(0, "Mapping", exception.Message)]));
        }
    }

    /// <inheritdoc />
    public Task<CsvHeaderPeekResult> PeekHeadersAsync(string csvContent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PeekHeadersInternal(csvContent));
    }

    /// <inheritdoc />
    public Task<CsvParseResult> ParseAsync(
        string csvContent,
        CsvColumnMapping columnMapping,
        bool ignoreExtraColumns,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(columnMapping);

        var normalisedContent = StripLeadingBom(csvContent);

        if (string.IsNullOrWhiteSpace(normalisedContent))
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV file is empty.")]));
        }

        if (!columnMapping.Assignments.ContainsKey(ImportColumnFieldIds.TaskName))
        {
            return Task.FromResult(new CsvParseResult(
                [],
                [new ImportValidationError(0, "Task Name", "Task Name column is required.")]));
        }

        var readerResult = TryCreateReader(normalisedContent);
        if (readerResult.Error is not null)
        {
            return Task.FromResult(new CsvParseResult([], [readerResult.Error]));
        }

        using var reader = readerResult.Reader!;
        var config = readerResult.Configuration!;
        using var csv = new CsvReader(reader, config);

        var errors = new List<ImportValidationError>();
        var rows = new List<CsvTaskRow>();

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            return Task.FromResult(new CsvParseResult([], [new ImportValidationError(0, "File", "CSV header row is missing.")]));
        }

        ValidateMappedHeaders(csv.HeaderRecord, columnMapping, errors, ignoreExtraColumns);

        var taskNameHeader = ToCsvHelperHeader(columnMapping.Assignments[ImportColumnFieldIds.TaskName]);
        var descriptionHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.Description);
        var priorityHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.Priority);
        var bucketHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.Bucket);
        var goalHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.Goal);
        var dueDateHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.DueDate);
        var assignedToHeader = TryGetMappedHeader(columnMapping, ImportColumnFieldIds.AssignedTo);

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowNumber = csv.Parser.Row;
            var taskName = csv.GetField(taskNameHeader)?.Trim();
            var description = Normalise(GetFieldOrNull(csv, descriptionHeader));
            var priorityText = Normalise(GetFieldOrNull(csv, priorityHeader));
            var bucket = Normalise(GetFieldOrNull(csv, bucketHeader));
            var goal = Normalise(GetFieldOrNull(csv, goalHeader));
            var dueDateText = Normalise(GetFieldOrNull(csv, dueDateHeader));
            var assignedToText = GetFieldOrNull(csv, assignedToHeader);

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

            rows.Add(new CsvTaskRow(
                rowNumber,
                taskName,
                description,
                priority,
                bucket,
                goal,
                dueDate,
                ParseAssigneeAddresses(assignedToText)));
        }

        return Task.FromResult(new CsvParseResult(rows, errors));
    }

    private static CsvHeaderPeekResult PeekHeadersInternal(string? csvContent)
    {
        var normalisedContent = StripLeadingBom(csvContent);

        if (string.IsNullOrWhiteSpace(normalisedContent))
        {
            return new CsvHeaderPeekResult
            {
                Headers = [],
                ValidationErrors = [new ImportValidationError(0, "File", "CSV file is empty.")],
            };
        }

        var readerResult = TryCreateReader(normalisedContent);
        if (readerResult.Error is not null)
        {
            return new CsvHeaderPeekResult
            {
                Headers = [],
                ValidationErrors = [readerResult.Error],
            };
        }

        using var reader = readerResult.Reader!;
        var config = readerResult.Configuration!;
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            return new CsvHeaderPeekResult
            {
                Headers = [],
                ValidationErrors = [new ImportValidationError(0, "File", "CSV header row is missing.")],
            };
        }

        var headers = csv.HeaderRecord.Select(header => header.Trim()).ToArray();
        var duplicateErrors = FindDuplicateNormalisedHeaderErrors(headers);
        if (duplicateErrors.Count > 0)
        {
            return new CsvHeaderPeekResult
            {
                Headers = headers,
                ValidationErrors = duplicateErrors,
            };
        }

        return new CsvHeaderPeekResult
        {
            Headers = headers,
            ValidationErrors = [],
        };
    }

    private static List<ImportValidationError> FindDuplicateNormalisedHeaderErrors(IReadOnlyList<string> headers)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<ImportValidationError>();

        foreach (var header in headers)
        {
            var normalised = ImportColumnHeaderNormalisation.NormaliseForMatch(header);
            if (normalised.Length == 0)
            {
                continue;
            }

            if (seen.TryGetValue(normalised, out var firstHeader)
                && !string.Equals(firstHeader, header, StringComparison.Ordinal))
            {
                errors.Add(new ImportValidationError(
                    0,
                    "Mapping",
                    $"Duplicate column heading detected: '{header}' matches '{firstHeader}' after normalisation."));
            }
            else
            {
                seen[normalised] = header;
            }
        }

        return errors;
    }

    private static void ValidateMappedHeaders(
        IEnumerable<string> headers,
        CsvColumnMapping columnMapping,
        List<ImportValidationError> errors,
        bool ignoreExtraColumns)
    {
        var rawHeaders = headers.Select(header => header.Trim()).ToHashSet(StringComparer.Ordinal);
        var mappedSources = columnMapping.Assignments.Values.ToHashSet(StringComparer.Ordinal);

        foreach (var mappedSource in mappedSources)
        {
            if (!rawHeaders.Contains(mappedSource))
            {
                errors.Add(new ImportValidationError(0, "Mapping", $"Mapped column '{mappedSource}' was not found in the header row."));
            }
        }

        if (ignoreExtraColumns)
        {
            return;
        }

        foreach (var header in rawHeaders)
        {
            if (!mappedSources.Contains(header))
            {
                errors.Add(new ImportValidationError(0, header, "Unexpected column."));
            }
        }
    }

    private static string? TryGetMappedHeader(CsvColumnMapping columnMapping, string fieldId)
        => columnMapping.Assignments.TryGetValue(fieldId, out var sourceHeader)
            ? ToCsvHelperHeader(sourceHeader)
            : null;

    private static string ToCsvHelperHeader(string rawHeader)
        => rawHeader.Trim().ToLowerInvariant();

    private static string? GetFieldOrNull(CsvReader csv, string? csvHelperHeader)
        => csvHelperHeader is null ? null : csv.GetField(csvHelperHeader);

    private sealed record CsvReaderOpenResult(StringReader? Reader, CsvConfiguration? Configuration, ImportValidationError? Error);

    private static CsvReaderOpenResult TryCreateReader(string normalisedContent)
    {
        var headerLine = GetFirstHeaderLine(normalisedContent);
        var separatorDetection = DetectFieldSeparator(headerLine);

        if (separatorDetection == FieldSeparatorDetection.Ambiguous)
        {
            return new CsvReaderOpenResult(
                null,
                null,
                new ImportValidationError(
                    0,
                    "File",
                    "The field separator could not be determined. Save the file as comma-separated UTF-8 and upload again."));
        }

        if (separatorDetection == FieldSeparatorDetection.Unsupported)
        {
            return new CsvReaderOpenResult(
                null,
                null,
                new ImportValidationError(
                    0,
                    "File",
                    "This separator is not supported. Save the file as comma-separated UTF-8 and upload again."));
        }

        var delimiter = separatorDetection == FieldSeparatorDetection.Semicolon ? ";" : ",";
        var reader = new StringReader(normalisedContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty,
        };

        return new CsvReaderOpenResult(reader, config, null);
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

    private static List<string> ParseAssigneeAddresses(string? assignedToText)
    {
        if (string.IsNullOrWhiteSpace(assignedToText))
        {
            return [];
        }

        var addresses = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in assignedToText.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                addresses.Add(trimmed);
            }
        }

        return addresses;
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
