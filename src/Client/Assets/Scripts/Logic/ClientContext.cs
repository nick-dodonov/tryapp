using System;
using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    [CreateAssetMenu(menuName = "[Client]/"+nameof(ClientContext), fileName = nameof(ClientContext)+".asset")]
    public class ClientContext : ScriptableObject
    {
         public DumpLink.Options dumpLinkOptions;
         
         [NonSerialized]
         public DumpLink.StatsInfo dumpLinkStats;
    }
}