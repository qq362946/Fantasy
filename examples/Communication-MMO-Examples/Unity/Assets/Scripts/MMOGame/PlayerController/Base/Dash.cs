using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement
    {
        [Header("冲刺设置")]
        [Tooltip("玩家能够进行冲刺吗？")]
        public bool CanDash = true;

        [Tooltip("冲刺的冷却时间。")] public float DashCooldown = 3;

        [Tooltip("冲刺的力量，值越大冲刺距离越远。")]
        public float DashForce = 5f;

        private bool _dash;
        private float _dashCooldown;

        public void Dash()
        {
            if (!CanDash || DashCooldown > 0 || _flyJetPack)
            {
                return;
            }

            DashCooldown = _dashCooldown;

            if (DashEffect)
            {
                Instantiate(DashEffect, transform.position, _characterTransform.rotation);
            }

            SetDashAnimation();
            StartCoroutine(Dashing(DashForce / 10));
            _velocity += Vector3.Scale(transform.forward,
                DashForce * new Vector3((Mathf.Log(1f / (Time.deltaTime * DragForce.x + 1)) / -Time.deltaTime),
                    0, (Mathf.Log(1f / (Time.deltaTime * DragForce.z + 1)) / -Time.deltaTime)));
        }

        public void ActivateDeactivateDash(bool canDash)
        {
            CanDash = canDash;
        }

        //dash coroutine.
        private IEnumerator Dashing(float time)
        {
            CanControl = false;
            if (!_isGrounded)
            {
                Gravity = 0;
                _velocity.y = 0;
            }

            //animate hear to true
            yield return new WaitForSeconds(time);
            CanControl = true;
            //animate hear to false
            Gravity = _gravity;
        }

        #region Animator
        private void SetDashAnimation()
        {
            animator.SetTrigger("Dash");
        }
        #endregion
    }
}