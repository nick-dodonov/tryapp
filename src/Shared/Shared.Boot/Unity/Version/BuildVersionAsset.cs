using System;
using UnityEngine;

namespace Shared.Boot.Version
{
    [Serializable]
    public class BuildVersionAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public BuildVersion buildVersion;
        public string? timeIso8601;

        void ISerializationCallbackReceiver.OnBeforeSerialize() 
            => timeIso8601 = buildVersion.Time.ToString("O");

        void ISerializationCallbackReceiver.OnAfterDeserialize()
            => buildVersion.Time = DateTime.Parse(timeIso8601);
    }
}