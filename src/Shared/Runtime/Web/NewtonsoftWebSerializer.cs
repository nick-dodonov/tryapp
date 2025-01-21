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

        public string SerializeObject<T>(T obj)
            => JsonConvert.SerializeObject(obj, JsonSettings);

        public string SerializeObject<T>(T obj, bool pretty)
            => JsonConvert.SerializeObject(obj, pretty ? PrettyJsonSettings : JsonSettings);

        public void SerializeToWriter<T>(IBufferWriter<byte> writer, T obj)
            => throw new NotImplementedException();

        public T DeserializeObject<T>(string json)
        {
            var obj = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            if (obj == null)
                throw new SerializationException($"Failed to deserialize {typeof(T).FullName} from json: {json}");
            return obj;
        }

        public T DeserializeObject<T>(ReadOnlySpan<byte> spans)
        {
            var json = Encoding.UTF8.GetString(spans);
            return DeserializeObject<T>(json);
        }
    }
}