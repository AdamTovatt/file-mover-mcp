using FileMoverMcp.Core.Interfaces;
using FileMoverMcp.Core.Models;
using System.Text.Json;

namespace FileMoverMcp.Core.Services
{
    /// <summary>
    /// Keeps the session in a JSON file, so it survives between separate CLI invocations.
    /// Every process using the same file shares one session.
    /// </summary>
    public class FileSessionStorage : ISessionStorage
    {
        private readonly string _sessionFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSessionStorage"/> class.
        /// </summary>
        /// <param name="sessionFilePath">The file the session is kept in.</param>
        public FileSessionStorage(string sessionFilePath)
        {
            _sessionFilePath = sessionFilePath;
        }

        /// <inheritdoc/>
        public async Task<Session?> LoadSessionAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_sessionFilePath))
            {
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(_sessionFilePath, cancellationToken);
                return SessionJson.Deserialize(json);
            }
            catch (JsonException)
            {
                // A corrupt file can never load, so drop it. A read error (IOException) is left to
                // throw: it may be temporary, and deleting would lose the staged moves.
                await DeleteSessionAsync(cancellationToken);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SaveSessionAsync(Session session, CancellationToken cancellationToken)
        {
            // Plain write, no temp-file-and-rename: only one person's CLI commands use this file,
            // one at a time, and a torn file loads as corrupt and is dropped.
            await File.WriteAllTextAsync(_sessionFilePath, SessionJson.Serialize(session), cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteSessionAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> SessionExistsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(File.Exists(_sessionFilePath));
        }
    }
}
