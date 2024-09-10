using SRF;

namespace SRDebugger.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    static class SRInternalEditorUtil
    {
        // Path to this file from the root path
        private const string TestPath = "SRDebugger/README.txt";

        private static GUIStyle _bgStyle;
        private static Texture2D _logoTexture;
        private static Texture2D _welcomeLogoTexture;
        private static Texture2D _bgTexture;
        private static GUIStyle _middleAlign;

        /// <summary>
        /// Finds the path to the SRDebugger installation folder (e.g. Assets/StompyRobot/SRDebugger)
        /// </summary>
        /// <returns></returns>
        public static string GetRootPath()
        {
            // Find assets that match this file name
            var potentialAssets = AssetDatabase.FindAssets("README");

            foreach (var potentialAsset in potentialAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(potentialAsset);

                if (path.Contains(TestPath))
                {
                    var rootPath = Path.GetDirectoryName(path);
                    return rootPath;
                }
            }

            throw new Exception("Unable to find SRDebugger root path. Please ensure the README file in StompyRobot/SRDebugger still exists.");
        }

        /// <summary>
        /// Finds the path to an SRDebugger asset relative to the installation root.
        /// </summary>
        /// <param name="relativeToRoot"></param>
        /// <returns></returns>
        public static string GetAssetPath(string relativeToRoot)
        {
            if (!relativeToRoot.StartsWith("/"))
            {
                relativeToRoot = "/" + relativeToRoot;
            }

            var p = GetRootPath() + relativeToRoot;
            return p;
        }

        public static T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            var p = GetAssetPath(path);
            //Debug.Log("[SRDebugger] Loading " + p);
            var asset = AssetDatabase.LoadAssetAtPath(p, typeof(T));
            return asset as T;
        }


#if !DISABLE_SRDEBUGGER

        public enum SettingsResult
        {
            Cache,
            Loaded,
            Waiting,
            Error
        }

        public static class EditorSettings
        {
            internal const string SettingsFilePath = "/usr/Resources/SRDebugger/Settings.asset";

            internal const string DisabledSettingsFilePath = "/usr" + SRDebugEditor.DisabledDirectoryPostfix + "/Resources/SRDebugger/Settings.asset";

            private static Settings _instance;

            public static SettingsResult TryGetOrCreate(out Settings instance, out string message)
            {
                if (_instance != null)
                {
                    instance = _instance;
                    message = string.Empty;
                    return SettingsResult.Cache;
                }

                try
                {
                    SettingsResult result = InternalTryGetOrCreateSettings(out _instance, out message);
                    instance = _instance;
                    return result;
                }
                catch (Exception e)
                {
                    instance = null;
                    message = e.ToString();
                    return SettingsResult.Error;
                }
            }

            public static void ClearCache()
            {
                Settings.ClearCache(); // Just in case runtime settings are loaded.

                if (_instance == null)
                {
                    return;
                }

                var instance = _instance;
                _instance = null;

                Resources.UnloadAsset(instance);
                instance = null;
                GC.Collect();

                EditorUtility.UnloadUnusedAssetsImmediate();
                Resources.UnloadUnusedAssets();
            }

            private static SettingsResult InternalTryGetOrCreateSettings(out Settings instance, out string message)
            {
                instance = null;
                message = null;

                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    message = "Settings can only be modified while in edit-mode. Exit play mode to make changes to SRDebugger settings.";
                    return SettingsResult.Waiting;
                }

                // If compiling, wait for that to finish. We might be disabling/enabling SRDebugger and don't want to recreate the settings while that is in progress.
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    message = "Waiting for Unity to finish compiling/updating...";
                    return SettingsResult.Waiting;
                }

                // Check if there is a 'disabled' settings file, we don't want to create a new settings file if a disabled one exists.
                string disabledSettingsFile = SRInternalEditorUtil.GetAssetPath(DisabledSettingsFilePath);
                if (File.Exists(disabledSettingsFile))
                {
                    message = "A settings file already exists but is disabled. Please ensure SRDebugger is correctly enabled or disabled.";
                    return SettingsResult.Error;
                }

                // Get resources folder path
                var settingsAssetPath = SRInternalEditorUtil.GetAssetPath(SettingsFilePath);

                // Load existing asset.
                if (File.Exists(settingsAssetPath))
                {
                    instance = AssetDatabase.LoadAssetAtPath<Settings>(settingsAssetPath);

                    if (instance == null)
                    {
                        message = "Error loading settings asset.";
                        return SettingsResult.Error;
                    }

                    return SettingsResult.Loaded;
                }

                Debug.Log("[SRDebugger] Creating settings asset at {0}".Fmt(settingsAssetPath));

                instance = ScriptableObject.CreateInstance<Settings>();

                string containingDirectory = Path.GetDirectoryName(settingsAssetPath);
                if (containingDirectory == null)
                {
                    message = "Error finding target settings directory.";
                    return SettingsResult.Error;
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(containingDirectory);

                // Save instance if in editor
                AssetDatabase.CreateAsset(instance, settingsAssetPath);

                return SettingsResult.Loaded;
            }
        }
