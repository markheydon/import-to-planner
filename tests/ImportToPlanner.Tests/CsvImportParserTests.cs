using ImportToPlanner.Application.CsvImport.Services;
using ImportToPlanner.Tests.TestData;

namespace ImportToPlanner.Tests;

public sealed class CsvImportParserTests
{
    [Fact]
    public async Task ParseAsync_WithMissingTaskName_ReturnsRowLevelValidationError()
    {
        // Arrange
        var parser = new CsvImportParser();
        var csv = CsvFixtureLoader.Load("invalid-missing-task-name.csv");

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 2 &&
            error.Field == "Task Name" &&
            error.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_WithMissingTaskNameHeader_ReturnsHeaderValidationError()
    {
        // Arrange
        const string csv = "Description,Priority,Bucket,Goal\nTask without header,3,Operations,Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "Task Name");
    }

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

    [Fact]
    public async Task ParseAsync_WithExtraColumnsAndIgnoreDisabled_ReturnsUnexpectedColumnError()
    {
        // Arrange
        var parser = new CsvImportParser();
        var csv = CsvFixtureLoader.Load("with-extra-columns.csv");

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: false);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error =>
            string.Equals(error.Field, "Owner", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(error.Field, "Due Date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_WithExtraColumnsAndIgnoreEnabled_ParsesRowsSuccessfully()
    {
        // Arrange
        var parser = new CsvImportParser();
        var csv = CsvFixtureLoader.Load("with-extra-columns.csv");

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: true);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task with extras", result.Rows[0].TaskName);
    }
}
