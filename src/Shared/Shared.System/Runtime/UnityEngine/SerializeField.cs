#if !UNITY_5_6_OR_NEWER
using System;

namespace UnityEngine
{
    /// <summary>
    ///   <para>Force Unity to serialize a private field.</para>
    /// </summary>
    public sealed class SerializeField : Attribute
    {
    }
}
#endif