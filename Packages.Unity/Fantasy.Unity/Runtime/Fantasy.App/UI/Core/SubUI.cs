using UnityEngine;
using Object = UnityEngine.Object;

namespace Fantasy
{
    public abstract class SubUI : Entity
    {
        public GameObject GameObject;
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
            EntitiesSystem.Instance.Awake(uiComponent);
            EntitiesSystem.Instance.StartUpdate(uiComponent);
            return uiComponent;
        }

        public async FTask<T> AddComponentAsync<T>(UILayer layer = UILayer.None) where T : UI, new()
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