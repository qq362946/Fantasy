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
            AssetBundleHelper.UnloadBundle(BundleName);
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
        
        public T AddComponent<T>(UILayer layer = UILayer.BaseRoot) where T : UI, new()
        {
            var uiComponent = UIComponent.Instance.Create<T>(layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.BaseRoot) where T : UI, new()
        {
            var uiComponent = await UIComponent.Instance.CreateAsync<T>(layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
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