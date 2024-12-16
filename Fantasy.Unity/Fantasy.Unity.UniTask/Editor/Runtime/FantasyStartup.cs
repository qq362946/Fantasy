using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    [InitializeOnLoad]
    public static class FantasyStartup
    {
        private const string ScriptAssemblies = "Library/ScriptAssemblies/";

        static FantasyStartup()
        {
            if (!FantasySettingsScriptableObject.Instance.autoCopyAssembly)
            {
                return;
            }
            
            var hotUpdatePath = FantasySettingsScriptableObject.Instance.hotUpdatePath;
            
            if (string.IsNullOrEmpty(hotUpdatePath))
            {
                Debug.LogError("请先在菜单Fantasy-Fantasy Settings里设置HotUpdatePath目录位置");
                return; 
            }
            
            if (!Directory.Exists(hotUpdatePath))
            {
                Directory.CreateDirectory(hotUpdatePath); 
            }

            // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
            if (hotUpdatePath.LastIndexOf("/") != hotUpdatePath.Length - 1)
            {
                FantasySettingsScriptableObject.Instance.hotUpdatePath += "/";
                hotUpdatePath = FantasySettingsScriptableObject.Instance.hotUpdatePath;
            }

            foreach (var instanceHotUpdateAssemblyDefinition in FantasySettingsScriptableObject.Instance.hotUpdateAssemblyDefinitions)
            {
                var dll = instanceHotUpdateAssemblyDefinition.name;
                File.Copy($"{ScriptAssemblies}{dll}.dll", $"{hotUpdatePath}/{dll}.dll.bytes", true);
                File.Copy($"{ScriptAssemblies}{dll}.pdb", $"{hotUpdatePath}/{dll}.pdb.bytes", true);
            }
            
            AssetDatabase.Refresh();
        }
    }
}