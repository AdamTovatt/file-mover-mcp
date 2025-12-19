using System.Text.Json.Serialization;

namespace FileMoverMcp.Core.Models
{
    /// <summary>
    /// Represents a file move session containing staged file moves.
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Gets or initializes the unique identifier for this session.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the base path for this session.
        /// </summary>
        [JsonPropertyName("basePath")]
        public string BasePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the list of staged file moves.
        /// </summary>
        [JsonPropertyName("stagedMoves")]
        public List<FileMove> StagedMoves { get; init; } = new List<FileMove>();

        /// <summary>
        /// Gets or initializes the creation timestamp of this session.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new session with the specified base path.
        /// </summary>
        /// <param name="basePath">The base directory path for file operations.</param>
        /// <returns>A new session instance.</returns>
        public static Session Create(string basePath)
        {
            return new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                BasePath = basePath,
                StagedMoves = new List<FileMove>(),
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
