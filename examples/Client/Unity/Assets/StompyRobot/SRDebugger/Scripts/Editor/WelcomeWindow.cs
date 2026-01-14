using System;
using SRDebugger.Internal;
using SRDebugger.Internal.Editor;
using SRF;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private const string WelcomeWindowPlayerPrefsKey = "SRDEBUGGER_WELCOME_SHOWN_VERSION";
        private Texture2D _demoSprite;
        private Vector2 _scrollPosition;

        static WelcomeWindow()
        {
            EditorApplication.update += OpenUpdate;
        }

        private static void OpenUpdate()
        {
            if (ShouldOpen())
            {
                Open();
            }

            EditorApplication.update -= OpenUpdate;
        }

        [MenuItem(SRDebugPaths.WelcomeItemPath)]
        public static void Open()
        {
            GetWindowWithRect<WelcomeWindow>(new Rect(0, 0, 449, 500), true, "SRDebugger - Welcome", true);
        }

        public static bool ShouldOpen()
        {
            var hasKey = EditorPrefs.HasKey(WelcomeWindowPlayerPrefsKey);

            if (!hasKey)
            {
                return true;
            }

            var value = EditorPrefs.GetString(WelcomeWindowPlayerPrefsKey);

            if (value != SRDebug.Version)
            {
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            EditorPrefs.SetString(WelcomeWindowPlayerPrefsKey, SRDebug.Version);
        }

        private void OnGUI()
        {
            // Draw header area 
            SRDebugEditorUtil.BeginDrawBackground();
            SRDebugEditorUtil.DrawLogo(SRDebugEditorUtil.GetWelcomeLogo());
            SRDebugEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRDebugEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Welcome", SRDebugEditorUtil.Styles.HeaderLabel);

            GUILayout.Label(
                "Thank you for purchasing SRDebugger, your support is very much appreciated and we hope you find it useful for your project. " +
                "This window contains a quick guide to get to help get you started with SRDebugger.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            if (SRDebugEditorUtil.ClickableLabel(
                "Note: For more detailed information <color={0}>click here</color> to visit the online documentation."
                    .Fmt(SRDebugEditorUtil.Styles.LinkColour),
                SRDebugEditorUtil.Styles.ParagraphLabel))
            {
                Application.OpenURL(SRDebugStrings.Current.SettingsDocumentationUrl);
            }

#if UNITY_5_3_0 || UNITY_5_3_1 || UNITY_5_3_2
            EditorGUILayout.HelpBox(
                "On Unity versions prior to 5.3.3 there is a bug causing errors to be printed to the console when using the docked tools. Please upgrade to at least Unity 5.3.3 to prevent this bug.",
                MessageType.Warning, true);
#endif
            GUILayout.Label("Quick Start", SRDebugEditorUtil.Styles.HeaderLabel);
#if UNITY_5 || UNITY_5_3_OR_NEWER

            GUILayout.Label(
                "Now that you have imported the package, you should find the trigger available in the top-left of your game window when in play mode. " +
                "Triple-clicking this trigger will bring up the debug panel. The trigger is hidden until clicked.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "By default, SRDebugger loads automatically when your game starts. " +
                "You can change this behaviour from the SRDebugger Settings window.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

#else

            GUILayout.Label(
                "Drag the <b>SRDebugger.Init</b> prefab into the first scene of your game. " +
                "Once initialised, SRDebugger will be available even after loading new scenes. We recommend adding the SRDebugger.Init prefab to the first scene " +
                "of your game so that the debug panel is available in all subsequent scenes.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "Once the prefab is in your scene, you should find the trigger available in the top-left of your game window when in play mode. " +
                "Triple-clicking this trigger will bring up the debug panel. The trigger is hidden until clicked.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

#endif

            DrawVideo();

            EditorGUILayout.Space();

            GUILayout.Label("Customization", SRDebugEditorUtil.Styles.HeaderLabel);

            if (SRDebugEditorUtil.ClickableLabel(
                "Many features of SRDebugger can be configured from the <color={0}>SRDebugger Settings</color> window."
                    .Fmt(
                        SRDebugEditorUtil.Styles.LinkColour), SRDebugEditorUtil.Styles.ParagraphLabel))
            {
                SRDebuggerSettingsWindow.Open();
            }

            GUILayout.Label(
                "From the settings window you can configure loading behaviour, trigger position, docked tools layout, and more. " +
                "You can enable the bug reporter service by using the sign-up form to get a free API key.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label("What Next?", SRDebugEditorUtil.Styles.HeaderLabel);

            if (SRDebugEditorUtil.ClickableLabel(
                "For more detailed information about SRDebugger's features or details about the Options Tab and script API, check the <color={0}>online documentation</color>."
                    .Fmt(SRDebugEditorUtil.Styles.LinkColour), SRDebugEditorUtil.Styles.ParagraphLabel))
            {
                Application.OpenURL(SRDebugStrings.Current.SettingsDocumentationUrl);
            }

            GUILayout.Label(
                "Thanks again for purchasing SRDebugger. " +
                "If you find it useful please consider leaving a rating or review on the Asset Store page to help us spread the word. ",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "If you have any questions or concerns please do not hesitate to get in touch with us via email or the Unity forums.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            SRDebugEditorUtil.DrawFooterLayout(position.width - 15);

            EditorGUILayout.EndScrollView();

            Repaint();
        }

        private void DrawVideo()
        {
            if (_demoSprite == null)
            {
                _demoSprite = SRDebugEditorUtil.LoadResource<Texture2D>("Editor/DemoSprite.png");
            }

            if (_demoSprite == null)
                return;

            var frameWidth = 400;
            var frameHeight = 300;
            var framePadding = 0;
            var extraFramesStart = 5;
            var extraFramesEnd = 20;
            var totalFrames = 29;
            var fps = 16f;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            var rect = GUILayoutUtility.GetRect(400*0.75f, 300*0.75f, GUILayout.ExpandHeight(false),
                GUILayout.ExpandWidth(false));

            var frame = ((int) (EditorApplication.timeSinceStartup*fps))%
                        (totalFrames + extraFramesStart + extraFramesEnd);
            frame -= extraFramesStart;

            var actualFrame = Mathf.Clamp(frame, 0, totalFrames);

            SRDebugEditorUtil.RenderGif(rect, _demoSprite, actualFrame, frameWidth, frameHeight, 5, framePadding,
                framePadding);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }
    }
}
