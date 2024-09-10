using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    public static partial class SRDebugEditor
    {
        internal const string DisableSRDebuggerCompileDefine = "DISABLE_SRDEBUGGER";

        /// <summary>
        /// Is SRDebugger currently enabled or disabled.
        /// </summary>
        public static readonly bool IsEnabled =
#if DISABLE_SRDEBUGGER
                false
#else
                true
#endif
            ;

        /// <summary>
        /// Set SRDebugger to be enabled or disabled.
        /// This is a synchronous operation, which means calling this as part of a build pipeline should be possible. 
        /// </summary>
        /// <param name="enable"></param>
        public static void SetEnabled(bool enable)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            {
                Debug.LogError(
                    "[SRDebugger.SetEnabled] Can't change SRDebugger enabled state while in play mode or compiling scripts.");
                throw new InvalidOperationException(
                    "Can't change SRDebugger enabled state while in play mode or compiling scripts.");
            }

#if !DISABLE_SRDEBUGGER
            AssetDatabase.SaveAssets(); // In case any pending changes to files about to be moved

            // Try and unload the settings asset to prevent errors later (harmless error, but annoying)
            SRInternalEditorUtil.EditorSettings.ClearCache(); 
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate(true);
#endif
           
            AssetDatabase.ReleaseCachedFileHandles();

            SetCompileDefine(DisableSRDebuggerCompileDefine, !enable);
            SetResourcesEnabled(enable);

            ForceRecompile();
        }

        /// <summary>
        /// Runs through a series of integrity checks that are fast to perform.
        /// </summary>
       internal static IEnumerable<IntegrityIssue> QuickIntegrityCheck()
        {
            int enabledCount = 0;
            int disabledCount = 0;

            foreach (ResourceDirectory directory in GetResourcePaths())
            {
                if (directory.IsEnabled && directory.IsDisabled)
                {
                    yield return new SomeResourcesAreEnabledAndDisabledIntegrityIssue();
                    yield break;
                }

                if (directory.IsEnabled) enabledCount++;
                if (directory.IsDisabled) disabledCount++;
            }

            if (enabledCount > 0 && disabledCount > 0)
            {
#if DISABLE_SRDEBUGGER
                yield return new SomeResourcesEnabledIntegrityIssue();
#else
                yield return new SomeResourcesDisabledIntegrityIssue();
#endif
                yield break; // Don't do any further resource-related checks.
            }

            if (!IsEnabled && enabledCount > 0)
            {
                yield return new ScriptsDisabledButResourcesEnabled();
            }

            if (IsEnabled && disabledCount > 0)
            {
                yield return new ScriptsEnabledButResourcesDisabled();
            }
        }
        
        internal static void DrawDisabledWindowGui(ref bool isWorking)
        {
            SRInternalEditorUtil.BeginDrawBackground();
            SRInternalEditorUtil.DrawLogo(SRInternalEditorUtil.GetLogo());
            SRInternalEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRInternalEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

            GUILayout.Label("SRDebugger Disabled", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            GUILayout.Label(
                "SRDebugger is currently disabled. SRDebugger must be enabled in order to access editor features.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            EditorGUILayout.HelpBox("Enabling SRDebugger will result in the tools being included in all builds of your game until it is disabled again.", MessageType.Warning);

            GUILayout.Label("• "+ DisableSRDebuggerCompileDefine + " compiler define will be removed from all build configurations.", SRInternalEditorUtil.Styles.ListBulletPoint);
            GUILayout.Label("• Disabled SRDebugger folders will be renamed so Unity imports them.", SRInternalEditorUtil.Styles.ListBulletPoint);
            GUILayout.Label("• You can disable SRDebugger again at any time.", SRInternalEditorUtil.Styles.ListBulletPoint);

            if (isWorking && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                isWorking = false;
            }

            if (isWorking)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    GUILayout.Button("Working...");
                }
            } 
            else if (GUILayout.Button("Enable SRDebugger"))
            {
                isWorking = true;
                try
                {
                    SetEnabled(true);
                }
                catch (Exception)
                {
                    isWorking = false;
                    throw;
                }
            }
        }

#if DISABLE_SRDEBUGGER
        class SomeResourcesEnabledIntegrityIssue : IntegrityIssue
        {
            private new const string Title = "Some SRDebugger resources are enabled.";

            private new const string Description =
                "SRDebugger is disabled, but some SRDebugger resource directories are enabled. \n\n" +
                "This can occur if an unhandled error occurs while SRDebugger is being enabled or disabled, or if the resource directories are modified by hand.";

            public SomeResourcesEnabledIntegrityIssue() : base(Title, Description)
            {

            }

            protected override IEnumerable<Fix> CreateFixes()
            {
                yield return new DelegateFix(
                    "Disable all SRDebugger resources",
                    "All resource directories will be disabled.",
                    () => { SetResourcesEnabled(false); });
                yield return new DelegateFix(
                    "Enable SRDebugger",
                    "Fully enable SRDebugger (activate scripts and enable resources).",
                    () => { SetEnabled(true); });
            }
        }
