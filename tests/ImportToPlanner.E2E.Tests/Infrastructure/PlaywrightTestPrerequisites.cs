using Microsoft.Playwright;

namespace ImportToPlanner.E2E.Tests.Infrastructure;

internal static class PlaywrightTestPrerequisites
{
    public static async Task<IBrowser> LaunchChromiumAsync(IPlaywright playwright)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync();
        }
        catch (PlaywrightException exception) when (IsMissingBrowserExecutable(exception))
        {
            Assert.Skip(
                "Playwright browsers are not installed. Run: pwsh tests/ImportToPlanner.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium");
            throw;
        }
    }

    private static bool IsMissingBrowserExecutable(PlaywrightException exception)
        => exception.Message.Contains("Executable doesn't exist", StringComparison.Ordinal);
}
