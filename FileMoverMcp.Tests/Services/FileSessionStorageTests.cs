using FileMoverMcp.Core.Models;
using FileMoverMcp.Core.Services;

namespace FileMoverMcp.Tests.Services
{
    public class FileSessionStorageTests : IDisposable
    {
        private readonly string _testDir;
        private readonly string _sessionFilePath;

        public FileSessionStorageTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "fm-storage-test-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDir);
            _sessionFilePath = Path.Combine(_testDir, "session.json");
        }

        [Fact]
        public async Task LoadSessionAsync_FromAnotherInstance_ReturnsSavedSession()
        {
            // Arrange
            string basePath = "/base";
            Session session = Session.Create(basePath);
            session.StagedMoves.Add(new FileMove("a.txt", "b.txt", true, IsDirectory: false));
            await new FileSessionStorage(_sessionFilePath).SaveSessionAsync(session, CancellationToken.None);

            // Act
            Session? loaded = await new FileSessionStorage(_sessionFilePath).LoadSessionAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(session.SessionId, loaded.SessionId);
            Assert.Equal(basePath, loaded.BasePath);
            Assert.Equal(session.StagedMoves, loaded.StagedMoves);
        }

        [Fact]
        public async Task LoadSessionAsync_WhenNoFile_ReturnsNull()
        {
            // Arrange
            FileSessionStorage storage = new FileSessionStorage(_sessionFilePath);

            // Act
            Session? loaded = await storage.LoadSessionAsync(CancellationToken.None);

            // Assert
            Assert.Null(loaded);
        }

        [Fact]
        public async Task LoadSessionAsync_WhenFileIsCorrupt_ReturnsNullAndDeletesFile()
        {
            // Arrange
            await File.WriteAllTextAsync(_sessionFilePath, "{ not json");
            FileSessionStorage storage = new FileSessionStorage(_sessionFilePath);

            // Act
            Session? loaded = await storage.LoadSessionAsync(CancellationToken.None);

            // Assert
            Assert.Null(loaded);
            Assert.False(File.Exists(_sessionFilePath));
        }

        [Fact]
        public async Task SessionExistsAsync_BeforeAndAfterSave_ReportsFalseThenTrue()
        {
            // Arrange
            FileSessionStorage storage = new FileSessionStorage(_sessionFilePath);
            bool existsBefore = await storage.SessionExistsAsync(CancellationToken.None);

            // Act
            await storage.SaveSessionAsync(Session.Create("/base"), CancellationToken.None);

            // Assert
            Assert.False(existsBefore);
            Assert.True(await storage.SessionExistsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task DeleteSessionAsync_WhenSessionExists_RemovesFile()
        {
            // Arrange
            FileSessionStorage storage = new FileSessionStorage(_sessionFilePath);
            await storage.SaveSessionAsync(Session.Create("/base"), CancellationToken.None);

            // Act
            await storage.DeleteSessionAsync(CancellationToken.None);

            // Assert
            Assert.False(File.Exists(_sessionFilePath));
            Assert.False(await storage.SessionExistsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task DeleteSessionAsync_WhenNoFile_DoesNotThrow()
        {
            // Arrange
            FileSessionStorage storage = new FileSessionStorage(_sessionFilePath);

            // Act
            Exception? exception = await Record.ExceptionAsync(() => storage.DeleteSessionAsync(CancellationToken.None));

            // Assert
            Assert.Null(exception);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
    }
}
