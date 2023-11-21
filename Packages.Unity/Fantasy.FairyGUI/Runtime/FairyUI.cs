using System;
using FairyGUI;
using Fantasy;

namespace Fantasy.FairyGUI
{
    public abstract class FairyUI : Entity
    {
        public GComponent GComponent;
        public virtual UILayer Layer { get; private set; }
        public virtual string PackageName { get; private set; }
        public virtual string ComponentName { get; private set; }
        public virtual string ConfigBundleName { get; private set; }
        public virtual string ResourceBundleName { get; private set; }

        public abstract void OnCreate();

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            if (Layer != UILayer.None)
            {
                FUIComponent.Instance.LayerDictionary[(int)Layer].RemoveChild(GComponent);
                Layer = UILayer.None;
            }

            FUIPackageHelper.UnloadPackage(PackageName);

            PackageName = null;
            ComponentName = null;
            ConfigBundleName = null;
            ResourceBundleName = null;

            if (GComponent != null)
            {
                GComponent.Dispose();
            }

            FUIComponent.Instance.UIComponents.Remove(Id);
            base.Dispose();
        }

        public void SetLayer(UILayer layer)
        {
            if (Layer != UILayer.None)
            {
                FUIComponent.Instance.LayerDictionary[(int)Layer].RemoveChild(GComponent);
            }

            Layer = layer;
            FUIComponent.Instance.LayerDictionary[(int)layer].AddChild(GComponent);
        }

        public void Hide()
        {
            GComponent.visible = false;
        }

        public void Show()
        {
            GComponent.visible = true;
        }

        public void Close(bool removeImmediately = false)
        {
            GComponent.visible = false;

            if (removeImmediately)
            {
                Dispose();
            }
        }

        public T AddComponent<T>(UILayer layer = UILayer.BaseRoot) where T : FairyUI, new()
        {
            var uiComponent = FUIComponent.Instance.Create<T>(Scene, layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.BaseRoot) where T : FairyUI, new()
        {
            var uiComponent = await FUIComponent.Instance.CreateAsync<T>(Scene, layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }
    }
}