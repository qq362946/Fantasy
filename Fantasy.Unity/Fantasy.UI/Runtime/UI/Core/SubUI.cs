using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fantasy
{
    public abstract class SubUI : Entity, ISupportedMultiEntity
    {
        public GameObject GameObject;
        public abstract string AssetName { get; protected set; }
        public abstract string BundleName { get; protected set; }
        public abstract void Initialize();

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Object.Destroy(GameObject);

            GameObject = null;
            base.Dispose();
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
                subUI.GameObject = Object.Instantiate(gameObject, root);
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

        public async FTask<T> AddWidgetAsync<T>(Transform root, bool isRunEvent = true) where T : SubUI, new()
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
                subUI.GameObject = Object.Instantiate(gameObject, root);
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