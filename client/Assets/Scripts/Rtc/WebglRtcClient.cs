using System.Threading;
using System.Threading.Tasks;
using Shared;
using Shared.Meta.Api;

namespace Rtc
{
    public class WebglRtcClient : IRtcClient
    {
        public WebglRtcClient()
        {
            StaticLog.Info("WebglRtcClient: created");
        }

        public async Task<string> TryCall(IMeta meta, CancellationToken cancellationToken)
        {
            StaticLog.Info("WebglRtcClient: TryCall: TODO");
            await Task.Yield();
            return "TODO";
        }
    }
}