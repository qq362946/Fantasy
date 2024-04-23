#if FANTASY_UNITY
using UnityEngine;
namespace Fantasy
{
    public sealed class AssetBundleInfo
    {
        public string Name;
        public int RefCount;
        public Scene Scene;
        public AssetBundle AssetBundle;

        public static AssetBundleInfo Create(Scene scene, string abName, AssetBundle assetBundle = null)
        {
            var assetBundleInfo = scene.Pool.Rent<AssetBundleInfo>();
            assetBundleInfo.Name = abName;
            assetBundleInfo.Scene = scene;
            assetBundleInfo.RefCount = 1;
            assetBundleInfo.AssetBundle = assetBundle;
            return assetBundleInfo;
        }

        private void Dispose()
        {
            Name = null;
            RefCount = 0;
            AssetBundle = null;
            Scene.Pool.Return(this);
        }

        public void Destroy(bool unload)
        {
            if (AssetBundle != null)
            {
                AssetBundle.Unload(unload);
            }

            Dispose();
        }

        public async FTask DestroyAsync(bool unload)
        {
            var task = FTask.Create();

            if (AssetBundle != null)
            {
                var assetBundle = AssetBundle;
                var asyncOperation = assetBundle.UnloadAsync(unload);
                asyncOperation.completed += operation => task.SetResult();
            }
            
            Dispose();
            await task;
        }
    }
}
#endif
