using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement
    {
        [Header("Platforms")]
        public Transform CurrentActivePlatform;

        private Vector3 _activeGlobalPlatformPoint;
        private Vector3 _activeLocalPlatformPoint;
        private Quaternion _activeGlobalPlatformRotation;
        private Quaternion _activeLocalPlatformRotation;

        protected Vector3 _moveDirection;

        public void UpdatePlatform()
        {
            // 平台
            if (!CurrentActivePlatform || !CurrentActivePlatform.CompareTag("Platform")) return;
            if (CurrentActivePlatform)
            {
                var newGlobalPlatformPoint = CurrentActivePlatform.TransformPoint(_activeLocalPlatformPoint);
                _moveDirection = newGlobalPlatformPoint - _activeGlobalPlatformPoint;
                if (_moveDirection.magnitude > 0.01f)
                {
                    Move(_moveDirection);
                }

                if (!CurrentActivePlatform) return;

                // 支持移动平台旋转
                var newGlobalPlatformRotation = CurrentActivePlatform.rotation * _activeLocalPlatformRotation;
                var rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_activeGlobalPlatformRotation);
                // 防止本地向上矢量的旋转
                rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;
                _characterTransform.rotation = rotationDiff * _characterTransform.rotation;
                _characterTransform.eulerAngles = new Vector3(0, _characterTransform.eulerAngles.y, 0);

                UpdateMovingPlatform();
            }
            else
            {
                if (!(_moveDirection.magnitude > minThreshold)) return;
                _moveDirection = Vector3.Lerp(_moveDirection, Vector3.zero, Time.deltaTime);
                Move(_moveDirection);
            }
        }

        private void UpdateMovingPlatform()
        {
            _activeGlobalPlatformPoint = transform.position;
            _activeLocalPlatformPoint = CurrentActivePlatform.InverseTransformPoint(_characterTransform.position);
            // Support moving platform rotation
            _activeGlobalPlatformRotation = transform.rotation;
            _activeLocalPlatformRotation =
                Quaternion.Inverse(CurrentActivePlatform.rotation) * _characterTransform.rotation;
        }
    }
}