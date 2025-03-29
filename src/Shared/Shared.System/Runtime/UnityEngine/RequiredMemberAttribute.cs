#if !UNITY_5_6_OR_NEWER
#nullable disable
using System;

// ReSharper disable once CheckNamespace
namespace UnityEngine.Scripting
{
    /// <summary>
    ///   <para>When a type is marked, all of its members with [RequiredMember] are marked.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class RequiredMemberAttribute : Attribute
    {
    }
}
#endif