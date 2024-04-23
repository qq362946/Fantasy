using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fantasy
{
    public class FantasyUISettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _uiGenerateSavePath;
        private SerializedProperty _fantasyUIAutoRefFunction;
        private SerializedProperty _fantasyUIAutoRefSettings;
        public FantasyUISettingsProvider() : base("Project/FantasyUI Settings", SettingsScope.Project) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Init();
            base.OnActivate(searchContext, rootElement);
        }
        
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            FantasyUISettingsScriptableObject.Save();
        }

        private void Init()
        {
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(FantasyUISettingsScriptableObject.Instance);
            _uiGenerateSavePath = _serializedObject.FindProperty("uiGenerateSavePath");
            _fantasyUIAutoRefFunction = _serializedObject.FindProperty("fantasyUIAutoRefFunction");
            _fantasyUIAutoRefSettings = _serializedObject.FindProperty("fantasyUIAutoRefSettings");
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
                EditorGUILayout.PropertyField(_uiGenerateSavePath);
                EditorGUILayout.PropertyField(_fantasyUIAutoRefFunction);
                EditorGUILayout.PropertyField(_fantasyUIAutoRefSettings);
                
                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    FantasyUISettingsScriptableObject.Save();
                    EditorApplication.RepaintHierarchyWindow();
                }
                
                base.OnGUI(searchContext);
            }
        }

        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
        
        static FantasyUISettingsProvider _provider;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (FantasyUISettingsScriptableObject.Instance && _provider == null)
            {
                _provider = new FantasyUISettingsProvider();
                using (var so = new SerializedObject(FantasyUISettingsScriptableObject.Instance))
                {
                    _provider.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }
            return _provider;
        }
    }
}