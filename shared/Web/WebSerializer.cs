//TODO: test System.Text.Json with enabled WebGL embedded data (because System.Text.Json is better having span variants)
//WEBGL-DISABLE: using System.Text.Json;

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Shared.Web
{
    /// <summary>
    /// Helper converting models the same way as JS or ASP 
    /// </summary>
    public class WebSerializer
    {
        //ASP Web Controller default json formatting
        //WEBGL-DISABLE: private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
            }
        };

        public static string SerializeObject<T>(T obj) 
            => JsonConvert.SerializeObject(obj, JsonSettings);

        public static T DeserializeObject<T>(string json)
        {
            var obj = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            if (obj == null)
                throw new SerializationException($"Failed to deserialize object {typeof(T).FullName} from json: {json}");
            return obj;
        }
    }
}