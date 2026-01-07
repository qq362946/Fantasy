using System.ComponentModel;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

namespace Fantasy
{
    public sealed class MapComponentDestroySystem : DestroySystem<MapComponent>
    {
        protected override void Destroy(MapComponent self)
        {
            Object.Destroy(self.GameObject);
            Object.Destroy(self.PlayersGameObject);
        }
    }

    public sealed class MapComponent : Entity
    {
        public GameObject GameObject { get; private set; }
        public GameObject PlayersGameObject { get; private set; }

        public void Initialize(string mapPath)
        {
            var mapGameObject = Resources.Load<GameObject>(mapPath);
            GameObject = Object.Instantiate(mapGameObject, Vector3.zero, Quaternion.identity);
            PlayersGameObject = GameObject.transform.Find("Players").gameObject;
        }
    }
}