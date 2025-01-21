using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text;
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

        public string Serialize<T>(T obj)
            => JsonSerializer.Serialize(obj, _serializerOptions);

        public string Serialize<T>(T obj, bool pretty)
            => JsonSerializer.Serialize(obj, pretty ? _prettySerializerOptions : _serializerOptions);

        public void Serialize<T>(IBufferWriter<byte> writer, T obj)
        {
            var jsonWriter = new Utf8JsonWriter(writer); //TODO: cache utf8 writer
            JsonSerializer.Serialize(jsonWriter, obj, _serializerOptions);
        }

        public T Deserialize<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, _serializerOptions)
            ?? ThrowDeserializeFailure<T>(json);

        public T Deserialize<T>(ReadOnlySpan<byte> spans) =>
            JsonSerializer.Deserialize<T>(spans, _serializerOptions)
            ?? ThrowDeserializeFailure<T>(Encoding.UTF8.GetString(spans));

        private static T ThrowDeserializeFailure<T>(string source) =>
            throw new SerializationException($"Failed to deserialize {typeof(T).FullName}, source: {source}");
    }
}