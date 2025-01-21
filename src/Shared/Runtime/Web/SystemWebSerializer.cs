using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Shared.Web
{
    public class SystemWebSerializer : IWebSerializer
    {
        //ASP Web Controller default json formatting
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
        {
            IncludeFields = true
        };

        private static readonly JsonSerializerOptions _prettySerializerOptions = new(_serializerOptions)
        {
            WriteIndented = true
        };

        public string SerializeObject<T>(T obj)
            => JsonSerializer.Serialize(obj, _serializerOptions);

        public string SerializeObject<T>(T obj, bool pretty)
            => JsonSerializer.Serialize(obj, pretty ? _prettySerializerOptions : _serializerOptions);

        public void SerializeToWriter<T>(IBufferWriter<byte> writer, T obj)
        {
            var jsonWriter = new Utf8JsonWriter(writer); //TODO: cache utf8 writer
            JsonSerializer.Serialize(jsonWriter, obj, _serializerOptions);
        }

        public T DeserializeObject<T>(string json)
        {
            var obj = JsonSerializer.Deserialize<T>(json, _serializerOptions);
            if (obj == null)
                throw new SerializationException($"Failed to deserialize {typeof(T).FullName} from json: {json}");
            return obj;
        }
    }
}