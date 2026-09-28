namespace ImportToPlanner.Web.Infrastructure;

internal static class E2ETestingHostEnvironment
{
    public const string EnvironmentName = "E2ETesting";

    /// <summary>
    /// Environment variable that must be set when starting the host in <see cref="EnvironmentName"/>.
    /// Prevents accidental deployment with test-only authentication endpoints enabled.
    /// </summary>
    public const string AllowEnvironmentVariableName = "IMPORT_TO_PLANNER_ALLOW_E2E_TESTING";

    public static bool IsE2ETesting(IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        return string.Equals(hostEnvironment.EnvironmentName, EnvironmentName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsE2ETestingOptInEnabled()
    {
        var allowed = Environment.GetEnvironmentVariable(AllowEnvironmentVariableName);
        return string.Equals(allowed, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static void EnsureStartupAllowed(IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        if (!IsE2ETesting(hostEnvironment))
        {
            return;
        }

        if (!IsE2ETestingOptInEnabled())
        {
            throw new InvalidOperationException(
                $"The {EnvironmentName} environment is reserved for automated browser tests. " +
                $"Do not use it for deployed hosts. Test hosts must set {AllowEnvironmentVariableName}=true.");
        }
    }
}
