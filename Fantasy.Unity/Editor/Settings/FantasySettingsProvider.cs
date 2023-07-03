using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Fantasy.Helper;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fantasy.Editor
{
    public class FantasySettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _autoCopyAssembly;
        private SerializedProperty _uiGenerateSavePath;
        private SerializedProperty _editorModel;
        private SerializedProperty _remoteUpdatePath;
        private SerializedProperty _hotUpdatePath;
        private SerializedProperty _hotUpdateAssemblyDefinitions;
        private readonly List<string> _sourcePlugins = new List<string>()
        {
            "/Plugins~/Android/arm64_v8a/libkcp.so",
            "/Plugins~/Android/armeabi-v7a/libkcp.so",
            "/Plugins~/Android/x86/libkcp.so",
            "/Plugins~/IOS/libkcp.a",
            "/Plugins~/x86_64/kcp.dll",
        };
                
        private readonly List<string> _targetPlugins = new List<string>()
        {
            "Assets/Plugins/Android/arm64_v8a/libkcp.so",
            "Assets/Plugins/Android/armeabi-v7a/libkcp.so",
            "Assets/Plugins/Android/x86/libkcp.so",
            "Assets/Plugins/IOS/libkcp.a",
            "Assets/Plugins/x86_64/kcp.dll"
        };
        public FantasySettingsProvider() : base("Project/Fantasy Settings", SettingsScope.Project) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EditorStatusWatcher.OnEditorFocused += OnEditorFocused;
            Init();
            base.OnActivate(searchContext, rootElement);
        }
        
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            EditorStatusWatcher.OnEditorFocused -= OnEditorFocused;
            FantasySettingsScriptableObject.Save();
        }
        
        private void OnEditorFocused()
        {
            Init();
            Repaint();
        }

        private void Init()
        {
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(FantasySettingsScriptableObject.Instance);
            _autoCopyAssembly = _serializedObject.FindProperty("autoCopyAssembly");
            _uiGenerateSavePath = _serializedObject.FindProperty("uiGenerateSavePath");
            _editorModel = _serializedObject.FindProperty("editorModel");
            _remoteUpdatePath = _serializedObject.FindProperty("remoteUpdatePath");
            _hotUpdatePath = _serializedObject.FindProperty("hotUpdatePath");
            _hotUpdateAssemblyDefinitions = _serializedObject.FindProperty("hotUpdateAssemblyDefinitions");
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedObject == null || !_serializedObject.targetObject)
            {
                Init();
            }

            using (CreateSettingsWindowGUIScope())
            {
                _serializedObject.Update();
            
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();
                var isInstall = IsInstall();
                EditorGUILayout.LabelField($"安装状态：{(isInstall ? "已安装" : "未安装")}", EditorStyles.boldLabel);

                if (GUILayout.Button(isInstall ? "重新安装" : "安装框架", GUILayout.Width(100)))
                {
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/com.fantasy.unity");
                    var resolvedPath = packageInfo.resolvedPath;

                    for (var i = 0; i < _sourcePlugins.Count; i++)
                    {
                        FileHelper.Copy($"{resolvedPath}{_sourcePlugins[i]}", _targetPlugins[i], true);
                    }

                    FileHelper.CopyDirectory($"{resolvedPath}/Plugins~/MacOS/kcp.bundle", "Assets/Plugins/MacOS/kcp.bundle", true);
                    Debug.Log("安装框架完成");
                    AssetDatabase.Refresh();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Separator();
            
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.PropertyField(_autoCopyAssembly);
                EditorGUILayout.PropertyField(_uiGenerateSavePath);
                EditorGUILayout.PropertyField(_editorModel);
                EditorGUILayout.PropertyField(_remoteUpdatePath);
                EditorGUILayout.PropertyField(_hotUpdatePath);
                EditorGUILayout.PropertyField(_hotUpdateAssemblyDefinitions);
                
                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    FantasySettingsScriptableObject.Save();
                }
                
                base.OnGUI(searchContext);
            }
        }
        
        private bool IsInstall()
        {
            foreach (var plugin in _targetPlugins)
            {
                if (File.Exists(plugin))
                {
                    continue;
                }

                return false;
            }
            
            return Directory.Exists("Assets/Plugins/MacOS/kcp.bundle");
        }
        
        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
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