using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement : Movement
    {
        [Header("跳跃设置")]
        [Tooltip("Jump max elevation for the character.")]
        public float JumpHeight = 2f;

        [Tooltip("允许角色跳跃。")]
        public bool CanJump = true;

        [Tooltip("允许角色在空中进行双重跳跃。")]
        public bool CanDoubleJump = true;
        [Tooltip("玩家下落时的最大速度。")] [Range(0, 100)]
        public float MaxDownYVelocity = 15;

        private bool _doubleJump;

        protected bool _jump;

        public void Jump(float jumpHeight)
        {
            if (!CanJump || !CanControl)
            {
                return;
            }

            CurrentActivePlatform = null;
            //removing parachute if active;
            _slowFall = false;
            SlowFallObject.SetActive(false);

            //
            if (_isGrounded)
            {
                _hitNormal = Vector3.zero;
                // SetJumpAnimation();
                _doubleJump = true;
                _velocity.y = 0;
                _velocity.y += Mathf.Sqrt(jumpHeight * -2f * Gravity);

                //Instantiate jump effect
                if (JumpEffect)
                {
                    Instantiate(JumpEffect, LowZonePosition.position, LowZonePosition.rotation);
                }
            }
            else if (CanDoubleJump && _doubleJump)
            {
                _doubleJump = false;
                _velocity.y = 0;
                _velocity.y += Mathf.Sqrt(jumpHeight * -2f * Gravity);

                //Instantiate jump effect
                if (JumpEffect)
                {
                    Instantiate(JumpEffect, LowZonePosition.position, LowZonePosition.rotation);
                }
            }
        }

        public void ActivateDeactivateJump(bool canJump)
        {
            CanJump = canJump;
        }

        public void ActivateDeactivateDoubleJump(bool canDoubleJump)
        {
            //if double jump is active activate normal jump
            if (canDoubleJump) CanJump = true;
            CanDoubleJump = canDoubleJump;
        }
    }
}