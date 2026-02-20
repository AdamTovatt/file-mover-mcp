namespace FileMoverMcp.Core.Models
{
    /// <summary>
    /// Represents a file move operation to be staged and executed.
    /// </summary>
    /// <param name="SourcePath">The relative path of the source file.</param>
    /// <param name="DestinationPath">The relative path of the destination file.</param>
    /// <param name="Overwrite">Indicates whether to overwrite the destination if it exists.</param>
    /// <param name="IsDirectory">Indicates whether this move operates on a directory rather than a single file.</param>
    public record FileMove(string SourcePath, string DestinationPath, bool Overwrite, bool IsDirectory = false);

}
