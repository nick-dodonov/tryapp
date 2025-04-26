using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Tp;
using Shared.Tp.Data;
using Shared.Tp.Data.Web;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;

namespace Common.Logic
{
    public static class CommonSession
    {
        public static ITpApi CreateApi<TLocalState, TRemoteState>(
            ITpApi rtcApi,
            TLocalState localState,
            HandLink<TRemoteState>.LinkIdProvider linkIdProvider,
            IOptionsMonitor<DumpLink.Options> dumpLinkOptions,
            ILoggerFactory loggerFactory)
        {
            return new HandApi<TRemoteState>(
                new TimeLink.Api(
                    new DumpLink.Api(
                        rtcApi,
                        dumpLinkOptions,
                        loggerFactory
                    ),
                    loggerFactory
                ),
                new OwnWriter<TLocalState>(localState, new WebObjWriter<TLocalState>()),
                new WebObjReader<TRemoteState>(),
                linkIdProvider,
                loggerFactory);
        }
    }
}