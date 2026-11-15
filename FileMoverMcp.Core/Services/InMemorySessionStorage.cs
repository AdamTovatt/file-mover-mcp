using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;

namespace FileMoverMcp.Core.Services
{
    /// <summary>
    /// Keeps the session in this process only, so each MCP server (one per agent) has its own.
    /// Holds the serialized form, so a loaded session is a private copy until it is saved,
    /// exactly as with <see cref="FileSessionStorage"/>.
    /// </summary>
    public class InMemorySessionStorage : ISessionStorage
    {
        private volatile string? _sessionJson;

        /// <inheritdoc/>
        public Task<Session?> LoadSessionAsync(CancellationToken cancellationToken)
        {
            string? json = _sessionJson;
            return Task.FromResult(json == null ? null : SessionJson.Deserialize(json));
        }

        /// <inheritdoc/>
        public Task SaveSessionAsync(Session session, CancellationToken cancellationToken)
        {
            _sessionJson = SessionJson.Serialize(session);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSessionAsync(CancellationToken cancellationToken)
        {
            _sessionJson = null;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> SessionExistsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_sessionJson != null);
        }
    }
}
