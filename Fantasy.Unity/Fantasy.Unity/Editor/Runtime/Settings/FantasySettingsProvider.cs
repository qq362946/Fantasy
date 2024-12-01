using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fantasy
{
    public class FantasySettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _autoCopyAssembly;
        private SerializedProperty _hotUpdatePath;
        private SerializedProperty _hotUpdateAssemblyDefinitions;
        private SerializedProperty _linkAssemblyDefinitions;
        public FantasySettingsProvider() : base("Project/Fantasy Settings", SettingsScope.Project) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Init();
            base.OnActivate(searchContext, rootElement);
        }
        
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            FantasySettingsScriptableObject.Save();
        }

        private void Init()
        {
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(FantasySettingsScriptableObject.Instance);
            _autoCopyAssembly = _serializedObject.FindProperty("autoCopyAssembly");
            _hotUpdatePath = _serializedObject.FindProperty("hotUpdatePath");
            _hotUpdateAssemblyDefinitions = _serializedObject.FindProperty("hotUpdateAssemblyDefinitions");
            _linkAssemblyDefinitions = _serializedObject.FindProperty("linkAssemblyDefinitions");
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedObject == null || !_serializedObject.targetObject)
            {
                Init();
            }

            using (CreateSettingsWindowGUIScope())
            {
                _serializedObject!.Update();
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_autoCopyAssembly);
                EditorGUILayout.PropertyField(_hotUpdatePath);
                EditorGUILayout.PropertyField(_hotUpdateAssemblyDefinitions);
                EditorGUILayout.PropertyField(_linkAssemblyDefinitions);
                EditorGUILayout.HelpBox("默认包括Assembly-CSharp和Fantasy.Unity，所以不需要再次指定。", MessageType.Info);

                if (GUILayout.Button("GenerateLinkXml"))
                {
                    LinkXmlGenerator.GenerateLinkXml();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    FantasySettingsScriptableObject.Save();
                    EditorApplication.RepaintHierarchyWindow();
                }
                
                base.OnGUI(searchContext);
            }
        }

        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
        
        static FantasySettingsProvider _provider;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (FantasySettingsScriptableObject.Instance && _provider == null)
            {
                _provider = new FantasySettingsProvider();
                using (var so = new SerializedObject(FantasySettingsScriptableObject.Instance))
                {
                    _provider.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }
            return _provider;
        }
    }
}