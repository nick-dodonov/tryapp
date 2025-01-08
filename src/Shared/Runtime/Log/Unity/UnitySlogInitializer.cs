#if UNITY_5_6_OR_NEWER
using Microsoft.Extensions.Logging;

namespace Shared.Log.Unity
{
    public class UnitySlogInitializer : Slog.IInitializer
    {
        public ILoggerFactory CreateDefaultFactory()
        {
            var factory = LoggerFactory.Create(builder => builder.AddProvider(new UnityLogger.Provider()));
            return factory;
        }
    }
}
#endif