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
    }
}