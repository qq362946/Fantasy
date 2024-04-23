using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fantasy
{
    public static class FantasyUIHierarchyHelper
    {
        private static readonly Dictionary<int, FantasyRef> IsReference = new();
        private static readonly Dictionary<int, bool> FantasyRefs = new();
        private static readonly Dictionary<int, FantasyRef> NotReference = new();
        private static readonly HashSet<int> RemoveFlag = new();

        private static Dictionary<string, string> NameComponentNameMap => FantasyUISettingsScriptableObject.Instance.FantasyUIAutoRefSettings;

        private static GUIStyle Style => new(EditorStyles.label) { normal = { textColor = Color.green } };

        [InitializeOnLoadMethod]
        public static void DrawFantasyUIMark()
        {
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
            IsReference.Clear();
            NotReference.Clear();
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
                FantasyRefs.TryRemove(remove, out _);
            }

            foreach (var (key, value) in FantasyRefs)
            {
                if (!value)
                    continue;
                var gameObject = (GameObject)EditorUtility.InstanceIDToObject(key);
                var c = gameObject.GetComponent<FantasyRef>();
                c.list.ForEach(o =>
                {
                    var instanceID = (o.gameObject as Component)?.gameObject.GetInstanceID() ?? o.gameObject.GetInstanceID();
                    IsReference.Add(instanceID, c);
                });

                var transformList = c.GetComponentsInChildren<Transform>(true);
                foreach (var t in transformList)
                {
                    var instanceID = t.gameObject.GetInstanceID();
                    if (t != c.transform && !IsReference.ContainsKey(instanceID))
                    {
                        NotReference.TryAdd(instanceID, c);
                    }
                }
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
                Rect r = new Rect(selectionRect.xMax - 60, selectionRect.y, 40, selectionRect.height);
                if (c is FantasyUIRef)
                {
                    GUI.Label(r, "UI", Style);
                }
                else
                {
                    GUI.Label(r, c.isUI ? "SubUI" : "Ref", Style);
                }

                r.x -= 20;
                r.width = 10;
                FantasyRefs[instanceID] = GUI.Toggle(r, value, GUIContent.none);

                if (FantasyRefs[instanceID] && FantasyRefs[instanceID] != value)
                {
                    int? target = null;
                    foreach (var (key, b) in FantasyRefs)
                    {
                        if (b && key != instanceID) target = key;
                    }

                    if (target != null)
                        FantasyRefs[(int)target] = false;
                }

                if (FantasyRefs[instanceID] != value)
                    RefreshHierarchy();
            }

            if (IsReference.TryGetValue(instanceID, out var fantasyRef))
            {
                Rect r = new Rect(selectionRect.xMax - 60, selectionRect.y, 45, selectionRect.height);
                GUI.backgroundColor = Color.red;
                if (GUI.Button(r, "移除"))
                {
                    var gameObject = EditorUtility.InstanceIDToObject(instanceID);
                    var list = gameObject.name.Split('_', 2);
                    if (list.Length > 0)
                    {
                        var first = list[0];
                        if (NameComponentNameMap.TryGetValue(first, out var componentNameStr))
                        {
                            var componentNameList = componentNameStr.Split(',');
                            Component component = null;
                            foreach (var componentName in componentNameList)
                            {
                                if (component)
                                    break;
                                component = (gameObject as GameObject)?.GetComponent(componentName);
                            }

                            gameObject = component ? component : gameObject;
                        }
                    }

                    var targetInstanceId = gameObject.GetInstanceID();
                    var idx = fantasyRef.list.FindIndex(data => data.gameObject.GetInstanceID() == targetInstanceId);
                    if (idx >= 0 && idx < fantasyRef.list.Count)
                    {
                        fantasyRef.list.RemoveAt(idx);
                        EditorUtility.SetDirty(fantasyRef);
                    }

                    RefreshHierarchy();
                }

                GUI.backgroundColor = Color.white;
            }

            if (NotReference.TryGetValue(instanceID, out fantasyRef))
            {
                Rect r = new Rect(selectionRect.xMax - 60, selectionRect.y, 45, selectionRect.height);
                GUI.backgroundColor = Color.blue;
                if (GUI.Button(r, "引用"))
                {
                    var gameObject = EditorUtility.InstanceIDToObject(instanceID);
                    var list = gameObject.name.Split('_', 2);
                    if (list.Length > 0)
                    {
                        var first = list[0];
                        if (NameComponentNameMap.TryGetValue(first, out var componentNameStr))
                        {
                            componentNameStr = componentNameStr.Replace(" ", "");
                            var componentNameList = componentNameStr.Split(',');
                            Component component = null;
                            foreach (var componentName in componentNameList)
                            {
                                if (component)
                                    break;
                                component = (gameObject as GameObject)?.GetComponent(componentName);
                            }

                            gameObject = component ? component : gameObject;
                        }
                    }

                    fantasyRef.list.Add(new FantasyUIData()
                    {
                        key = gameObject.name,
                        gameObject = gameObject,
                    });
                    EditorUtility.SetDirty(fantasyRef);
                    RefreshHierarchy();
                }

                GUI.backgroundColor = Color.white;
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

            // if (gameObject.TryGetComponent<FantasyGridScrollView>(out var gridScrollView))
            // {
            //     fantasyScrollView = true;
            //     if (!gridScrollView.scrollRect || !gridScrollView.contentSizeFitter || !gridScrollView.gridLayoutGroup || !gridScrollView.itemTemplate)
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(warnIcon));
            //     }
            //     else
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(scrollIcon));
            //     }
            // }

            // if (gameObject.TryGetComponent<FantasyVerticalScrollView>(out var verticalScrollView))
            // {
            //     fantasyScrollView = true;
            //     if (!verticalScrollView.scrollRect || !verticalScrollView.contentSizeFitter || !verticalScrollView.verticalLayoutGroup || !verticalScrollView.itemTemplate)
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(warnIcon));
            //     }
            //     else
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(scrollIcon));
            //     }
            // }

            // if (gameObject.TryGetComponent<FantasyHorizontalScrollView>(out var horizontalScrollView))
            // {
            //     fantasyScrollView = true;
            //     if (!horizontalScrollView.scrollRect || !horizontalScrollView.contentSizeFitter || !horizontalScrollView.horizontalLayoutGroup || !horizontalScrollView.itemTemplate)
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(warnIcon));
            //     }
            //     else
            //     {
            //         EditorGUI.LabelField(r, new GUIContent(scrollIcon));
            //     }
            // }

            var list = gameObject.name.Split('_', 2);
            var canRecognize = list.Length > 0 && NameComponentNameMap.ContainsKey(list[0]);

            if (!canRecognize) return;
            var componentNames = NameComponentNameMap[list[0]];
            if (string.IsNullOrEmpty(componentNames)) return;
            var componentNameList = componentNames.Replace(" ", "").Split(",");
            Component component = null;
            foreach (var componentName in componentNameList)
            {
                component = gameObject.GetComponent(componentName);
                if(component)
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

            switch (componentNames)
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