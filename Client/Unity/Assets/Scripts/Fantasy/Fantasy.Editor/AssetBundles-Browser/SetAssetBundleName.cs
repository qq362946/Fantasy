using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class SetAssetBundleName
    {
        [MenuItem("Fantasy/AssetBundle/SetUIName")]
        static void SetAllName()
        {
            var dir = new DirectoryInfo("Assets/Bundles/UI");
            var directoryInfos = dir.GetDirectories();
            
            foreach (var directoryInfo in directoryInfos)
            {
                var assetPath = directoryInfo.FullName.Replace(Application.dataPath, "Assets");
                var assetImporter = AssetImporter.GetAtPath(assetPath);
                assetImporter.assetBundleName = directoryInfo.Name.ToLower();
            }
            
            Debug.Log($"UI文件夹所有AssetBundleName设置完成 共:{directoryInfos.Length}个");
        }
        
        [MenuItem("Assets/SetABName", false, priority = 0)]
        static void SetName(MenuCommand menuCommand)
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetImporter = AssetImporter.GetAtPath(assetPath);
                var replace = assetPath.Replace(".", "", StringComparison.Ordinal);
                assetImporter.assetBundleName = Path.GetFileName(replace).ToLower();
            }
        }
    }
}