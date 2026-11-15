using FileMoverMcp.Core.Models;
using FileMoverMcp.Core.Services;

namespace FileMoverMcp.Tests.Services
{
    public class InMemorySessionStorageTests
    {
        [Fact]
        public async Task SaveSessionAsync_OnOneInstance_LeavesOtherInstanceEmpty()
        {
            // Arrange
            InMemorySessionStorage first = new InMemorySessionStorage();
            InMemorySessionStorage second = new InMemorySessionStorage();

            // Act
            await first.SaveSessionAsync(Session.Create("/first"), CancellationToken.None);

            // Assert
            Assert.True(await first.SessionExistsAsync(CancellationToken.None));
            Assert.False(await second.SessionExistsAsync(CancellationToken.None));
            Assert.Null(await second.LoadSessionAsync(CancellationToken.None));
        }

        [Fact]
        public async Task LoadSessionAsync_WhenLoadedCopyIsChanged_StoredSessionIsUnchanged()
        {
            // Arrange
            InMemorySessionStorage storage = new InMemorySessionStorage();
            await storage.SaveSessionAsync(Session.Create("/base"), CancellationToken.None);
            Session loaded = (await storage.LoadSessionAsync(CancellationToken.None))!;

            // Act
            loaded.StagedMoves.Add(new FileMove("a.txt", "b.txt", false));

            // Assert
            Session reloaded = (await storage.LoadSessionAsync(CancellationToken.None))!;
            Assert.Empty(reloaded.StagedMoves);
        }

        [Fact]
        public async Task SaveSessionAsync_WithChangedSession_StoresTheChange()
        {
            // Arrange
            InMemorySessionStorage storage = new InMemorySessionStorage();
            FileMove expectedMove = new FileMove("a.txt", "b.txt", false);
            Session session = Session.Create("/base");
            session.StagedMoves.Add(expectedMove);

            // Act
            await storage.SaveSessionAsync(session, CancellationToken.None);

            // Assert
            Session reloaded = (await storage.LoadSessionAsync(CancellationToken.None))!;
            Assert.Equal(new[] { expectedMove }, reloaded.StagedMoves);
        }

        [Fact]
        public async Task DeleteSessionAsync_WhenSessionExists_RemovesSession()
        {
            // Arrange
            InMemorySessionStorage storage = new InMemorySessionStorage();
            await storage.SaveSessionAsync(Session.Create("/base"), CancellationToken.None);

            // Act
            await storage.DeleteSessionAsync(CancellationToken.None);

            // Assert
            Assert.False(await storage.SessionExistsAsync(CancellationToken.None));
            Assert.Null(await storage.LoadSessionAsync(CancellationToken.None));
        }
    }
}
