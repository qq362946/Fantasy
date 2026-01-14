using Cinemachine;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

namespace Fantasy
{
    public sealed class UnitCameraComponentAwakeSystem : AwakeSystem<UnitCameraComponent>
    {
        protected override void Awake(UnitCameraComponent self)
        {
            self.Initialize();
        }
    }

    public sealed class UnitCameraComponent : Entity
    {
        private const string VirtualCameraPath = "Starter Assets/Prefabs/PlayerCamera";

        public GameObject GameObject { get; private set; }
        public CinemachineCollider Collider { get; private set; }
        public CinemachineVirtualCamera VirtualCamera { get; private set; }

        public void Initialize()
        {
            var unit = GetParent<Unit>();
            var unitGameObject = unit.GameObject;
            var prefab = Resources.Load<GameObject>(VirtualCameraPath);

            GameObject = UnityEngine.Object.Instantiate(
                prefab,
                unitGameObject.transform.position,
                Quaternion.identity,
                unitGameObject.transform.parent
            );

            VirtualCamera = GameObject.GetComponent<CinemachineVirtualCamera>();
            Collider = GameObject.GetComponent<CinemachineCollider>();

            var playerInfo = unitGameObject.GetComponent<PlayerInfo>();
            VirtualCamera.Follow = playerInfo.followGameObject.transform;
            VirtualCamera.LookAt = playerInfo.lookAtGameObject.transform;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            Object.Destroy(GameObject);
            GameObject = null;
            Collider = null;
            VirtualCamera = null;
            base.Dispose();
        }
    }
}