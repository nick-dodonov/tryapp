using System;
using Shared.Tp.Data;
using UnityEngine;
using UnityEngine.Scripting;

namespace Shared.Tp.St.Sync
{
    [Serializable]
    public class SyncOptions
    {
        [field: SerializeField] [RequiredMember]
        public int BasicSendRate { get; set; } = 1;
    }
    
    public interface ISyncHandler<TLocal, TRemote>
    {
        SyncOptions Options { get; }

        IObjWriter<StCmd<TLocal>> LocalWriter { get; }
        IObjReader<StCmd<TRemote>> RemoteReader { get; }

        int TimeMs { get; }
        TLocal MakeLocalState();

        void RemoteUpdated();
        void RemoteDisconnected();
    }
}