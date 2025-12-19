using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using Moq;
using Xunit;

namespace FileMoverMcp.Tests.Commands;

public class PreviewCommandTests
{
    private readonly Mock<ISessionManager> _mockSessionManager;

    public PreviewCommandTests()
    {
        _mockSessionManager = new Mock<ISessionManager>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoSession_ReturnsFailureResult()
    {
        // Arrange
        _mockSessionManager
            .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        PreviewCommand command = new PreviewCommand(_mockSessionManager.Object);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No session initialized", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionHasNoMoves_ReturnsSuccessWithNoMoves()
    {
        // Arrange
        Session session = Session.Create("C:\\TestPath");
        _mockSessionManager
            .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        PreviewCommand command = new PreviewCommand(_mockSessionManager.Object);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("No moves staged", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionHasMoves_ReturnsSuccessWithDetails()
    {
        // Arrange
        Session session = Session.Create("C:\\TestPath");
        session.StagedMoves.Add(new FileMove("file1.txt", "file2.txt", false));
        session.StagedMoves.Add(new FileMove("file3.txt", "file4.txt", true));

        _mockSessionManager
            .Setup(x => x.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        PreviewCommand command = new PreviewCommand(_mockSessionManager.Object);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("2 move(s) staged", result.Message);
        Assert.NotNull(result.Details);
        Assert.Contains("file1.txt", result.Details);
        Assert.Contains("file2.txt", result.Details);
        Assert.Contains("[OVERWRITE]", result.Details);
    }
}

