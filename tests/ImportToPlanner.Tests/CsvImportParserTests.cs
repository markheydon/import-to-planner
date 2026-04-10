using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Tests;

public sealed class CsvImportParserTests
{
    [Fact]
    public async Task ParseAsync_WithInvalidPriority_ReturnsValidationError()
    {
        // Arrange
        const string csv = "Task Name,Description,Priority,Bucket,Goal\nTask A,Desc,not-valid,Ops,Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error => error.Field == "Priority");
    }

    [Theory]
    [InlineData("Urgent", 1)]
    [InlineData("Important", 3)]
    [InlineData("Medium", 5)]
    [InlineData("Low", 9)]
    [InlineData("7", 7)]
    public async Task ParseAsync_WithValidPriority_ParsesPriority(string priorityText, int expected)
    {
        // Arrange
        var csv = $"Task Name,Description,Priority,Bucket,Goal\nTask A,Desc,{priorityText},Ops,Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal(expected, result.Rows[0].Priority);
    }

    [Fact]
    public async Task ParseAsync_WithCaseInsensitiveHeaders_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "task name,description,priority,bucket,goal\nTask A,Desc,5,Ops,Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
    }
}
