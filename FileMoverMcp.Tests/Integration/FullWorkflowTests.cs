using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Services;
using Xunit;

namespace FileMoverMcp.Tests.Integration;

public class FullWorkflowTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ISessionStorage _sessionStorage;
    private readonly ISessionManager _sessionManager;
    private readonly IFileOperationService _fileOperationService;

    public FullWorkflowTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileMoverTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _sessionStorage = new SessionStorage();
        _sessionManager = new SessionManager(_sessionStorage);
        _fileOperationService = new FileOperationService();
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }

        // Clean up any session file
        Task.Run(async () => await _sessionStorage.DeleteSessionAsync(CancellationToken.None)).Wait();
    }

    [Fact]
    public async Task FullWorkflow_InitMovePreviewCommit_WorksEndToEnd()
    {
        // Arrange - Create test files
        string sourceFile = Path.Combine(_testDirectory, "source.txt");
        string destFile = Path.Combine(_testDirectory, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "Test content");

        // Act & Assert - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        CommandResult initResult = await initCommand.ExecuteAsync(CancellationToken.None);
        Assert.True(initResult.Success);

        // Act & Assert - Stage move
        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "source.txt",
            "dest.txt",
            false);
        CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);
        Assert.True(moveResult.Success);

        // Act & Assert - Preview
        PreviewCommand previewCommand = new PreviewCommand(_sessionManager);
        CommandResult previewResult = await previewCommand.ExecuteAsync(CancellationToken.None);
        Assert.True(previewResult.Success);
        Assert.Contains("1 move(s) staged", previewResult.Message);

        // Assert - Source file still exists before commit
        Assert.True(File.Exists(sourceFile));
        Assert.False(File.Exists(destFile));

        // Act & Assert - Commit
        CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
        CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);
        Assert.True(commitResult.Success);

        // Assert - File was moved
        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(destFile));
        string content = await File.ReadAllTextAsync(destFile);
        Assert.Equal("Test content", content);

        // Assert - Session is cleared
        bool sessionExists = await _sessionStorage.SessionExistsAsync(CancellationToken.None);
        Assert.False(sessionExists);
    }

    [Fact]
    public async Task FullWorkflow_InitMoveCancel_DiscardsChanges()
    {
        // Arrange - Create test file
        string sourceFile = Path.Combine(_testDirectory, "source.txt");
        await File.WriteAllTextAsync(sourceFile, "Test content");

        // Act - Initialize and stage move
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "source.txt",
            "dest.txt",
            false);
        await moveCommand.ExecuteAsync(CancellationToken.None);

        // Act - Cancel
        CancelCommand cancelCommand = new CancelCommand(_sessionManager);
        CommandResult cancelResult = await cancelCommand.ExecuteAsync(CancellationToken.None);

        // Assert - Cancel succeeded
        Assert.True(cancelResult.Success);

        // Assert - File was not moved
        Assert.True(File.Exists(sourceFile));

        // Assert - Session is cleared
        bool sessionExists = await _sessionStorage.SessionExistsAsync(CancellationToken.None);
        Assert.False(sessionExists);
    }

    [Fact]
    public async Task FullWorkflow_MultipleMovesInSession_AllExecuted()
    {
        // Arrange - Create test files
        string file1 = Path.Combine(_testDirectory, "file1.txt");
        string file2 = Path.Combine(_testDirectory, "file2.txt");
        string dest1 = Path.Combine(_testDirectory, "dest1.txt");
        string dest2 = Path.Combine(_testDirectory, "dest2.txt");

        await File.WriteAllTextAsync(file1, "Content 1");
        await File.WriteAllTextAsync(file2, "Content 2");

        // Act - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        // Act - Stage multiple moves
        MoveCommand move1 = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "file1.txt",
            "dest1.txt",
            false);
        await move1.ExecuteAsync(CancellationToken.None);

        MoveCommand move2 = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "file2.txt",
            "dest2.txt",
            false);
        await move2.ExecuteAsync(CancellationToken.None);

        // Act - Commit
        CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
        CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);

        // Assert - Both files were moved
        Assert.True(commitResult.Success);
        Assert.False(File.Exists(file1));
        Assert.False(File.Exists(file2));
        Assert.True(File.Exists(dest1));
        Assert.True(File.Exists(dest2));

        string content1 = await File.ReadAllTextAsync(dest1);
        string content2 = await File.ReadAllTextAsync(dest2);
        Assert.Equal("Content 1", content1);
        Assert.Equal("Content 2", content2);
    }

    [Fact]
    public async Task FullWorkflow_WithSubdirectories_CreatesDirectoriesAsNeeded()
    {
        // Arrange - Create test file
        string sourceFile = Path.Combine(_testDirectory, "source.txt");
        await File.WriteAllTextAsync(sourceFile, "Test content");

        // Act - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        // Act - Stage move to subdirectory
        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "source.txt",
            "subdir/nested/dest.txt",
            false);
        await moveCommand.ExecuteAsync(CancellationToken.None);

        // Act - Commit
        CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
        CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);

        // Assert - File was moved and directories created
        Assert.True(commitResult.Success);
        string destFile = Path.Combine(_testDirectory, "subdir", "nested", "dest.txt");
        Assert.True(File.Exists(destFile));
        Assert.False(File.Exists(sourceFile));
    }

    [Fact]
    public async Task FullWorkflow_OverwriteFlag_ReplacesExistingFile()
    {
        // Arrange - Create test files
        string sourceFile = Path.Combine(_testDirectory, "source.txt");
        string destFile = Path.Combine(_testDirectory, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "New content");
        await File.WriteAllTextAsync(destFile, "Old content");

        // Act - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        // Act - Stage move with overwrite
        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "source.txt",
            "dest.txt",
            true);
        await moveCommand.ExecuteAsync(CancellationToken.None);

        // Act - Commit
        CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
        CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);

        // Assert - File was replaced
        Assert.True(commitResult.Success);
        Assert.True(File.Exists(destFile));
        string content = await File.ReadAllTextAsync(destFile);
        Assert.Equal("New content", content);
    }

    [Fact]
    public async Task FullWorkflow_DestinationExistsWithoutOverwrite_Fails()
    {
        // Arrange - Create test files
        string sourceFile = Path.Combine(_testDirectory, "source.txt");
        string destFile = Path.Combine(_testDirectory, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "New content");
        await File.WriteAllTextAsync(destFile, "Old content");

        // Act - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        // Act - Stage move without overwrite
        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "source.txt",
            "dest.txt",
            false);
        CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);

        // Assert - Move fails validation
        Assert.False(moveResult.Success);
        Assert.Contains("Destination exists", moveResult.Message);
        Assert.Contains("--overwrite", moveResult.Message);
    }

    [Fact]
    public async Task FullWorkflow_SourceFileNotFound_Fails()
    {
        // Act - Initialize session
        InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
        await initCommand.ExecuteAsync(CancellationToken.None);

        // Act - Try to stage move for non-existent file
        MoveCommand moveCommand = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            "nonexistent.txt",
            "dest.txt",
            false);
        CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);

        // Assert - Move fails validation
        Assert.False(moveResult.Success);
        Assert.Contains("Source file not found", moveResult.Message);
    }

    [Fact]
    public async Task FullWorkflow_InitTwice_Fails()
    {
        // Act - Initialize session first time
        InitCommand initCommand1 = new InitCommand(_sessionManager, _testDirectory);
        CommandResult initResult1 = await initCommand1.ExecuteAsync(CancellationToken.None);
        Assert.True(initResult1.Success);

        // Act - Try to initialize again
        InitCommand initCommand2 = new InitCommand(_sessionManager, _testDirectory);
        CommandResult initResult2 = await initCommand2.ExecuteAsync(CancellationToken.None);

        // Assert - Second init fails
        Assert.False(initResult2.Success);
        Assert.Contains("Session already active", initResult2.Message);
    }
}

