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

            _fantasyRef.isUI = EditorGUILayout.Toggle(new GUIContent("Is UI", "生成时是否继承SubUI"), _fantasyRef.isUI);
            _fantasyRef.moduleName = EditorGUILayout.TextField(new GUIContent("Module Name", "模块名，如果不为空，则生成时会生成对应名称的文件夹和命名空间"), _fantasyRef.moduleName);
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
            var generatePath = FantasySettingsScriptableObject.Instance.uiGenerateSavePath;

            if (string.IsNullOrEmpty(_fantasyRef.componentName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"componentName is null", "OK");
                return;
            }

            bool isUI = _fantasyRef.isUI;
            bool hasModule = !string.IsNullOrEmpty(_fantasyRef.moduleName);
            generatePath = hasModule ? $"{generatePath}/Module/{_fantasyRef.moduleName}" : $"{generatePath}/SubUI";

            if (!isUI && !hasModule)
            {
                EditorUtility.DisplayDialog("Generate Code", $"If FantasyRef is not a UI, then it should have a moduleName!", "OK");
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
            sb.AppendLine(hasModule ? $"\nnamespace Fantasy.{_fantasyRef.moduleName}\n{{" : "\nnamespace Fantasy\n{");
            sb.AppendLine($"\tpublic partial class {_fantasyRef.componentName} : {(isUI ? "SubUI" : "Entity")}\n\t{{");

            if (!isUI)
                sb.AppendLine($"\t\tpublic GameObject GameObject;");

            foreach (var property in propertyStr)
            {
                sb.AppendLine(property);
            }

            sb.AppendLine($"\n\t\tpublic{(isUI ? " override" : "")} void Initialize()\n\t\t{{");
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
            Log.Debug($"代码生成位置:{combinePath}");
        }
    }
}