using Common.Editor;
using Common.Logic;
using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    [CreateAssetMenu(menuName = "[Client]/" + nameof(ClientContext), fileName = nameof(ClientContext) + ".asset")]
    public class ClientContext : ScriptableObject
    {
        public SyncOptions syncOptions;

        public DumpLink.Options dumpLinkOptions;

        //[System.NonSerialized]
        [ReadOnly]
        public DumpStats dumpLinkStats;
    }
}