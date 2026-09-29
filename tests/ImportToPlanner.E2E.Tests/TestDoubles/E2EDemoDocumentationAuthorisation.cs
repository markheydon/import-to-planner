using ImportToPlanner.Application.Abstractions;

namespace ImportToPlanner.E2E.Tests.TestDoubles;

/// <summary>
/// Permits demonstration controls during opt-in screenshot capture in the E2E host.
/// </summary>
internal sealed class E2EDemoDocumentationAuthorisation : IDemoModeAuthorisationService
{
    public bool CanShowDemoControls() => true;

    public bool CanActivateDemo() => true;
}
