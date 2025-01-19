//TODO: test System.Text.Json with enabled WebGL embedded data (because System.Text.Json is better having span variants)
//  https://docs.unity3d.com/Manual/webgl-embeddedresources.html
//WEBGL-DISABLE: using System.Text.Json;

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Shared.Web
{
    /// <summary>
    /// Helper converting models the same way as JS or ASP 
    /// </summary>
    public static class WebSerializer
    {
        //ASP Web Controller default json formatting
        //WEBGL-DISABLE: private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            },
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        private static readonly JsonSerializerSettings PrettyJsonSettings = new(JsonSettings)
        {
            Formatting = Formatting.Indented
        };

        public static string SerializeObject<T>(T obj)
            => JsonConvert.SerializeObject(obj, JsonSettings);

        public static string SerializeObject<T>(T obj, bool pretty)
            => JsonConvert.SerializeObject(obj, pretty ? PrettyJsonSettings: JsonSettings);
        
        public static T DeserializeObject<T>(string json)
        {
            var obj = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            if (obj == null)
                throw new SerializationException(
                    $"Failed to deserialize object {typeof(T).FullName} from json: {json}");
            return obj;
        }
    }
}