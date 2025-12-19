using System.Text;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Commands;

/// <summary>
/// Command to preview staged file moves.
/// </summary>
public class PreviewCommand : ICommand
{
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewCommand"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    public PreviewCommand(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
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
                return new CommandResult(
                    true,
                    "No moves staged.",
                    $"Session initialized at: {session.BasePath}");
            }

            StringBuilder details = new StringBuilder();
            details.AppendLine($"Session: {session.BasePath}");
            details.AppendLine($"Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            details.AppendLine();
            details.AppendLine("Staged moves:");

            for (int i = 0; i < session.StagedMoves.Count; i++)
            {
                FileMove move = session.StagedMoves[i];
                string overwriteFlag = move.Overwrite ? " [OVERWRITE]" : "";
                details.AppendLine($"  {i + 1}. {move.SourcePath} -> {move.DestinationPath}{overwriteFlag}");
            }

            return new CommandResult(
                true,
                $"{session.StagedMoves.Count} move(s) staged",
                details.ToString());
        }
        catch (Exception ex)
        {
            return new CommandResult(false, "Error: " + ex.Message);
        }
    }
}

