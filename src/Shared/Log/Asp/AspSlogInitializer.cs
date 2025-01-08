#if !UNITY_5_6_OR_NEWER
using Microsoft.Extensions.Logging;

namespace Shared.Log.Asp
{
    public class AspSlogInitializer : Slog.IInitializer
    {
        public ILoggerFactory CreateDefaultFactory()
        {
            //TODO: use default app builder using default options (to support json output too in startup static logs)
            var factory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "[HH:mm:ss.ffffff] ";
                o.UseUtcTimestamp = true;
            }));
            return factory;
        }
    }
}
#endif
