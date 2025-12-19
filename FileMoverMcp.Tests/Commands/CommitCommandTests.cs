using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using Moq;

namespace FileMoverMcp.Tests.Commands
{
    public class CommitCommandTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IFileOperationService> _mockFileOperationService;

        public CommitCommandTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockFileOperationService = new Mock<IFileOperationService>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoSession_ReturnsFailureResult()
        {
            // Arrange
            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            CommitCommand command = new CommitCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoMoves_ClearsSessionAndReturnsSuccess()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            CommitCommand command = new CommitCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("No moves to commit", result.Message);
            _mockSessionManager.Verify(
                x => x.CancelSessionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMovesSucceed_ReturnsSuccessAndClearsSession()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            session.StagedMoves.Add(new FileMove("file1.txt", "file2.txt", false));

            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _mockFileOperationService
                .Setup(x => x.ExecuteFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            CommitCommand command = new CommitCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Committed 1 move(s) successfully", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("file1.txt", result.Details);
            _mockSessionManager.Verify(
                x => x.CancelSessionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSomeMovesFail_ReturnsPartialSuccess()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            session.StagedMoves.Add(new FileMove("file1.txt", "file2.txt", false));
            session.StagedMoves.Add(new FileMove("file3.txt", "file4.txt", false));

            _mockSessionManager
                .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _mockFileOperationService
                .SetupSequence(x => x.ExecuteFileMoveAsync(
                    It.IsAny<FileMove>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .ThrowsAsync(new IOException("File locked"));

            CommitCommand command = new CommitCommand(
                _mockSessionManager.Object,
                _mockFileOperationService.Object);

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Completed with 1 error(s)", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("Successfully moved 1 file(s)", result.Details);
            Assert.Contains("Failed to move 1 file(s)", result.Details);
        }
    }
}
