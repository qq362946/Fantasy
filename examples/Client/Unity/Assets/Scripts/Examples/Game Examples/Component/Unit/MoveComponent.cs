using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using UnityEngine;

namespace Fantasy
{
    public sealed class MoveComponentAwakeSystem : AwakeSystem<MoveComponent>
    {
        protected override void Awake(MoveComponent self)
        {
            self.Initialize();
        }
    }
    
    public sealed class MoveComponentUpdateSystem : UpdateSystem<MoveComponent>
    {
        protected override void Update(MoveComponent self)
        {
            self.Update();
        }
    }

    public sealed class MoveComponent : Entity
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int MotionSpeed = Animator.StringToHash("MotionSpeed");

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed = 5f;
        /// <summary>
        /// 到达目标点的最小距离
        /// </summary>
        public const float StopDistance = 0.1f;
        /// <summary>
        /// Animator组件
        /// </summary>
        public Animator Animator { get; private set; }
        /// <summary>
        /// 动画速度平滑时间
        /// </summary>
        public const float AnimationSmoothTime = 0.1f;
        /// <summary>
        /// 角色旋转速度（度/秒）- 越小越平滑，不晕
        /// </summary>
        public const float RotationSpeed = 360f;
        /// <summary>
        /// 当前动画速度
        /// </summary>
        private float _currentAnimationSpeed = 0f;
        private float _animationVelocity = 0f;
        /// <summary>
        /// Unit的Transform
        /// </summary>
        public Transform Transform { get; private set; }
        /// <summary>
        /// 要移动的目标位置
        /// </summary>
        public Vector3 TargetPosition;
        /// <summary>
        /// 标记是否移动中
        /// </summary>
        private bool _isMoving = false;
        /// <summary>
        /// 标记是否是自己操作
        /// </summary>
        private bool _isSelf = false;
        private Unit _unit;
        private EventComponent _eventComponent;

        public void Initialize(bool isSelf = false)
        {
            var unit = GetParent<Unit>();
            var unitGameObject = unit.GameObject;
            
            _isSelf = isSelf;
            _unit = GetParent<Unit>();
            Transform = unitGameObject.transform;
            _eventComponent = Scene.EventComponent;
            Animator = unitGameObject.GetComponent<Animator>();
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            _unit = null;
            _eventComponent = null;
            Transform = null;
            Animator = null;
            
            _isMoving = false;
            TargetPosition = Vector3.zero;
            _animationVelocity = 0;
            _currentAnimationSpeed = 0;
            base.Dispose();
        }

        public void SetTargetPosition(Vector3 targetPosition)
        {
            TargetPosition = targetPosition;
            TargetPosition.y = Transform.position.y;
            _isMoving = true;
            _eventComponent.Publish(new OnUnitMoveStateChange(_unit, ref targetPosition, UnitMoveState.StartMove));
        }

        public void Update()
        {
            MoveToTarget();
            UpdateAnimation();
        }

        private void MoveToTarget()
        {
            if (!_isMoving)
            {
                return;
            }
            
            var direction = TargetPosition - Transform.position;
            var distance = direction.magnitude;
            
            if (distance <= StopDistance)
            {
                _isMoving = false;
                if (_isSelf)
                {
                    _eventComponent.Publish(new OnUnitMoveStateChange(_unit, UnitMoveState.TargetPoint));
                }
                return;
            }
            
            var moveDirection = direction.normalized;
            Transform.position += moveDirection * MoveSpeed * Time.deltaTime;

            if (moveDirection == Vector3.zero)
            {
                return;
            }
            
            var targetRotation = Quaternion.LookRotation(moveDirection);
            Transform.rotation = Quaternion.RotateTowards(
                Transform.rotation,
                targetRotation,
                RotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimation()
        {
            if (Animator == null)
            {
                return;
            }

            var targetSpeed = _isMoving ? MoveSpeed : 0f;
            
            _currentAnimationSpeed = Mathf.SmoothDamp(
                _currentAnimationSpeed,
                targetSpeed,
                ref _animationVelocity,
                AnimationSmoothTime
            );
            
            // Speed: 0=Idle, 2=Walk, 6=Run
            Animator.SetFloat(Speed, _currentAnimationSpeed);
            Animator.SetFloat(MotionSpeed, 1f);
        }
    }
}