#endif

        public static Texture2D GetLogo()
        {
            if (_logoTexture != null)
            {
                return _logoTexture;
            }

            return
                _logoTexture =
                    LoadResource<Texture2D>("Editor/Logo_" + (EditorGUIUtility.isProSkin ? "DarkBG" : "LightBG") +
                                            ".png");
        }

        public static Texture2D GetWelcomeLogo()
        {
            if (_welcomeLogoTexture != null)
            {
                return _welcomeLogoTexture;
            }

            return
                _welcomeLogoTexture =
                    LoadResource<Texture2D>("Editor/WelcomeLogo_" +
                                            (EditorGUIUtility.isProSkin ? "DarkBG" : "LightBG") + ".png");
        }

        public static Texture2D GetBackground()
        {
            if (_bgTexture != null)
            {
                return _bgTexture;
            }

            return
                _bgTexture =
                    LoadResource<Texture2D>("Editor/BG_" + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".png");
        }

        public static void DrawLogo(Texture2D logo)
        {
            if (logo == null)
            {
                Debug.LogError("Error loading SRDebugger logo");
                return;
            }

#if !DISABLE_SRDEBUGGER
            var rect =
#endif
                EditorGUILayout.BeginVertical();

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.DrawTexture(
                GUILayoutUtility.GetRect(logo.width, logo.width, logo.height, logo.height, GUILayout.ExpandHeight(false),
                    GUILayout.ExpandWidth(false)),
                logo);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            EditorGUILayout.EndVertical();

#if !DISABLE_SRDEBUGGER
            var size = EditorStyles.miniLabel.CalcSize(new GUIContent(SRDebug.Version));
            GUI.Label(new Rect(rect.xMax - size.x, rect.yMax - size.y, size.x, size.y), SRDebug.Version,
                EditorStyles.miniLabel);
#endif
        }

        public static bool DrawInspectorFoldout(bool isVisible, string content)
        {
            isVisible = EditorGUILayout.Foldout(isVisible, content, Styles.InspectorHeaderFoldoutStyle);

            EditorGUILayout.Separator();

            return isVisible;
        }

        public static void BeginDrawBackground()
        {
            if (_bgStyle == null)
            {
                _bgStyle = new GUIStyle();
                _bgStyle.margin = _bgStyle.padding = new RectOffset(0, 0, 0, 0);
            }

            var rect = EditorGUILayout.BeginVertical(_bgStyle);

            DrawTextureTiled(rect, GetBackground());
        }

        public static void EndDrawBackground()
        {
            EditorGUILayout.EndVertical();
        }

        public static void DrawTextureTiled(Rect rect, Texture2D tex)
        {
            GUI.BeginGroup(rect);

            var tilesX = Mathf.Max(1, Mathf.CeilToInt(rect.width / tex.width));
            var tilesY = Mathf.Max(1, Mathf.CeilToInt(rect.height / tex.height));

            for (var x = 0; x < tilesX; x++)
            {
                for (var y = 0; y < tilesY; y++)
                {
                    var pos = new Rect(x * tex.width, y * tex.height, tex.width, tex.height);
                    pos.x += rect.x;
                    pos.y += rect.y;

                    GUI.DrawTexture(pos, tex, ScaleMode.ScaleAndCrop);
                }
            }

            GUI.EndGroup();
        }

        public static bool ClickableLabel(string text, GUIStyle style)
        {
            var rect = EditorGUILayout.BeginVertical(Styles.NoPaddingNoMargin);

            GUILayout.Label(text, style);

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseUp && rect.Contains(Event.current.mousePosition))
            {
                return true;
            }

            return false;
        }

