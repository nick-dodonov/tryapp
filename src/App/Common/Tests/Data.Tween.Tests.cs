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
            provider.Get<ClientState>();
            provider.Get<PeerState>();
            provider.Get<ServerState>();
        }
    }
}