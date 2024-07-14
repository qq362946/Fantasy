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
    [CustomEditor(typeof(FantasyUIRef))]
    public class FantasyUIRefEditor : Editor
    {
        private FantasyUIRef _fantasyUIRef;
        private readonly HashSet<int> _remove = new();

        private void OnEnable()
        {
            _fantasyUIRef = (FantasyUIRef)target;
        }

        public override void OnInspectorGUI()
        {
            var dataProperty = serializedObject.FindProperty("list");
            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(_fantasyUIRef.componentName))
            {
                _fantasyUIRef.componentName = _fantasyUIRef.gameObject.name;
            }

            _fantasyUIRef.isWidget = EditorGUILayout.Toggle(new GUIContent("Is Widget", "是否为控件ui"), _fantasyUIRef.isWidget);
            _fantasyUIRef.nameSpaceName = EditorGUILayout.TextField(new GUIContent("Namespace Name", "命名空间名，如果不为空，则生成时会生成对应名称的文件夹和命名空间"), _fantasyUIRef.nameSpaceName);

            _fantasyUIRef.componentName = EditorGUILayout.TextField(new GUIContent("Component Name", "生成的脚本名"), _fantasyUIRef.componentName);
            _fantasyUIRef.bundleName = EditorGUILayout.TextField(new GUIContent("BundleName", "所在的AB包名"), _fantasyUIRef.bundleName);
            _fantasyUIRef.assetName = EditorGUILayout.TextField(new GUIContent("AssetName", "AB包中预制体的名资源名"), _fantasyUIRef.assetName);
            if (!_fantasyUIRef.isWidget)
                _fantasyUIRef.uiLayer = (UILayer)EditorGUILayout.EnumPopup(new GUIContent("UILayer", "生成UI时的节点位置"), _fantasyUIRef.uiLayer);
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
                    EditorUtility.SetDirty(_fantasyUIRef);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }

            if (GUILayout.Button("Sort"))
            {
                _fantasyUIRef.list.Sort(new FantasyUIDataComparer());
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

            for (var i = _fantasyUIRef.list.Count - 1; i >= 0; i--)
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

        private void Generate(SerializedProperty dataProperty)
        {
            var generatePath = FantasyUISettingsScriptableObject.Instance.uiGenerateSavePath;
            var isWidget = _fantasyUIRef.isWidget;

            if (string.IsNullOrEmpty(_fantasyUIRef.assetName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"assetName is null", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_fantasyUIRef.bundleName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"bundleName is null", "OK");
                return;
            }

            if (!isWidget && _fantasyUIRef.uiLayer == UILayer.None)
            {
                EditorUtility.DisplayDialog("Generate Code", $"UILayer is None", "OK");
                return;
            }

            if (string.IsNullOrEmpty(generatePath))
            {
                EditorUtility.DisplayDialog("Generate Code", $"UI Generate Save Path is null, please set it in FantasyUISettings", "OK");
                return;
            }

            bool hasModule = !string.IsNullOrEmpty(_fantasyUIRef.nameSpaceName);
            generatePath = hasModule ? $"{generatePath}/Module/{_fantasyUIRef.nameSpaceName}" : $"{generatePath}/Entity";

            if (!Directory.Exists(generatePath))
            {
                Directory.CreateDirectory(generatePath);
            }

            var createStr = new List<string>();
            var propertyStr = new List<string>();

            for (var i = _fantasyUIRef.list.Count - 1; i >= 0; i--)
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
            sb.AppendLine(hasModule ? $"\nnamespace Fantasy.{_fantasyUIRef.nameSpaceName.Replace('/', '.')}\n{{" : "\nnamespace Fantasy\n{");
            sb.AppendLine(isWidget ? $"\tpublic partial class {_fantasyUIRef.componentName} : SubUI\n\t{{" : $"\tpublic partial class {_fantasyUIRef.componentName} : UI\n\t{{");
            sb.AppendLine($"\t\tpublic override string AssetName {{ get; protected set; }} = \"{_fantasyUIRef.assetName}\";");
            sb.AppendLine($"\t\tpublic override string BundleName {{ get; protected set; }} = \"{_fantasyUIRef.bundleName.ToLower()}\";");
            if (!isWidget)
                sb.AppendLine($"\t\tpublic override UILayer Layer {{ get; protected set; }} = UILayer.{_fantasyUIRef.uiLayer.ToString()};\n");

            foreach (var property in propertyStr)
            {
                sb.AppendLine(property);
            }

            sb.AppendLine("\n\t\tpublic override void Initialize()\n\t\t{");
            sb.AppendLine("\t\t\tvar referenceComponent = GameObject.GetComponent<FantasyUIRef>();");

            foreach (var str in createStr)
            {
                sb.AppendLine(str);
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var combinePath = Path.Combine(generatePath, $"{_fantasyUIRef.componentName}.cs");
            using var entityStreamWriter = new StreamWriter(combinePath);
            entityStreamWriter.Write(sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}