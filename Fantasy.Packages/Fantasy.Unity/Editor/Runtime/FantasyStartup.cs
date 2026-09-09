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
            var settings = FantasySettingsScriptableObject.Instance;
            if (settings == null || !settings.autoCopyAssembly)
            {
                return;
            }
            
            var hotUpdatePath = settings.hotUpdatePath;
            
            if (string.IsNullOrEmpty(hotUpdatePath))
            {
                Debug.LogError("请先在菜单Fantasy-Fantasy Settings里设置自动拷贝程序集输出目录位置");
                return; 
            }
            
            if (!Directory.Exists(hotUpdatePath))
            {
                Directory.CreateDirectory(hotUpdatePath); 
            }
            else
            {
                foreach (var file in Directory.GetFiles(hotUpdatePath))
                {
                    File.Delete(file);
                }
            }
            
            // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
            if (hotUpdatePath.LastIndexOf("/") != hotUpdatePath.Length - 1)
            {
                settings.hotUpdatePath += "/";
                hotUpdatePath = settings.hotUpdatePath;
            }

            foreach (var instanceHotUpdateAssemblyDefinition in settings.hotUpdateAssemblyDefinitions)
            {
                var dll = instanceHotUpdateAssemblyDefinition.name;
                File.Copy($"{ScriptAssemblies}{dll}.dll", $"{hotUpdatePath}/{dll}.dll.bytes", true);
                File.Copy($"{ScriptAssemblies}{dll}.pdb", $"{hotUpdatePath}/{dll}.pdb.bytes", true);
            }
            
            AssetDatabase.Refresh();
        }
    }
}