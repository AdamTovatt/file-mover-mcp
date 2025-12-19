using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using Moq;
using Xunit;

namespace FileMoverMcp.Tests.Commands;

public class CancelCommandTests
{
    private readonly Mock<ISessionManager> _mockSessionManager;

    public CancelCommandTests()
    {
        _mockSessionManager = new Mock<ISessionManager>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionExists_ReturnsSuccess()
    {
        // Arrange
        _mockSessionManager
            .Setup(x => x.CancelSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        CancelCommand command = new CancelCommand(_mockSessionManager.Object);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Session cancelled", result.Message);
        _mockSessionManager.Verify(
            x => x.CancelSessionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager
            .Setup(x => x.CancelSessionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No session initialized"));

        CancelCommand command = new CancelCommand(_mockSessionManager.Object);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No session initialized", result.Message);
    }
}

