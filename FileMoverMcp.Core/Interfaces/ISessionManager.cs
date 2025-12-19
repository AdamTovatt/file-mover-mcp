using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Interfaces
{
    /// <summary>
    /// Manages file move sessions.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Gets the currently active session, if one exists.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The active session, or null if no session exists.</returns>
        Task<Session?> GetActiveSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new session with the specified base path.
        /// </summary>
        /// <param name="basePath">The base directory path for file operations.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The newly created session.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a session already exists.</exception>
        Task<Session> CreateSessionAsync(string basePath, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a file move to the active session.
        /// </summary>
        /// <param name="fileMove">The file move to stage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task AddFileMoveAsync(FileMove fileMove, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels and removes the active session.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task CancelSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Validates that an active session exists.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if a session exists, false otherwise.</returns>
        Task<bool> HasActiveSessionAsync(CancellationToken cancellationToken);
    }
}
