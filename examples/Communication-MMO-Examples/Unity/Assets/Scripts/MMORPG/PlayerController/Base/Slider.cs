using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement : Movement
    {
        [Header("滑动控制设置")]
        [Tooltip("滑动的斜坡角度限制。")]
        public float SlopeLimit = 45;

        [Tooltip("滑动摩擦力。")] [Range(0.1f, 0.9f)]
        public float SlideFriction = 0.3f;

        private bool _isCorrectGrounded; // 过于陡峭的地面
    }
}