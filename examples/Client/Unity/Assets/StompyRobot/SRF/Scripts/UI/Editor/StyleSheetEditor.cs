using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof (StyleSheet))]
    public class StyleSheetEditor : UnityEditor.Editor
    {
        public const float ColourColumnWidth = 40f;
        public const float OverrideColumnWidth = 20f;
        private string _newKeyField = "";

        public override void OnInspectorGUI()
        {
            var styleSheet = target as StyleSheet;

            if (styleSheet == null)
            {
                Debug.LogWarning("Expected target to be StyleSheer", target);
                return;
            }

            var parentStyleSheet = styleSheet.Parent;

            styleSheet.Parent =
                EditorGUILayout.ObjectField("Parent", styleSheet.Parent, typeof (StyleSheet), false) as StyleSheet;

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginVertical();

            // Draw table header
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(OverrideColumnWidth);
            GUILayout.Label("Name");
            GUILayout.Label("Img", GUILayout.Width(ColourColumnWidth));
            GUILayout.Label("Norm.", GUILayout.Width(ColourColumnWidth));
            GUILayout.Label("Hov.", GUILayout.Width(ColourColumnWidth));
            GUILayout.Label("Actv.", GUILayout.Width(ColourColumnWidth));
            GUILayout.Label("Dsbld.", GUILayout.Width(ColourColumnWidth));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            var keys = styleSheet.GetStyleKeys(false);

            if (parentStyleSheet != null)
            {
                keys = keys.Union(parentStyleSheet.GetStyleKeys());
            }

            // Draw rows
            foreach (var key in keys)
            {
                // Style from the current stylesheet
                var style = styleSheet.GetStyle(key, false);

                // Style from the parent stylesheet
                Style parentStyle = null;

                if (parentStyleSheet != null)
                {
                    parentStyle = parentStyleSheet.GetStyle(key, true);
                }

                EditorGUILayout.BeginHorizontal();

                var canEdit = style != null;

                // If there is a parent stylesheet, and the parent contains this key
                if (parentStyleSheet != null && parentStyle != null)
                {
                    var overrideParent = GUILayout.Toggle(style != null, "", GUILayout.Width(OverrideColumnWidth));

                    if (overrideParent && style == null)
                    {
                        // Copy the style to the current stylesheet
                        Undo.RecordObject(styleSheet, "Override Style");
                        styleSheet.AddStyle(key);
                        style = styleSheet.GetStyle(key, false);
                        style.CopyFrom(parentStyle);
                        EditorUtility.SetDirty(styleSheet);
                        canEdit = true;
                    }
                    else if (!overrideParent && style != null)
                    {
                        Undo.RecordObject(styleSheet, "Delete Style");
                        styleSheet.DeleteStyle(key);
                        style = null;
                        EditorUtility.SetDirty(styleSheet);
                        canEdit = false;
                    }
                }
                else
                {
                    // Otherwise display a delete button

                    if (GUILayout.Button("X", GUILayout.Width(OverrideColumnWidth)))
                    {
                        Undo.RecordObject(styleSheet, "Delete Style");
                        styleSheet.DeleteStyle(key);
                        EditorUtility.SetDirty(styleSheet);

                        continue;
                    }
                }

                GUI.enabled = canEdit;

                GUILayout.Label(key);

                EditorGUI.BeginChangeCheck();

                var img =
                    EditorGUILayout.ObjectField(style != null ? style.Image : parentStyle.Image, typeof (Sprite), false,
                        GUILayout.Width(ColourColumnWidth)) as Sprite;

                var normalColor = EditorGUILayout.ColorField(
                    style != null ? style.NormalColor : parentStyle.NormalColor,
                    GUILayout.Width(ColourColumnWidth));
                var hoverColor = EditorGUILayout.ColorField(style != null ? style.HoverColor : parentStyle.HoverColor,
                    GUILayout.Width(ColourColumnWidth));
                var activeColor = EditorGUILayout.ColorField(
                    style != null ? style.ActiveColor : parentStyle.ActiveColor,
                    GUILayout.Width(ColourColumnWidth));
                var disabledColor =
                    EditorGUILayout.ColorField(style != null ? style.DisabledColor : parentStyle.DisabledColor,
                        GUILayout.Width(ColourColumnWidth));

                if (EditorGUI.EndChangeCheck() && canEdit)
                {
                    Undo.RecordObject(styleSheet, "Update Style");

                    style.Image = img;
                    style.NormalColor = normalColor;
                    style.HoverColor = hoverColor;
                    style.ActiveColor = activeColor;
                    style.DisabledColor = disabledColor;

                    EditorUtility.SetDirty(styleSheet);
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("New Style");

            _newKeyField = EditorGUILayout.TextField(_newKeyField);

            GUI.enabled = !string.IsNullOrEmpty(_newKeyField) && styleSheet.GetStyle(_newKeyField) == null;

            if (GUILayout.Button("Add"))
            {
                Undo.RecordObject(styleSheet, "Add Style");
                styleSheet.AddStyle(_newKeyField);
                EditorUtility.SetDirty(styleSheet);

                _newKeyField = "";
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }
}
