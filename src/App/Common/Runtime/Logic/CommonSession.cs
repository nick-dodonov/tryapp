using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Tp;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;

namespace Common.Logic
{
    public static class CommonSession
    {
        public static ITpApi CreateApi(
            ITpApi rtcApi,
            ConnectState connectState,
            IOptionsMonitor<DumpLink.Options> dumpLinkOptions,
            ILoggerFactory loggerFactory)
        {
            return new HandApi<ConnectState, ConnectState>(
                new TimeLink.Api(
                    new DumpLink.Api(
                        rtcApi,
                        dumpLinkOptions,
                        loggerFactory
                    ),
                    loggerFactory
                ),
                new StdLocalStateProvider<ConnectState>(connectState, static (state) => state.LinkId),
                new StdRemoteStateProvider<ConnectState>(static (state) => state.LinkId),
                loggerFactory);
        }
    }
}