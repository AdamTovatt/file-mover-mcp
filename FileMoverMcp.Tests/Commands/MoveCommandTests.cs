using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using Moq;

namespace FileMoverMcp.Tests.Commands
{
    public class MoveCommandTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IFileOperationService> _mockFileOperationService;

        public MoveCommandTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockFileOperationService = new Mock<IFileOperationService>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccessful_ReturnsSuccessResult()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _mockFileOperationService
                .Setup(x => x.ValidateFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true));

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "source.txt",
                "dest.txt",
                false);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Staged move", result.Message);
            _mockSessionManager.Verify(
                x => x.AddFileMoveAsync(It.IsAny<FileMove>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoSession_ReturnsFailureResult()
        {
            // Arrange
            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "source.txt",
                "dest.txt",
                false);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenValidationFails_ReturnsFailureResult()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _mockFileOperationService
                .Setup(x => x.ValidateFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "Source file not found"));

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "nonexistent.txt",
                "dest.txt",
                false);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Source file not found", result.Message);
            _mockSessionManager.Verify(
                x => x.AddFileMoveAsync(It.IsAny<FileMove>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSourceIsDirectory_SetsIsDirectoryTrue()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            FileMove? capturedFileMove = null;

            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Source is not a file
            _mockFileOperationService
                .Setup(x => x.FileExistsAsync("mydir", "C:\\TestPath", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Source is a directory
            _mockFileOperationService
                .Setup(x => x.DirectoryExistsAsync("mydir", "C:\\TestPath", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileOperationService
                .Setup(x => x.ValidateFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true));

            _mockSessionManager
                .Setup(x => x.AddFileMoveAsync(It.IsAny<FileMove>(), It.IsAny<CancellationToken>()))
                .Callback<FileMove, CancellationToken>((fm, ct) => capturedFileMove = fm)
                .Returns(Task.CompletedTask);

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "mydir",
                "mydir2",
                false);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedFileMove);
            Assert.True(capturedFileMove.IsDirectory);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSourceIsFile_SetsIsDirectoryFalse()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            FileMove? capturedFileMove = null;

            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Source is a file
            _mockFileOperationService
                .Setup(x => x.FileExistsAsync("source.txt", "C:\\TestPath", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileOperationService
                .Setup(x => x.ValidateFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true));

            _mockSessionManager
                .Setup(x => x.AddFileMoveAsync(It.IsAny<FileMove>(), It.IsAny<CancellationToken>()))
                .Callback<FileMove, CancellationToken>((fm, ct) => capturedFileMove = fm)
                .Returns(Task.CompletedTask);

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "source.txt",
                "dest.txt",
                false);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedFileMove);
            Assert.False(capturedFileMove.IsDirectory);
            // Should not check DirectoryExistsAsync when file exists
            _mockFileOperationService.Verify(
                x => x.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithOverwriteFlag_PassesOverwriteToFileMove()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            FileMove? capturedFileMove = null;

            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _mockFileOperationService
                .Setup(x => x.ValidateFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true));

            _mockSessionManager
                .Setup(x => x.AddFileMoveAsync(It.IsAny<FileMove>(), It.IsAny<CancellationToken>()))
                .Callback<FileMove, CancellationToken>((fm, ct) => capturedFileMove = fm)
                .Returns(Task.CompletedTask);

            MoveCommand command = new MoveCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object,
                "source.txt",
                "dest.txt",
                true);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedFileMove);
            Assert.True(capturedFileMove.Overwrite);
        }
    }
}
