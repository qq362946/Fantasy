using System;
using System.Collections.Generic;
using FairyGUI;
using Fantasy;
using UnityEngine;

namespace Fantasy.FairyGUI
{
    public sealed class FUIComponentAwakeSystem : AwakeSystem<FUIComponent>
    {
        protected override void Awake(FUIComponent self)
        {
            FUIComponent.Instance = self;
            UIComponent.Root = GameObject.Find("/Fantasy");

            if (UIComponent.Root == null)
            {
                Log.Error("We need to create a GameObject named Fantasy and set it to DontDestroyOnLoad");
            }
        }
    }
    
    public class FUIComponent : Entity
    {
        public static FUIComponent Instance;
        public readonly Dictionary<int, GComponent> LayerDictionary = new Dictionary<int, GComponent>();
        public readonly Dictionary<long, FairyUI> UIComponents = new Dictionary<long, FairyUI>();
        
        public void Initialize(int designResolutionX = 1080, int designResolutionY = 1920)
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

        public async FTask<T> CreateAsync<T>(Scene scene, UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : FairyUI, new()
        {
            var uiComponent = Entity.Create<T>(scene, false);

            if (!string.IsNullOrEmpty(uiComponent.PackageName))
            {
                await FUIPackageHelper.LoadPackageAsync(uiComponent.PackageName, uiComponent.ConfigBundleName, uiComponent.ResourceBundleName);
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

        public T Create<T>(Scene scene, UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : FairyUI, new()
        {
            var uiComponent = Entity.Create<T>(scene, false);

            if (!string.IsNullOrEmpty(uiComponent.PackageName))
            {
                FUIPackageHelper.LoadPackage(uiComponent.PackageName, uiComponent.ConfigBundleName,
                    uiComponent.ResourceBundleName);
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
        
        private GComponent CreateRootComponent(UILayer layer)
        {
            var gComponent = new GComponent
            {
                gameObjectName = layer.ToString()
            };

            GRoot.inst.AddChild(gComponent);
            return gComponent;
        }
    }
}