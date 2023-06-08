using System.Collections.Generic;
using System.Reflection;
using Fantasy.Helper;
using HybridCLR;
using UnityEngine;

namespace Fantasy
{
    public class AssemblyLoadHelper
    {
        private static readonly string HotfixAotAssemblyBundle = "AotAssembly".ToLower();
        private static readonly string HotfixModelDllBundle = "FantasyModelDll".ToLower();
        private static readonly string HotfixHotfixDllBundle = "FantasyHotfixDll".ToLower();

        public static async FTask Initialize()
        {
            if (!Define.IsEditor)
            {
                // 下载更新
                // ReSharper disable once ConvertToUsingDeclaration
                using (var updateAssetBundle = UpdateAssetBundle.Instance)
                {
                    if (!await updateAssetBundle.StartAsync())
                    {
                        Log.Error("UpdateAssetBundle fail!");
                        return;
                    }
                    Log.Debug("更新完成");
                }
                // 加载元数据
                LoadMetadataForAOTAssemblies();
            }

            LoadModelDll();
            LoadHotfixDll();
        }

        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        private static void LoadMetadataForAOTAssemblies()
        {
            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            
            var aotMetaAssemblyFiles = new List<string>()
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "Fantasy.Core.dll"
            };
            
            AssetBundleHelper.LoadBundle(HotfixAotAssemblyBundle);
            
            var homologousImageMode = HomologousImageMode.SuperSet;
             
            foreach (var aotDllName in aotMetaAssemblyFiles)
            { 
                var dllBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixAotAssemblyBundle, aotDllName).bytes;
                Log.Debug($"aotDllName:{aotDllName} dllBytes:{dllBytes.Length}");
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                var err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, homologousImageMode);
                // Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{homologousImageMode} ret:{err}");
            }
            
            AssetBundleHelper.UnloadBundle(HotfixAotAssemblyBundle);
        }

        private static void LoadModelDll()
        {
#if UNITY_EDITOR || UNITY_EDITOR_64
            // UnityEditor下在Hotfix里实例化Model层的对象、实际会直接用依赖的Assembly，不是用ab包里的Dll的Assembly
            // 如果不这样加载的话、会同时有两个Model的Assembly、看似一样其实是两个完全不同的Assembly
            // 但打包后就会通过HybridCLR加载就不会有这样的问题
            // 考虑Model全是数据所以影响不大
            AssemblyManager.Load(AssemblyName.Model, typeof(Fantasy.Model.Entry).Assembly);
            return;
#endif
            AssetBundleHelper.LoadBundle(HotfixModelDllBundle);
            var dllBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixModelDllBundle, "Fantasy.Model.dll").bytes;
            var pdbBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixModelDllBundle, "Fantasy.Model.pdb").bytes;
            var assembly = Assembly.Load(dllBytes, pdbBytes);
            AssemblyManager.Load(AssemblyName.Model, assembly);
            AssetBundleHelper.UnloadBundle(HotfixModelDllBundle);
        }

        /// <summary>
        /// 可以用于编辑器开发的时候实现热重载
        /// </summary>
        public static void LoadHotfixDll()
        {
#if UNITY_EDITOR || UNITY_EDITOR_64
            AssemblyManager.Load(AssemblyName.Hotfix, typeof(Fantasy.Hotfix.Entry).Assembly);
            return;
#endif
            AssetBundleHelper.LoadBundle(HotfixHotfixDllBundle);
            var dllBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixHotfixDllBundle, "Fantasy.Hotfix.dll").bytes;
            var pdbBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixHotfixDllBundle, "Fantasy.Hotfix.pdb").bytes;
            var assembly = Assembly.Load(dllBytes, pdbBytes);
            AssemblyManager.Load(AssemblyName.Hotfix, assembly);
            AssetBundleHelper.UnloadBundle(HotfixHotfixDllBundle);
        }
    }
}