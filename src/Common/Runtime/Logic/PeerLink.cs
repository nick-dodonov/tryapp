using Shared.Tp;

namespace Common.Logic
{
    public class PeerApi : ExtApi<PeerLink>
    {
        public PeerApi(ITpApi innerApi) : base(innerApi) { }
    }

    public class PeerLink : ExtLink
    {
        public override string GetRemotePeerId() => "TODO-" + base.GetRemotePeerId();
    }
}