namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Injectable UTC clock for commercial credit month-boundary tests.
/// </summary>
public interface IUtcClock
{
    /// <summary>
    /// Gets the current UTC instant.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// System UTC clock implementation.
/// </summary>
public sealed class SystemUtcClock : IUtcClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
