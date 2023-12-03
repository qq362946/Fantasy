using System;
using System.Collections;
using UnityEngine;
using Fantasy;
namespace MicroCharacterController
{
    public partial class CharacterMovement : Movement
    {
        [Header("角色移动设置")]
        [Tooltip("玩家的速度.")]
        public float RunningSpeed = 5f;
        [Tooltip("最短移动距离.")]
        public float minThreshold = 0.01f;
        [Tooltip("重力力度。")] [Range(0, -100)]
        public float Gravity = -30f;
        protected float _gravity;

        //Input.
        public Inputs PlayerInputs;
        private float _horizontal;
        private float _vertical;

        //Movement.
        private bool _invertedControl;
        protected Vector3 _forward;
        protected Vector3 _right;
        protected Vector3 _move;  // 水平移动
        protected Vector3 _direction; // 前进方向
        protected Vector3 _velocity; // 垂直移动
        protected float _originalRunningSpeed;

        public CharacterController controller => base._controller as CharacterController;


        protected new void Awake(){
            base.Awake();
            
            PlayerInputs = GetComponent<Inputs>();
            _characterTransform = transform;
            _originalRunningSpeed = RunningSpeed;
        }

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _dashCooldown = DashCooldown;
            _gravity = Gravity;
        }

        public override void EnableController(bool enable = true)
        {
            base.EnableController(enable);
            _characterTransform = transform;
            _originalRunningSpeed = RunningSpeed;
        }

        public override void GetInputs()
        {
            PlayerInputs = GetComponent<Inputs>();
        }

        public override void Move(Vector3 motion)
        {
            controller.Move(motion);
        }

        public void Move_Horizontal(float horizontal)
        {
            _horizontal = horizontal;
        }

        public void Move_Vertical(float vertical)
        {
            _vertical = vertical;
        }

        public float GetHorizontal()
        {
            return _horizontal;
        }
        public float GetVertical()
        {
            return _vertical;
        }

        private void Update()
        {
            CheckGroundStatus();

            // 在这个区域捕捉输入，你可以使用PlayerInput类，
            // 或者简单地用"_jump = PlayerInputs.Jump()"替换为"_jump = Input.GetButtonDown("buttonName")"。
            if (PlayerInputs != null)
            {
                _horizontal = PlayerInputs.GetHorizontal();
                _vertical = PlayerInputs.GetVertical();
                _jump = PlayerInputs.Jump();
                _dash = PlayerInputs.Dash();
                _flyJetPack = PlayerInputs.JetPack();
                _activeFall = PlayerInputs.Parachute();
            }

            // 反转控制
            if (_invertedControl)
            {
                _horizontal *= -1;
                _vertical *= -1;
                _jump = PlayerInputs.Dash();
                _dash = PlayerInputs.Jump();
            }

            if (_jump && !HoldingObject)
            {
                Jump(JumpHeight);
            }

            if (_dash && !HoldingObject)
            {
                Dash();
            }

            // 如果玩家可以控制角色
            if (CanControl)
            {
                // 喷气背包
                if (Jetpack && _flyJetPack && JetPackFuel > 0 && !HoldingObject)
                {
                    // 如果慢落正在进行，则停用。
                    if (_slowFall)
                    {
                        _slowFall = false;
                        SlowFallObject.SetActive(false);
                    }

                    FlyByJetPack();
                }

                // 慢落
                if (_activeFall)
                {
                    _slowFall = !_slowFall;
                    _activeFall = false;
                }
            }
            else
            {
                _horizontal = 0;
                _vertical = 0;
            }

            // 冲刺冷却
            if (DashCooldown > 0)
            {
                DashCooldown -= Time.fixedDeltaTime;
            }
            else
            {
                DashCooldown = 0;
            }

            // 刷新移动状态
            state = UpdateMoveState();
            // 刷新动画状态
            UpdateAnimations();
            // 平台
            UpdatePlatform();
        }


