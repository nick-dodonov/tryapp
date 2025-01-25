using System;
using System.Collections.Generic;
using System.Reflection;
using RuntimeInspectorNamespace;
using Shared.Tp.Ext.Misc;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Client.Logic
{
    /// <summary>
    /// Sample of readonly runtime editor for specific type
    /// </summary>
    [Preserve, RuntimeInspectorCustomEditor(typeof(ClientContext))]
    public class ClientContextEditor : IRuntimeInspectorCustomEditor
    {
        public void GenerateElements(ObjectField parent)
        {
            parent.CreateDrawersForVariablesExcluding(nameof(ClientContext.name));

            //TODO: maybe create helper to add all NonSerialized fields/properies for readonly inspection
            parent.CreateDrawerForVariable(typeof(ClientContext).GetField(nameof(ClientContext.dumpLinkStats),
                BindingFlags.Public | BindingFlags.Instance));

            parent.ExpandExpandableControls();
        }

        public void Refresh() { }
        public void Cleanup() { }
    }

    /// <summary>
    /// Readonly runtime editor for specific types
    /// </summary>
    [Preserve]
    [RuntimeInspectorCustomEditor(typeof(DumpLink.StatsInfo))]
    [RuntimeInspectorCustomEditor(typeof(DumpLink.StatsInfo.Dir))]
    public class ReadOnlyExpandedEditor : IRuntimeInspectorCustomEditor
    {
        public void GenerateElements(ObjectField parent)
        {
            parent.CreateDrawersForVariables(null);
            parent.DisableInteractableControls();
            parent.ExpandExpandableControls();
        }

        public void Refresh() { }
        public void Cleanup() { }
    }

    public static class TransformExtensions
    {
        public static IEnumerable<T> FindInChildrenDeep<T>(this Transform transform)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(transform);
            while (queue.TryDequeue(out transform))
            {
                foreach (var found in transform.GetComponents<T>())
                    yield return found;

                for (var i = 0; i < transform.childCount; i++)
                    queue.Enqueue(transform.GetChild(i));
            }
        }

        public static void ForEachChildrenDeep<T>(this Transform transform, Action<T> action)
        {
            var fields = transform.FindInChildrenDeep<T>();
            foreach (var field in fields)
                action(field);
        }
    }

    public static class ObjectFieldExtensions
    {
        public static void DisableInteractableControls(this ObjectField parent) =>
            parent.transform.ForEachChildrenDeep<Selectable>(
                static x => x.interactable = false);

        public static void ExpandExpandableControls(this ObjectField parent) =>
            parent.transform.ForEachChildrenDeep<ExpandableInspectorField>(
                static x => x.IsExpanded = true);
    }
}