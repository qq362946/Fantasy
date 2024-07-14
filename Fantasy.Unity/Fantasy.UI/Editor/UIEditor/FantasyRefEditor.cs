using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fantasy
{
    [CustomEditor(typeof(FantasyRef))]
    public class FantasyRefEditor : Editor
    {
        private FantasyRef _fantasyRef;
        private readonly HashSet<int> _remove = new();

        private SerializedProperty _dataProperty;

        private void OnEnable()
        {
            _fantasyRef = (FantasyRef)target;
            _dataProperty = serializedObject.FindProperty("list");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(_fantasyRef.componentName))
            {
                _fantasyRef.componentName = _fantasyRef.gameObject.name;
            }

            _fantasyRef.nameSpaceName = EditorGUILayout.TextField(new GUIContent("Namespace Name", "命名空间名，如果不为空，则生成时会生成对应名称的文件夹和命名空间"), _fantasyRef.nameSpaceName);
            EditorGUILayout.Space();
            _fantasyRef.componentName = EditorGUILayout.TextField("Component Name", _fantasyRef.componentName);

            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add"))
                {
                    AddReference(_dataProperty, Guid.NewGuid().GetHashCode().ToString(), null);
                }

                if (GUILayout.Button("Clear"))
                {
                    _dataProperty.ClearArray();
                    EditorUtility.SetDirty(this);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                }

                if (GUILayout.Button("Delete Empty"))
                {
                    for (var i = _dataProperty.arraySize - 1; i >= 0; i--)
                    {
                        var gameObjectProperty = _dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");

                        if (gameObjectProperty.objectReferenceValue != null)
                        {
                            continue;
                        }

                        _dataProperty.DeleteArrayElementAtIndex(i);
                        EditorUtility.SetDirty(_fantasyRef);
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.UpdateIfRequiredOrScript();
                    }
                }

                if (GUILayout.Button("Sort"))
                {
                    _fantasyRef.list.Sort(new FantasyUIDataComparer());
                    EditorUtility.SetDirty(this);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }

            if (GUILayout.Button(new GUIContent("Generate Code")))
            {
                GenerateCode();
            }

            for (var i = _fantasyRef.list.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                var property = _dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
                property = _dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");
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
                        AddReference(_dataProperty, o.name, o);
                    }
                }

                Event.current.Use();
            }

            if (_remove.Count > 0)
            {
                foreach (var removeIndex in _remove)
                {
                    _dataProperty.DeleteArrayElementAtIndex(removeIndex);
                }

                _remove.Clear();
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
            FantasyUIHierarchyHelper.RefreshHierarchy();
            EditorApplication.RepaintHierarchyWindow();
        }

        private void AddReference(SerializedProperty dataProperty, string key, Object obj)
        {
            var index = dataProperty.arraySize;
            dataProperty.InsertArrayElementAtIndex(index);
            var element = dataProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("gameObject").objectReferenceValue = obj;
        }

        private void GenerateCode()
        {
            var generatePath = FantasyUISettingsScriptableObject.Instance.uiGenerateSavePath;

            if (string.IsNullOrEmpty(_fantasyRef.componentName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"componentName is null", "OK");
                return;
            }


            bool hasModule = !string.IsNullOrEmpty(_fantasyRef.nameSpaceName);


            generatePath = hasModule ? $"{generatePath}/Module/{_fantasyRef.nameSpaceName}" : $"{generatePath}/SubUI";

            if (!hasModule)
            {
                EditorUtility.DisplayDialog("Generate Code", $"FantasyRef should have a moduleName!", "OK");
                return;
            }

            if (!Directory.Exists(generatePath))
            {
                Directory.CreateDirectory(generatePath);
            }

            var createStr = new List<string>();
            var propertyStr = new List<string>();

            for (var i = _fantasyRef.list.Count - 1; i >= 0; i--)
            {
                var keyProperty = _dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                var gameObjectProperty = _dataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("gameObject");
                var refName = gameObjectProperty.objectReferenceValue.GetType().FullName;
                var gameObjectName = Regex.Replace(keyProperty.stringValue, @"\s+", "");
                propertyStr.Add($"\t\tpublic {refName} {gameObjectName};");
                createStr.Add($"\t\t\t{gameObjectName} = referenceComponent.GetReference<{refName}>(\"{keyProperty.stringValue}\");");
            }


            var sb = new StringBuilder();

            sb.AppendLine("using Fantasy;");
            sb.AppendLine(hasModule ? $"\nnamespace Fantasy.{_fantasyRef.nameSpaceName.Replace('/', '.')}\n{{" : "\nnamespace Fantasy\n{");
            sb.AppendLine($"\tpublic partial class {_fantasyRef.componentName} : Entity\n\t{{");

            sb.AppendLine($"\t\tpublic GameObject GameObject;");

            foreach (var property in propertyStr)
            {
                sb.AppendLine(property);
            }

            sb.AppendLine($"\n\t\tpublic void Initialize(GameObject gameObject)\n\t\t{{");
            sb.AppendLine("\t\t\tGameObject = fantasyRef.gameObject;");
            sb.AppendLine("\t\t\tvar referenceComponent = GameObject.GetComponent<FantasyRef>();");

            foreach (var str in createStr)
            {
                sb.AppendLine(str);
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var combinePath = Path.Combine(generatePath, $"{_fantasyRef.componentName}.cs");
            using var entityStreamWriter = new StreamWriter(combinePath);
            entityStreamWriter.Write(sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}