using Common.Logic.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Tp;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;

namespace Common.Logic
{
    public static class CommonSession
    {
        public static ITpApi CreateApi<TLocalState, TRemoteState>(
            ITpApi rtcApi,
            TLocalState localState,
            LinkIdProvider<TLocalState> localLinkIdProvider,
            LinkIdProvider<TRemoteState> remoteLinkIdProvider,
            IOptionsMonitor<DumpLink.Options> dumpLinkOptions,
            ILoggerFactory loggerFactory)
        {
            return new HandApi<TLocalState, TRemoteState>(
                new TimeLink.Api(
                    new DumpLink.Api(
                        rtcApi,
                        dumpLinkOptions,
                        loggerFactory
                    ),
                    loggerFactory
                ),
                new StdLocalStateProvider<TLocalState>(localState, localLinkIdProvider),
                new StdRemoteStateProvider<TRemoteState>(remoteLinkIdProvider),
                loggerFactory);
        }
    }
}