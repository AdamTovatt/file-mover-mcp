using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;

namespace FileMoverMcp.Tests.Commands
{
    public class HelpCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsSuccessWithHelpText()
        {
            // Arrange
            HelpCommand command = new HelpCommand();

            // Act
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.Message);
            Assert.Contains("FileMover", result.Message);
            Assert.Contains("init", result.Message);
            Assert.Contains("mv", result.Message);
            Assert.Contains("preview", result.Message);
            Assert.Contains("commit", result.Message);
            Assert.Contains("cancel", result.Message);
        }

        [Fact]
        public void HelpText_ContainsAllCommands()
        {
            // Arrange & Act
            string helpText = HelpCommand.HelpText;

            // Assert
            Assert.Contains("init", helpText);
            Assert.Contains("mv", helpText);
            Assert.Contains("preview", helpText);
            Assert.Contains("commit", helpText);
            Assert.Contains("cancel", helpText);
            Assert.Contains("help", helpText);
        }

        [Fact]
        public void HelpText_ContainsExamples()
        {
            // Arrange & Act
            string helpText = HelpCommand.HelpText;

            // Assert
            Assert.Contains("EXAMPLES", helpText);
            Assert.Contains("fm init", helpText);
            Assert.Contains("fm mv", helpText);
        }

        [Fact]
        public void HelpText_ContainsErrorHandling()
        {
            // Arrange & Act
            string helpText = HelpCommand.HelpText;

            // Assert
            Assert.Contains("ERROR HANDLING", helpText);
            Assert.Contains("No session initialized", helpText);
        }
    }
}
