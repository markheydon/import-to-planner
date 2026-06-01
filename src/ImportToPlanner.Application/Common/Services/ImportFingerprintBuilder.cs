using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ImportToPlanner.Application.ImportPlanning.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Common.Services;

internal static class ImportFingerprintBuilder
{
    public static string BuildRequestFingerprint(ImportPlanningRequest request)
    {
        var lines = new List<string>
        {
            request.ContainerId,
            request.PlanId,
            request.PlanName,
            request.ContainerType.ToString(),
        };

        lines.AddRange(request.Rows
            .OrderBy(row => row.RowNumber)
            .Select(row => string.Join("|",
                row.RowNumber,
                row.TaskName.Trim(),
                row.Description?.Trim() ?? string.Empty,
                row.Priority?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                row.Bucket?.Trim() ?? string.Empty,
                row.Goal?.Trim() ?? string.Empty)));

        return ComputeFingerprint(string.Join("\n", lines));
    }

    public static string BuildPlannerStateFingerprint(
        IReadOnlyCollection<PlannerBucket> buckets,
        IReadOnlyCollection<PlannerTaskSnapshot> tasks)
    {
        var stateLines = buckets
            .Select(bucket => $"B:{bucket.Name.Trim()}")
            .Concat(tasks.Select(task => $"T:{task.Title.Trim()}"))
            .OrderBy(line => line, StringComparer.OrdinalIgnoreCase);

        return ComputeFingerprint(string.Join("\n", stateLines));
    }

    private static string ComputeFingerprint(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
