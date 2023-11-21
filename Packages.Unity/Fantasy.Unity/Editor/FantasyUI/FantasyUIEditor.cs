using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Fantasy
{
    [CustomEditor(typeof(FantasyUI))]
    public class FantasyUIEditor : UnityEditor.Editor
    {
        private FantasyUI _fantasyUI;
        private readonly HashSet<int> _remove = new HashSet<int>();

        private void OnEnable()
        {
            _fantasyUI = (FantasyUI)target;
        }

        public override void OnInspectorGUI()
        {
            var dataProperty = serializedObject.FindProperty("list");
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            
            if (string.IsNullOrEmpty(_fantasyUI.componentName))
            {
                _fantasyUI.componentName = _fantasyUI.gameObject.name;
            }
            
            _fantasyUI.componentName = EditorGUILayout.TextField("Component Name", _fantasyUI.componentName);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _fantasyUI.assetName = EditorGUILayout.TextField("AssetName", _fantasyUI.assetName);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _fantasyUI.bundleName = EditorGUILayout.TextField("BundleName", _fantasyUI.bundleName);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _fantasyUI.uiLayer = (UILayer)EditorGUILayout.EnumPopup("UILayer", _fantasyUI.uiLayer);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add"))
            {
                AddReference(dataProperty, Guid.NewGuid().GetHashCode().ToString(), null);
            }

            if (GUILayout.Button("Clear"))
            {
                dataProperty.ClearArray();
                EditorUtility.SetDirty(this);
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
            }

            if (GUILayout.Button("Delete Empty"))
            {
                for (var i = dataProperty.arraySize - 1; i >= 0; i--)
                {
                    var gameObjectProperty = dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");

                    if (gameObjectProperty.objectReferenceValue != null)
                    {
                        continue;
                    }
                    
                    dataProperty.DeleteArrayElementAtIndex(i);
                    EditorUtility.SetDirty(_fantasyUI);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }

            if (GUILayout.Button("Sort"))
            {
                _fantasyUI.list.Sort(new FantasyUIDataComparer());
                EditorUtility.SetDirty(this);
                serializedObject.ApplyModifiedProperties();
                serializedObject.UpdateIfRequiredOrScript();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Generate Code")))
            {
                Generate(dataProperty);
            }
            
            GUILayout.EndHorizontal();

            for (var i = _fantasyUI.list.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                var property = dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
                property = dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");
                property.objectReferenceValue = EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(Object), true);
                
                if (GUILayout.Button("X"))
                {
                    _remove.Add(i);
                }
                
                GUILayout.EndHorizontal();
            }
            
            var eventType = Event.current.type;
            
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            
                if (eventType == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var o in DragAndDrop.objectReferences)
                    {
                        AddReference(dataProperty, o.name, o);
                    }
                }
            
                Event.current.Use();
            }

            if (_remove.Count > 0)
            {
                foreach (var removeIndex in _remove)
                {
                    dataProperty.DeleteArrayElementAtIndex(removeIndex);
                }
                
                _remove.Clear();
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }
        
        private void AddReference(SerializedProperty dataProperty, string key, Object obj)
        {
            var index = dataProperty.arraySize;
            dataProperty.InsertArrayElementAtIndex(index);
            var element = dataProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("gameObject").objectReferenceValue = obj;
        }

        private void Generate(SerializedProperty dataProperty)
        {
            var generatePath = FantasySettingsScriptableObject.Instance.uiGenerateSavePath;

            if (!Directory.Exists($"{generatePath}/Entity"))
            {
                Directory.CreateDirectory($"{generatePath}/Entity");
                return;
            }
            
            // if (string.IsNullOrEmpty(generatePath))
            // {
            //     EditorUtility.DisplayDialog("Generate Code", "Please enter the path in the menu first Fantasy/ReferencePreferences Set GeneratePath", "OK");
            //     return;
            // }
            //
            // if (!Directory.Exists(generatePath))
            // {
            //     EditorUtility.DisplayDialog("Generate Code", $"{generatePath} is not a valid path", "OK");
            //     return;
            // }

            if (string.IsNullOrEmpty(_fantasyUI.assetName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"assetName is null", "OK");
                return;
            }
            
            if (string.IsNullOrEmpty(_fantasyUI.bundleName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"bundleName is null", "OK");
                return;
            }

            if (_fantasyUI.uiLayer == UILayer.None)
            {
                EditorUtility.DisplayDialog("Generate Code", $"UILayer is None", "OK");
                return;
            }

            var createStr = new List<string>();
            var propertyStr = new List<string>();

            for (var i = _fantasyUI.list.Count - 1; i >= 0; i--)
            {
                var keyProperty = dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                var gameObjectProperty = dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");
                var refName = gameObjectProperty.objectReferenceValue.GetType().FullName;
                var gameObjectName = Regex.Replace(keyProperty.stringValue, @"\s+", "");
                propertyStr.Add($"\t\tpublic {refName} {gameObjectName};");
                createStr.Add($"\t\t\t{gameObjectName} = referenceComponent.GetReference<{refName}>(\"{keyProperty.stringValue}\");");
            }

            var sb = new StringBuilder();

            sb.AppendLine("using Fantasy;");
            sb.AppendLine("\nnamespace Fantasy\n{");
            sb.AppendLine($"\tpublic partial class {_fantasyUI.componentName} : UI\n\t{{");
            sb.AppendLine($"\t\tpublic override string AssetName {{ get; protected set; }} = \"{_fantasyUI.assetName}\";");
            sb.AppendLine($"\t\tpublic override string BundleName {{ get; protected set; }} = \"{_fantasyUI.bundleName.ToLower()}\";");
            sb.AppendLine($"\t\tpublic override UILayer Layer {{ get; protected set; }} = UILayer.{_fantasyUI.uiLayer.ToString()};\n");
            
            foreach (var property in propertyStr)
            {
                sb.AppendLine(property);
            }

            sb.AppendLine("\n\t\tpublic override void Initialize()\n\t\t{");
            sb.AppendLine("\t\t\tvar referenceComponent = GameObject.GetComponent<FantasyUI>();");

            foreach (var str in createStr)
            {
                sb.AppendLine(str);
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var combinePath = Path.Combine(generatePath, $"Entity/{_fantasyUI.componentName}.cs");
            using var entityStreamWriter = new StreamWriter(combinePath);
            entityStreamWriter.Write(sb.ToString());
            AssetDatabase.Refresh();
            Log.Debug($"代码生成位置:{combinePath}");
        }
    }
}