        private void FixedUpdate()
        {
            FixUpdateJetpack();

            // 获取相机位置的输入方向。
            _forward = _cameraTransform.TransformDirection(Vector3.forward);
            _forward.y = 0f;
            _forward = _forward.normalized;
            _right = new Vector3(_forward.z, 0.0f, -_forward.x);

            // 计算水平移动与前进方向
            _move = (_horizontal * _right + _vertical * _forward);
            _direction = (_horizontal * _right + _vertical * _forward);

            #if UNITY_EDITOR
            // 辅助可视化场景视图中的前进方向10米处的点
            Debug.DrawLine(
                transform.position + (Vector3.up * 1f),
                transform.position + (Vector3.up * 1f)+ _direction*10,
                Color.red
                );
            #endif

            // 如果正确着地，则执行滑动。
            if (!_isCorrectGrounded && _isGrounded)
            {
                _move.x += (1f - _hitNormal.y) * _hitNormal.x * (1f - SlideFriction);
                _move.z += (1f - _hitNormal.y) * _hitNormal.z * (1f - SlideFriction);
            }

            _move.Normalize();
            // 如果未激活慢落（这样可以避免改变下落速度），并且控制器已启用，则移动玩家。
            // ==> _move 水平方向的移动
            if (!_slowFall && controller.enabled)
                Move(Time.deltaTime * RunningSpeed * _move);

            // 检查是否正确着地。
            _isCorrectGrounded = (Vector3.Angle(Vector3.up, _hitNormal) <= SlopeLimit);

            // 设置前进方向
            if (_direction != Vector3.zero)
                transform.forward = _direction;
            
            // 重力力量
            if (_velocity.y >= -MaxDownYVelocity)
                _velocity.y += Gravity * Time.deltaTime;
            // 如果玩家在水上，则停止重力
            if (Swimming)
                _velocity.y = 0;
            
            // 应用阻尼效果，减缓速度
            _velocity.x /= 1 + DragForce.x * Time.deltaTime;
            _velocity.y /= 1 + DragForce.y * Time.deltaTime;
            _velocity.z /= 1 + DragForce.z * Time.deltaTime;

            // ==> _velocity 垂直方向的移动
            if (controller.enabled)
                Move(_velocity * Time.deltaTime);
            
            SetGroundedState();
        }

        // 更新角色移动状态
        protected override MoveState UpdateMoveState()
        {
            if (EventDied())
                return MoveState.DEAD;
            else if (EventFalling())
                return MoveState.AIRBORNE;
            else if (EventJumpRequested())
                return MoveState.JUMP;
            else if (EventUnderWater() && IsMoving())
                return MoveState.SWIMMING;
            else if (EventUnderWater())
                return MoveState.SWIMMINGIDLE;
            else if (IsMoving())
                return MoveState.RUNNING;
            else if (EventLanded())
                return MoveState.IDLE; 
            
            return MoveState.IDLE;   
        }

        public override bool IsMoving()
        {
            return (Math.Abs(_horizontal) > 0 || Math.Abs(_vertical) > 0); 
        }

        public override bool IsGrounded()
        {
            return controller.isGrounded;
        }

        public override void Navigate(Vector3 destination, float stoppingDistance)
        {
            // 角色控制器运动不允许导航（尚未实现）
        }

        public void LookAtTarget(Transform target)
        {
            transform.LookAt(target, Vector3.up);
            transform.rotation = Quaternion.Euler(0, _characterTransform.eulerAngles.y, 0);
        }

        // 更改玩家的速度
        public override void SetSpeed(float speed)
        {
            RunningSpeed = speed;
        }

        // 在一段时间内更改玩家的速度
        public void ChangeSpeedInTime(float speedPlus, float time)
        {
            StartCoroutine(ModifySpeedByTime(speedPlus, time));
        }

        // 反转玩家控制（类似混乱技能）
        public void InvertPlayerControls(float invertTime)
        {
            // 检查是否已经反转
            if (!_invertedControl)
            {
                StartCoroutine(InvertControls(invertTime));
            }
        }


        #region MoveState
        protected bool EventDied()
        {
            return false;
        }
        protected bool EventJumpRequested()
        {
            return isGroundedWithinTolerance && _jump;
        }
        protected bool EventFalling()
        {
            return !isGroundedWithinTolerance;
        }
        protected bool EventLanded()
        {
            return _isGrounded;
        }
        public bool EventUnderWater()
        {
            if (_swimmingController == null) return false;
            if (_swimmingController.waterCollider == null) return false;
            Collider waterCollider = _swimmingController.waterCollider;
        
            // 从水底到玩家位置的光线投射
            Vector3 origin = new Vector3(transform.position.x,
                                        waterCollider.bounds.max.y,
                                        transform.position.z);
            float distance = controller.height * underwaterThreshold;
            Debug.DrawLine(origin, origin + Vector3.down * distance, Color.cyan);

            // 如果光线投射没有击中任何东西就在水下
            return !Utils.RaycastWithout(origin, Vector3.down, out RaycastHit hit,
                 distance, gameObject, canStandInWaterCheckLayers); 
        }
        #endregion

        #region Coroutine
        // 使用此方法在一段时间内停用玩家控制。
        public IEnumerator DeactivatePlayerControlByTime(float time)
        {
            controller.enabled = false;
            CanControl = false;
            yield return new WaitForSeconds(time);
            CanControl = true;
            controller.enabled = true;
        }

        // 通过时间修改速度的协程。
        private IEnumerator ModifySpeedByTime(float speedPlus, float time)
        {
            if (RunningSpeed + speedPlus > 0)
            {
                RunningSpeed += speedPlus;
            }
            else
            {
                RunningSpeed = 0;
            }

            yield return new WaitForSeconds(time);
            RunningSpeed = _originalRunningSpeed;
        }

        private IEnumerator InvertControls(float invertTime)
        {
            yield return new WaitForSeconds(0.1f);
            _invertedControl = true;
            yield return new WaitForSeconds(invertTime);
            _invertedControl = false;
        }
        #endregion
    }
}