#if !DISABLE_SRDEBUGGER
using SRF;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    [InitializeOnLoad]
    class WelcomeWindow : EditorWindow
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

        [MenuItem(SRDebugEditorPaths.WelcomeItemPath)]
        public static void Open()
        {
            GetWindowWithRect<WelcomeWindow>(new Rect(0, 0, 449, 500), true, "SRDebugger - Welcome", true);
        }

        private static bool ShouldOpen()
        {
            var settings = Settings.GetInstance();
            if (settings != null)
            {
                if (settings.DisableWelcomePopup)
                {
                    return false;
                }
            }

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
            SRInternalEditorUtil.BeginDrawBackground();
            SRInternalEditorUtil.DrawLogo(SRInternalEditorUtil.GetWelcomeLogo());
            SRInternalEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRInternalEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Welcome", SRInternalEditorUtil.Styles.HeaderLabel);

            GUILayout.Label(
                "Thank you for purchasing SRDebugger, your support is very much appreciated and we hope you find it useful for your project. " +
                "This window contains a quick guide to get to help get you started with SRDebugger.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            if (SRInternalEditorUtil.ClickableLabel(
                "Note: For more detailed information <color={0}>click here</color> to visit the online documentation."
                    .Fmt(SRInternalEditorUtil.Styles.LinkColour),
                SRInternalEditorUtil.Styles.ParagraphLabel))
            {
                Application.OpenURL(SRDebugEditorStrings.Current.SettingsDocumentationUrl);
            }

            GUILayout.Label("Quick Start", SRInternalEditorUtil.Styles.HeaderLabel);
            GUILayout.Label(
                "Now that you have imported the package, you should find the trigger available in the top-left of your game window when in play mode. " +
                "Triple-clicking this trigger will bring up the debug panel. The trigger is hidden until clicked.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "By default, SRDebugger loads automatically when your game starts. " +
                "You can change this behaviour from the SRDebugger Settings window.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            DrawVideo();

            EditorGUILayout.Space();

            GUILayout.Label("Customization", SRInternalEditorUtil.Styles.HeaderLabel);

            if (SRInternalEditorUtil.ClickableLabel(
                "Many features of SRDebugger can be configured from the <color={0}>SRDebugger Settings</color> window."
                    .Fmt(
                        SRInternalEditorUtil.Styles.LinkColour), SRInternalEditorUtil.Styles.ParagraphLabel))
            {
                SRDebuggerSettingsWindow.Open();
            }

            GUILayout.Label(
                "From the settings window you can configure loading behaviour, trigger position, docked tools layout, and more. " +
                "You can enable the bug reporter service by using the sign-up form to get a free API key.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label("What Next?", SRInternalEditorUtil.Styles.HeaderLabel);

            if (SRInternalEditorUtil.ClickableLabel(
                "For more detailed information about SRDebugger's features or details about the Options Tab and script API, check the <color={0}>online documentation</color>."
                    .Fmt(SRInternalEditorUtil.Styles.LinkColour), SRInternalEditorUtil.Styles.ParagraphLabel))
            {
                Application.OpenURL(SRDebugEditorStrings.Current.SettingsDocumentationUrl);
            }

            GUILayout.Label(
                "Thanks again for purchasing SRDebugger. " +
                "If you find it useful please consider leaving a rating or review on the Asset Store page as this helps us continue to provide updates and support to our users. ",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "If you have any questions or concerns please do not hesitate to get in touch with us via email or the Unity forums.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            SRInternalEditorUtil.DrawFooterLayout(position.width - 15);

            EditorGUILayout.EndScrollView();

            Repaint();
        }

        private void DrawVideo()
        {
            if (_demoSprite == null)
            {
                _demoSprite = SRInternalEditorUtil.LoadResource<Texture2D>("Editor/DemoSprite.png");
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

            SRInternalEditorUtil.RenderGif(rect, _demoSprite, actualFrame, frameWidth, frameHeight, 5, framePadding,
                framePadding);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif