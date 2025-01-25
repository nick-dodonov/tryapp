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
    [Preserve, RuntimeInspectorCustomEditor(typeof(DumpLink.Stats), false)]
    public class ColliderEditor : IRuntimeInspectorCustomEditor
    {
        public void GenerateElements(ObjectField parent)
        {
            parent.CreateDrawersForVariables(null);

            var inputFields = Object.FindObjectsByType<InputField>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var inputField in inputFields)
                inputField.interactable = false;
        }

        public void Refresh() { }
        public void Cleanup() { }
    }
}