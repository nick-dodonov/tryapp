using System;
using System.Buffers;

namespace Shared.Web
{
    public interface IWebSerializer
    {
        public string SerializeObject<T>(T obj);
        public string SerializeObject<T>(T obj, bool pretty);
        public void SerializeToWriter<T>(IBufferWriter<byte> writer, T obj);

        public T DeserializeObject<T>(string json);
        public T DeserializeObject<T>(ReadOnlySpan<byte> spans);
    }
}