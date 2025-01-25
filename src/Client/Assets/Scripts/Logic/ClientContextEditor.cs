using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RuntimeInspectorNamespace;
using Shared.Log;
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
            // add NonSerialized just for view
            parent.CreateDrawerForVariable(typeof(ClientContext).GetField(nameof(ClientContext.dumpLinkStats),
                BindingFlags.Public | BindingFlags.Instance));
        }
        public void Refresh() { }
        public void Cleanup() { }
    }

    /// <summary>
    /// Sample of readonly runtime editor for specific type
    /// </summary>
    [Preserve, RuntimeInspectorCustomEditor(typeof(DumpLink.StatsInfo.Dir))]
    public class DumpLinkStatsInfoDirEditor : IRuntimeInspectorCustomEditor
    {
        public void GenerateElements(ObjectField parent)
        {
            parent.CreateDrawersForVariables(null);

            var inputFields = FindInChildrenDeep<InputField>(parent.transform);
            foreach (var inputField in inputFields)
                inputField.interactable = false;
        }

        public void Refresh() { }
        public void Cleanup() { }

        private static IEnumerable<T> FindInChildrenDeep<T>(Transform parent)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(parent);
            while (queue.TryDequeue(out var transform))
            {
                Slog.Info($"transform: {transform.name}");
                foreach (var found in transform.GetComponents<T>())
                    yield return found;

                for (var i = 0; i < transform.childCount; i++)
                    queue.Enqueue(transform.GetChild(i));
            }
        }
    }
}