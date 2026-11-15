using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Services
{
    /// <summary>
    /// Manages file move sessions.
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly ISessionStorage _sessionStorage;

        // Changes load, modify and save the session, and an MCP client may send tool calls
        // concurrently, so changes run one at a time.
        private readonly SemaphoreSlim _changeLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManager"/> class.
        /// </summary>
        /// <param name="sessionStorage">The session storage implementation.</param>
        public SessionManager(ISessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        /// <inheritdoc/>
        public async Task<Session?> GetActiveSessionAsync(CancellationToken cancellationToken)
        {
            return await _sessionStorage.LoadSessionAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Session> CreateSessionAsync(string basePath, CancellationToken cancellationToken)
        {
            await _changeLock.WaitAsync(cancellationToken);
            try
            {
                return await CreateSessionLockedAsync(basePath, cancellationToken);
            }
            finally
            {
                _changeLock.Release();
            }
        }

        private async Task<Session> CreateSessionLockedAsync(string basePath, CancellationToken cancellationToken)
        {
            Session? existingSession = await _sessionStorage.LoadSessionAsync(cancellationToken);
            if (existingSession != null)
            {
                throw new InvalidOperationException(
                    $"Session already active at '{existingSession.BasePath}'. " +
                    "Use preview to review it or cancel to discard it.");
            }

            // Validate and normalize the base path
            string normalizedBasePath = Path.GetFullPath(basePath);
            if (!Directory.Exists(normalizedBasePath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {normalizedBasePath}");
            }

            Session session = Session.Create(normalizedBasePath);
            await _sessionStorage.SaveSessionAsync(session, cancellationToken);

            return session;
        }

        /// <inheritdoc/>
        public async Task AddFileMoveAsync(FileMove fileMove, CancellationToken cancellationToken)
        {
            await _changeLock.WaitAsync(cancellationToken);
            try
            {
                Session? session = await _sessionStorage.LoadSessionAsync(cancellationToken);
                if (session == null)
                {
                    throw new InvalidOperationException(
                        "No session initialized. Run init first.");
                }

                session.StagedMoves.Add(fileMove);
                await _sessionStorage.SaveSessionAsync(session, cancellationToken);
            }
            finally
            {
                _changeLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CancelSessionAsync(CancellationToken cancellationToken)
        {
            await _changeLock.WaitAsync(cancellationToken);
            try
            {
                bool sessionExists = await _sessionStorage.SessionExistsAsync(cancellationToken);
                if (!sessionExists)
                {
                    throw new InvalidOperationException(
                        "No session initialized. Run init first.");
                }

                await _sessionStorage.DeleteSessionAsync(cancellationToken);
            }
            finally
            {
                _changeLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HasActiveSessionAsync(CancellationToken cancellationToken)
        {
            return await _sessionStorage.SessionExistsAsync(cancellationToken);
        }
    }
}
