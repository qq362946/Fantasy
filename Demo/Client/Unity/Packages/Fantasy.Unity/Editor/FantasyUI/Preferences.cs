using System;
using System.Collections.Generic;
using System.IO;
using Fantasy.Helper;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Fantasy.Core.Editor
{
    public class Preferences : EditorWindow
    {
        private string _generatePath;
        private const string GeneratePathKey = "FantasyUIGeneratePath";
        
        [MenuItem("Fantasy/Preferences")]
        public static void ShowWindow()
        {
            var size = new Vector2(500, 110);
            var window = GetWindow<Preferences>();
            window.maxSize = size;
            window.minSize = size;
            window.titleContent = new GUIContent("Preferences");
            window.Show();
        }

        public static string GetGeneratePath()
        {
            return EditorPrefs.GetString(GeneratePathKey);
        }

        private void OnEnable()
        {
            _generatePath = EditorPrefs.GetString(GeneratePathKey);
        }

        private bool IsInstall()
        {
            var plugins = new List<string>()
            {
                "Assets/Plugins/Android/arm64_v8a/libkcp.so",
                "Assets/Plugins/Android/armeabi-v7a/libkcp.so",
                "Assets/Plugins/Android/x86/libkcp.so",
                "Assets/Plugins/IOS/libkcp.a",
                "Assets/Plugins/x86_64/kcp.dll",
                "Assets/csc.rsp"
            };

            foreach (var plugin in plugins)
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"安装状态：{(IsInstall() ? "已安装" : "未安装")}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("安装框架", GUILayout.Width(100)))
            {
                var sourcePlugins = new List<string>()
                {
                    "Packages/Fantasy.Unity/Plugins~/Android/arm64_v8a/libkcp.so",
                    "Packages/Fantasy.Unity/Plugins~/Android/armeabi-v7a/libkcp.so",
                    "Packages/Fantasy.Unity/Plugins~/Android/x86/libkcp.so",
                    "Packages/Fantasy.Unity/Plugins~/IOS/libkcp.a",
                    "Packages/Fantasy.Unity/Plugins~/x86_64/kcp.dll",
                    "Packages/Fantasy.Unity/Plugins~/csc.rsp",
                };
                
                var targetPlugins = new List<string>()
                {
                    "Assets/Plugins/Android/arm64_v8a/libkcp.so",
                    "Assets/Plugins/Android/armeabi-v7a/libkcp.so",
                    "Assets/Plugins/Android/x86/libkcp.so",
                    "Assets/Plugins/IOS/libkcp.a",
                    "Assets/Plugins/x86_64/kcp.dll",
                    "Assets/csc.rsp"
                };
                
                for (var i = 0; i < sourcePlugins.Count; i++)
                {
                    FileHelper.Copy(sourcePlugins[i], targetPlugins[i], true);
                }
                
                FileHelper.CopyDirectory("Packages/Fantasy.Unity/Plugins~/MacOS/kcp.bundle",
                    "Assets/Plugins/MacOS/kcp.bundle", true);

                Debug.Log("安装框架完成");
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("GeneratePath为生成代码的路径，如:Assets/Resources/", MessageType.Info);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            _generatePath = EditorGUILayout.TextField("    GeneratePath", _generatePath);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("保存"))
            {
                Log.Debug(_generatePath);
                EditorPrefs.SetString(GeneratePathKey, _generatePath);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}