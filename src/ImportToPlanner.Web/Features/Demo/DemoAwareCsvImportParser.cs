using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Application.Models;
namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Returns synthetic CSV parse results while demonstration mode is active.
/// </summary>
public sealed class DemoAwareCsvImportParser(
    ICsvImportParser liveParser,
    IDemoModeSession demoModeSession) : ICsvImportParser
{
    /// <inheritdoc />
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.SampleCsvParseResult)
            : liveParser.ParseAsync(csvContent, cancellationToken, ignoreExtraColumns);

    /// <inheritdoc />
    public Task<CsvHeaderPeekResult> PeekHeadersAsync(string csvContent, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.SampleHeaderPeek)
            : liveParser.PeekHeadersAsync(csvContent, cancellationToken);

    /// <inheritdoc />
    public Task<CsvParseResult> ParseAsync(
        string csvContent,
        CsvColumnMapping columnMapping,
        bool ignoreExtraColumns,
        CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.SampleCsvParseResult)
            : liveParser.ParseAsync(csvContent, columnMapping, ignoreExtraColumns, cancellationToken);
}
