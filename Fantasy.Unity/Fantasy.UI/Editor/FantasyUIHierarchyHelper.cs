using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fantasy
{
    public static class FantasyUIHierarchyHelper
    {
        private static readonly Dictionary<int, bool> FantasyRefs = new();
        private static readonly HashSet<int> RemoveFlag = new();

        private static Dictionary<string, string> NameComponentNameMap => FantasyUISettingsScriptableObject.Instance.FantasyUIAutoRefSettings;

        private static GUIStyle Style => new(EditorStyles.label) { normal = { textColor = Color.green } };

        [InitializeOnLoadMethod]
        public static void DrawFantasyUIMark()
        {
            OnHierarchyChanged();
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemOnGUI;
        }

        private static void OnHierarchyChanged()
        {
            RefreshHierarchy();
        }

        public static void RefreshHierarchy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            
            FantasyRef[] fantasyUIs = null;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                fantasyUIs = prefabStage.prefabContentsRoot.GetComponentsInChildren<FantasyRef>();
            }

            fantasyUIs ??= Object.FindObjectsByType<FantasyRef>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var (key, _) in FantasyRefs)
            {
                RemoveFlag.Add(key);
            }
            foreach (var c in fantasyUIs)
            {
                var gameObjectInstanceID = c.gameObject.GetInstanceID();
                FantasyRefs.TryAdd(gameObjectInstanceID, false);
                RemoveFlag.Remove(gameObjectInstanceID);
            }
            foreach (var remove in RemoveFlag)
            {
                FantasyRefs.Remove(remove, out _);
            }
        }

        private static void OnHierarchyItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !FantasyUISettingsScriptableObject.Instance.fantasyUIAutoRefFunction)
            {
                return;
            }

            DrawFantasyComponentTips(instanceID, selectionRect);

            if (FantasyRefs.TryGetValue(instanceID, out var value))
            {
                var gameObject = (GameObject)EditorUtility.InstanceIDToObject(instanceID);
                if (!gameObject)
                    return;
                var c = gameObject.GetComponent<FantasyRef>();
                Rect r = new Rect(selectionRect.xMax - 100, selectionRect.y, 50, selectionRect.height);
                if (c is FantasyUIRef fr)
                {
                    GUI.Label(r, fr.isWidget ? "Widget" : "Window", Style);
                }
                else if(c is not null)
                {
                    GUI.Label(r, "Ref", Style);
                }
            }
        }

        private static void DrawFantasyComponentTips(int instanceID, Rect selectionRect)
        {
            var gameObject = (GameObject)EditorUtility.InstanceIDToObject(instanceID);
            if (!gameObject)
                return;


            Rect r = new Rect(selectionRect.xMax - 10, selectionRect.y, 20, selectionRect.height);

            // ReSharper disable once StringLiteralTypo
            var warnIcon = EditorGUIUtility.IconContent("console.warnicon");
            var scrollIcon = EditorGUIUtility.IconContent("d_ScrollRect Icon");

            var fantasyScrollView = false;

            var list = FantasyAutoRefRuleHelper.GetRefInfoByName(gameObject.name);
            var canRecognize = !string.IsNullOrEmpty(list[0]) && NameComponentNameMap.ContainsKey(list[0]);

            if (!canRecognize) return;
            var componentNames = NameComponentNameMap[list[0]];
            if (string.IsNullOrEmpty(componentNames)) return;
            var componentNameList = componentNames.Replace(" ", "").Split(",");
            Component component = null;
            string curComponentName = null;
            foreach (var componentName in componentNameList)
            {
                if (componentName == "GameObject")
                {
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_GameObject Icon")));   
                    return;
                }
                component = gameObject.GetComponent(componentName);
                curComponentName = componentName;
                if (component)
                    break;
            }

            if (component == null) return;
            switch (component)
            {
                case Button:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Button Icon")));
                    break;
                case Image:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Image Icon")));
                    break;
                case RawImage:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_RawImage Icon")));
                    break;
                case Text:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Text Icon")));
                    break;
                case Toggle:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Toggle Icon")));
                    break;
                case ToggleGroup:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_ToggleGroup Icon")));
                    break;
                case Slider:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Slider Icon")));
                    break;
                case Canvas:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Canvas Icon")));
                    break;
                case CanvasGroup:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_CanvasGroup Icon")));
                    break;
                case Scrollbar:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Scrollbar Icon")));
                    break;
                case ScrollRect:
                    if (!fantasyScrollView)
                        EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_ScrollRect Icon")));
                    break;
                case InputField:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_InputField Icon")));
                    break;
                case Dropdown:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Dropdown Icon")));
                    break;
                case RectTransform:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_RectTransform Icon")));
                    break;
                case Transform:
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Transform Icon")));
                    break;
            }

            switch (curComponentName)
            {
                case "TextMeshProUGUI":
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Text Icon")));
                    break;
                case "TMP_InputField":
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_InputField Icon")));
                    break;
                case "TMP_Dropdown":
                    EditorGUI.LabelField(r, new GUIContent(EditorGUIUtility.IconContent("d_Dropdown Icon")));
                    break;
            }
        }
    }
}