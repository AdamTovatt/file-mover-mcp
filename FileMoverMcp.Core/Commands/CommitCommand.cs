using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using System.Text;

namespace FileMoverMcp.Core.Commands
{
    /// <summary>
    /// Command to commit (execute) all staged file moves.
    /// </summary>
    public class CommitCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly IFileOperationService _fileOperationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="fileOperationService">The file operation service.</param>
        public CommitCommand(
            ISessionManager sessionManager,
            IFileOperationService fileOperationService)
        {
            _sessionManager = sessionManager;
            _fileOperationService = fileOperationService;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Session? session = await _sessionManager.GetActiveSessionAsync(cancellationToken);
                if (session == null)
                {
                    return new CommandResult(
                        false,
                        "Error: No session initialized. Run 'fm init' first.");
                }

                if (session.StagedMoves.Count == 0)
                {
                    await _sessionManager.CancelSessionAsync(cancellationToken);
                    return new CommandResult(true, "No moves to commit. Session cleared.");
                }

                // Execute each move
                List<string> successfulMoves = new List<string>();
                List<string> failedMoves = new List<string>();

                foreach (FileMove move in session.StagedMoves)
                {
                    try
                    {
                        await _fileOperationService.ExecuteFileMoveAsync(
                            move,
                            session.BasePath,
                            cancellationToken);

                        successfulMoves.Add($"{move.SourcePath} -> {move.DestinationPath}");
                    }
                    catch (Exception ex)
                    {
                        failedMoves.Add($"{move.SourcePath} -> {move.DestinationPath}: {ex.Message}");
                    }
                }

                // Clear the session
                await _sessionManager.CancelSessionAsync(cancellationToken);

                // Build result message
                StringBuilder details = new StringBuilder();
                if (successfulMoves.Count > 0)
                {
                    details.AppendLine($"Successfully moved {successfulMoves.Count} file(s):");
                    foreach (string move in successfulMoves)
                    {
                        details.AppendLine($"  ✓ {move}");
                    }
                }

                if (failedMoves.Count > 0)
                {
                    details.AppendLine();
                    details.AppendLine($"Failed to move {failedMoves.Count} file(s):");
                    foreach (string move in failedMoves)
                    {
                        details.AppendLine($"  ✗ {move}");
                    }
                }

                bool allSuccessful = failedMoves.Count == 0;
                string message = allSuccessful
                    ? $"Committed {successfulMoves.Count} move(s) successfully"
                    : $"Completed with {failedMoves.Count} error(s)";

                return new CommandResult(allSuccessful, message, details.ToString());
            }
            catch (Exception ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }
    }
}
