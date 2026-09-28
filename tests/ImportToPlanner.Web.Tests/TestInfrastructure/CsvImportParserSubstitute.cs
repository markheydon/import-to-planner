using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Infrastructure.Graph.Import;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CsvImportParserSubstitute
{
    private readonly CsvImportParser innerParser = new();

    public CsvImportParserSubstitute()
    {
        Instance = Substitute.For<ICsvImportParser>();
        Instance.ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(callInfo => ParseAsync(
                callInfo.ArgAt<string>(0),
                callInfo.ArgAt<bool>(2),
                columnMapping: null,
                callInfo.ArgAt<CancellationToken>(1)));

        Instance.ParseAsync(Arg.Any<string>(), Arg.Any<CsvColumnMapping>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ParseAsync(
                callInfo.ArgAt<string>(0),
                callInfo.ArgAt<bool>(2),
                callInfo.ArgAt<CsvColumnMapping>(1),
                callInfo.ArgAt<CancellationToken>(3)));

        Instance.PeekHeadersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => PeekHeadersAsync(
                callInfo.ArgAt<string>(0),
                callInfo.ArgAt<CancellationToken>(1)));
    }

    public ICsvImportParser Instance { get; }

    public bool UseRealParser { get; set; }

    private Task<CsvParseResult> ParseAsync(
        string csvContent,
        bool ignoreExtraColumns,
        CsvColumnMapping? columnMapping,
        CancellationToken cancellationToken)
    {
        if (UseRealParser)
        {
            if (columnMapping is null)
            {
                return innerParser.ParseAsync(csvContent, cancellationToken, ignoreExtraColumns);
            }

            return innerParser.ParseAsync(csvContent, columnMapping, ignoreExtraColumns, cancellationToken);
        }

        return Task.FromResult(new CsvParseResult(
            [new CsvTaskRow(2, "Stub Task", null, null, null, null, null, [])],
            []));
    }

    private Task<CsvHeaderPeekResult> PeekHeadersAsync(string csvContent, CancellationToken cancellationToken)
    {
        if (UseRealParser)
        {
            return innerParser.PeekHeadersAsync(csvContent, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return Task.FromResult(new CsvHeaderPeekResult
            {
                Headers = [],
                ValidationErrors = [new ImportValidationError(0, "File", "CSV file is empty.")],
            });
        }

        var headerLine = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
        var headers = headerLine.Split(',', StringSplitOptions.TrimEntries).ToArray();

        return Task.FromResult(new CsvHeaderPeekResult
        {
            Headers = headers,
            ValidationErrors = [],
        });
    }
}
