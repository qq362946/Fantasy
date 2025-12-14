using System.Collections.Generic;
using System.IO;
using System.Linq;
using SRDebugger.Internal.Editor;
using SRF;
using UnityEngine;
using UnityEditor;
#pragma warning disable 162

namespace SRDebugger.Editor
{
    [InitializeOnLoad]
    static class Migrations
    {
        static Migrations()
        {
            RunMigrations();
        }

        private const bool EnableLog = false;

        public class Migration
        {
            public readonly string Id;
            public readonly string[] ObsoleteFiles;

            public Migration(string id, string[] obsoleteFiles)
            {
                Id = id;
                ObsoleteFiles = obsoleteFiles;
            }
        }

        public static List<Migration> AvailableMigrations = new List<Migration>()
        {
            new Migration("DeleteOldEditorResources", new[]
            {
                "Editor/Resources/SRDebugger/BG_Dark.png",
                "Editor/Resources/SRDebugger/BG_Light.png",
                "Editor/Resources/SRDebugger/DemoSprite.png",
                "Editor/Resources/SRDebugger/Logo_DarkBG.png",
                "Editor/Resources/SRDebugger/Logo_LightBG.png",
                "Editor/Resources/SRDebugger/WelcomeLogo_DarkBG.png",
                "Editor/Resources/SRDebugger/WelcomeLogo_LightBG.png",
                "Editor/Resources/SRDebugger/Icons/Dark/console-25.png",
                "Editor/Resources/SRDebugger/Icons/Dark/options-25.png",
                "Editor/Resources/SRDebugger/Icons/Dark/profiler-25.png",
                "Editor/Resources/SRDebugger/Icons/Light/console-25.png",
                "Editor/Resources/SRDebugger/Icons/Light/options-25.png",
                "Editor/Resources/SRDebugger/Icons/Light/profiler-25.png",
            })
        };

        public static void RunMigrations(bool forceRun = false)
        {
            if(EnableLog)
                Debug.Log("[SRDebugger] Running Migrations...");

            foreach (var m in AvailableMigrations)
            {
                var key = GetProjectPrefsKey(m.Id);

                if (!forceRun && EditorPrefs.GetBool(key, false))
                {
                    continue;
                }

                EditorPrefs.SetBool(key, true);
                RunMigration(m);
            }
        }

        public static void RunMigration(Migration migration)
        {
            if (EnableLog)
                Debug.Log("Running Migration: " + migration.Id);

            var assetPaths = AssetDatabase.GetAllAssetPaths();
            var root = new DirectoryInfo(SRDebugEditorUtil.GetRootPath());

            if(EnableLog)
                Debug.Log("Using Root Path: " + root.FullName);

            var obsoleteAssets = migration.ObsoleteFiles.Select(p => root + "/" + p).ToList();
            var deleteQueue = assetPaths.Where(assetPath => obsoleteAssets.Contains(assetPath)).ToList();

            if (deleteQueue.Count == 0)
                return;

            var message = "The following files used by a previous version of SRDebugger are obsolete and can be safely deleted: \n\n" +
                          deleteQueue.Aggregate((s1, s2) => s1 + "\n" + s2);

            Debug.Log(message);

            message += "\n\nIt is recommended to delete these files.";

            if (EditorUtility.DisplayDialog("SRDebugger Migration Assistant",
                message, "Delete Now", "Ignore"))
            {
                foreach (var s in deleteQueue)
                {
                    Debug.Log("[SRDebugger] Deleting Asset {0}".Fmt(s));

                    if (!AssetDatabase.DeleteAsset(s))
                    {
                        Debug.LogWarning("[SRDebugger] Error deleting asset {0}".Fmt(s));
                    }
                }

                Debug.Log("[SRDebugger] Migration Complete");
            }
            else
            {
                EditorUtility.DisplayDialog("SRDebugger Migration Assitant",
                    "You can run this migration check again via the \"Run Migrations\" button in the advanced tab of the SRDebugger settings window.", "OK");
            }
        }

        private static string GetProjectPrefsKey(string key)
        {
            return "SRDebugger_Migration_" + Application.dataPath + "_" + key;
        }
    }
}