#if !DISABLE_SRDEBUGGER
        public static void DrawLayoutPreview(Rect rect, Settings settings)
        {
            const int profilerWidth = 120;
            const int profilerHeight = 70;
            const int optionsWidth = 150;
            const int optionsHeight = 36;

            if (_middleAlign == null)
            {
                _middleAlign = new GUIStyle(EditorStyles.helpBox);
                _middleAlign.alignment = TextAnchor.MiddleCenter;
            }

            var iconPath = "Editor/Icons/" + (EditorGUIUtility.isProSkin ? "Light" : "Dark");

            const float consoleHeight = 90;

            GUI.Box(rect, "", EditorStyles.helpBox);

            var consoleAlignment = settings.ConsoleAlignment;

            var consoleRect = new Rect(rect.x,
                consoleAlignment == ConsoleAlignment.Top ? rect.y : rect.yMax - consoleHeight, rect.width,
                consoleHeight);

            GUI.Box(consoleRect, new GUIContent(LoadResource<Texture2D>(iconPath + "/console-25.png"), "Console"),
                _middleAlign);

            var workRect = rect;

            if (consoleAlignment == ConsoleAlignment.Top)
            {
                workRect.yMin += consoleHeight;
            }
            else
            {
                workRect.yMax -= consoleHeight;
            }

            var opAlignment = settings.OptionsAlignment;
            var proAlignment = settings.ProfilerAlignment;

            GUI.Box(GetAlignedRect(profilerWidth, profilerHeight, proAlignment, workRect),
                new GUIContent(LoadResource<Texture2D>(iconPath + "/profiler-25.png"), "Profiler"), _middleAlign);

            var optionsRect = workRect;

            if ((opAlignment == PinAlignment.TopCenter && proAlignment == PinAlignment.TopLeft) || (opAlignment == PinAlignment.BottomCenter && proAlignment == PinAlignment.BottomLeft))
            {
                optionsRect.x += profilerWidth;
                optionsRect.width -= profilerWidth;
            }
            else if ((opAlignment == PinAlignment.TopCenter && proAlignment == PinAlignment.TopRight) ||
                     opAlignment == PinAlignment.BottomCenter && proAlignment == PinAlignment.BottomRight)
            {
                optionsRect.width -= profilerWidth;
            }

            GUI.Box(GetAlignedRect(optionsWidth, optionsHeight, opAlignment, optionsRect),
                new GUIContent(LoadResource<Texture2D>(iconPath + "/options-25.png"), "Pinned Options"), _middleAlign);

            if (settings.EnableTrigger != Settings.TriggerEnableModes.Off)
            {
                GUI.Box(GetAlignedRect(25, 25, settings.TriggerPosition, rect),
                    new GUIContent("", "Entry Trigger"),
                    _middleAlign);
            }
        }

        private static Rect GetAlignedRect(int width, int height, PinAlignment alignment, Rect workRect)
        {
            var rect = new Rect(0, 0, width, height);

            if (alignment == PinAlignment.BottomLeft || alignment == PinAlignment.BottomRight || alignment == PinAlignment.BottomCenter)
            {
                rect.position = new Vector2(0, workRect.height - rect.height);
            }
            else if (alignment == PinAlignment.CenterLeft || alignment == PinAlignment.CenterRight)
            {
                rect.position = new Vector2(0, workRect.height / 2 - rect.height / 2);
            }

            if (alignment == PinAlignment.TopRight || alignment == PinAlignment.BottomRight || alignment == PinAlignment.CenterRight)
            {
                rect.position += new Vector2(workRect.width - rect.width, 0);
            }
            else if (alignment == PinAlignment.TopCenter || alignment == PinAlignment.BottomCenter)
            {
                rect.position += new Vector2(workRect.width / 2 - rect.width / 2, 0);
            }

            rect.position += workRect.position;

            return rect;
        }

