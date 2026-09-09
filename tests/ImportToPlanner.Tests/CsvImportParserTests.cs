using ImportToPlanner.Infrastructure.Graph.Import;
using ImportToPlanner.Tests.TestData;

namespace ImportToPlanner.Tests;

public sealed class CsvImportParserTests
{
    private const string AmbiguousSeparatorMessage =
        "The field separator could not be determined. Save the file as comma-separated UTF-8 and upload again.";

    private const string UnsupportedSeparatorMessage =
        "This separator is not supported. Save the file as comma-separated UTF-8 and upload again.";

    private const string EmptyFileMessage = "CSV file is empty.";
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

    [Fact]
    public async Task ParseAsync_WithDescriptionExceedingPlannerLimit_ReturnsValidationError()
    {
        // Arrange
        var parser = new CsvImportParser();
        var description = new string('x', 32_769);
        var csv = $"Task Name,Description\nTask A,{description}";

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 2 &&
            error.Field == "Description" &&
            error.Message.Contains("32,768", StringComparison.Ordinal));
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
            string.Equals(error.Field, "Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.ValidationErrors, error =>
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
        Assert.Equal(new DateOnly(2026, 5, 31), result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithSemicolonDelimitedCsv_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "Task Name;Description;Priority;Bucket;Goal\nTask A;Desc;3;Ops;Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
        Assert.Equal("Desc", result.Rows[0].Description);
        Assert.Equal(3, result.Rows[0].Priority);
        Assert.Equal("Ops", result.Rows[0].Bucket);
        Assert.Equal("Goal A", result.Rows[0].Goal);
    }

    [Fact]
    public async Task ParseAsync_WithCommaAndSemicolonTwins_ProduceEquivalentRows()
    {
        // Arrange
        const string commaCsv = "Task Name,Description,Priority,Bucket,Goal\nTask A,Desc,3,Ops,Goal A";
        const string semicolonCsv = "Task Name;Description;Priority;Bucket;Goal\nTask A;Desc;3;Ops;Goal A";
        var parser = new CsvImportParser();

        // Act
        var commaResult = await parser.ParseAsync(commaCsv, CancellationToken.None);
        var semicolonResult = await parser.ParseAsync(semicolonCsv, CancellationToken.None);

        // Assert
        Assert.False(commaResult.HasErrors);
        Assert.False(semicolonResult.HasErrors);
        Assert.Equal(commaResult.Rows.Count, semicolonResult.Rows.Count);
        Assert.Equal(commaResult.Rows[0].TaskName, semicolonResult.Rows[0].TaskName);
        Assert.Equal(commaResult.Rows[0].Description, semicolonResult.Rows[0].Description);
        Assert.Equal(commaResult.Rows[0].Priority, semicolonResult.Rows[0].Priority);
        Assert.Equal(commaResult.Rows[0].Bucket, semicolonResult.Rows[0].Bucket);
        Assert.Equal(commaResult.Rows[0].Goal, semicolonResult.Rows[0].Goal);
    }

    [Fact]
    public async Task ParseAsync_WithSemicolonDelimitedExtraColumnsAndIgnoreEnabled_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "Task Name;Description;Priority;Bucket;Goal;Assigned To;Owner;Due Date\nTask with extras;Contains additional columns;5;Operations;Q2 Delivery;;Mark;2026-05-31";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: true);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task with extras", result.Rows[0].TaskName);
        Assert.Equal(new DateOnly(2026, 5, 31), result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithoutDueDateColumn_LeavesDueDateNull()
    {
        const string csv = "Task Name,Description,Priority,Bucket,Goal\nTask A,Desc,3,Ops,Goal A";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyDueDateCell_LeavesDueDateNull()
    {
        const string csv = "Task Name,Due Date\nTask A,";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].DueDate);
    }

