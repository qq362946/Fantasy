using System;
using System.Collections.Generic;
using System.IO;
using Fantasy.Editor;
using Fantasy.Helper;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Fantasy.Core.Editor
{
    public class FantasySettings : EditorWindow
    {
        private bool _hasUnsavedChanges;
        private SerializedObject _serializedObject;

        private readonly List<string> _sourcePlugins = new List<string>()
        {
            "Packages/Fantasy.Unity/Plugins~/Android/arm64_v8a/libkcp.so",
            "Packages/Fantasy.Unity/Plugins~/Android/armeabi-v7a/libkcp.so",
            "Packages/Fantasy.Unity/Plugins~/Android/x86/libkcp.so",
            "Packages/Fantasy.Unity/Plugins~/IOS/libkcp.a",
            "Packages/Fantasy.Unity/Plugins~/x86_64/kcp.dll",
        };
                
        private readonly List<string> _targetPlugins = new List<string>()
        {
            "Assets/Plugins/Android/arm64_v8a/libkcp.so",
            "Assets/Plugins/Android/armeabi-v7a/libkcp.so",
            "Assets/Plugins/Android/x86/libkcp.so",
            "Assets/Plugins/IOS/libkcp.a",
            "Assets/Plugins/x86_64/kcp.dll"
        };
        
        [MenuItem("Fantasy/Fantasy Settings")]
        public static void ShowWindow()
        {
            var size = new Vector2(650, 420);
            var window = GetWindow<FantasySettings>(true,"Fantasy Settings");
            window.minSize = size;
            window._serializedObject = new SerializedObject(FantasySettingsScriptableObject.Instance);
        }

        private void OnEnable()
        {
            _hasUnsavedChanges = EditorPrefs.GetBool("HasUnsavedChanges", false);
        }

        private void OnDestroy()
        {
            EditorPrefs.SetBool("HasUnsavedChanges", _hasUnsavedChanges);
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

        private void OnGUI()
        {
            _serializedObject.Update();
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"安装状态：{(IsInstall() ? "已安装" : "未安装")}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("安装框架", GUILayout.Width(100)))
            {
                for (var i = 0; i < _sourcePlugins.Count; i++)
                {
                    FileHelper.Copy(_sourcePlugins[i], _targetPlugins[i], true);
                }
                
                FileHelper.CopyDirectory("Packages/Fantasy.Unity/Plugins~/MacOS/kcp.bundle", "Assets/Plugins/MacOS/kcp.bundle", true);
                Debug.Log("安装框架完成");
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            
            var serializedProperty = _serializedObject.GetIterator();

            serializedProperty.NextVisible(true);
            serializedProperty.Next(false);
            serializedProperty.Next(false);

            while (serializedProperty.Next(false))
            {
                EditorGUILayout.PropertyField(serializedProperty);
            }

            _serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                _hasUnsavedChanges = true;
            }
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("保存") && _hasUnsavedChanges)
            {
                _hasUnsavedChanges = false;
                FantasySettingsScriptableObject.Save();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}