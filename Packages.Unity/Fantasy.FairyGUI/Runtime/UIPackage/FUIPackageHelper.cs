using System;
using System.Collections.Generic;
using System.IO;
using FairyGUI;

namespace Fantasy
{
    public static class FUIPackageHelper
    {
        private const string EditorPath = "Assets/Bundles/UI";
        private static readonly Dictionary<string, FUIPackageInfo> UIPackages = new Dictionary<string, FUIPackageInfo>();

        public static UIPackage LoadPackage(string packageName, string bundleName, string resourceBundleName)
        {
            if (UIPackages.TryGetValue(packageName, out var uiPackageInfo))
            {
                uiPackageInfo.RefCount++;
                return uiPackageInfo.UIPackage;
            }

            uiPackageInfo = FUIPackageInfo.Create(packageName);
#if UNITY_EDITOR || UNITY_EDITOR_64
            uiPackageInfo.UIPackage = UIPackage.AddPackage($"{EditorPath}/{packageName}/{packageName}");
#else
            AssetBundleHelper.LoadBundle(bundleName);
            var assetBundle = AssetBundleHelper.GetAssetBundle(bundleName);

            uiPackageInfo.AssetBundles.Add(bundleName);

            if (!string.IsNullOrEmpty(resourceBundleName) && File.Exists(resourceBundleName.ToAssetBundlePath()))
            {
                AssetBundleHelper.LoadBundle(resourceBundleName);
                var resourceAssetBundle = AssetBundleHelper.GetAssetBundle(resourceBundleName);
                uiPackageInfo.UIPackage = UIPackage.AddPackage(assetBundle, resourceAssetBundle);
                uiPackageInfo.AssetBundles.Add(resourceBundleName);
            }
            else
            {
                uiPackageInfo.UIPackage = UIPackage.AddPackage(assetBundle);
            }
#endif
            if (uiPackageInfo.UIPackage == null)
            {
                Log.Error(
                    $"UIPackage packageName:{packageName} bundleName:{bundleName} resourceBundleName:{resourceBundleName} not found");
            }

            UIPackages.Add(packageName, uiPackageInfo);
            return uiPackageInfo.UIPackage;
        }

        public static async FTask<UIPackage> LoadPackageAsync(string packageName, string bundleName, string resourceBundleName)
        {
            if (UIPackages.TryGetValue(packageName, out var uiPackageInfo))
            {
                uiPackageInfo.RefCount++;
                return uiPackageInfo.UIPackage;
            }

            uiPackageInfo = FUIPackageInfo.Create(packageName);

#if UNITY_EDITOR || UNITY_EDITOR_64
            await FTask.CompletedTask;
            uiPackageInfo.UIPackage = UIPackage.AddPackage($"{EditorPath}/{packageName}/{packageName}");
#else
            await AssetBundleHelper.LoadBundleAsync(bundleName);
            var assetBundle = AssetBundleHelper.GetAssetBundle(bundleName);

            uiPackageInfo.AssetBundles.Add(bundleName);

            if (!string.IsNullOrEmpty(resourceBundleName) && File.Exists(resourceBundleName.ToAssetBundlePath()))
            {
                await AssetBundleHelper.LoadBundleAsync(resourceBundleName);
                var resourceAssetBundle = AssetBundleHelper.GetAssetBundle(resourceBundleName);
                uiPackageInfo.UIPackage = UIPackage.AddPackage(assetBundle, resourceAssetBundle);
                uiPackageInfo.AssetBundles.Add(resourceBundleName);
            }
            else
            {
                uiPackageInfo.UIPackage = UIPackage.AddPackage(assetBundle);
            }
#endif
            if (uiPackageInfo.UIPackage == null)
            {
                Log.Error($"UIPackage packageName:{packageName} bundleName:{bundleName} resourceBundleName:{resourceBundleName} not found");
            }

            UIPackages.Add(packageName, uiPackageInfo);
            return uiPackageInfo.UIPackage;
        }

        public static void UnloadPackage(string packageName)
        {
            if (!UIPackages.TryGetValue(packageName, out var uiPackageInfo))
            {
                return;
            }
            
            if (--uiPackageInfo.RefCount > 0)
            {
                return;
            }
            
            UIPackages.Remove(packageName);
            uiPackageInfo.Dispose();
        }

        public static async FTask UnloadPackageAsync(string packageName)
        {
            if (!UIPackages.TryGetValue(packageName, out var uiPackageInfo))
            {
                return;
            }
            
            if (--uiPackageInfo.RefCount > 0)
            {
                return;
            }
            
            UIPackages.Remove(packageName);
            await uiPackageInfo.DestroyAsync();
        }
    }
}