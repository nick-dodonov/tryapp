// using Client.Logic;
// using UnityEditor;
//
// namespace Client.Editor
// {
//     [CustomEditor(typeof(ClientContext))]
//     public class ClientContextEditor : UnityEditor.Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             //serializedObject.Update();
//             base.OnInspectorGUI();
//             
//             using (new EditorGUI.DisabledScope(true))
//             {
//                 EditorGUILayout.LabelField("label");
//                 EditorGUILayout.Space();
//                 var serializedProperty = serializedObject.FindProperty(nameof(ClientContext.dumpLinkStats));
//                 EditorGUILayout.PropertyField(serializedProperty);
//             }
//         }
//     }
// }