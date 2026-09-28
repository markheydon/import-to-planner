namespace ImportToPlanner.Application.Import;

/// <summary>
/// Header normalisation for layout signatures and alias matching.
/// </summary>
public static class ImportColumnHeaderNormalisation
{
    /// <summary>
    /// Builds a stable layout signature from ordered raw header labels.
    /// </summary>
    public static string BuildLayoutSignature(IReadOnlyList<string> rawHeaders)
    {
        ArgumentNullException.ThrowIfNull(rawHeaders);

        if (rawHeaders.Count == 0)
        {
            return string.Empty;
        }

        return string.Join('\u001f', rawHeaders.Select(NormaliseForMatch));
    }

    /// <summary>
    /// Normalises a header for comparison: trim, invariant case fold, remove spaces and <c>.</c>, <c>-</c>, <c>_</c>.
    /// </summary>
    public static string NormaliseForMatch(string? rawHeader)
    {
        if (string.IsNullOrWhiteSpace(rawHeader))
        {
            return string.Empty;
        }

        var trimmed = rawHeader.Trim();
        var folded = trimmed.ToUpperInvariant();
        Span<char> buffer = folded.Length <= 256 ? stackalloc char[folded.Length] : new char[folded.Length];
        var length = 0;

        foreach (var character in folded)
        {
            if (character is ' ' or '.' or '-' or '_')
            {
                continue;
            }

            buffer[length++] = character;
        }

        return length == 0 ? string.Empty : new string(buffer[..length]);
    }

    /// <summary>
    /// Compares two raw headers using normalised match rules.
    /// </summary>
    public static bool HeadersMatch(string? left, string? right)
        => string.Equals(NormaliseForMatch(left), NormaliseForMatch(right), StringComparison.Ordinal);
}
