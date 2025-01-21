using System.Buffers;

namespace Shared.Web
{
    /// <summary>
    /// Helper converting models the same way as JavaScript or ASP 
    /// </summary>
    public static class WebSerializer
    {
        public static readonly IWebSerializer Default = new SystemWebSerializer();

        public static string SerializeObject<T>(T obj)
            => Default.SerializeObject(obj);

        public static string SerializeObject<T>(T obj, bool pretty)
            => Default.SerializeObject(obj, pretty);

        public static void SerializeToWriter<T>(IBufferWriter<byte> writer, T obj)
            => Default.SerializeToWriter(writer, obj);

        public static T DeserializeObject<T>(string json)
            => Default.DeserializeObject<T>(json);
    }
}