using System;
using UnityEngine;

namespace Fantasy
{
    public abstract class UI : Entity
    {
        public GameObject GameObject;
        public virtual UILayer Layer { get; protected set; }
        public abstract string AssetName { get; protected set; }
        public abstract string BundleName { get; protected set; }

        public abstract void Initialize();

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            UnityEngine.Object.Destroy(GameObject);
            Scene.AssetBundleComponent.UnloadBundle(BundleName);
            UIComponent.Instance.RemoveComponent(Id, false);

            GameObject = null;
            base.Dispose();
        }

        public void SetLayer(UILayer layer)
        {
            if (Layer != UILayer.None)
            {
                UIComponent.LayerDictionary[(int)Layer].Remove(Id, false);
            }

            Layer = layer;
            UIComponent.LayerDictionary[(int)layer].Add(this);
        }

        public T AddComponent<T>(UILayer layer = UILayer.None) where T : UI, new()
        {
            var uiComponent = UIComponent.Instance.Create<T>(layer, false);
            AddComponent(uiComponent);
            Scene.EntityComponent.Awake(uiComponent);
            Scene.EntityComponent.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.None) where T : UI, new()
        {
            var uiComponent = await UIComponent.Instance.CreateAsync<T>(layer, false);
            AddComponent(uiComponent);
            Scene.EntityComponent.Awake(uiComponent);
            Scene.EntityComponent.StartUpdate(uiComponent);
            return uiComponent;
        }

        public T AddWidget<T>(Transform root, bool isRunEvent = true) where T : SubUI, new()
        {
            var subUI = Create<T>(Scene, false, false);
            try
            {
                if (string.IsNullOrEmpty(subUI.AssetName) || string.IsNullOrEmpty(subUI.BundleName))
                {
                    Log.Error($"subUI.AssetName or subUI.BundleName is null");
                    return default;
                }

                // 加载AB包资源
                Scene.AssetBundleComponent.LoadBundle(subUI.BundleName);
                var gameObject = Scene.AssetBundleComponent.GetAsset<GameObject>(subUI.BundleName, subUI.AssetName);
                // 实例化GameObject
                subUI.GameObject = UnityEngine.Object.Instantiate(gameObject, root);
                // 执行初始化
                subUI.Initialize();

                if (isRunEvent)
                {
                    Scene.EntityComponent.Awake(subUI);
                    Scene.EntityComponent.StartUpdate(subUI);
                }
                
                AddComponent(subUI);
            }
            catch (Exception e)
            {
                subUI.Dispose();
                Log.Error(e);
            }

            return subUI;
        }

        public async FTask<T> AddWidgetAsync<T>(Transform root ,bool isRunEvent = true) where T : SubUI, new()
        {
            var subUI = Create<T>(Scene, false, false);
            
            try
            {
                if (string.IsNullOrEmpty(subUI.AssetName) || string.IsNullOrEmpty(subUI.BundleName))
                {
                    Log.Error($"subUI.AssetName or subUI.BundleName is null");
                    return default;
                }

                // 加载AB包资源
                await Scene.AssetBundleComponent.LoadBundleAsync(subUI.BundleName);
                var gameObject = Scene.AssetBundleComponent.GetAsset<GameObject>(subUI.BundleName, subUI.AssetName);
                // 实例化GameObject
                subUI.GameObject = UnityEngine.Object.Instantiate(gameObject, root);
                // 执行初始化
                subUI.Initialize();

                if (isRunEvent)
                {
                    Scene.EntityComponent.Awake(subUI);
                    Scene.EntityComponent.StartUpdate(subUI);
                }
                
                AddComponent(subUI);
            }
            catch (Exception e)
            {
                subUI.Dispose();
                Log.Error(e);
            }

            return subUI;
        }

        public void Hide()
        {
            if (GameObject == null || !GameObject.activeSelf)
            {
                return;
            }

            GameObject.SetActive(false);
        }

        public void Show()
        {
            if (GameObject == null || GameObject.activeSelf)
            {
                return;
            }

            GameObject.SetActive(true);
        }
    }
}