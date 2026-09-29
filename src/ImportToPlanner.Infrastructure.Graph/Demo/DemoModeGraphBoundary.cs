using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.Infrastructure.Graph.Demo;

/// <summary>
/// Fail-closed guard when demonstration mode is active.
/// </summary>
internal static class DemoModeGraphBoundary
{
    internal static void EnsureGraphAllowed(IDemoModeSession? demoModeSession)
    {
        if (demoModeSession?.IsActive == true)
        {
            throw new InvalidOperationException(
                "Microsoft Graph operations are disabled while demonstration mode is active.");
        }
    }

    internal static void EnsureRealCsvUploadAllowed(IDemoModeSession? demoModeSession)
    {
        if (demoModeSession?.IsActive == true)
        {
            throw new InvalidOperationException(
                "Real CSV upload parsing is disabled while demonstration mode is active.");
        }
    }
}
