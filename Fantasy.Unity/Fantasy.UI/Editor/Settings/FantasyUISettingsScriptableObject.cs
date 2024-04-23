using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fantasy
{
    [ScriptableObjectPath("ProjectSettings/FantasyUISettings.asset")]
    public class FantasyUISettingsScriptableObject : ScriptableObjectSingleton<FantasyUISettingsScriptableObject>, ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("UIGenerateSavePath")] [Header("UI生层代码路径")]
        public string uiGenerateSavePath;
        [FormerlySerializedAs("FantasyUIAutoRefFunction")] [Header("FantasyUI自动引用功能开启")]
        public bool fantasyUIAutoRefFunction;
        [FormerlySerializedAs("FantasyUIAutoRefSettings")] [Header("FantasyUI自动引用设置")] 
        [SerializeField]
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