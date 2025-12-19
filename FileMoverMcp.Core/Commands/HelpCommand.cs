using FileMoverMcp.Core.Interfaces;

namespace FileMoverMcp.Core.Commands
{
    /// <summary>
    /// Command to display help information.
    /// </summary>
    public class HelpCommand : ICommand
    {
        /// <summary>
        /// Gets the help text for the FileMover tool.
        /// </summary>
        public static string HelpText => @"FileMover - Stage and execute file moves safely

WORKFLOW:
  1. fm init [path]       - Start a new session
  2. fm mv <src> <dst>    - Stage file moves
  3. fm preview           - Review staged changes
  4. fm commit            - Execute all moves

COMMANDS:
  init [path]             Initialize session at path (default: current directory)
                          Response confirms initialization and displays the path.
                          If a session already exists, displays error with
                          instructions to preview or cancel.

  mv <source> <dest>      Stage a file move
                          Example: fm mv ""folder1/file.txt"" ""folder2/file.txt""

  mv <source> <dest> --overwrite
                          Stage a file move with overwrite permission
                          Example: fm mv ""file.txt"" ""backup.txt"" --overwrite

  preview                 Show all staged moves with details
                          Displays session path, creation time, and list of moves

  commit                  Execute all staged moves and clear session
                          Shows success/failure for each move

  cancel                  Cancel current session and discard all staged moves

  help                    Show this help message

EXAMPLES:
  fm init ""c:/users/adam/desktop""
  fm mv ""test1/file.png"" ""test2/file.png""
  fm mv ""test1/file2.png"" ""test1/file1.png"" --overwrite
  fm preview
  fm commit

ERROR HANDLING:
  - If no session exists when running mv/preview/commit/cancel:
    ""Error: No session initialized. Run 'fm init' first.""

  - If session already exists when running init:
    ""Error: Session already active at [path]. Use 'fm preview' to review or 'fm cancel' to discard.""

  - If source file not found:
    ""Error: Source file not found: [path]""

  - If destination exists without --overwrite:
    ""Error: Destination exists: [path]. Use --overwrite flag to replace.""

  - If path is outside initialized directory:
    ""Error: Path is outside initialized directory: [path]""

AI AGENT USAGE:
  When using as an MCP server, the same commands are available as tools:
  - fm_init(path?: string)
  - fm_move(source: string, destination: string, overwrite?: boolean)
  - fm_preview()
  - fm_commit()
  - fm_cancel()
  - fm_help()

For more information, visit: https://github.com/AdamTovatt/file-mover-mcp
";

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new CommandResult(true, HelpText));
        }
    }
}
