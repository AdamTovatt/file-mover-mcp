using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using Moq;
using Xunit;

namespace FileMoverMcp.Tests.Commands;

public class InitCommandTests
{
    private readonly Mock<ISessionManager> _mockSessionManager;

    public InitCommandTests()
    {
        _mockSessionManager = new Mock<ISessionManager>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        string basePath = "C:\\TestPath";
        Session session = Session.Create(basePath);

        _mockSessionManager
            .Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        InitCommand command = new InitCommand(_mockSessionManager.Object, basePath);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Session initialized", result.Message);
        Assert.Contains(basePath, result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionExists_ReturnsFailureResult()
    {
        // Arrange
        string basePath = "C:\\TestPath";
        _mockSessionManager
            .Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Session already active"));

        InitCommand command = new InitCommand(_mockSessionManager.Object, basePath);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error", result.Message);
        Assert.Contains("Session already active", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirectoryNotFound_ReturnsFailureResult()
    {
        // Arrange
        string basePath = "C:\\NonExistent";
        _mockSessionManager
            .Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DirectoryNotFoundException("Directory not found"));

        InitCommand command = new InitCommand(_mockSessionManager.Object, basePath);

        // Act
        CommandResult result = await command.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Error", result.Message);
        Assert.Contains("Directory not found", result.Message);
    }
}

