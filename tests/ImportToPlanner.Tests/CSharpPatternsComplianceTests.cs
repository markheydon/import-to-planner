namespace ImportToPlanner.Tests;

public sealed class CSharpPatternsComplianceTests
{
    private static readonly string[] ForbiddenSourceTokens =
    [
        "HttpContext.Current",
        ".Wait()",
        "GetAwaiter().GetResult()",
        "new HttpClient(",
    ];

    [Fact]
    public void MaintainedSource_DoesNotUseForbiddenBlockingOrLegacyHttpPatterns()
    {
        var rootPath = GetRepositoryRootPath();
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            var codeWithoutLineComments = StripLineComments(content);

            foreach (var forbiddenToken in ForbiddenSourceTokens)
            {
                Assert.DoesNotContain(
                    forbiddenToken,
                    codeWithoutLineComments,
                    StringComparison.Ordinal);
            }
        }
    }

    private static string StripLineComments(string content)
    {
        using var reader = new StringReader(content);
        var builder = new System.Text.StringBuilder();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var commentIndex = IndexOfLineCommentStart(line);
            builder.AppendLine(commentIndex >= 0 ? line[..commentIndex] : line);
        }

        return builder.ToString();
    }

    private static int IndexOfLineCommentStart(string line)
    {
        var inString = false;
        for (var index = 0; index < line.Length - 1; index++)
        {
            var current = line[index];
            if (current == '"' && (index == 0 || line[index - 1] != '\\'))
            {
                inString = !inString;
                continue;
            }

            if (!inString && line[index] == '/' && line[index + 1] == '/')
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetRepositoryRootPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
}
