using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using AssetBundleBrowser.AssetBundleDataSource;
using Fantasy;
using Debug = UnityEngine.Debug;

namespace AssetBundleBrowser
{
    [System.Serializable]
    internal class AssetBundleBuildTab
    {
        const string k_BuildPrefPrefix = "ABBBuild:";

        private string m_streamingPath = "Assets/StreamingAssets";

        [SerializeField]
        private bool m_AdvancedSettings;

        [SerializeField]
        private Vector2 m_ScrollPosition;


        class ToggleData
        {
            internal ToggleData(bool s, 
                string title, 
                string tooltip,
                List<string> onToggles,
                BuildAssetBundleOptions opt = BuildAssetBundleOptions.None)
            {
                if (onToggles.Contains(title))
                    state = true;
                else
                    state = s;
                content = new GUIContent(title, tooltip);
                option = opt;
            }
            //internal string prefsKey
            //{ get { return k_BuildPrefPrefix + content.text; } }
            internal bool state;
            internal GUIContent content;
            internal BuildAssetBundleOptions option;
        }

        private AssetBundleInspectTab m_InspectTab;

        [SerializeField]
        private BuildTabData m_UserData;

        List<ToggleData> m_ToggleData;
        ToggleData m_ForceRebuild;
        ToggleData m_CopyToStreaming;
        ToggleData m_IncludeManifest;
        GUIContent m_TargetContent;
        GUIContent m_CompressionContent;
        internal enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression,
            ChunkBasedCompression,
        }
        GUIContent[] m_CompressionOptions =
        {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
        };
        int[] m_CompressionValues = { 0, 1, 2 };


        internal AssetBundleBuildTab()
        {
            m_AdvancedSettings = false;
            m_UserData = new BuildTabData();
            m_UserData.m_OnToggles = new List<string>();
            m_UserData.m_UseDefaultPath = true;
        }

