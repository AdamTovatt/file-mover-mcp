using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Interfaces;

/// <summary>
/// Service for performing file operations.
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Validates that a source file exists and destination path is valid.
    /// </summary>
    /// <param name="fileMove">The file move to validate.</param>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Validation result containing success status and any error messages.</returns>
    Task<ValidationResult> ValidateFileMoveAsync(
        FileMove fileMove,
        string basePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a file move operation.
    /// </summary>
    /// <param name="fileMove">The file move to execute.</param>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ExecuteFileMoveAsync(
        FileMove fileMove,
        string basePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The relative path to check.</param>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(
        string path,
        string basePath,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of a file move validation.
/// </summary>
/// <param name="IsValid">Indicates whether the file move is valid.</param>
/// <param name="ErrorMessage">The error message if validation failed.</param>
public record ValidationResult(bool IsValid, string? ErrorMessage = null);

