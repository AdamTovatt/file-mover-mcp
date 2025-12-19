namespace FileMoverMcp.Core.Models;

/// <summary>
/// Represents a file move operation to be staged and executed.
/// </summary>
/// <param name="SourcePath">The relative path of the source file.</param>
/// <param name="DestinationPath">The relative path of the destination file.</param>
/// <param name="Overwrite">Indicates whether to overwrite the destination if it exists.</param>
public record FileMove(string SourcePath, string DestinationPath, bool Overwrite);

