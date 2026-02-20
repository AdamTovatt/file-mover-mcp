using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Services;

namespace FileMoverMcp.Tests.Integration
{
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
        public async Task FullWorkflow_DirectoryMove_MovesAllFiles()
        {
            // Arrange - Create source directory with files
            string sourceDir = Path.Combine(_testDirectory, "srcdir");
            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file1.txt"), "Content 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file2.txt"), "Content 2");

            // Act - Initialize session
            InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
            await initCommand.ExecuteAsync(CancellationToken.None);

            // Act - Stage directory move
            MoveCommand moveCommand = new MoveCommand(
                _sessionManager,
                _fileOperationService,
                "srcdir",
                "destdir",
                false);
            CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(moveResult.Success);

            // Act - Preview
            PreviewCommand previewCommand = new PreviewCommand(_sessionManager);
            CommandResult previewResult = await previewCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(previewResult.Success);
            Assert.NotNull(previewResult.Details);
            Assert.Contains("[DIR]", previewResult.Details);

            // Act - Commit
            CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
            CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(commitResult.Success);

            // Assert - Source directory is gone, destination has all files
            Assert.False(Directory.Exists(sourceDir));
            string destDir = Path.Combine(_testDirectory, "destdir");
            Assert.True(Directory.Exists(destDir));
            Assert.True(File.Exists(Path.Combine(destDir, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(destDir, "file2.txt")));
            Assert.Equal("Content 1", await File.ReadAllTextAsync(Path.Combine(destDir, "file1.txt")));
            Assert.Equal("Content 2", await File.ReadAllTextAsync(Path.Combine(destDir, "file2.txt")));
        }

        [Fact]
        public async Task FullWorkflow_DirectoryMoveWithSubdirectories_PreservesStructure()
        {
            // Arrange - Create source directory with nested structure
            string sourceDir = Path.Combine(_testDirectory, "srcdir");
            string subDir = Path.Combine(sourceDir, "sub");
            Directory.CreateDirectory(subDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "root.txt"), "Root content");
            await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "Nested content");

            // Act - Init, move, commit
            InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
            await initCommand.ExecuteAsync(CancellationToken.None);

            MoveCommand moveCommand = new MoveCommand(
                _sessionManager,
                _fileOperationService,
                "srcdir",
                "destdir",
                false);
            CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(moveResult.Success);

            CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
            CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(commitResult.Success);

            // Assert - Nested structure preserved
            Assert.False(Directory.Exists(sourceDir));
            string destDir = Path.Combine(_testDirectory, "destdir");
            Assert.True(File.Exists(Path.Combine(destDir, "root.txt")));
            Assert.True(File.Exists(Path.Combine(destDir, "sub", "nested.txt")));
            Assert.Equal("Root content", await File.ReadAllTextAsync(Path.Combine(destDir, "root.txt")));
            Assert.Equal("Nested content", await File.ReadAllTextAsync(Path.Combine(destDir, "sub", "nested.txt")));
        }

        [Fact]
        public async Task FullWorkflow_DirectoryDestinationExistsWithoutOverwrite_Fails()
        {
            // Arrange - Create source and destination directories
            string sourceDir = Path.Combine(_testDirectory, "srcdir");
            string destDir = Path.Combine(_testDirectory, "destdir");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file.txt"), "Content");

            // Act
            InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
            await initCommand.ExecuteAsync(CancellationToken.None);

            MoveCommand moveCommand = new MoveCommand(
                _sessionManager,
                _fileOperationService,
                "srcdir",
                "destdir",
                false);
            CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);

            // Assert - Fails validation
            Assert.False(moveResult.Success);
            Assert.Contains("Destination exists", moveResult.Message);
            Assert.Contains("--overwrite", moveResult.Message);
        }

        [Fact]
        public async Task FullWorkflow_DirectoryDestinationExistsWithOverwrite_Replaced()
        {
            // Arrange - Create source and destination directories
            string sourceDir = Path.Combine(_testDirectory, "srcdir");
            string destDir = Path.Combine(_testDirectory, "destdir");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "new.txt"), "New content");
            await File.WriteAllTextAsync(Path.Combine(destDir, "old.txt"), "Old content");

            // Act
            InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
            await initCommand.ExecuteAsync(CancellationToken.None);

            MoveCommand moveCommand = new MoveCommand(
                _sessionManager,
                _fileOperationService,
                "srcdir",
                "destdir",
                true);
            CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(moveResult.Success);

            CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
            CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(commitResult.Success);

            // Assert - Destination replaced with source contents
            Assert.False(Directory.Exists(sourceDir));
            Assert.True(File.Exists(Path.Combine(destDir, "new.txt")));
            Assert.False(File.Exists(Path.Combine(destDir, "old.txt")));
            Assert.Equal("New content", await File.ReadAllTextAsync(Path.Combine(destDir, "new.txt")));
        }

        [Fact]
        public async Task FullWorkflow_SourceDirectoryNotFound_Fails()
        {
            // Act
            InitCommand initCommand = new InitCommand(_sessionManager, _testDirectory);
            await initCommand.ExecuteAsync(CancellationToken.None);

            MoveCommand moveCommand = new MoveCommand(
                _sessionManager,
                _fileOperationService,
                "nonexistent_dir",
                "destdir",
                false);
            CommandResult moveResult = await moveCommand.ExecuteAsync(CancellationToken.None);

            // Assert - Neither file nor directory exists, validation catches it
            Assert.False(moveResult.Success);
            Assert.Contains("not found", moveResult.Message);
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
}
