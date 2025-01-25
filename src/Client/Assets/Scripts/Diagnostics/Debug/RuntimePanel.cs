using RuntimeInspectorNamespace;
using Shared.Log;
using UnityEngine;

namespace Diagnostics.Debug
{
    public class RuntimePanel : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();

        public RuntimeHierarchy hierarchy;
        public RuntimeInspector inspector;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _log.Info(".");
        }

        /// <summary>
        /// TODO: customize RuntimePanel with extensions for context
        /// </summary>
        public static void SetInspectorContext(object context)
        {
            var runtimePanel = FindFirstObjectByType<RuntimePanel>(FindObjectsInactive.Include);
            runtimePanel.Inspect(context);
        }
        private void Inspect(object context)
        {
            hierarchy.gameObject.SetActive(false);
            inspector.ShowInspectReferenceButton = false;
            inspector.ShowAddComponentButton = false;
            inspector.ShowRemoveComponentButton = false;
            inspector.Inspect(context);
        }
    }
}