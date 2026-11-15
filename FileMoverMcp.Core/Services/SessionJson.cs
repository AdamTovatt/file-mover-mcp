using FileMoverMcp.Core.Models;
using System.Text.Json;

namespace FileMoverMcp.Core.Services
{
    /// <summary>
    /// The one serialized form of a <see cref="Session"/>, shared by every session storage.
    /// </summary>
    internal static class SessionJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Serializes a session to its stored JSON form.
        /// </summary>
        /// <param name="session">The session to serialize.</param>
        /// <returns>The session as JSON.</returns>
        public static string Serialize(Session session)
        {
            return JsonSerializer.Serialize(session, Options);
        }

        /// <summary>
        /// Reads a session back from its stored JSON form.
        /// </summary>
        /// <param name="json">The JSON written by <see cref="Serialize"/>.</param>
        /// <returns>The session, or null if the JSON is the literal null.</returns>
        /// <exception cref="JsonException">The JSON is not a valid session.</exception>
        public static Session? Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Session>(json, Options);
        }
    }
}
