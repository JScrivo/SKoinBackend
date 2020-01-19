using System.Text.Json;

namespace Sheridan.SKoin.API
{
    /// <summary>
    /// A static class for safely serializing objects and deserializing JSON strings.
    /// </summary>
    public class Json
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="json">The serialized JSON string.</param>
        /// <returns>True if the serialization was successful, false otherwise.</returns>
        public static bool TrySerialize<T>(T obj, out string json)
        {
            try
            {
                json = JsonSerializer.Serialize(obj);
                return true;
            }
            catch
            {
                json = null;
                return false;
            }
        }

        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="obj">The deserialized object.</param>
        /// <returns>True if the deserialization was successful, false otherwise.</returns>
        public static bool TryDeserialize<T>(string json, out T obj)
        {
            try
            {
                obj = JsonSerializer.Deserialize<T>(json);
                return true;
            }
            catch
            {
                obj = default;
                return false;
            }
        }
    }
}
