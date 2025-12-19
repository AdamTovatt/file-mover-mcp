using System.ComponentModel;
using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using ModelContextProtocol.Server;

namespace FileMoverMcp.Cli;

/// <summary>
/// MCP tools for file moving operations.
/// </summary>
[McpServerToolType]
public class McpTools
{
    private readonly ISessionManager _sessionManager;
    private readonly IFileOperationService _fileOperationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpTools"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="fileOperationService">The file operation service.</param>
    public McpTools(
        ISessionManager sessionManager,
        IFileOperationService fileOperationService)
    {
        _sessionManager = sessionManager;
        _fileOperationService = fileOperationService;
    }

    /// <summary>
    /// Initialize a new file move session.
    /// </summary>
    /// <param name="path">The base directory path. If not provided, uses current directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    [McpServerTool(Name = "fm_init")]
    [Description("Initialize a new file move session at the specified path")]
    public async Task<string> InitializeSessionAsync(
        [Description("The base directory path (optional, defaults to current directory)")]
        string? path,
        CancellationToken cancellationToken)
    {
        string basePath = string.IsNullOrEmpty(path) 
            ? Directory.GetCurrentDirectory() 
            : path;

        InitCommand command = new InitCommand(_sessionManager, basePath);
        CommandResult result = await command.ExecuteAsync(cancellationToken);

        return FormatResult(result);
    }

    /// <summary>
    /// Stage a file move operation.
    /// </summary>
    /// <param name="source">The source file path (relative to session base path).</param>
    /// <param name="destination">The destination file path (relative to session base path).</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    [McpServerTool(Name = "fm_move")]
    [Description("Stage a file move operation")]
    public async Task<string> StageMoveAsync(
        [Description("The source file path")]
        string source,
        [Description("The destination file path")]
        string destination,
        [Description("Whether to overwrite existing destination file")]
        bool overwrite,
        CancellationToken cancellationToken)
    {
        MoveCommand command = new MoveCommand(
            _sessionManager,
            _fileOperationService,
            source,
            destination,
            overwrite);

        CommandResult result = await command.ExecuteAsync(cancellationToken);
        return FormatResult(result);
    }

    /// <summary>
    /// Preview all staged file moves.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview of staged moves.</returns>
    [McpServerTool(Name = "fm_preview")]
    [Description("Preview all staged file moves")]
    public async Task<string> PreviewMovesAsync(CancellationToken cancellationToken)
    {
        PreviewCommand command = new PreviewCommand(_sessionManager);
        CommandResult result = await command.ExecuteAsync(cancellationToken);
        return FormatResult(result);
    }

    /// <summary>
    /// Commit (execute) all staged file moves.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the commit operation.</returns>
    [McpServerTool(Name = "fm_commit")]
    [Description("Execute all staged file moves and clear the session")]
    public async Task<string> CommitMovesAsync(CancellationToken cancellationToken)
    {
        CommitCommand command = new CommitCommand(_sessionManager, _fileOperationService);
        CommandResult result = await command.ExecuteAsync(cancellationToken);
        return FormatResult(result);
    }

    /// <summary>
    /// Cancel the current session and discard all staged moves.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the cancel operation.</returns>
    [McpServerTool(Name = "fm_cancel")]
    [Description("Cancel the current session and discard all staged moves")]
    public async Task<string> CancelSessionAsync(CancellationToken cancellationToken)
    {
        CancelCommand command = new CancelCommand(_sessionManager);
        CommandResult result = await command.ExecuteAsync(cancellationToken);
        return FormatResult(result);
    }

    /// <summary>
    /// Get help information about the FileMover tool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Help text.</returns>
    [McpServerTool(Name = "fm_help")]
    [Description("Get help information about the FileMover tool")]
    public async Task<string> GetHelpAsync(CancellationToken cancellationToken)
    {
        HelpCommand command = new HelpCommand();
        CommandResult result = await command.ExecuteAsync(cancellationToken);
        return FormatResult(result);
    }

    private static string FormatResult(CommandResult result)
    {
        if (string.IsNullOrEmpty(result.Details))
        {
            return result.Message;
        }

        return $"{result.Message}\n\n{result.Details}";
    }
}

