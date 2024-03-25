using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fantasy
{
    [ScriptableObjectPath("ProjectSettings/FantasySettings.asset")]
    public class FantasySettingsScriptableObject : ScriptableObjectSingleton<FantasySettingsScriptableObject>, ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("AutoCopyAssembly")] [Header("自动拷贝程序集到HotUpdatePath目录中")]
        public bool autoCopyAssembly = true;

        [FormerlySerializedAs("UIGenerateSavePath")] [Header("UI生层代码路径")]
        public string uiGenerateSavePath;

        [FormerlySerializedAs("HotUpdatePath")] [Header("HotUpdate目录(Unity编译后会把所有HotUpdate程序集Copy一份到这个目录下)")]
        public string hotUpdatePath;

        [FormerlySerializedAs("HotUpdateAssemblyDefinitions")] [Header("HotUpdate程序集")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [FormerlySerializedAs("FantasyUIAutoRefFunction")] [Header("FantasyUI自动引用功能开启")]
        public bool fantasyUIAutoRefFunction;

        [FormerlySerializedAs("FantasyUIAutoRefSettings")] [Header("FantasyUI自动引用设置")] [SerializeField]
        private FantasyUIRefData[] fantasyUIAutoRefSettings =
        {
            new() { key = "btn", value = "Button" },
            new() { key = "img", value = "Image" },
            new() { key = "input", value = "TMP_InputField, InputField" },
            new() { key = "dropdown", value = "TMP_Dropdown" },
            new() { key = "txt", value = "TextMeshProUGUI, Text" },
            new() { key = "label", value = "TextMeshProUGUI, Text" },
            new() { key = "rect", value = "RectTransform" },
            new() { key = "tog", value = "Toggle" },
            new() { key = "cg", value = "CanvasGroup" },
            new() { key = "scroll", value = "FantasyVerticalScrollView, FantasyHorizontalScrollView, FantasyGridScrollView, ScrollRect" },
        };

        public readonly Dictionary<string, string> FantasyUIAutoRefSettings = new();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            FantasyUIAutoRefSettings.Clear();
            foreach (var kvp in fantasyUIAutoRefSettings)
            {
                if (string.IsNullOrEmpty(kvp.key) || string.IsNullOrEmpty(kvp.value))
                    continue;
                FantasyUIAutoRefSettings.TryAdd(kvp.key, kvp.value);
            }
        }
    }
}