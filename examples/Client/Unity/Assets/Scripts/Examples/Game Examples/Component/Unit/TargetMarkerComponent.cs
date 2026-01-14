using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

namespace Fantasy
{
    public sealed class TargetMarkerComponentAwakeSystem : AwakeSystem<TargetMarkerComponent>
    {
        protected override void Awake(TargetMarkerComponent self)
        {
            self.Initialize();
        }
    }

    public sealed class TargetMarkerComponent : Entity
    {
        /// <summary>
        /// 标记缩放大小
        /// </summary>
        private const float MarkerScale = 0.5f;
        private GameObject _targetMarkerInstance;
        
        public void Initialize()
        {
            if (_targetMarkerInstance == null)
            {
                // 创建简单的圆环标记
                _targetMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _targetMarkerInstance.name = "TargetMarker";
            
                // 移除碰撞体，避免干扰物理
                Object.Destroy(_targetMarkerInstance.GetComponent<Collider>());
            
                // 设置材质颜色
                var renderer = _targetMarkerInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0f, 1f, 0f, 0.5f);
                }
            
                // 设置为扁平的圆盘形状
                _targetMarkerInstance.transform.localScale = new Vector3(MarkerScale, 0.05f, MarkerScale);
            }
           
            // 初始隐藏
            _targetMarkerInstance.SetActive(false);
        }

        public void UpdateMarkerPosition(ref Vector3 targetPosition)
        {
            if (_targetMarkerInstance != null)
            {
                _targetMarkerInstance.transform.position = targetPosition + Vector3.up * 0.1f;
                _targetMarkerInstance.SetActive(true);
            }
        }

        public void HideMarker()
        {
            if (_targetMarkerInstance != null)
            {
                _targetMarkerInstance.SetActive(false);
            }
        }
    }
}