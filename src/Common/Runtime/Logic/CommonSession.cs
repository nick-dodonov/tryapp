using Microsoft.Extensions.Logging;
using Shared.Tp;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;

namespace Common.Logic
{
    public static class CommonSession
    {
        public static ITpApi CreateApi(
            ITpApi rtcApi,
            ConnectState? connectState,
            ILoggerFactory loggerFactory)
        {
            return new HandApi(
                new TimeLink.Api(
                    new DumpLink.Api(
                        rtcApi,
                        loggerFactory
                    ),
                    loggerFactory
                ),
                new ConnectStateProvider(connectState),
                loggerFactory);
        }
    }
}