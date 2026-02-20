# FileMoverMcp

[![Tests](https://github.com/AdamTovatt/file-mover-mcp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AdamTovatt/file-mover-mcp/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/FileMoverMcp.svg)](https://www.nuget.org/packages/FileMoverMcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FileMoverMcp.svg)](https://www.nuget.org/packages/FileMoverMcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A file mover tool that stages and executes file moves safely. Works as both a CLI tool and an MCP (Model Context Protocol) server for AI agents.

## Installation

```bash
dotnet tool install --global FileMoverMcp
```

```bash
dotnet tool update --global FileMoverMcp
```

```bash
dotnet tool uninstall --global FileMoverMcp
```

After installation, the `fm` command will be available globally.

### MCP setup

To register as an MCP tool in Claude Code:

```bash
claude mcp add filemover -- fm --mcp
```

For Cursor or other MCP clients, add this to your MCP configuration:

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

The workflow is: initialize a session, stage moves, preview, then commit (or cancel).

```bash
fm init [path]                      # Initialize at path (default: current directory)
fm mv <source> <dest>               # Stage a file or directory move
fm mv <source> <dest> --overwrite   # Allow overwriting existing destination
fm preview                          # See all staged moves
fm commit                           # Execute all staged moves
fm cancel                           # Discard all staged moves
fm help                             # Show help information
```

### Examples

```bash
# Move files on desktop
fm init "c:/users/adam/desktop"
fm mv "old-folder/file.png" "new-folder/file.png"
fm mv "document.txt" "archive/document-backup.txt"
fm preview
fm commit

# Move an entire directory
fm init
fm mv "source-folder" "dest-folder"
fm commit

# Overwrite an existing file
fm init
fm mv "source.txt" "destination.txt" --overwrite
fm commit
```

## Behavior

### Directories are auto-created

If the destination path includes directories that don't exist yet, they will be created automatically. For example, `fm mv "file.txt" "a/b/c/file.txt"` will create `a/b/c/` if needed.

### Files and directories are auto-detected

When you run `fm mv`, the tool checks whether the source is a file or a directory and handles it accordingly. Directory moves preserve the full internal structure, including nested subdirectories.

### Overwrite is opt-in

If the destination already exists (file or directory), the move will be rejected unless you pass `--overwrite`. With `--overwrite`, the existing destination is replaced entirely.

### Paths are sandboxed

All paths are relative to the session's base directory. Attempting to reference paths outside it will be rejected.

### Sessions are shared

CLI and MCP mode share the same session storage, so you can initialize a session in one mode and commit in the other.

## As MCP Server

```bash
fm --mcp
```

When running as an MCP server, the following tools are available:

- `fm_init(path?: string)` - Initialize a session
- `fm_move(source: string, destination: string, overwrite: boolean)` - Stage a move
- `fm_preview()` - Preview staged moves
- `fm_commit()` - Execute staged moves
- `fm_cancel()` - Cancel session
- `fm_help()` - Get help

## Development

```bash
git clone <repository-url>
cd FileMoverMcp
dotnet build FileMoverMcp.sln
dotnet test FileMoverMcp.sln
```

To run as MCP server during development:

```bash
dotnet run --project FileMoverMcp.Cli/FileMoverMcp.Cli.csproj -- --mcp
```

To package:

```bash
dotnet pack FileMoverMcp.Cli/FileMoverMcp.Cli.csproj --configuration Release
```

## License

MIT License
