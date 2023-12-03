using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement : Movement
    {
        [Header("喷气背包")] [Tooltip("玩家有喷气背包吗？")]
        public bool Jetpack = true;

        [Tooltip("喷气背包的最大燃料容量。")]
        public float JetPackMaxFuelCapacity = 90;

        [Tooltip("喷气背包的当前燃料，如果为0，则关闭喷气背包。")]
        public float JetPackFuel;

        [Tooltip("喷气背包的推力，使玩家上升。")]
        public float JetPackForce;

        [Tooltip("喷气背包每秒消耗的燃料量。")]
        public float FuelConsumeSpeed;

        [Header("慢落")] [Tooltip("允许玩家慢慢下落，可以使用像降落伞这样的物品。")]
        public bool HaveSlowFall;

        [Tooltip("慢慢下落的垂直速度。")] [Range(0, 5)]
        public float SlowFallSpeed = 1.5f;

        [Tooltip("慢慢下落的前进速度。")] [Range(0, 1)]
        public float SlowFallForwardSpeed = 0.1f;

        [Header("推动设置:")]
        [Tooltip(
            "为true时，仅将物体移动到被推动面的相反方向。为false时，根据推动的方向移动物体。")]
        public bool PushInFixedDirections;

        [Tooltip("推动物体的力量")] public float PushPower = 2.0f;

        [Tooltip("这是角色的阻力力，标准值为 (8, 0, 8)。")]
        public Vector3 DragForce;

        [Tooltip("玩家状态：是否持有物体")]
        public bool HoldingObject;

        [Header("特效")]
        [Tooltip("这个位置位于角色的脚部，用于实例化特效。")]
        public Transform LowZonePosition;
        public GameObject JumpEffect;
        public GameObject DashEffect;
        public GameObject JetPackObject;
        public GameObject SlowFallObject;

        private bool _flyJetPack;
        private bool _slowFall;
        private bool _activeFall;

        public void FixUpdateJetpack()
        {
            if (CanControl)
            {
                // 激活或停用喷气背包对象和效果。
                JetPackObject.SetActive(Jetpack && _flyJetPack && JetPackFuel > 0 && !HoldingObject);

                if (HaveSlowFall && !_isGrounded && _slowFall)
                {
                    SlowFall();
                }
                else
                {
                    SlowFallObject.SetActive(false);
                    _slowFall = false;
                }
            }
        }

        private void FlyByJetPack()
        {
            JetPackFuel -= Time.deltaTime * FuelConsumeSpeed;
            _velocity.y = 0;
            _velocity.y += Mathf.Sqrt(JetPackForce * -2f * Gravity);
        }

        //this is for a slow fall like a parachute.
        private void SlowFall()
        {
            SlowFallObject.SetActive(true);

            Move(transform.forward * SlowFallForwardSpeed);
            _velocity.y = 0;
            _velocity.y += -SlowFallSpeed;
        }

        //add fuel to the jet pack
        public void AddFuel(float fuel)
        {
            JetPackFuel += fuel;
            if (JetPackFuel > JetPackMaxFuelCapacity)
            {
                JetPackFuel = JetPackMaxFuelCapacity;
            }

            Debug.Log("Fuel +" + fuel);
        }

        public void ActivateDeactivateSlowFall(bool canSlowFall)
        {
            HaveSlowFall = canSlowFall;
        }

        public void ActivateDeactivateJetpack(bool haveJetPack)
        {
            Jetpack = haveJetPack;
        }
    }
}