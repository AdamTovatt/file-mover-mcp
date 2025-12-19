using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;

namespace FileMoverMcp.Core.Services;

/// <summary>
/// Factory for creating command instances from command-line arguments.
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly ISessionManager _sessionManager;
    private readonly IFileOperationService _fileOperationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandFactory"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="fileOperationService">The file operation service.</param>
    public CommandFactory(
        ISessionManager sessionManager,
        IFileOperationService fileOperationService)
    {
        _sessionManager = sessionManager;
        _fileOperationService = fileOperationService;
    }

    /// <inheritdoc/>
    public ICommand CreateCommand(string[] args)
    {
        if (args.Length == 0)
        {
            return new HelpCommand();
        }

        string commandName = args[0].ToLowerInvariant();

        return commandName switch
        {
            "init" => CreateInitCommand(args),
            "mv" or "move" => CreateMoveCommand(args),
            "preview" => CreatePreviewCommand(args),
            "commit" => CreateCommitCommand(args),
            "cancel" => CreateCancelCommand(args),
            "help" or "--help" or "-h" => new HelpCommand(),
            _ => throw new ArgumentException(
                $"Unknown command: '{commandName}'. Use 'fm help' to see available commands.")
        };
    }

    private ICommand CreateInitCommand(string[] args)
    {
        // fm init [path]
        string basePath;

        if (args.Length > 1)
        {
            basePath = args[1];
        }
        else
        {
            // Default to current directory
            basePath = Directory.GetCurrentDirectory();
        }

        return new InitCommand(_sessionManager, basePath);
    }

    private ICommand CreateMoveCommand(string[] args)
    {
        // fm mv <source> <dest> [--overwrite]
        if (args.Length < 3)
        {
            throw new ArgumentException(
                "Move command requires source and destination paths. " +
                "Usage: fm mv <source> <destination> [--overwrite]");
        }

        string sourcePath = args[1];
        string destinationPath = args[2];
        bool overwrite = args.Length > 3 && args[3].Equals("--overwrite", StringComparison.OrdinalIgnoreCase);

        return new MoveCommand(
            _sessionManager,
            _fileOperationService,
            sourcePath,
            destinationPath,
            overwrite);
    }

    private ICommand CreatePreviewCommand(string[] args)
    {
        // fm preview
        return new PreviewCommand(_sessionManager);
    }

    private ICommand CreateCommitCommand(string[] args)
    {
        // fm commit
        return new CommitCommand(_sessionManager, _fileOperationService);
    }

    private ICommand CreateCancelCommand(string[] args)
    {
        // fm cancel
        return new CancelCommand(_sessionManager);
    }
}

