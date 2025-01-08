#if !UNITY_5_6_OR_NEWER
using System;

#nullable disable
// ReSharper disable once CheckNamespace
namespace UnityEngine
{
    /// <summary>
    /// Copy of internal UnityEngine implementation
    /// </summary>
    /// <summary>
    ///   <para>Marks the methods you want to hide from the Console window callstack. When you hide these methods they are removed from the detail area of the selected message in the Console window.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HideInCallstackAttribute : Attribute
    {
    }
}
#endif