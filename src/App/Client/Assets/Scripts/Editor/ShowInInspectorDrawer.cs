// using UnityEditor; 
// using UnityEngine; 
// using Client.Attributes;
//
// namespace Client.Editor
// {
//     [CustomPropertyDrawer(typeof(ShowInInspectorAttribute))] 
//     public class ShowInInspectorDrawer : PropertyDrawer 
//     { 
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
//         { 
//             EditorGUI.BeginProperty(position, label, property); 
//             var targetObject = property.serializedObject.targetObject; 
//             var value = fieldInfo.GetValue(targetObject);
//             EditorGUI.LabelField(position, $"{label.text}: {value}"); 
//             EditorGUI.EndProperty(); 
//         } 
//     }
// }