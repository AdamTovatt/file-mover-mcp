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

            if (fileMove.IsDirectory)
            {
                // Check if source directory exists
                bool sourceExists = await fileSystem.DirectoryExistsAsync(fileMove.SourcePath, cancellationToken);
                if (!sourceExists)
                {
                    return new ValidationResult(false, $"Source directory not found: {fileMove.SourcePath}");
                }

                // Check if destination directory exists (conflict check)
                bool destinationExists = await fileSystem.DirectoryExistsAsync(fileMove.DestinationPath, cancellationToken);
                if (destinationExists && !fileMove.Overwrite)
                {
                    return new ValidationResult(
                        false,
                        $"Destination exists: {fileMove.DestinationPath}. Use --overwrite flag to replace.");
                }
            }
            else
            {
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

            if (fileMove.IsDirectory)
            {
                await ExecuteDirectoryMoveAsync(fileSystem, fileMove, basePath, cancellationToken);
            }
            else
            {
                await ExecuteFileMoveInternalAsync(fileSystem, fileMove, cancellationToken);
            }
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

        /// <inheritdoc/>
        public async Task<bool> DirectoryExistsAsync(
            string path,
            string basePath,
            CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = new LocalFileSystem(basePath);
            return await fileSystem.DirectoryExistsAsync(path, cancellationToken);
        }

        private static async Task ExecuteFileMoveInternalAsync(
            IFileSystem fileSystem,
            FileMove fileMove,
            CancellationToken cancellationToken)
        {
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

        private static async Task ExecuteDirectoryMoveAsync(
            IFileSystem fileSystem,
            FileMove fileMove,
            string basePath,
            CancellationToken cancellationToken)
        {
            // If destination exists and overwrite is true, delete it first
            if (fileMove.Overwrite)
            {
                bool destinationExists = await fileSystem.DirectoryExistsAsync(
                    fileMove.DestinationPath,
                    cancellationToken);

                if (destinationExists)
                {
                    await fileSystem.DeleteDirectoryAsync(fileMove.DestinationPath, deleteNonEmpty: true, cancellationToken);
                }
            }

            // Create the destination directory
            await fileSystem.CreateDirectoryAsync(fileMove.DestinationPath, cancellationToken);

            // Enumerate all files recursively and copy each one
            string fullSourcePath = Path.GetFullPath(Path.Combine(basePath, fileMove.SourcePath));
            IEnumerable<string> allFiles = Directory.EnumerateFiles(fullSourcePath, "*", SearchOption.AllDirectories);

            foreach (string fullFilePath in allFiles)
            {
                // Get the path relative to the source directory
                string relativePath = Path.GetRelativePath(fullSourcePath, fullFilePath);

                // Construct source and destination paths relative to basePath
                string sourceRelative = Path.Combine(fileMove.SourcePath, relativePath);
                string destRelative = Path.Combine(fileMove.DestinationPath, relativePath);

                // Ensure destination subdirectory exists
                string? destDir = Path.GetDirectoryName(destRelative);
                if (!string.IsNullOrEmpty(destDir))
                {
                    await fileSystem.CreateDirectoryAsync(destDir, cancellationToken);
                }

                // Copy the file
                await fileSystem.CopyFileAsync(sourceRelative, destRelative, cancellationToken);
            }

            // Delete the source directory
            await fileSystem.DeleteDirectoryAsync(fileMove.SourcePath, deleteNonEmpty: true, cancellationToken);
        }
    }
}