        internal void OnDisable()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_UserData);
            file.Close();

        }
        internal void OnEnable(EditorWindow parent)
        {
            m_InspectTab = (parent as AssetBundleBrowserMain).m_InspectTab;

            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                var data = bf.Deserialize(file) as BuildTabData;
                if (data != null)
                    m_UserData = data;
                file.Close();
            }
            
            m_ToggleData = new List<ToggleData>();
            m_ToggleData.Add(new ToggleData(
                false,
                "Exclude Type Information",
                "Do not include type information within the asset bundle (don't write type tree).",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.DisableWriteTypeTree));
            m_ToggleData.Add(new ToggleData(
                false,
                "Force Rebuild",
                "Force rebuild the asset bundles",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.ForceRebuildAssetBundle));
            m_ToggleData.Add(new ToggleData(
                false,
                "Ignore Type Tree Changes",
                "Ignore the type tree changes when doing the incremental build check.",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.IgnoreTypeTreeChanges));
            m_ToggleData.Add(new ToggleData(
                false,
                "Append Hash",
                "Append the hash to the assetBundle name.",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.AppendHashToAssetBundleName));
            m_ToggleData.Add(new ToggleData(
                false,
                "Strict Mode",
                "Do not allow the build to succeed if any errors are reporting during it.",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.StrictMode));
            m_ToggleData.Add(new ToggleData(
                false,
                "Dry Run Build",
                "Do a dry run build.",
                m_UserData.m_OnToggles,
                BuildAssetBundleOptions.DryRunBuild));


            m_ForceRebuild = new ToggleData(
                false,
                "Clear Folders",
                "Will wipe out all contents of build directory as well as StreamingAssets/AssetBundles if you are choosing to copy build there.",
                m_UserData.m_OnToggles);
            m_CopyToStreaming = new ToggleData(
                false,
                "Copy to StreamingAssets",
                "After build completes, will copy all build content to " + m_streamingPath + " for use in stand-alone player.",
                m_UserData.m_OnToggles);
            m_IncludeManifest = new ToggleData(
                false,
                "Include Manifest",
                "Do not export Manifests for other AssetBundles.",
                m_UserData.m_OnToggles);
            m_TargetContent = new GUIContent("Build Target", "Choose target platform to build for.");
            m_CompressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");

            if(m_UserData.m_UseDefaultPath)
            {
                ResetPathToDefault();
            }
        }

        internal void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            bool newState = false;
            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUILayout.Label(new GUIContent("Example build setup"), centeredStyle);
            //basic options
            EditorGUILayout.Space();
            GUILayout.BeginVertical();
            // GUILayout.BeginHorizontal();
            // if (string.IsNullOrEmpty(m_UserData.m_ManifestName))
            // {
            //     m_UserData.m_ManifestName = "Fantasy";
            // }
            // var manifestName = EditorGUILayout.TextField("Manifest Name", m_UserData.m_ManifestName);
            // if (!System.String.IsNullOrEmpty(manifestName) && manifestName != m_UserData.m_ManifestName)
            // {
            //     m_UserData.m_ManifestName = manifestName;
            // }
            // GUILayout.EndHorizontal();
            // EditorGUILayout.Space();
            // build target
            using (new EditorGUI.DisabledScope (!AssetBundleModel.Model.DataSource.CanSpecifyBuildTarget)) 
            {
                ValidBuildTarget tgt = (ValidBuildTarget)EditorGUILayout.EnumPopup(m_TargetContent, m_UserData.m_BuildTarget);
                
                if (tgt != m_UserData.m_BuildTarget)
                {
                    m_UserData.m_BuildTarget = tgt;
                    m_UserData.m_OutputPath = "Assets/AssetBundles/";
                    m_UserData.m_OutputPath += m_UserData.m_BuildTarget.ToString();
                    // if(m_UserData.m_UseDefaultPath)
                    // {
                    //     Debug.Log($"m_UserData.m_OutputPath:{m_UserData.m_OutputPath}");
                    //     //EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
                    // }
                }
            }
            ////output path
            using (new EditorGUI.DisabledScope (!AssetBundleModel.Model.DataSource.CanSpecifyBuildOutputDirectory)) {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                var newPath = EditorGUILayout.TextField("Output Path", m_UserData.m_OutputPath);
                if (!System.String.IsNullOrEmpty(newPath) && newPath != m_UserData.m_OutputPath)
                {
                    m_UserData.m_UseDefaultPath = false;
                    m_UserData.m_OutputPath = newPath;
                    //EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse", GUILayout.MaxWidth(75f)))
                    BrowseForFolder();
                if (GUILayout.Button("Reset", GUILayout.MaxWidth(75f)))
                    ResetPathToDefault();
                //if (string.IsNullOrEmpty(m_OutputPath))
                //    m_OutputPath = EditorUserBuildSettings.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath");
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                newState = GUILayout.Toggle(
                    m_ForceRebuild.state,
                    m_ForceRebuild.content);
                if (newState != m_ForceRebuild.state)
                {
                    if (newState)
                        m_UserData.m_OnToggles.Add(m_ForceRebuild.content.text);
                    else
                        m_UserData.m_OnToggles.Remove(m_ForceRebuild.content.text);
                    m_ForceRebuild.state = newState;
                }
                
                newState = GUILayout.Toggle(
                    m_CopyToStreaming.state,
                    m_CopyToStreaming.content);
                if (newState != m_CopyToStreaming.state)
                {
                    if (newState)
                        m_UserData.m_OnToggles.Add(m_CopyToStreaming.content.text);
                    else
                        m_UserData.m_OnToggles.Remove(m_CopyToStreaming.content.text);
                    m_CopyToStreaming.state = newState;
                }
                
                newState = GUILayout.Toggle(
                    m_IncludeManifest.state,
                    m_IncludeManifest.content);
                if (newState != m_IncludeManifest.state)
                {
                    if (newState)
                        m_UserData.m_OnToggles.Add(m_IncludeManifest.content.text);
                    else
                        m_UserData.m_OnToggles.Remove(m_IncludeManifest.content.text);
                    m_IncludeManifest.state = newState;
                }
            }

            // advanced options
            using (new EditorGUI.DisabledScope (!AssetBundleModel.Model.DataSource.CanSpecifyBuildOptions)) {
                EditorGUILayout.Space();
                m_AdvancedSettings = EditorGUILayout.Foldout(m_AdvancedSettings, "Advanced Settings");
                if(m_AdvancedSettings)
                {
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    CompressOptions cmp = (CompressOptions)EditorGUILayout.IntPopup(
                        m_CompressionContent, 
                        (int)m_UserData.m_Compression,
                        m_CompressionOptions,
                        m_CompressionValues);

                    if (cmp != m_UserData.m_Compression)
                    {
                        m_UserData.m_Compression = cmp;
                    }
                    foreach (var tog in m_ToggleData)
                    {
                        newState = EditorGUILayout.ToggleLeft(
                            tog.content,
                            tog.state);
                        if (newState != tog.state)
                        {

                            if (newState)
                                m_UserData.m_OnToggles.Add(tog.content.text);
                            else
                                m_UserData.m_OnToggles.Remove(tog.content.text);
                            tog.state = newState;
                        }
                    }
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel = indent;
                }
            }

            // build.
            EditorGUILayout.Space();
            if (GUILayout.Button("Build") )
            {
                EditorApplication.delayCall += ExecuteBuild;
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ExecuteBuild()
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (AssetBundleModel.Model.DataSource.CanSpecifyBuildOutputDirectory) 
            {
                if (string.IsNullOrEmpty(m_UserData.m_OutputPath))
                {
                    BrowseForFolder();
                }

                if (string.IsNullOrEmpty(m_UserData.m_OutputPath)) //in case they hit "cancel" on the open browser
                {
                    Debug.LogError("AssetBundle Build: No valid output path for build.");
                    return;
                }

                if (m_ForceRebuild.state)
                {
                    string message = "Do you want to delete all files in the directory " + m_UserData.m_OutputPath;
                    
                    if (m_CopyToStreaming.state)
                    {
                        message += " and " + m_streamingPath;
                    }
                        
                    message += "?";
                    
                    if (EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No"))
                    {
                        try
                        {
                            if (Directory.Exists(m_UserData.m_OutputPath))
                            {
                                Directory.Delete(m_UserData.m_OutputPath, true);
                            }
                            
                            if (m_CopyToStreaming.state && Directory.Exists(m_streamingPath))
                            {
                                Directory.Delete(m_streamingPath, true);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                if (!Directory.Exists(m_UserData.m_OutputPath))
                {
                    Directory.CreateDirectory(m_UserData.m_OutputPath);
                }
            }

            BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;

            if (AssetBundleModel.Model.DataSource.CanSpecifyBuildOptions) 
            {
                if (m_UserData.m_Compression == CompressOptions.Uncompressed)
                {
                    opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
                }
                else if (m_UserData.m_Compression == CompressOptions.ChunkBasedCompression)
                {
                    opt |= BuildAssetBundleOptions.ChunkBasedCompression;
                }
                    
                foreach (var tog in m_ToggleData)
                {
                    if (tog.state)
                    {
                        opt |= tog.option;
                    }
                }
            }

            ABBuildInfo buildInfo = new ABBuildInfo
            {
                outputDirectory = m_UserData.m_OutputPath,
                options = opt,
                buildTarget = (BuildTarget)m_UserData.m_BuildTarget
            };

            buildInfo.onBuild = (assetBundleName) =>
            {
                if (m_InspectTab == null)
                {
                    return;
                }
                    
                m_InspectTab.AddBundleFolder(buildInfo.outputDirectory);
                m_InspectTab.RefreshBundles();
            };

            AssetBundleModel.Model.DataSource.BuildAssetBundles(buildInfo);

            // 改名AssetBundleManifest
            var oldManifestPath = $"{m_UserData.m_OutputPath}/{Path.GetFileName(m_UserData.m_OutputPath)}.manifest";
            var newManifestPath = $"{m_UserData.m_OutputPath}/{Define.AssetBundleManifestName}.manifest";
            File.Move(oldManifestPath,newManifestPath);
            // 计算所有文件的MD5
            var versionPath = $"{m_UserData.m_OutputPath}/{Define.VersionName}";
            var versionMD5Path = $"{m_UserData.m_OutputPath}/{Define.VersionMD5Name}";
            var currentManifest = $"{m_UserData.m_OutputPath}/{m_UserData.m_BuildTarget.ToString()}";
            // 给assetBundleManifest改名字
            if (File.Exists(currentManifest))
            {
                File.Move(currentManifest, $"{m_UserData.m_OutputPath}/{Define.AssetBundleManifestName}");
            }
            var versionBytes = AssetBundleHelper.CalculateMD5(m_UserData.m_OutputPath, m_IncludeManifest.state);
            File.WriteAllBytes(versionPath, versionBytes);
            File.WriteAllBytes(versionMD5Path, Encoding.UTF8.GetBytes(MD5Helper.FileMD5(versionPath)));
            // 拷贝AssetBundle到Streaming
            if (m_CopyToStreaming.state)
            {
                DirectoryCopy(m_UserData.m_OutputPath, m_streamingPath);
            }
            
            stopwatch.Stop();
            var timeSpan = stopwatch.Elapsed;
            var formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            Debug.Log($"AssetBundle打包完成 总时间:{formattedTime}");
            AssetDatabase.Refresh();
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            foreach (string folderPath in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories))
            {
                if (!Directory.Exists(folderPath.Replace(sourceDirName, destDirName)))
                    Directory.CreateDirectory(folderPath.Replace(sourceDirName, destDirName));
            }

            foreach (string filePath in Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories))
            {
                var fileDirName = Path.GetDirectoryName(filePath).Replace("\\", "/");
                var fileName = Path.GetFileName(filePath);
                string newFilePath = Path.Combine(fileDirName.Replace(sourceDirName, destDirName), fileName);

                File.Copy(filePath, newFilePath, true);
            }
        }

        private void BrowseForFolder()
        {
            m_UserData.m_UseDefaultPath = false;
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_UserData.m_OutputPath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
                    newPath = newPath.Remove(0, gamePath.Length+1);
                m_UserData.m_OutputPath = newPath;
                //EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
            }
        }
        private void ResetPathToDefault()
        {
            m_UserData.m_UseDefaultPath = true;
            m_UserData.m_OutputPath = "AssetBundles/";
            m_UserData.m_OutputPath += m_UserData.m_BuildTarget.ToString();
            // m_UserData.m_ManifestName = "Fantasy";
            //EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
        }

        //Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
        internal enum ValidBuildTarget
        {
            /// <summary>
            /// <para>Build a macOS standalone (Intel 64-bit).</para>
            /// </summary>
            StandaloneOSX = 2,
            /// <summary>
            /// <para>Build a Windows standalone.</para>
            /// </summary>
            StandaloneWindows = 5,
            /// <summary>
            /// <para>Build an iOS player.</para>
            /// </summary>
            iOS = 9,
            /// <summary>
            /// <para>Build an Android .apk standalone app.</para>
            /// </summary>
            Android = 13, // 0x0000000D
            /// <summary>
            /// <para>Build a Windows 64-bit standalone.</para>
            /// </summary>
            StandaloneWindows64 = 19, // 0x00000013
            /// <summary>
            /// <para>WebGL.</para>
            /// </summary>
            WebGL = 20, // 0x00000014
            /// <summary>
            /// <para>Build an Windows Store Apps player.</para>
            /// </summary>
            WSAPlayer = 21, // 0x00000015
            /// <summary>
            /// <para>Build a Linux 64-bit standalone.</para>
            /// </summary>
            StandaloneLinux64 = 24, // 0x00000018
            /// <summary>
            /// <para>Build a PS4 Standalone.</para>
            /// </summary>
            PS4 = 31, // 0x0000001F
            /// <summary>
            /// <para>Build a Xbox One Standalone.</para>
            /// </summary>
            XboxOne = 33, // 0x00000021
            /// <summary>
            /// <para>Build to Apple's tvOS platform.</para>
            /// </summary>
            tvOS = 37, // 0x00000025
            /// <summary>
            /// <para>Build a Nintendo Switch player.</para>
            /// </summary>
            Switch = 38, // 0x00000026
            Lumin = 39, // 0x00000027
            /// <summary>
            /// <para>Build a Stadia standalone.</para>
            /// </summary>
            Stadia = 40, // 0x00000028
            /// <summary>
            /// <para>Build a LinuxHeadlessSimulation standalone.</para>
            /// </summary>
            LinuxHeadlessSimulation = 41, // 0x00000029
            GameCoreScarlett = 42, // 0x0000002A
            GameCoreXboxSeries = 42, // 0x0000002A
            GameCoreXboxOne = 43, // 0x0000002B
            /// <summary>
            /// <para>Build to PlayStation 5 platform.</para>
            /// </summary>
            PS5 = 44, // 0x0000002C
            EmbeddedLinux = 45, // 0x0000002D
        }

        [System.Serializable]
        internal class BuildTabData
        {
            internal List<string> m_OnToggles;
            internal ValidBuildTarget m_BuildTarget = ValidBuildTarget.StandaloneWindows;
            internal CompressOptions m_Compression = CompressOptions.StandardCompression;
            internal string m_OutputPath = string.Empty;
            // internal string m_ManifestName = string.Empty;
            internal bool m_UseDefaultPath = true;
        }
    }

}