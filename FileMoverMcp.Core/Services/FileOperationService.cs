using EasyReasy.FileStorage;
using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Services
{
    /// <summary>
    /// Service for performing file operations using EasyReasy.FileStorage.
    /// </summary>
    public class FileOperationService : IFileOperationService
    {
        /// <inheritdoc/>
        public async Task<ValidationResult> ValidateFileMoveAsync(
            FileMove fileMove,
            string basePath,
            CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = new LocalFileSystem(basePath);

            // Check if source file exists
            bool sourceExists = await fileSystem.FileExistsAsync(fileMove.SourcePath, cancellationToken);
            if (!sourceExists)
            {
                return new ValidationResult(false, $"Source file not found: {fileMove.SourcePath}");
            }

            // Check if destination file exists (conflict check)
            bool destinationExists = await fileSystem.FileExistsAsync(fileMove.DestinationPath, cancellationToken);
            if (destinationExists && !fileMove.Overwrite)
            {
                return new ValidationResult(
                    false,
                    $"Destination exists: {fileMove.DestinationPath}. Use --overwrite flag to replace.");
            }

            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public async Task ExecuteFileMoveAsync(
            FileMove fileMove,
            string basePath,
            CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = new LocalFileSystem(basePath);

            // Ensure the destination directory exists
            string? destinationDirectory = Path.GetDirectoryName(fileMove.DestinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                await fileSystem.CreateDirectoryAsync(destinationDirectory, cancellationToken);
            }

            // If destination exists and overwrite is true, delete it first
            if (fileMove.Overwrite)
            {
                bool destinationExists = await fileSystem.FileExistsAsync(
                    fileMove.DestinationPath,
                    cancellationToken);

                if (destinationExists)
                {
                    await fileSystem.DeleteFileAsync(fileMove.DestinationPath, cancellationToken);
                }
            }

            // Copy the file to the destination, then delete the source
            await fileSystem.CopyFileAsync(
                fileMove.SourcePath,
                fileMove.DestinationPath,
                cancellationToken);

            await fileSystem.DeleteFileAsync(fileMove.SourcePath, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> FileExistsAsync(
            string path,
            string basePath,
            CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = new LocalFileSystem(basePath);
            return await fileSystem.FileExistsAsync(path, cancellationToken);
        }
    }
}
