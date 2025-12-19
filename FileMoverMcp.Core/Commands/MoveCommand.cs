using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Commands;

/// <summary>
/// Command to stage a file move operation.
/// </summary>
public class MoveCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly IFileOperationService _fileOperationService;
    private readonly string _sourcePath;
    private readonly string _destinationPath;
    private readonly bool _overwrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveCommand"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="fileOperationService">The file operation service.</param>
    /// <param name="sourcePath">The source file path.</param>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    public MoveCommand(
        ISessionManager sessionManager,
        IFileOperationService fileOperationService,
        string sourcePath,
        string destinationPath,
        bool overwrite)
    {
        _sessionManager = sessionManager;
        _fileOperationService = fileOperationService;
        _sourcePath = sourcePath;
        _destinationPath = destinationPath;
        _overwrite = overwrite;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Ensure a session exists
            Session? session = await _sessionManager.GetActiveSessionAsync(cancellationToken);
            if (session == null)
            {
                return new CommandResult(
                    false,
                    "Error: No session initialized. Run 'fm init' first.");
            }

            // Create the file move
            FileMove fileMove = new FileMove(_sourcePath, _destinationPath, _overwrite);

            // Validate the file move
            ValidationResult validationResult = await _fileOperationService.ValidateFileMoveAsync(
                fileMove,
                session.BasePath,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                return new CommandResult(false, "Error: " + validationResult.ErrorMessage);
            }

            // Add to session
            await _sessionManager.AddFileMoveAsync(fileMove, cancellationToken);

            string message = $"Staged move: {_sourcePath} -> {_destinationPath}";
            return new CommandResult(true, message);
        }
        catch (InvalidOperationException ex)
        {
            return new CommandResult(false, "Error: " + ex.Message);
        }
    }
}

