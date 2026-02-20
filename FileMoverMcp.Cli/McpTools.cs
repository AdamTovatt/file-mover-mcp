using FileMoverMcp.Core.Commands;
using FileMoverMcp.Core.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace FileMoverMcp.Cli
{
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
        [Description("Initialize a new file move session. Must be called before any other fm_ tool. All paths used in fm_move will be relative to the base path set here. Only one session can be active at a time.")]
        public async Task<string> InitializeSessionAsync(
            [Description("The base directory path. All subsequent move operations will use paths relative to this directory. Defaults to current directory if not provided.")]
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
        [Description("Stage a file or directory move. Can be called multiple times to add more moves to the session before committing. The source type (file or directory) is auto-detected. Directory moves preserve the full internal structure including nested subdirectories. Destination directories that don't exist will be created automatically. Nothing is moved until fm_commit is called.")]
        public async Task<string> StageMoveAsync(
            [Description("Source path relative to the session base directory. Can be a file or a directory.")]
            string source,
            [Description("Destination path relative to the session base directory. Parent directories will be created if they don't exist.")]
            string destination,
            [Description("Set to true to replace the destination if it already exists. If false and the destination exists, the operation will be rejected.")]
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
        [Description("Preview all staged moves before committing. Shows each move with its source, destination, and flags like [DIR] for directory moves and [OVERWRITE] for overwrites.")]
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
        [Description("Execute all staged moves and clear the session. This is when files and directories are actually moved. Each move reports success or failure individually. The session is cleared afterwards regardless of outcome.")]
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
        [Description("Cancel the current session and discard all staged moves without executing them. No files or directories will be moved.")]
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
        [Description("Get detailed help information about all available FileMover commands and their usage.")]
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
}
