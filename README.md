# FileMoverMcp

A file mover tool that stages and executes file moves safely. Works as both a CLI tool and an MCP (Model Context Protocol) server for AI agents.

## Features

- **Session-based workflow**: Stage multiple file moves, preview them, then commit all at once
- **Safety first**: Preview changes before executing, validate file paths and conflicts
- **Dual mode**: Works as both a command-line tool (`fm`) and an MCP server for AI agents
- **Overwrite control**: Explicit opt-in for overwriting existing files
- **Clear error messages**: Actionable guidance when things go wrong

## Installation

### As a .NET Global Tool

```bash
dotnet tool install --global FileMoverMcp
```

After installation, the `fm` command will be available globally, the CLI is ready to use.

If you want it as an MCP tool in for example Cursor, add this to your MCP configuration:

```json
{
  "mcpServers": {
    "filemover": {
      "command": "fm",
      "args": ["--mcp"]
    }
  }
}
```

## Usage

### CLI Tool

#### Workflow

1. Initialize a session in a directory
2. Stage file moves
3. Preview the changes
4. Commit (or cancel) the moves

#### Commands

**Initialize a session:**
```bash
fm init [path]              # Initialize at specific path
fm init                     # Initialize at current directory
```

**Stage file moves:**
```bash
fm mv <source> <dest>       # Stage a file move
fm mv <source> <dest> --overwrite  # Allow overwriting
```

**Preview staged changes:**
```bash
fm preview                  # See all staged moves
```

**Execute or cancel:**
```bash
fm commit                   # Execute all staged moves
fm cancel                   # Discard all staged moves
```

**Get help:**
```bash
fm help                     # Show help information
```

#### Examples

```bash
# Move files on desktop
fm init "c:/users/adam/desktop"
fm mv "old-folder/file.png" "new-folder/file.png"
fm mv "document.txt" "archive/document-backup.txt"
fm preview
fm commit

# Overwrite an existing file
fm init
fm mv "source.txt" "destination.txt" --overwrite
fm commit

# Cancel without executing
fm init
fm mv "file1.txt" "file2.txt"
fm preview
fm cancel
```

### As MCP Server

Run the tool in MCP server mode:

```bash
fm --mcp
```

Or using dotnet run during development:

```bash
dotnet run --project FileMoverMcp.Cli/FileMoverMcp.Cli.csproj -- --mcp
```

#### MCP Tools

When running as an MCP server, the following tools are available:

- `fm_init(path?: string)` - Initialize a session
- `fm_move(source: string, destination: string, overwrite: boolean)` - Stage a move
- `fm_preview()` - Preview staged moves
- `fm_commit()` - Execute staged moves
- `fm_cancel()` - Cancel session
- `fm_help()` - Get help

#### MCP Configuration Example

Add to your MCP client configuration:

```json
{
  "mcpServers": {
    "filemover": {
      "command": "fm",
      "args": ["--mcp"]
    }
  }
}
```

## Architecture

The project follows clean architecture principles:

- **FileMoverMcp.Core** - Core business logic, interfaces, and models
  - Commands (Init, Move, Preview, Commit, Cancel, Help)
  - Services (SessionManager, SessionStorage, FileOperationService)
  - Models (Session, FileMove)
  - Interfaces (ICommand, ISessionManager, etc.)

- **FileMoverMcp.Cli** - CLI and MCP server entry point
  - Program.cs - Dual-mode entry point
  - McpTools.cs - MCP tool definitions

- **FileMoverMcp.Tests** - Comprehensive test suite
  - Unit tests for all commands and services
  - Integration tests for full workflows

### Key Design Decisions

1. **Session storage in temp directory** - Sessions survive across CLI invocations and are cleaned up by the OS
2. **Files only** - Simplified scope, no directory recursion
3. **Overwrite flag** - Explicit opt-in for safety
4. **Shared sessions** - CLI and MCP modes share the same session storage
5. **Command pattern** - Clean, testable, extensible architecture
6. **EasyReasy.FileStorage** - Secure file operations with directory traversal protection

## Error Handling

The tool provides clear, actionable error messages:

- **No session initialized**: `"Error: No session initialized. Run 'fm init' first."`
- **Session already exists**: `"Error: Session already active at [path]. Use 'fm preview' to review or 'fm cancel' to discard."`
- **Source file not found**: `"Error: Source file not found: [path]"`
- **Destination exists**: `"Error: Destination exists: [path]. Use --overwrite flag to replace."`
- **Path outside base directory**: `"Error: Path is outside initialized directory: [path]"`

## Testing

The project includes comprehensive tests:

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

**Test Coverage:**
- 31 unit tests covering all commands and services
- 8 integration tests covering full workflows
- All tests passing ✓

## Dependencies

- **.NET 8.0** - Target framework
- **ModelContextProtocol** (0.5.0-preview.1) - MCP server functionality
- **EasyReasy.FileStorage** (2.0.2) - Secure file operations
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **Microsoft.Extensions.Hosting** - Hosting infrastructure

## Development

### Getting the repository

```bash
git clone <repository-url>
cd FileMoverMcp
```

### Building

```bash
dotnet build FileMoverMcp.sln
```

### Running Tests

```bash
dotnet test FileMoverMcp.sln
```

### Packaging

```bash
dotnet pack FileMoverMcp.Cli/FileMoverMcp.Cli.csproj --configuration Release
```

The package will be created in `FileMoverMcp.Cli/nupkg/`.

### Project Structure

```
FileMoverMcp/
├── FileMoverMcp.Core/
│   ├── Commands/        # Command implementations
│   ├── Interfaces/      # Core interfaces
│   ├── Models/          # Domain models
│   └── Services/        # Business logic services
├── FileMoverMcp.Cli/
│   ├── Program.cs       # Entry point
│   └── McpTools.cs      # MCP tool definitions
├── FileMoverMcp.Tests/
│   ├── Commands/        # Command unit tests
│   ├── Services/        # Service unit tests
│   └── Integration/     # Integration tests
└── README.md
```

## License

MIT License

## Contributing

Contributions are welcome! Please ensure all tests pass before submitting a pull request.
