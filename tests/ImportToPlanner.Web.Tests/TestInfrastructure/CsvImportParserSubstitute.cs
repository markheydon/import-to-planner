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
                callInfo.ArgAt<CancellationToken>(1)));
    }

    public ICsvImportParser Instance { get; }

    public bool UseRealParser { get; set; }

    private Task<CsvParseResult> ParseAsync(string csvContent, bool ignoreExtraColumns, CancellationToken cancellationToken)
    {
        if (UseRealParser)
        {
            return innerParser.ParseAsync(csvContent, cancellationToken, ignoreExtraColumns);
        }

        return Task.FromResult(new CsvParseResult(
            [new CsvTaskRow(2, "Stub Task", null, null, null, null)],
            []));
    }
}
