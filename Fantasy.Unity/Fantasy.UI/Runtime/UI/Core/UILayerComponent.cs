using System.Collections.Generic;
using UnityEngine;

namespace Fantasy
{
    public sealed class UILayerComponent : Entity
    {
        public GameObject GameObject;
        private readonly Dictionary<long, UI> _uis = new Dictionary<long, UI>();

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            Object.Destroy(GameObject);
            GameObject = null;
            
            foreach (var (_, ui) in _uis)
            {
                ui.Dispose();
            }

            _uis.Clear();
            base.Dispose();
        }

        public void Add(UI uiComponent)
        {
            _uis.Add(uiComponent.Id, uiComponent);
            uiComponent.GameObject.transform.SetParent(GameObject.transform, false);
        }

        public T Get<T>(long id) where T : UI
        {
            return _uis.TryGetValue(id, out var ui) ? (T)ui : default;
        }

        public bool TryGet<T>(long id, out T uiComponent) where T : UI
        {
            if (!_uis.TryGetValue(id, out var ui))
            {
                uiComponent = default;
                return false;
            }

            uiComponent = (T)ui;
            return true;
        }

        public void Remove(long id, bool isDispose = true)
        {
            if (_uis.Remove(id, out var ui))
            {
                return;
            }

            if (isDispose)
            {
                ui.Dispose();
            }
        }
    }
}