using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Services;

/// <summary>
/// Manages file move sessions.
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ISessionStorage _sessionStorage;

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
        Session? existingSession = await _sessionStorage.LoadSessionAsync(cancellationToken);
        if (existingSession != null)
        {
            throw new InvalidOperationException(
                $"Session already active at '{existingSession.BasePath}'. " +
                "Use 'fm preview' to review or 'fm cancel' to discard.");
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
        Session? session = await _sessionStorage.LoadSessionAsync(cancellationToken);
        if (session == null)
        {
            throw new InvalidOperationException(
                "No session initialized. Run 'fm init' first.");
        }

        // Add the file move to the session
        session.StagedMoves.Add(fileMove);
        await _sessionStorage.SaveSessionAsync(session, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CancelSessionAsync(CancellationToken cancellationToken)
    {
        bool sessionExists = await _sessionStorage.SessionExistsAsync(cancellationToken);
        if (!sessionExists)
        {
            throw new InvalidOperationException(
                "No session initialized. Run 'fm init' first.");
        }

        await _sessionStorage.DeleteSessionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasActiveSessionAsync(CancellationToken cancellationToken)
    {
        return await _sessionStorage.SessionExistsAsync(cancellationToken);
    }
}

