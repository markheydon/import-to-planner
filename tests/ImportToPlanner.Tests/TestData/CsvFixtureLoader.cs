namespace ImportToPlanner.Tests.TestData;

internal static class CsvFixtureLoader
{
    private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Load(string fixtureFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureFileName);
        var filePath = Path.Combine(FixturesDirectory, fixtureFileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV fixture was not found: {fixtureFileName}", filePath);
        }

        return File.ReadAllText(filePath);
    }
}
