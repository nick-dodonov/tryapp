using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Shared.Web
{
    public class NewtonsoftWebSerializer : IWebSerializer
    {
        //ASP Web Controller default json formatting
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Converters = new JsonConverter[] { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };

        private static readonly JsonSerializerSettings PrettyJsonSettings = new(JsonSettings)
            { Formatting = Formatting.Indented };

        string IWebSerializer.Serialize<T>(T obj)
            => JsonConvert.SerializeObject(obj, JsonSettings);

        string IWebSerializer.Serialize<T>(T obj, bool pretty)
            => JsonConvert.SerializeObject(obj, pretty ? PrettyJsonSettings : JsonSettings);

        void IWebSerializer.Serialize<T>(IBufferWriter<byte> writer, T obj)
            => throw new NotImplementedException();

        T IWebSerializer.Deserialize<T>(string json) 
            => DeserializeFromString<T>(json);

        T IWebSerializer.Deserialize<T>(ReadOnlySpan<byte> span)
        {
            var json = Encoding.UTF8.GetString(span);
            return DeserializeFromString<T>(json);
        }

        void IWebSerializer.Deserialize<T>(ReadOnlySpan<byte> span, ref T obj)
        {
            var json = Encoding.UTF8.GetString(span);
            obj = DeserializeFromString<T>(json);
        }

        private static T DeserializeFromString<T>(string json)
        {
            var obj = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            if (obj == null)
                throw new SerializationException($"Failed to deserialize {typeof(T).FullName} from json: {json}");
            return obj;
        }
    }
}