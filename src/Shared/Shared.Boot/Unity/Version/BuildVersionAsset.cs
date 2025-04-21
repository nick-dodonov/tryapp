using System;
using UnityEngine;

namespace Shared.Boot.Version
{
    [Serializable]
    public class BuildVersionAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public BuildVersion buildVersion;
        public long timeTicks; // workaround for DateTime isn't serializable in unity        

        void ISerializationCallbackReceiver.OnBeforeSerialize()
            => timeTicks = buildVersion.Time.Ticks;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
            => buildVersion.Time = new(timeTicks);
    }
}