using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using FileMoverMcp.Core.Services;
using Moq;

namespace FileMoverMcp.Tests.Services
{
    public class SessionManagerTests
    {
        private readonly Mock<ISessionStorage> _mockSessionStorage;
        private readonly SessionManager _sessionManager;

        public SessionManagerTests()
        {
            _mockSessionStorage = new Mock<ISessionStorage>();
            _sessionManager = new SessionManager(_mockSessionStorage.Object);
        }

        [Fact]
        public async Task GetActiveSessionAsync_WhenSessionExists_ReturnsSession()
        {
            // Arrange
            Session expectedSession = Session.Create("C:\\TestPath");
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSession);

            // Act
            Session? result = await _sessionManager.GetActiveSessionAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSession.BasePath, result.BasePath);
        }

        [Fact]
        public async Task GetActiveSessionAsync_WhenNoSession_ReturnsNull()
        {
            // Arrange
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act
            Session? result = await _sessionManager.GetActiveSessionAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenNoExistingSession_CreatesNewSession()
        {
            // Arrange
            string basePath = Directory.GetCurrentDirectory();
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act
            Session result = await _sessionManager.CreateSessionAsync(basePath, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Path.GetFullPath(basePath), result.BasePath);
            _mockSessionStorage.Verify(
                x => x.SaveSessionAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenSessionExists_ThrowsInvalidOperationException()
        {
            // Arrange
            Session existingSession = Session.Create("C:\\ExistingPath");
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingSession);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.CreateSessionAsync("C:\\NewPath", CancellationToken.None));

            Assert.Contains("Session already active", exception.Message);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenDirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            string nonExistentPath = "C:\\NonExistentDirectory12345";
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => _sessionManager.CreateSessionAsync(nonExistentPath, CancellationToken.None));
        }

        [Fact]
        public async Task AddFileMoveAsync_WhenSessionExists_AddsFileMove()
        {
            // Arrange
            Session session = Session.Create("C:\\TestPath");
            FileMove fileMove = new FileMove("source.txt", "dest.txt", false);

            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            await _sessionManager.AddFileMoveAsync(fileMove, CancellationToken.None);

            // Assert
            Assert.Single(session.StagedMoves);
            Assert.Equal(fileMove, session.StagedMoves[0]);
            _mockSessionStorage.Verify(
                x => x.SaveSessionAsync(session, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddFileMoveAsync_WhenNoSession_ThrowsInvalidOperationException()
        {
            // Arrange
            FileMove fileMove = new FileMove("source.txt", "dest.txt", false);
            _mockSessionStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.AddFileMoveAsync(fileMove, CancellationToken.None));

            Assert.Contains("No session initialized", exception.Message);
        }

        [Fact]
        public async Task CancelSessionAsync_WhenSessionExists_DeletesSession()
        {
            // Arrange
            _mockSessionStorage
                .Setup(x => x.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _sessionManager.CancelSessionAsync(CancellationToken.None);

            // Assert
            _mockSessionStorage.Verify(
                x => x.DeleteSessionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CancelSessionAsync_WhenNoSession_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockSessionStorage
                .Setup(x => x.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.CancelSessionAsync(CancellationToken.None));

            Assert.Contains("No session initialized", exception.Message);
        }

        [Fact]
        public async Task HasActiveSessionAsync_WhenSessionExists_ReturnsTrue()
        {
            // Arrange
            _mockSessionStorage
                .Setup(x => x.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            bool result = await _sessionManager.HasActiveSessionAsync(CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasActiveSessionAsync_WhenNoSession_ReturnsFalse()
        {
            // Arrange
            _mockSessionStorage
                .Setup(x => x.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            bool result = await _sessionManager.HasActiveSessionAsync(CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddFileMoveAsync_WhenCalledConcurrently_KeepsEveryMove()
        {
            // Arrange
            // A load that pauses lets every call read the session before any call saves,
            // so without serialization all but one move would be lost.
            InMemorySessionStorage storage = new InMemorySessionStorage();
            Mock<ISessionStorage> slowStorage = new Mock<ISessionStorage>();
            slowStorage
                .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken ct) =>
                {
                    Session? session = await storage.LoadSessionAsync(ct);
                    await Task.Delay(20, ct);
                    return session;
                });
            slowStorage
                .Setup(x => x.SaveSessionAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                .Returns((Session session, CancellationToken ct) => storage.SaveSessionAsync(session, ct));
            await storage.SaveSessionAsync(Session.Create("/base"), CancellationToken.None);
            SessionManager sessionManager = new SessionManager(slowStorage.Object);
            int moveCount = 10;

            // Act
            await Task.WhenAll(Enumerable.Range(0, moveCount).Select(i =>
                sessionManager.AddFileMoveAsync(new FileMove($"{i}.txt", $"moved/{i}.txt", false), CancellationToken.None)));

            // Assert
            Session result = (await storage.LoadSessionAsync(CancellationToken.None))!;
            Assert.Equal(moveCount, result.StagedMoves.Count);
        }
    }
}
