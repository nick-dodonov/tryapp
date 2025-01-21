using System;
using System.Buffers;
using System.Runtime.CompilerServices;
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

        [ThreadStatic] 
        private static Utf8JsonWriter? _cachedUtf8JsonWriter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Utf8JsonWriter GetCachedUtf8JsonWriter(IBufferWriter<byte> writer)
        {
            if (_cachedUtf8JsonWriter == null)
                _cachedUtf8JsonWriter = new(writer);
            else
                _cachedUtf8JsonWriter.Reset(writer);
            return _cachedUtf8JsonWriter;
        }

        public string Serialize<T>(T obj) => 
            JsonSerializer.Serialize(obj, _serializerOptions);

        public string Serialize<T>(T obj, bool pretty) => 
            JsonSerializer.Serialize(obj, pretty ? _prettySerializerOptions : _serializerOptions);

        public void Serialize<T>(IBufferWriter<byte> writer, T obj) => 
            JsonSerializer.Serialize(GetCachedUtf8JsonWriter(writer), obj, _serializerOptions);

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