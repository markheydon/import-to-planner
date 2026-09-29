using ImportToPlanner.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace ImportToPlanner.E2E.Tests;

/// <summary>
/// Opt-in capture of demonstration-mode workflow screenshots for the public Hugo site.
/// Set <c>CAPTURE_DEMO_WORKFLOW_SCREENSHOTS=1</c> and run via <c>scripts/capture-demo-workflow-screenshots.sh</c>.
/// </summary>
[Collection(nameof(BrowserE2ETests))]
public sealed class DemoWorkflowScreenshotCaptureTests
{
    private const string CaptureEnvironmentVariable = "CAPTURE_DEMO_WORKFLOW_SCREENSHOTS";

    [Fact]
    public async Task Capture_import_workflow_screenshots_under_demo_mode()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(CaptureEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        var outputDirectory = ResolveScreenshotOutputDirectory();
        Directory.CreateDirectory(outputDirectory);

        await using var factory = await ImportToPlannerWebApplicationFactory.StartForDemoScreenshotCaptureAsync(
            TestContext.Current.CancellationToken);
        var baseAddress = factory.ServerBaseAddress;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await PlaywrightTestPrerequisites.LaunchChromiumAsync(playwright);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(new Uri(baseAddress, "/e2e/sign-in").ToString());
        await page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 15000 });
        await page.GetByText("synthetic and Microsoft Graph", new() { Exact = false })
            .WaitForAsync(new() { Timeout = 15000 });

        var workingPane = page.Locator(".wizard-working-pane");
        await workingPane.WaitForAsync(new() { Timeout = 15000 });

        await workingPane.GetByRole(AriaRole.Button, new() { Name = "Refresh locations" }).ClickAsync();
        await CapturePaneAsync(page, outputDirectory, "step-1-select-location.png");

        await ActivateStepperStepAsync(page, "Select plan");
        await CapturePaneAsync(page, outputDirectory, "step-2-select-plan.png");

        await ActivateStepperStepAsync(page, "Upload CSV");
        await CapturePaneAsync(page, outputDirectory, "step-3-upload-csv.png");

        await ActivateStepperStepAsync(page, "Preview and confirm");
        await CapturePaneAsync(page, outputDirectory, "step-4-preview-and-confirm.png");

        await ActivateStepperStepAsync(page, "Report");
        await CapturePaneAsync(page, outputDirectory, "step-5-execution-report.png");
    }

    private static async Task ActivateStepperStepAsync(IPage page, string stepLabel)
    {
        await page.GetByRole(AriaRole.Tab, new() { Name = stepLabel, Exact = false }).First
            .ClickAsync(new() { Force = true });
        await page.Locator(".wizard-working-pane").WaitForAsync(new() { Timeout = 15000 });
    }

    private static async Task CapturePaneAsync(IPage page, string outputDirectory, string fileName)
    {
        var pane = page.Locator(".wizard-working-pane");
        await pane.WaitForAsync(new() { Timeout = 15000 });
        await pane.ScreenshotAsync(new LocatorScreenshotOptions
        {
            Path = Path.Combine(outputDirectory, fileName),
        });
    }

    private static string ResolveScreenshotOutputDirectory()
    {
        var repositoryRoot = FindRepositoryRoot();
        return Path.Combine(repositoryRoot, "website", "static", "import-workflow");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "website"))
                && File.Exists(Path.Combine(directory.FullName, "ImportToPlanner.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
