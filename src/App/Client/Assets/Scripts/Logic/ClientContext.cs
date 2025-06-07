using Client.Attributes;
using Client.LocalSample;
using Shared.Tp.Ext.Misc;
using Shared.Tp.St.Sync;
using UnityEngine;

namespace Client.Logic
{
    [CreateAssetMenu(menuName = "[Client]/" + nameof(ClientContext), fileName = nameof(ClientContext) + ".asset")]
    public class ClientContext : ScriptableObject
    {
        public SampleOptions sampleOptions;
        
        public SyncOptions syncOptions;

        public DumpLink.Options dumpLinkOptions;

        public ClientTimeOptions clientTimeOptions;
        
        [ReadOnly]
        public DumpStats dumpLinkStats;
    }
}