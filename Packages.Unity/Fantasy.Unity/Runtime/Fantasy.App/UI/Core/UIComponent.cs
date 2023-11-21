using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fantasy
{
    public class UIComponentAwakeSystem : AwakeSystem<UIComponent>
    {
        protected override void Awake(UIComponent self)
        {
            UIComponent.Instance = self;
            UIComponent.Root = GameObject.Find("/Fantasy");

            if (UIComponent.Root == null)
            {
                Log.Error("We need to create a GameObject named Fantasy and set it to DontDestroyOnLoad");
            }
        }
    }
    
    public class UIComponentDestroySystem : DestroySystem<UIComponent>
    {
        protected override void Destroy(UIComponent self)
        {
            if (UIComponent.Root != null)
            {
                UnityEngine.Object.Destroy(UIComponent.Root);
            }

            UIComponent.Root = null;
            UIComponent.UIRoot = null;
            UIComponent.Camera = null;
            UIComponent.Instance = null;

            foreach (var (_, ui) in self.UIComponents)
            {
                ui.Dispose();
            }
            
            self.UIComponents.Clear();
            
            foreach (var (_, layer) in UIComponent.LayerDictionary)
            {
                layer.Dispose();
            }
            
            UIComponent.LayerDictionary.Clear();
        }
    }

    public sealed class UIComponent : Entity
    {
        public static Camera Camera;
        public static GameObject Root;
        public static GameObject UIRoot;
        public static UIComponent Instance;
        public readonly Dictionary<long, UI> UIComponents = new ();
        public static readonly Dictionary<int, UILayerComponent> LayerDictionary = new ();

        #region Initialize

        public void Initialize(int designResolutionX = 1920, int designResolutionY = 1080,
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            bool addAudioListener = true)
        {
            UIRoot = GameObject.Find("/Fantasy/UI");

            if (UIRoot == null)
            {
                UIRoot = new GameObject("UI")
                {
                    transform =
                    {
                        parent = Root.transform
                    },
                    layer = LayerMask.NameToLayer("UI")
                };
            }
            
            CameraInitialize(addAudioListener);
            UILayerInitialize(designResolutionX, designResolutionY, scaleMode, screenMatchMode);
        }

        private void CameraInitialize(bool addAudioListener)
        {
            GameObject cameraGameObject = null;
            var objects = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var camera in objects)
            {
                if (camera.name != "UICamera")
                {
                    continue;
                }

                Camera = camera;
                cameraGameObject = camera.gameObject;
            }

            if (cameraGameObject != null)
            {
                if (!cameraGameObject.activeSelf)
                {
                    cameraGameObject.SetActive(true);
                }
                
                return;
            }

            cameraGameObject = new GameObject("UICamera")
            {
                transform =
                {
                    parent = UIRoot.transform
                },
                layer = LayerMask.NameToLayer("UI")
            };

            Camera = cameraGameObject.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.Depth;
            Camera.cullingMask = LayerMask.GetMask("UI");
            
            cameraGameObject.AddComponent<FlareLayer>();

            if (addAudioListener)
            {
                cameraGameObject.AddComponent<AudioListener>();
            }
        }

        private void UILayerInitialize(int designResolutionX, int designResolutionY, CanvasScaler.ScaleMode scaleMode, CanvasScaler.ScreenMatchMode screenMatchMode)
        {
            var uiLayers = Enum.GetNames(typeof(UILayer));

            for (var i = 1; i < uiLayers.Length; i++)
            {
                var layerName = uiLayers[i];
                var layer = GameObject.Find($"/Fantasy/UI/{layerName}");

                if (layer == null)
                {
                    layer = new GameObject(layerName)
                    {
                        transform =
                        {
                            parent = UIRoot.transform
                        },
                        layer = LayerMask.NameToLayer("UI")
                    };

                    layer.AddComponent<RectTransform>();
                    var canvas= layer.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = Camera;
                    canvas.sortingOrder = i;
                    canvas.vertexColorAlwaysGammaSpace = true;
                    var canvasScaler = layer.AddComponent<CanvasScaler>();
                    canvasScaler.uiScaleMode = scaleMode;
                    canvasScaler.screenMatchMode = screenMatchMode;
                    canvasScaler.matchWidthOrHeight = .5f;
                    canvasScaler.referenceResolution = new Vector2(designResolutionX, designResolutionY);
                    layer.AddComponent<GraphicRaycaster>();
                }

                var uiLayerComponent = Create<UILayerComponent>(Scene);
                uiLayerComponent.GameObject = layer;
                LayerDictionary.Add(i, uiLayerComponent);
            }
        }

        #endregion

        #region Create

        public async FTask<T> CreateAsync<T>(UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : UI, new()
        {
            var ui = Create<T>(Scene, false);

            try
            {
                if (string.IsNullOrEmpty(ui.AssetName) || string.IsNullOrEmpty(ui.BundleName))
                {
                    Log.Error($"ui.AssetName or ui.BundleName is null");
                    return default;
                }
                
                // 异步加载AB包资源
                await AssetBundleHelper.LoadBundleAsync(ui.BundleName);
                var gameObject = AssetBundleHelper.GetAsset<GameObject>(ui.BundleName, ui.AssetName);
                // 实例化GameObject
                ui.GameObject = Object.Instantiate(gameObject);
                // 设置UI的Layer
                ui.SetLayer(layer);
                // 执行初始化
                ui.Initialize();
                
                if (isRunEvent)
                {
                    EntitiesSystem.Instance.Awake(ui);
                    EntitiesSystem.Instance.StartUpdate(ui);
                }

                UIComponents.Add(ui.Id, ui);
            }
            catch (Exception e)
            {
                LayerDictionary[(int)layer].Remove(ui.Id, false);
                ui.Dispose();
                Log.Error(e);
            }
            
            return ui;
        }

        public T Create<T>(UILayer layer = UILayer.BaseRoot, bool isRunEvent = true) where T : UI, new()
        {
            var ui = Create<T>(Scene, false);

            try
            {
                if (string.IsNullOrEmpty(ui.AssetName) || string.IsNullOrEmpty(ui.BundleName))
                {
                    Log.Error($"ui.AssetName or ui.BundleName is null");
                    return default;
                }

                // 加载AB包资源
                AssetBundleHelper.LoadBundle(ui.BundleName);
                var gameObject = AssetBundleHelper.GetAsset<GameObject>(ui.BundleName, ui.AssetName);
                // 实例化GameObject
                ui.GameObject = Object.Instantiate(gameObject);
                // 设置UI的Layer
                ui.SetLayer(layer);
                // 执行初始化
                ui.Initialize();
                
                if (isRunEvent)
                {
                    EntitiesSystem.Instance.Awake(ui);
                    EntitiesSystem.Instance.StartUpdate(ui);
                }

                UIComponents.Add(ui.Id, ui);
            }
            catch (Exception e)
            {
                // 失败的时候要在对应层里移除掉、不然会产生内存泄露
                LayerDictionary[(int)layer].Remove(ui.Id, false);
                ui.Dispose();
                Log.Error(e);
            }
            
            return ui;
        }

        #endregion

        #region Component

        public T AddComponent<T>(UILayer layer = UILayer.BaseRoot) where T : UI, new()
        {
            var uiComponent = Create<T>(layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.BaseRoot) where T : UI, new()
        {
            var uiComponent = await CreateAsync<T>(layer, false);
            AddComponent(uiComponent);
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public void RemoveComponent(long id, bool isDispose = true)
        {
            if (!UIComponents.Remove(id, out var ui))
            {
                return;
            }

            LayerDictionary[(int)ui.Layer].Remove(id, false);
            
            if (isDispose)
            {
                ui.Dispose();
            }
        }

        #endregion
    }
}