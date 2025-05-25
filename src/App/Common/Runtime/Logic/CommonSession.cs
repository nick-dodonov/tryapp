using Common.Data;
using Common.Tween;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Tp;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Tween;

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
            CommonData.Initialize();
            return new HandApi<TRemoteState>(
                new TimeLink.Api(
                    new DumpLink.Api(
                        rtcApi,
                        dumpLinkOptions,
                        loggerFactory
                    )
                ),
                HandStateFactory.CreateOwnWriter(localState),
                HandStateFactory.CreateObjReader<TRemoteState>(),
                linkIdProvider,
                loggerFactory);
        }

        //TODO: possible move to CommonClient because it's not required on Server now
        public static TweenerProvider CreateTweenerProvider()
        {
            var provider = new TweenerProvider();
            provider.Register(new PeerStateArrayTweener(provider));
            return provider;
        }
    }
}