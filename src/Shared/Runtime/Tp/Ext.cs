using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp
{
    public class ExtApi : ITpApi
    {
        private readonly ITpApi _hostApi;
        public ExtApi(ITpApi hostApi) => _hostApi = hostApi;

        async Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = await _hostApi.Connect(receiver, cancellationToken);
            return new ExtLink(link);
        }

        void ITpApi.Listen(ITpListener listener) => throw new NotImplementedException();
    }

    public class ExtLink : ITpLink
    {
        private readonly ITpLink _hostLink;

        public ExtLink(ITpLink hostLink) => _hostLink = hostLink;
        void IDisposable.Dispose() => _hostLink.Dispose();

        string ITpLink.GetRemotePeerId() => throw new NotImplementedException();
        void ITpLink.Send(byte[] bytes) => _hostLink.Send(bytes);
    }
}