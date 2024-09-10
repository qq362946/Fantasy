using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    public partial class SRDebugEditor
    {
        internal const string DisabledDirectoryPostfix = "_DISABLED~";

        // Paths to enable/disable (relative to SRDebugger root directory)
        private static readonly string[] _resourcePaths = new[]
        {
            "Resources",
            "usr",
            "UI/Prefabs"
        };

        static void SetResourcesEnabled(bool enable)
        {
            AssetDatabase.StartAssetEditing();

            foreach (ResourceDirectory d in GetResourcePaths())
            {
                d.SetDirectoryEnabled(enable);
            }

            AssetDatabase.StopAssetEditing();

            AssetDatabase.Refresh();

            AssetDatabase.ImportAsset(SRInternalEditorUtil.GetRootPath(
            ), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        internal static IEnumerable<ResourceDirectory> GetResourcePaths()
        {
            foreach (string resourcePath in _resourcePaths)
            {
                string enabledPath = Path.Combine(SRInternalEditorUtil.GetRootPath(), resourcePath);
                string disabledPath = Path.Combine(SRInternalEditorUtil.GetRootPath(), resourcePath) + DisabledDirectoryPostfix;

                yield return new ResourceDirectory(enabledPath, disabledPath);
            }
        }


        internal class ResourceDirectory
        {
            public readonly string EnabledPath;
            public readonly string DisabledPath;

            public readonly string EnabledPathMetaFile;
            public readonly string DisabledPathMetaFile;
            public readonly string DisabledPathBackupMetaFile;

            public bool IsEnabled
            {
                get { return Directory.Exists(EnabledPath); }
            }

            public bool IsDisabled
            {
                get { return Directory.Exists(DisabledPath); }
            }

            public ResourceDirectory(string enabledPath, string disabledPath)
            {
                EnabledPath = enabledPath;
                DisabledPath = disabledPath;

                EnabledPathMetaFile = enabledPath + ".meta";
                DisabledPathMetaFile = disabledPath + ".meta";
                DisabledPathBackupMetaFile = disabledPath + ".meta.bak~";
            }

            public void SetDirectoryEnabled(bool enable)
            {
                if (IsEnabled && enable)
                {
                    return;
                }

                if (IsDisabled && !enable)
                {
                    return;
                }

                if (IsEnabled && IsDisabled)
                {
                    // TODO
                    throw new Exception();
                }

                string title = string.Format("SRDebugger - {0} Resources", enable ? "Enable" : "Disable");

                string oldPath = enable ? DisabledPath : EnabledPath;
                string newPath = enable ? EnabledPath : DisabledPath;
                bool useAssetDatabase = !enable;

                string error = null;

                if (useAssetDatabase)
                {
                    error = AssetDatabase.MoveAsset(oldPath, newPath);

                    if (!string.IsNullOrEmpty(error))
                    {
                        if (EditorUtility.DisplayDialog(title, GetErrorMessage(enable, error), "Force Move", "Abort"))
                        {
                            useAssetDatabase = false;
                        }
                    }
                }

                if (!useAssetDatabase)
                {
                    try
                    {
                        Directory.Move(oldPath, newPath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error moving directory");
                        Debug.LogException(e);
                        error = "Exception occurred, see console for details.";
                    }
                }

                if (!string.IsNullOrEmpty(error))
                {
                    string message = string.Format(
                        "An error occurred while attempting to {3} SRDebugger resource directory.\n\n Old Path: {0}\n New Path: {1}\n\n Error: \n{2}",
                        EnabledPath, DisabledPath, error, enable ? "enable" : "disable");

                    EditorUtility.DisplayDialog(title, message, "Continue");
                    return;
                }

                if (!enable)
                {
                    // Disable meta files
                    if (File.Exists(DisabledPathMetaFile))
                    {
                        if (File.Exists(DisabledPathBackupMetaFile))
                        {
                            File.Delete(DisabledPathBackupMetaFile);
                        }

                        File.Move(DisabledPathMetaFile, DisabledPathBackupMetaFile);
                    }
                }
                else
                {
                    // Enable backed up meta files
                    if (File.Exists(DisabledPathBackupMetaFile))
                    {
                        if (File.Exists(EnabledPathMetaFile))
                        {
                            File.Delete(EnabledPathMetaFile);
                        }

                        File.Move(DisabledPathBackupMetaFile, EnabledPathMetaFile);
                    }
                }

            }

            private string GetErrorMessage(bool enable, string error)
            {
                return string.Format(
                    "An error occurred while attempting to {3} SRDebugger resources. \n\n Old Path: {0}\n New Path: {1}\n\n Error: \n{2}",
                    EnabledPath, DisabledPath, error, enable ? "enable" : "disable");
            }
        }
    }
}