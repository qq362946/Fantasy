using System;
using System.Collections.Generic;
using FairyGUI;

namespace Fantasy
{
    public sealed class FUIPackageInfo : IDisposable
    {
        public string Name;
        public int RefCount;
        public UIPackage UIPackage;
        public readonly List<string> AssetBundles = new List<string>();

        public static FUIPackageInfo Create(string packageName)
        {
            var uiPackageInfo = Pool<FUIPackageInfo>.Rent();
            uiPackageInfo.Name = packageName;
            uiPackageInfo.RefCount = 1;
            return uiPackageInfo;
        }
        
        public void Dispose()
        {
            Name = null;
            RefCount = 0;
            UIPackage.RemovePackage(UIPackage.id);
            
            foreach (var assetBundle in AssetBundles)
            {
                AssetBundleHelper.UnloadBundle(assetBundle);
            }
            
            UIPackage = null;
            AssetBundles.Clear();
        }

        public async FTask DestroyAsync()
        {
            Name = null;
            RefCount = 0;
            UIPackage.RemovePackage(UIPackage.id);
            
            foreach (var assetBundle in AssetBundles)
            {
                await AssetBundleHelper.UnloadBundleAsync(assetBundle);
            }
            
            UIPackage = null;
            AssetBundles.Clear();
        }
    }
}