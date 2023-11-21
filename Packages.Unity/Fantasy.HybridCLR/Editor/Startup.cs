using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    [InitializeOnLoad]
    public static class Startup
    {
        private const string CopyCodeDir = "Assets/Bundles/HotUpdate/";
        
        static Startup()
        {
            CompileDllCommand.CompileDllActiveBuildTarget();
            CopyHotUpdateDll();
        }

        [MenuItem("Fantasy/CopyAOTAssembly")]
        private static void CopyAOTAssembly()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            
            foreach (var dll in SettingsUtil.AOTAssemblyNames)
            {
                var srcDllPath = $"{aotAssembliesSrcDir}/{dll}.dll";
                
                if (!File.Exists(srcDllPath))
                {
                    Debug.LogError($"ab中添加AOT补充元数据dll:{srcDllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    continue; 
                }

                File.Copy(srcDllPath, $"Assets/Bundles/AOTAssembly/{dll}.dll.bytes", true);
            }
            
            AssetDatabase.Refresh();
        }

        [MenuItem("Fantasy/CopyHotUpdateDll")]
        private static void CopyHotUpdateDll()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);

            foreach (var dll in SettingsUtil.HotUpdateAssemblyNamesExcludePreserved)
            {
                var dllPath = $"{hotfixDllSrcDir}/{dll}";
                File.Copy($"{dllPath}.dll", $"{CopyCodeDir}/{dll}.dll.bytes", true);
                File.Copy($"{dllPath}.pdb", $"{CopyCodeDir}/{dll}.pdb.bytes", true);
                // UnityEngine.Debug.Log($"copy hotfix dll {dllPath} -> {CopyCodeDir}/{dll}");
            }

            UnityEngine.Debug.Log($"热更代码已经替换完成");
            AssetDatabase.Refresh();
        }
    }
}