    [Theory]
    [InlineData("2026-05-31", 2026, 5, 31)]
    [InlineData("31/05/2026", 2026, 5, 31)]
    [InlineData("31-05-2026", 2026, 5, 31)]
    public async Task ParseAsync_WithValidDueDateFormats_ParsesCalendarDate(string dueDateText, int year, int month, int day)
    {
        var csv = $"Task Name,Due Date\nTask A,{dueDateText}";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(new DateOnly(year, month, day), result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithTwoDigitYearDueDate_ParsesAs2000s()
    {
        const string csv = "Task Name,Due Date\nTask A,31/05/26";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(new DateOnly(2026, 5, 31), result.Rows[0].DueDate);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-05-31T17:00")]
    public async Task ParseAsync_WithInvalidDueDate_ReturnsDueDateValidationError(string dueDateText)
    {
        var csv = $"Task Name,Due Date\nTask A,{dueDateText}";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error => error.Field == "Due Date");
    }

    [Fact]
    public async Task ParseAsync_WithSemicolonDelimitedDueDate_MatchesCommaEquivalent()
    {
        const string commaCsv = "Task Name,Due Date\nTask A,2026-05-31";
        const string semicolonCsv = "Task Name;Due Date\nTask A;2026-05-31";
        var parser = new CsvImportParser();

        var commaResult = await parser.ParseAsync(commaCsv, CancellationToken.None);
        var semicolonResult = await parser.ParseAsync(semicolonCsv, CancellationToken.None);

        Assert.False(commaResult.HasErrors);
        Assert.False(semicolonResult.HasErrors);
        Assert.Equal(commaResult.Rows[0].DueDate, semicolonResult.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceOnlyDueDateCell_LeavesDueDateNull()
    {
        const string csv = "Task Name,Due Date\nTask A,   ";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].DueDate);
    }

    [Theory]
    [InlineData("44927")]
    [InlineData("2026-5-31")]
    public async Task ParseAsync_WithUnrecognisedDueDateShape_ReturnsDueDateValidationError(string dueDateText)
    {
        var csv = $"Task Name,Due Date\nTask A,{dueDateText}";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error => error.Field == "Due Date");
    }

    [Fact]
    public async Task ParseAsync_WithInvalidCalendarDueDate_ReturnsDueDateValidationError()
    {
        const string csv = "Task Name,Due Date\nTask A,31/02/2026";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Contains(result.ValidationErrors, error => error.Field == "Due Date");
    }

    [Fact]
    public async Task ParseAsync_WithUkDayFirstAmbiguousDueDate_ParsesDayBeforeMonth()
    {
        const string csv = "Task Name,Due Date\nTask A,05/06/2026";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(new DateOnly(2026, 6, 5), result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedDueDate_ParsesCalendarDate()
    {
        const string csv = "Task Name,Due Date\nTask A,\"31/05/2026\"";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(new DateOnly(2026, 5, 31), result.Rows[0].DueDate);
    }

    [Fact]
    public async Task ParseAsync_WithSingleColumnHeader_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "Task Name\nTask A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
    }

    [Fact]
    public async Task ParseAsync_WithCommaDelimitedBomPrefix_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "\uFEFFTask Name,Description\nTask A,Desc";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
        Assert.Equal("Desc", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithSemicolonDelimitedBomPrefix_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "\uFEFFTask Name;Description\nTask A;Desc";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
        Assert.Equal("Desc", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithoutBomPrefix_StillParsesSuccessfully()
    {
        // Arrange
        var parser = new CsvImportParser();
        var csv = CsvFixtureLoader.Load("valid-basic.csv");

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_WithAmbiguousHeader_ReturnsFileLevelError()
    {
        // Arrange
        const string csv = "Task Name,Bucket;Goal\nTask A,Ops,Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == AmbiguousSeparatorMessage);
        Assert.DoesNotContain(result.ValidationErrors, error =>
            error.Message.Contains("Task Name column is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_WithTabSeparatedHeader_ReturnsUnsupportedSeparatorError()
    {
        // Arrange
        const string csv = "Task Name\tDescription\nTask A\tDesc";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == UnsupportedSeparatorMessage);
    }

    [Fact]
    public async Task ParseAsync_WithPipeSeparatedHeader_ReturnsUnsupportedSeparatorError()
    {
        // Arrange
        const string csv = "Task Name|Description\nTask A|Desc";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == UnsupportedSeparatorMessage);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedCommasInDescription_PreservesFieldValue()
    {
        // Arrange
        const string csv = "Task Name,Description\nTask A,\"Has, comma inside\"";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Has, comma inside", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedSemicolonsInDescription_PreservesFieldValue()
    {
        // Arrange
        const string csv = "Task Name;Description\nTask A;\"Has; semicolon inside\"";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Has; semicolon inside", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedSeparatorInHeader_DoesNotAffectDelimiterDetection()
    {
        // Arrange
        const string csv = "Task Name,\"Description; alias\"\nTask A,Value";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: true);

        // Assert
        Assert.DoesNotContain(result.ValidationErrors, error => error.Field == "File");
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedSemicolonsInCommaDelimitedDescription_PreservesFieldValue()
    {
        // Arrange
        const string csv = "Task Name,Description\nTask A,\"Has; semicolon inside\"";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Has; semicolon inside", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithCrlfLineEndings_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "Task Name;Description\r\nTask A;Desc";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
        Assert.Equal("Desc", result.Rows[0].Description);
    }

    [Fact]
    public async Task ParseAsync_WithBomOnly_ReturnsEmptyFileError()
    {
        // Arrange
        const string csv = "\uFEFF";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == EmptyFileMessage);
    }

    [Fact]
    public async Task ParseAsync_WithBomAndWhitespaceOnly_ReturnsEmptyFileError()
    {
        // Arrange
        const string csv = "\uFEFF   \r\n  ";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == EmptyFileMessage);
    }

    [Fact]
    public async Task ParseAsync_WithNullContent_ReturnsEmptyFileError()
    {
        // Arrange
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(null!, CancellationToken.None);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains(result.ValidationErrors, error =>
            error.RowNumber == 0 &&
            error.Field == "File" &&
            error.Message == EmptyFileMessage);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedNewlineInSemicolonHeader_ParsesSuccessfully()
    {
        // Arrange
        const string csv = "Task Name;\"Part1\nPart2\";Priority;Bucket;Goal\nTask A;Desc;3;Ops;Goal A";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: true);

        // Assert
        Assert.DoesNotContain(result.ValidationErrors, error => error.Message == AmbiguousSeparatorMessage);
        Assert.DoesNotContain(result.ValidationErrors, error => error.Message == UnsupportedSeparatorMessage);
        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedPipeInHeader_DoesNotTreatAsUnsupportedSeparator()
    {
        // Arrange
        const string csv = "Task Name,\"Notes | extra\"\nTask A,Value";
        var parser = new CsvImportParser();

        // Act
        var result = await parser.ParseAsync(csv, CancellationToken.None, ignoreExtraColumns: true);

        // Assert
        Assert.DoesNotContain(result.ValidationErrors, error => error.Field == "File");
        Assert.Single(result.Rows);
        Assert.Equal("Task A", result.Rows[0].TaskName);
    }

    [Fact]
    public async Task ParseAsync_WithoutAssignedToColumn_LeavesAssigneeAddressesEmpty()
    {
        const string csv = "Task Name,Description\nTask A,Desc";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Empty(result.Rows[0].AssigneeAddresses!);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyAssignedToCell_LeavesAssigneeAddressesEmpty()
    {
        const string csv = "Task Name,Assigned To\nTask A,";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Empty(result.Rows[0].AssigneeAddresses!);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceOnlyAssignedToCell_LeavesAssigneeAddressesEmpty()
    {
        const string csv = "Task Name,Assigned To\nTask A,   ";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows);
        Assert.Empty(result.Rows[0].AssigneeAddresses!);
    }

    [Theory]
    [InlineData("\"a@contoso.com, b@contoso.com\"", "a@contoso.com", "b@contoso.com")]
    [InlineData("a@contoso.com; b@contoso.com", "a@contoso.com", "b@contoso.com")]
    public async Task ParseAsync_WithAssignedToList_SplitsOnCommaOrSemicolon(
        string assignedToCell,
        string firstAddress,
        string secondAddress)
    {
        var csv = $"Task Name,Assigned To\nTask A,{assignedToCell}";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Rows[0].AssigneeAddresses!.Count);
        Assert.Equal(firstAddress, result.Rows[0].AssigneeAddresses![0]);
        Assert.Equal(secondAddress, result.Rows[0].AssigneeAddresses![1]);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedAssignedToList_ParsesMultipleAddresses()
    {
        const string csv = "Task Name,Assigned To\nTask A,\"a@contoso.com; b@contoso.com\"";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Rows[0].AssigneeAddresses!.Count);
        Assert.Equal("a@contoso.com", result.Rows[0].AssigneeAddresses![0]);
        Assert.Equal("b@contoso.com", result.Rows[0].AssigneeAddresses![1]);
    }

    [Fact]
    public async Task ParseAsync_WithDuplicateAssignedToAddresses_IgnoresCaseInsensitiveDuplicates()
    {
        const string csv = "Task Name,Assigned To\nTask A,A@contoso.com, a@contoso.com";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows[0].AssigneeAddresses!);
        Assert.Equal("A@contoso.com", result.Rows[0].AssigneeAddresses![0]);
    }

    [Fact]
    public async Task ParseAsync_WithNonAddressAssignedToText_StillSucceeds()
    {
        const string csv = "Task Name,Assigned To\nTask A,Not a person";
        var parser = new CsvImportParser();

        var result = await parser.ParseAsync(csv, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Rows[0].AssigneeAddresses!);
        Assert.Equal("Not a person", result.Rows[0].AssigneeAddresses![0]);
    }

    [Fact]
    public async Task ParseAsync_WithSemicolonDelimitedAssignedTo_MatchesCommaEquivalent()
    {
        const string commaCsv = "Task Name,Assigned To\nTask A,\"a@contoso.com; b@contoso.com\"";
        const string semicolonCsv = "Task Name;Assigned To\nTask A;\"a@contoso.com; b@contoso.com\"";
        var parser = new CsvImportParser();

        var commaResult = await parser.ParseAsync(commaCsv, CancellationToken.None);
        var semicolonResult = await parser.ParseAsync(semicolonCsv, CancellationToken.None);

        Assert.False(commaResult.HasErrors);
        Assert.False(semicolonResult.HasErrors);
        Assert.Equal(commaResult.Rows[0].AssigneeAddresses, semicolonResult.Rows[0].AssigneeAddresses);
    }
}
