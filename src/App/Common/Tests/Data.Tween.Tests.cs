using Common.Data;
using Common.Logic;
using NUnit.Framework;

namespace Common.Tests
{
    public class DataTweenTests
    {
        private static PeerState MakePeerState(int id, int offset)
        {
            return new()
            {
                Id = $"ID-{id}",
                CycledRt = (ushort)(10 * id + offset),
                ClientState = new()
                {
                    X = id + offset,
                    Y = id + offset,
                    Color = 0xFF0000FF
                }
            };
        }
        
        [Test]
        public void TickState_Tweens()
        {
            var provider = CommonSession.CreateTweenerProvider();
            provider.Get<ClientState>();
            provider.Get<PeerState>();
            var tweener = provider.Get<ServerState>();

            var a = new ServerState
            {
                Peers = new[]
                {
                    MakePeerState(1, 0),
                    //MakePeerState(2, 0.0f)
                }
            };
            var b = new ServerState
            {
                Peers = new[]
                {
                    MakePeerState(1, 100),
                    //MakePeerState(2, 100.0f)
                }
            };
            var r = new ServerState();
            tweener.Process(ref r, 0.5f, in a, in b);
            
            Assert.AreEqual(a.Peers.Length, r.Peers.Length);
            Assert.That(r.Peers[0].CycledRt, Is.InRange(
                a.Peers[0].CycledRt, 
                b.Peers[0].CycledRt));
            Assert.That(r.Peers[0].ClientState.X, Is.InRange(
                a.Peers[0].ClientState.X, 
                b.Peers[0].ClientState.X));
        }
    }
}