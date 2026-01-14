using System.Data;
using Fantasy.Entitas;
using UnityEngine;

namespace Fantasy
{
    public static class UnitFactory
    {
        private const string PlayerPrefPath = "Starter Assets/Prefabs/PlayerArmature";
        
        public static Unit CreatePlayer(Scene scene, long unitId, Vector3 position, bool isSelf)
        {
            var playerGameObject = Resources.Load<GameObject>(PlayerPrefPath);

            if (playerGameObject == null)
            {
                throw new NoNullAllowedException($"Can't find player object {PlayerPrefPath}");
            }
            
            var mapComponent = scene.GetComponent<MapComponent>();

            if (mapComponent == null)
            {
                throw new NoNullAllowedException($"Can't find map component {scene}");
            }

            Unit unit = null;
            
            try
            {
                unit = Entity.Create<Unit>(scene, unitId, true, true);
                // 基础信息
                unit.UnitType = UnitType.Player;
                // 创建GameObject
                unit.GameObject = UnityEngine.Object.Instantiate(
                    playerGameObject,
                    position,
                    Quaternion.identity,
                    mapComponent.PlayersGameObject.transform
                );
                // 挂载移动组件
                unit.AddComponent<MoveComponent>().Initialize(isSelf);
                if (isSelf)
                {
                    // 如果是自己账号需要挂载输入采集组件
                    unit.AddComponent<InputComponent>();
                    // 目标标记组件
                    unit.AddComponent<TargetMarkerComponent>();
                    // 摄像头组件
                    unit.AddComponent<UnitCameraComponent>();
                }
                return unit;
            }
            catch
            {
                if (unit != null)
                {
                    unit.Dispose();
                }
                
                throw;
            }
        }
    }
}