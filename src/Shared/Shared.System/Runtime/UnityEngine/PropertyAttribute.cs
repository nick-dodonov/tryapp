#if !UNITY_5_6_OR_NEWER
using System;
using UnityEngine.Scripting;

#nullable disable
namespace UnityEngine
{
    /// <summary>
    ///   <para>Base class to derive custom property attributes from. Use this to create custom attributes for script variables.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public abstract class PropertyAttribute : Attribute
    {
        /// <summary>
        ///   <para>Optional field to specify the order that multiple DecorationDrawers should be drawn in.</para>
        /// </summary>
        public int order { get; set; }

        /// <summary>
        ///   <para>Makes attribute affect collections instead of their items.</para>
        /// </summary>
        public bool applyToCollection { get; }

        protected PropertyAttribute()
            : this(false)
        {
        }

        protected PropertyAttribute(bool applyToCollection)
        {
            this.applyToCollection = applyToCollection;
        }
    }
}
#endif