using System;
using System.Collections.Generic;
using FairyGUI;
using Fantasy.Core.UI;
using Fantasy.Helper;

namespace Fantasy.Core
{
    // public partial class UI_LoginComponent : UIComponent
    // {
    //     public override UILayer Layer => UILayer.BaseRoot;
    //     public override string PackageName => "LoginUI";
    //     public override string ComponentName => "LoginComponent";
    //     public override string ConfigBundleName => "LoginUI".ToLower();
    //     public override string ResourceBundleName => "LoginUI".ToLower();
    //
    //     public GImage LoginBG;
    //     public GImage Login_Title;
    //     public GImage CadPa;
    //     public GButton PlayButton;
    //     public const string URL = "ui://69hhdwjqnrl90";
    //
    //     public override void OnCreate()
    //     {
    //         LoginBG = (GImage)GComponent.GetChildAt(0);
    //         Login_Title = (GImage)GComponent.GetChildAt(1);
    //         CadPa = (GImage)GComponent.GetChildAt(2);
    //         PlayButton = (GButton)GComponent.GetChildAt(3);
    //     }
    // }
    // public partial class UI_LoginComponent : UIComponent
    // {
    //     public override string PackageName => "base.PackageName".ToLower();
    //
    //     public override string ComponentName => base.ComponentName;
    //
    //     public override string ConfigBundleName => base.ConfigBundleName;
    //     public override string ResourceBundleName => base.ResourceBundleName;
    //
    //     public override UILayer Layer => UILayer.BaseRoot;
    //
    //     public override void OnCreate()
    //     {
    //         var LoginBG = (GTextField)GComponent.GetChildAt(0);
    //
    //         LoginBG.text = "1";
    //     }
    // }
    public abstract class UIComponent : Entity
    {
        #region Static

        private static readonly Dictionary<int, GComponent> LayerDictionary = new Dictionary<int, GComponent>();
        private static readonly Dictionary<long, UIComponent> UIComponents = new Dictionary<long, UIComponent>();
        
        public static void Initialize(int designResolutionX = 1080, int designResolutionY = 1920)
        {
            // 创建UI层级
            LayerDictionary.Clear();
            LayerDictionary.Add((int)UILayer.GRoot, GRoot.inst);
            LayerDictionary.Add((int)UILayer.DeepBaseRoot, CreateRootComponent(UILayer.DeepBaseRoot));
            LayerDictionary.Add((int)UILayer.BaseRoot, CreateRootComponent(UILayer.BaseRoot));
            LayerDictionary.Add((int)UILayer.MiddleRoot, CreateRootComponent(UILayer.MiddleRoot));
            LayerDictionary.Add((int)UILayer.TopRoot, CreateRootComponent(UILayer.TopRoot));
            LayerDictionary.Add((int)UILayer.TopMostRoot, CreateRootComponent(UILayer.TopMostRoot));
            // 设置UI尺寸和适配方式
            GRoot.inst.SetContentScaleFactor(designResolutionX, designResolutionY, UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);
        }
        
        public static async FTask<T> CreateAsync<T>(Scene scene, UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : UIComponent, new()
        {
            var uiComponent = Entity.Create<T>(scene, false);

            if (!string.IsNullOrEmpty(uiComponent.PackageName))
            {
                await UIPackageHelper.LoadPackageAsync(uiComponent.PackageName, uiComponent.ConfigBundleName, uiComponent.ResourceBundleName);
            }

            var tcs = FTask<T>.Create();

            UIPackage.CreateObjectAsync(uiComponent.PackageName, uiComponent.ComponentName, gObject =>
            {
                uiComponent.GComponent = gObject.asCom;
                uiComponent.SetLayer(layer);
                uiComponent.OnCreate();
                tcs.SetResult(uiComponent);
            });

            await tcs;

            if (isRunEvent)
            {
                EntitiesSystem.Instance.Awake(uiComponent);
                EntitiesSystem.Instance.StartUpdate(uiComponent);
            }

            UIComponents.Add(uiComponent.Id, uiComponent);
            return uiComponent;
        }

        public static T Create<T>(Scene scene, UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : UIComponent, new()
        {
            var uiComponent = Entity.Create<T>(scene, false);

            if (!string.IsNullOrEmpty(uiComponent.PackageName))
            {
                UIPackageHelper.LoadPackage(uiComponent.PackageName, uiComponent.ConfigBundleName, uiComponent.ResourceBundleName);
            }

            uiComponent.GComponent = UIPackage.CreateObject(uiComponent.PackageName, uiComponent.ComponentName).asCom;
            uiComponent.SetLayer(layer);
            uiComponent.OnCreate();

            if (isRunEvent)
            {
                EntitiesSystem.Instance.Awake(uiComponent);
                EntitiesSystem.Instance.StartUpdate(uiComponent);
            }

            UIComponents.Add(uiComponent.Id, uiComponent);
            return uiComponent;
        }

        private static GComponent CreateRootComponent(UILayer layer)
        {
            var gComponent = new GComponent
            {
                gameObjectName = layer.ToString()
            };
            
            GRoot.inst.AddChild(gComponent);
            
            return gComponent;
        }

        #endregion

        public GComponent GComponent { get; private set; }
        public virtual UILayer Layer { get; private set; }
        public virtual string PackageName { get; private set; }
        public virtual string ComponentName { get; private set; }
        public virtual string ConfigBundleName { get; private set; }
        public virtual string ResourceBundleName { get; private set; }
        
        public event Action OnHide;
        public event Action OnShow;
        public event Action OnClose;

        public abstract void OnCreate();
        
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            UIPackageHelper.UnloadPackage(PackageName);

            Layer = UILayer.None;
            PackageName = null;
            ComponentName = null;
            ConfigBundleName = null;
            ResourceBundleName = null;

            if (GComponent != null)
            {
                GComponent.Dispose();
            }

            UIComponents.Remove(Id);
            base.Dispose();
        }
        
        private void SetLayer(UILayer layer)
        {
            if (Layer != UILayer.None)
            {
                LayerDictionary[(int)Layer].RemoveChild(GComponent);
            }

            Layer = layer;
            LayerDictionary[(int)layer].AddChild(GComponent);
        }
        
        public void Hide()
        {
            GComponent.visible = false;
            OnHide?.Invoke();
        }

        public void Show()
        {
            GComponent.visible = true;
            OnShow?.Invoke();
        }

        public void Close(bool removeImmediately = false)
        {
            GComponent.visible = false;
            OnClose?.Invoke();
            
            if (removeImmediately)
            {
                Dispose();
            }
        }

        public T AddComponent<T>(UILayer layer = UILayer.BaseRoot) where T : UIComponent, new()
        {
            var uiComponent = Create<T>(Scene, layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.BaseRoot) where T : UIComponent, new()
        {
            var uiComponent = await CreateAsync<T>(Scene, layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }
    }
}