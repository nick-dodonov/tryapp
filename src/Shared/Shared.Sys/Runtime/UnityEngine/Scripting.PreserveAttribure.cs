#if !UNITY_5_6_OR_NEWER
#nullable disable
using System;

// ReSharper disable once CheckNamespace
namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, Inherited = false)]
    public class PreserveAttribute : Attribute {}
}
#endif