using ImportToPlanner.Application.Consent.Models;

namespace ImportToPlanner.Application.Consent.Exceptions;

/// <summary>
/// Represents a consent resolution that blocks use-case execution.
/// </summary>
public sealed class ConsentBlockedException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ConsentBlockedException"/> class.
    /// </summary>
    /// <param name="resolution">The structured consent resolution that blocked execution.</param>
    public ConsentBlockedException(ConsentResolution resolution)
        : base("Consent resolution blocked use-case execution.")
    {
        ArgumentNullException.ThrowIfNull(resolution);
        Resolution = resolution;
    }

    /// <summary>
    /// Gets the structured consent resolution that blocked execution.
    /// </summary>
    public ConsentResolution Resolution { get; }
}
