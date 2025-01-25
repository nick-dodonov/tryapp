using RuntimeInspectorNamespace;
using Shared.Tp.Ext.Misc;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Client.Logic
{
    /// <summary>
    /// Sample of readonly runtime editor for specific type
    /// </summary>
    [Preserve, RuntimeInspectorCustomEditor(typeof(DumpLink.StatsInfo))]
    public class ColliderEditor : IRuntimeInspectorCustomEditor
    {
        public void GenerateElements(ObjectField parent)
        {
            parent.CreateDrawersForVariables(null);

            var inputFields = parent.gameObject.GetComponentsInChildren<InputField>(true);
            foreach (var inputField in inputFields)
                inputField.interactable = false;
        }

        public void Refresh() { }
        public void Cleanup() { }
    }
}