using System;
using UnityEditor;
using UnityEngine;

namespace UniRecast.Editor

{
    public static class UniRcGui
    {
        public static void DrawAgentDiagram(Rect rect, float agentRadius, float agentHeight, float agentClimb, float agentSlope)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            float num1 = agentRadius;
            float num2 = agentHeight;
            float num3 = agentClimb;
            float num4 = 0.35f;
            float num5 = 20f;
            float num6 = 10f;
            float b = rect.height - (num5 + num6);
            float num7 = Mathf.Min(b / (num2 + num1 * 2f * num4), b / (num1 * 2f));
            float num8 = num2 * num7;
            float num9 = num1 * num7;
            float num10 = Mathf.Min(num3 * num7, b - num9 * num4);
            float x1 = rect.xMin + rect.width * 0.5f;
            float y1 = (float)((double)rect.yMax - (double)num6 - (double)num9 * (double)num4);
            Vector3[] vector3Array1 = new Vector3[40];
            Vector3[] vector3Array2 = new Vector3[20];
            Vector3[] vector3Array3 = new Vector3[20];
            for (int index = 0; index < 20; ++index)
            {
                float f = (float)((double)index / 19.0 * 3.1415927410125732);
                float num11 = Mathf.Cos(f);
                float num12 = Mathf.Sin(f);
                vector3Array1[index] = new Vector3(x1 + num11 * num9, (float)((double)y1 - (double)num8 - (double)num12 * (double)num9 * (double)num4), 0.0f);
                vector3Array1[index + 20] = new Vector3(x1 - num11 * num9, y1 + num12 * num9 * num4, 0.0f);
                vector3Array2[index] = new Vector3(x1 - num11 * num9, (float)((double)y1 - (double)num8 + (double)num12 * (double)num9 * (double)num4), 0.0f);
                vector3Array3[index] = new Vector3(x1 - num11 * num9, (float)((double)y1 - (double)num10 + (double)num12 * (double)num9 * (double)num4), 0.0f);
            }

            Color color = Handles.color;
            float xMin = rect.xMin;
            float y2 = y1 - num10;
            float x2 = x1 - b * 0.75f;
            float y3 = y1;
            float x3 = x1 + b * 0.75f;
            float y4 = y1;
            float num13 = x3;
            float num14 = y4;
            float num15 = Mathf.Min(rect.xMax - x3, b);
            float x4 = num13 + Mathf.Cos(agentSlope * ((float)Math.PI / 180f)) * num15;
            float y5 = num14 - Mathf.Sin(agentSlope * ((float)Math.PI / 180f)) * num15;
            Vector3[] vector3Array4 = new Vector3[2]
            {
                new Vector3(xMin, y1, 0.0f),
                new Vector3(x3 + num15, y1, 0.0f)
            };
            Vector3[] vector3Array5 = new Vector3[5]
            {
                new Vector3(xMin, y2, 0.0f),
                new Vector3(x2, y2, 0.0f),
                new Vector3(x2, y3, 0.0f),
                new Vector3(x3, y4, 0.0f),
                new Vector3(x4, y5, 0.0f)
            };
            Handles.color = EditorGUIUtility.isProSkin ? new Color(0.0f, 0.0f, 0.0f, 0.5f) : new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawAAPolyLine(2f, vector3Array4);
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.5f) : new Color(0.0f, 0.0f, 0.0f, 0.5f);
            Handles.DrawAAPolyLine(3f, vector3Array5);
            Handles.color = Color.Lerp(new Color(0.0f, 0.75f, 1f, 1f), new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.2f);
            Handles.DrawAAConvexPolygon(vector3Array1);
            if ((double)agentClimb <= (double)agentHeight)
            {
                Handles.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
                Handles.DrawAAPolyLine(2f, vector3Array3);
            }

            Handles.color = new Color(1f, 1f, 1f, 0.4f);
            Handles.DrawAAPolyLine(2f, vector3Array2);
            Vector3[] vector3Array6 = new Vector3[2]
            {
                new Vector3(x1, y1 - num8, 0.0f),
                new Vector3(x1 + num9, y1 - num8, 0.0f)
            };
            Handles.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
            Handles.DrawAAPolyLine(2f, vector3Array6);
            GUI.Label(new Rect((float)((double)x1 + (double)num9 + 5.0), (float)((double)y1 - (double)num8 * 0.5 - 10.0), 150f, 20f), $"H = {agentHeight}");
            GUI.Label(new Rect(x1, (float)((double)y1 - (double)num8 - (double)num9 * (double)num4 - 15.0), 150f, 20f), $"R = {agentRadius}");
            GUI.Label(new Rect((float)(((double)xMin + (double)x2) * 0.5 - 20.0), y2 - 15f, 150f, 20f), $"{agentClimb}");
            GUI.Label(new Rect(x3 + 20f, y4 - 15f, 150f, 20f), $"{agentSlope}°");
            Handles.color = color;
        }

        public static float SnapFloat(float value, float snapValue)
        {
            return Mathf.Round(value / snapValue) * snapValue;
        }

        public static int SnapInt(int value, int snapValue)
        {
            return Mathf.RoundToInt(value / (float)snapValue) * snapValue;
        }

        public static void SliderFloat(string label, SerializedProperty property, float min, float max, float snapValue, params GUILayoutOption[] options)
        {
            EditorGUILayout.Slider(property, min, max, label, GUILayout.ExpandWidth(true));
            property.floatValue = SnapFloat(property.floatValue, snapValue);
        }

        public static void SliderInt(string label, SerializedProperty property, int min, int max)
        {
            SliderInt(label, property, min, max, 1);
        }

        public static void SliderInt(string label, SerializedProperty property, int min, int max, int snapValue)
        {
            EditorGUILayout.IntSlider(property, min, max, label, GUILayout.ExpandWidth(true));
            property.intValue = SnapInt(property.intValue, snapValue);
        }

        public static bool Checkbox(string label, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            bool prev = property.boolValue;
            var v = EditorGUILayout.Toggle(prev, GUILayout.ExpandWidth(true));
            if (prev != v)
            {
                property.boolValue = v;
            }

            EditorGUILayout.EndHorizontal();

            return v;
        }

        public static int CheckboxFlags(string label, SerializedProperty property, int flags)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            bool prev = 0 != (property.intValue & flags);
            var v = EditorGUILayout.Toggle(prev, GUILayout.ExpandWidth(true));
            if (prev != v)
            {
                property.intValue = v
                    ? property.intValue | flags
                    : property.intValue & ~flags;
            }

            EditorGUILayout.EndHorizontal();
            return property.intValue;
        }

        public static void RadioButton(string label, SerializedProperty property, int btnValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            GUILayout.FlexibleSpace();
            bool prev = property.intValue == btnValue;
            var v = EditorGUILayout.Toggle(prev, EditorStyles.radioButton, GUILayout.ExpandWidth(true));
            if (prev != v)
            {
                property.intValue = btnValue;
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void NewLine()
        {
            EditorGUILayout.LabelField(" ");
        }

        public static void Separator(float height = 5.0f)
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            rect.y += EditorGUIUtility.singleLineHeight / 10.0f;
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color32(128, 128, 128, 255));
        }

        public static void Text(string label)
        {
            EditorGUILayout.LabelField(label);
        }

        public static bool Button(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            bool clicked = GUILayout.Button(label);
            GUILayout.EndHorizontal();

            return clicked;
        }
    }
}