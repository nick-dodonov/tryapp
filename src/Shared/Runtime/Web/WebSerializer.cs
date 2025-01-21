#define SYSTEM_TEXT_JSON
//#define NEWTONSOFT_JSON

using System.Buffers;
using System.Runtime.Serialization;

#if SYSTEM_TEXT_JSON
using System.Text.Json;

#elif NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#else
#error No JSON serializer defined
#endif

//TODO: split variants by iface

namespace Shared.Web
{
    /// <summary>
    /// Helper converting models the same way as JS or ASP 
    /// </summary>
    public static class WebSerializer
    {
        //ASP Web Controller default json formatting
#if SYSTEM_TEXT_JSON
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
        {
            IncludeFields = true
        };

        private static readonly JsonSerializerOptions _prettySerializerOptions = new(_serializerOptions)
        {
            WriteIndented = true
        };
#elif NEWTONSOFT_JSON
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Converters = new JsonConverter[] { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };

        private static readonly JsonSerializerSettings PrettyJsonSettings = new(JsonSettings)
            { Formatting = Formatting.Indented };
#endif

        public static string SerializeObject<T>(T obj)
#if SYSTEM_TEXT_JSON
            => JsonSerializer.Serialize(obj, _serializerOptions);
#elif NEWTONSOFT_JSON
            => JsonConvert.SerializeObject(obj, JsonSettings);
#endif

        public static string SerializeObject<T>(T obj, bool pretty)
#if SYSTEM_TEXT_JSON
            => JsonSerializer.Serialize(obj, pretty ? _prettySerializerOptions : _serializerOptions);
#elif NEWTONSOFT_JSON
            => JsonConvert.SerializeObject(obj, pretty ? PrettyJsonSettings : JsonSettings);
#endif

        public static void SerializeToWriter<T>(IBufferWriter<byte> writer, T obj)
        {
#if SYSTEM_TEXT_JSON
            var jsonWriter = new Utf8JsonWriter(writer); //TODO: cache utf8 writer
            JsonSerializer.Serialize(jsonWriter, obj, _serializerOptions);
#else
            throw new System.NotImplementedException();
#endif
        }

        public static T DeserializeObject<T>(string json)
        {
#if SYSTEM_TEXT_JSON
            var obj = JsonSerializer.Deserialize<T>(json, _serializerOptions);
#elif NEWTONSOFT_JSON
            var obj = JsonConvert.DeserializeObject<T>(json, JsonSettings);
#endif
            if (obj == null)
                throw new SerializationException($"Failed to deserialize {typeof(T).FullName} from json: {json}");
            return obj;
        }
    }
}