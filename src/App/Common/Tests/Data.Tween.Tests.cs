using Common.Data;
using NUnit.Framework;
using Shared.Tp.Tween;

namespace Common.Tests
{
    public class DataTweenTests
    {
        [Test]
        public void TickState_Tweeners_Exist()
        {
            var provider = new TweenerProvider();
            provider.Register(new PeerStateArrayTweener());
            
            provider.Get<ClientState>();
            provider.Get<PeerState>();
            provider.Get<ServerState>();
        }
    }

    public class PeerStateArrayTweener: ITweener<PeerState[]>
    {
        public void Process(ref PeerState[] a, ref PeerState[] b, float t, ref PeerState[] r)
        {
            r = b; //TODO: clone to fix
        }
    }
}