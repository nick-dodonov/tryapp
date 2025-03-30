using Client.Utility;
using UnityEditor;

namespace Client.Editor
{
    public static class DebugEditorMenu
    {
        
        [MenuItem("[App]/Debug Locator")]
        public static void DebugLocator() => DebugActions.DebugLocator();
    }
}