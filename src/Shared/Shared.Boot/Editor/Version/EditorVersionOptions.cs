using UnityEditor;
using UnityEngine;

namespace Shared.Boot.Editor.Version
{
    public static class EditorVersionOptions
    {
        private const string UseRuntimeProviderMenuItemName = "[Shared]/Version/Use Runtime Provider";
        private static readonly string SessionKey = $"{nameof(EditorVersionOptions)}.{nameof(UseRuntimeProvider)}";

        public static bool UseRuntimeProvider
        {
            get => SessionState.GetBool(SessionKey, false);
            set => SessionState.SetBool(SessionKey, value);
        }

        [MenuItem(UseRuntimeProviderMenuItemName)]
        public static void UseRuntimeProviderToggle() => UseRuntimeProvider = !UseRuntimeProvider;
        [MenuItem(UseRuntimeProviderMenuItemName, true)]
        public static bool UseRuntimeProviderValidate()
        {
            Menu.SetChecked(UseRuntimeProviderMenuItemName, UseRuntimeProvider);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
        
    }
}