#else
        class SomeResourcesDisabledIntegrityIssue : IntegrityIssue
        {
            private new const string Title = "Some SRDebugger resources are disabled.";

            private new const string Description =
                "SRDebugger is enabled, but some SRDebugger resource directories are disabled. \n\n" +
                "This can occur if an unhandled error occurs while SRDebugger is being enabled or disabled, or if the resource directories are modified by hand.";

            public SomeResourcesDisabledIntegrityIssue() : base(Title, Description)
            {

            }

            protected override IEnumerable<Fix> CreateFixes()
            {
                yield return new DelegateFix(
                    "Enable all SRDebugger resources",
                    "All resource directories will be enabled.",
                    () => { SetResourcesEnabled(true); });

                yield return new DelegateFix(
                    "Disable SRDebugger",
                    "Fully disable SRDebugger (deactivate scripts, exclude all resources from builds of your game).",
                    () => { SetEnabled(false); });
            }
        }
#endif

        class SomeResourcesAreEnabledAndDisabledIntegrityIssue : IntegrityIssue
        {
            private new const string Title = "Duplicate SRDebugger resource directories";

            private new const string Description =
                "Some SRDebugger resource directories exist in both an enabled and disabled state. \n\n" +
                "This can occur if a new version of SRDebugger is installed while SRDebugger is disabled, or if an unhandled error occurs while SRDebugger is being enabled/disabled.";

            public SomeResourcesAreEnabledAndDisabledIntegrityIssue() : base(Title, Description)
            {

            }

            protected override IEnumerable<Fix> CreateFixes()
            {
                if (!IsEnabled)
                {
                    var deletePaths = GetResourcePaths().Where(p => p.IsDisabled && p.IsEnabled).ToList();
                    string paths = "  - " + string.Join("\n  - ", deletePaths
                        .SelectMany(p => new string[] { p.DisabledPath, p.DisabledPathBackupMetaFile }).ToArray());

                    yield return new DelegateFix(
                        "Keep enabled resources, disable SRDebugger",
                        "If you have just installed a new version of SRDebugger, this will keep the most up-to-date resources from the imported package. SRDebugger will be disabled after the old resources are deleted. \n\n The following paths will be deleted: \n\n" + paths,
                        () =>
                        {
                            foreach (ResourceDirectory rd in GetResourcePaths())
                            {
                                if (rd.IsEnabled && rd.IsDisabled)
                                {
                                    Debug.Log("[SRDebugger] Delete Path: " + rd.DisabledPath);
                                    Directory.Delete(rd.DisabledPath, true);

                                    Debug.Log("[SRDebugger] Delete File: " + rd.DisabledPathBackupMetaFile);
                                    File.Delete(rd.DisabledPathBackupMetaFile);
                                }
                            }

                            SetEnabled(false);
                        });
                }
            }
        }

        class ScriptsDisabledButResourcesEnabled : IntegrityIssue
        {
            private new const string Title = "SRDebugger resources are enabled while scripts are disabled";

            private new const string Description =
                "SRDebugger's resources directories are enabled, but SRDebugger scripts are disabled. \n" +
                "This can occur if the resource directories or if the C# compile defines are modified manually.";

            public ScriptsDisabledButResourcesEnabled() : base(Title, Description)
            {
            }

            protected override IEnumerable<Fix> CreateFixes()
            {
                yield return new DelegateFix(
                    "Enable SRDebugger scripts",
                    "Remove compiler define (" + DisableSRDebuggerCompileDefine + ") SRDebugger can be disabled again from the settings menu.",
                    () =>
                    {
                        SetCompileDefine(DisableSRDebuggerCompileDefine, false);
                    });
                yield return new DelegateFix(
                    "Disable SRDebugger resources",
                    "Resources will no longer be included in builds of your game (you can enable SRDebugger from the settings menu later)",
                    () =>
                    {
                        SetResourcesEnabled(false);
                    });
            }
        }

        class ScriptsEnabledButResourcesDisabled : IntegrityIssue
        {
            private new const string Title = "SRDebugger scripts are enabled while resources are disabled.";

            private new const string Description =
                "SRDebugger resources directories are disabled, but SRDebugger scripts are still enabled. \n" +
                "This can occur if the resource directories or C# compile defines are modified manually.";

            public ScriptsEnabledButResourcesDisabled() : base(Title, Description)
            {
            }

            protected override IEnumerable<Fix> CreateFixes()
            {
                yield return new DelegateFix(
                    "Disable SRDebugger scripts",
                    "Add compiler define (" + DisableSRDebuggerCompileDefine + ") to disable SRDebugger scripts (you can re-enable SRDebugger from the settings menu later)",
                    () =>
                    {
                        SetCompileDefine(DisableSRDebuggerCompileDefine, true);
                    });
                yield return new DelegateFix(
                    "Enable SRDebugger resources",
                    "Resources will be included in builds of your game (you can disable SRDebugger from the settings menu later)",
                    () =>
                    {
                        SetResourcesEnabled(true);
                    });
            }
        }
    }
}