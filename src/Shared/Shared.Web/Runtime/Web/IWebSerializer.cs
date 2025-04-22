using System;
using System.Buffers;

namespace Shared.Web
{
    public interface IWebSerializer
    {
        public string Serialize<T>(T obj);
        public string Serialize<T>(T obj, bool pretty);
        public void Serialize<T>(IBufferWriter<byte> writer, T obj);
        public int SerializeTo<T>(IBufferWriter<byte> writer, T obj);
            
        public T Deserialize<T>(string json);
        public T Deserialize<T>(ReadOnlySpan<byte> spans);
    }
}