#endif
        public static void RenderGif(Rect pos, Texture2D map, int frameNo, int frameWidth, int frameHeight, int perLine,
            int paddingX = 0, int paddingY = 0)
        {
            var x = frameNo % perLine;
            var y = Mathf.FloorToInt((float)frameNo / perLine);

            var xCoord = x * (frameWidth + paddingX);
            var yCoord = (y + 1) * (frameHeight + paddingY);

            var texCoords = new Rect(
                xCoord / (float)map.width,
                (map.height - yCoord) / (float)map.height,
                (frameWidth) / (float)map.width,
                (frameHeight) / (float)map.height);

            GUI.DrawTextureWithTexCoords(pos, map, texCoords);

            //Debug.Log(texCoords);
            //Debug.Log("x: " + x + ", y: " + y);
        }

        public static void DrawFooterLayout(float width)
        {
            EditorGUILayout.BeginHorizontal();

            var margin = (EditorStyles.miniButton.padding.left) / 2f;
            width = width - margin * 2;

            if (GUILayout.Button("Web Site", GUILayout.Width(width / 2f - margin)))
            {
                Application.OpenURL(SRDebugEditorStrings.Current.SettingsWebSiteUrl);
            }

            if (GUILayout.Button("Asset Store Page", GUILayout.Width(width / 2f - margin)))
            {
                Application.OpenURL(SRDebugEditorStrings.Current.SettingsAssetStoreUrl);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Documentation", GUILayout.Width(width / 2f - margin)))
            {
                Application.OpenURL(SRDebugEditorStrings.Current.SettingsDocumentationUrl);
            }

            if (GUILayout.Button("Support", GUILayout.Width(width / 2f - margin)))
            {
                Application.OpenURL(
                    SRDebugEditorStrings.Current.SettingsSupportUrl);
            }

            EditorGUILayout.EndHorizontal();
        }

        public static class Styles
        {
            private static GUIStyle _inspectorHeaderStyle;
            private static GUIStyle _inspectorHeaderFoldoutStyle;
            private static GUIStyle _settingsHeaderBoxStyle;
            private static GUIStyle _headerLabel;
            private static GUIStyle _paragraphLabel;
            private static GUIStyle _paragraphLabelItalic;
            private static GUIStyle _radioButtonDescription;
            private static GUIStyle _radioButton;
            private static GUIStyle _leftToggleButton;
            private static GUIStyle _noPaddingNoMargin;
            private static GUIStyle _richTextLabel;
            private static GUIStyle _listBulletPoint;

            public static string LinkColour
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        return "#7C8CB9";
                    }

                    return "#0032E6";
                }
            }

            public static GUIStyle InspectorHeaderStyle
            {
                get
                {
                    if (_inspectorHeaderStyle == null)
                    {
                        _inspectorHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                        _inspectorHeaderStyle.fontSize = 12;
                    }

                    return _inspectorHeaderStyle;
                }
            }

            public static GUIStyle InspectorHeaderFoldoutStyle
            {
                get
                {
                    if (_inspectorHeaderFoldoutStyle == null)
                    {
                        _inspectorHeaderFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                        _inspectorHeaderFoldoutStyle.fontSize = 12;
                        _inspectorHeaderFoldoutStyle.fontStyle = FontStyle.Bold;
                    }

                    return _inspectorHeaderFoldoutStyle;
                }
            }

            public static GUIStyle SettingsHeaderBoxStyle
            {
                get
                {
                    if (_settingsHeaderBoxStyle == null)
                    {
                        _settingsHeaderBoxStyle = new GUIStyle("OL Title");
                        _settingsHeaderBoxStyle.padding = new RectOffset(0, 0, 0, 0);
                        _settingsHeaderBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                        _settingsHeaderBoxStyle.clipping = TextClipping.Clip;
                        _settingsHeaderBoxStyle.overflow = new RectOffset(0, 0, 0, 0);
                        //_settingsHeaderBoxStyle.border = new RectOffset(1, 1, 1, 1);
                        _settingsHeaderBoxStyle.fixedHeight = 0.5f;
                    }

                    return _settingsHeaderBoxStyle;
                }
            }

            public static GUIStyle HeaderLabel
            {
                get
                {
                    if (_headerLabel == null)
                    {
                        _headerLabel = new GUIStyle(EditorStyles.largeLabel);
                        _headerLabel.fontSize = 18;
                        _headerLabel.fontStyle = FontStyle.Normal;
                        _headerLabel.margin = new RectOffset(5, 5, 5, 5);
                    }

                    return _headerLabel;
                }
            }

            public static GUIStyle ParagraphLabel
            {
                get
                {
                    if (_paragraphLabel == null)
                    {
                        _paragraphLabel = new GUIStyle(EditorStyles.label);
                        _paragraphLabel.margin = new RectOffset(5, 5, 5, 5);
                        _paragraphLabel.wordWrap = true;
                        _paragraphLabel.richText = true;
                    }

                    return _paragraphLabel;
                }
            }
            
            public static GUIStyle ParagraphLabelItalic
            {
                get
                {
                    if (_paragraphLabelItalic == null)
                    {
                        _paragraphLabelItalic = new GUIStyle(EditorStyles.label);
                        _paragraphLabelItalic.margin = new RectOffset(5, 5, 5, 5);
                        _paragraphLabelItalic.wordWrap = true;
                        _paragraphLabelItalic.richText = true;
                        _paragraphLabelItalic.fontStyle = FontStyle.Italic;
                    }

                    return _paragraphLabelItalic;
                }
            }

            public static GUIStyle LeftToggleButton
            {
                get
                {
                    if (_leftToggleButton == null)
                    {
                        _leftToggleButton = new GUIStyle(EditorStyles.label);
                        _leftToggleButton.contentOffset = new Vector2(_leftToggleButton.contentOffset.x + 5,
                            _leftToggleButton.contentOffset.y);
                    }

                    return _leftToggleButton;
                }
            }

            public static GUIStyle RadioButton
            {
                get
                {
                    if (_radioButton == null)
                    {
                        _radioButton = new GUIStyle(EditorStyles.radioButton);
                        _radioButton.contentOffset = new Vector2(_radioButton.contentOffset.x + 5,
                            _radioButton.contentOffset.y);
                    }

                    return _radioButton;
                }
            }

            public static GUIStyle RadioButtonDescription
            {
                get
                {
                    if (_radioButtonDescription == null)
                    {
                        _radioButtonDescription = new GUIStyle(ParagraphLabel);
                        _radioButtonDescription.padding.left = (int)RadioButton.contentOffset.x +
                                                               RadioButton.padding.left;
                    }

                    return _radioButtonDescription;
                }
            }

            public static GUIStyle NoPaddingNoMargin
            {
                get
                {
                    if (_noPaddingNoMargin == null)
                    {
                        _noPaddingNoMargin = new GUIStyle();
                        _noPaddingNoMargin.margin = new RectOffset(0, 0, 0, 0);
                        _noPaddingNoMargin.padding = new RectOffset(0, 0, 0, 0);
                    }

                    return _noPaddingNoMargin;
                }
            }

            public static GUIStyle RichTextLabel
            {
                get
                {
                    if (_richTextLabel == null)
                    {
                        _richTextLabel = new GUIStyle(EditorStyles.label);
                        _richTextLabel.richText = true;
                        _richTextLabel.margin = new RectOffset(2, 2, 0, 0);
                    }

                    return _richTextLabel;
                }
            }  
            
            public static GUIStyle ListBulletPoint
            {
                get
                {
                    if (_listBulletPoint == null)
                    {
                        _listBulletPoint = new GUIStyle(EditorStyles.miniBoldLabel);
                        _listBulletPoint.wordWrap = true;
                        _listBulletPoint.margin = new RectOffset(6, 2, 0, 0);
                    }

                    return _listBulletPoint;
                }
            }
        }
    }
}