using Fantasy;
using UnityEngine;
namespace MicroCharacterController
{
    public class Movement : BehaviourNonAlloc
    {
        public bool CanControl = false;

        public MoveState state;
        [HideInInspector]public Animator animator;

        // Controller
        protected Collider _controller;
        protected SwimmingController _swimmingController;

        //get direction for the camera
        protected Transform _cameraTransform;
        protected Transform _characterTransform;

        //Ground.
        protected Vector3 _hitNormal;
        protected bool _isGrounded;
        protected bool isGroundedWithinTolerance => _isGrounded ;

        [Tooltip("这是角色的阻力力，标准值为 (8, 0, 8)。")]
        public Vector3 DragForce;
        
        protected void Awake(){
            animator = GetComponent<Animator>();
            _controller = GetComponent<Collider>();
            _swimmingController = GetComponent<SwimmingController>();
        }

        public virtual void Move(Vector3 motion)
        {
        }

        public virtual void MoveTarget(MoveInfo moveInfo)
        {
        }

        public virtual void EnableController(bool enable = true)
        {
            _controller.enabled = enable;
        }

        public virtual void CanControlMove(bool enable = true)
        {
            CanControl = enable;
        }

        public virtual void GetInputs()
        {
        }

        // 是否移动,需要子类实现
        public virtual bool IsMoving()
        {
            return  false;
        }

        #region Animator
        public void SetGroundedState()
        {
            // 避免在动画器中多次设置 grounded 变量
            if (animator.GetBool("Grounded") != _isGrounded)
            {
                animator.SetBool("Grounded", _isGrounded);
            }
        }
        #endregion
        
        public virtual void SetSpeed(float speed){}

        public virtual void Navigate(Vector3 destination, float stoppingDistance){}

        public bool Running(){
            if (state == MoveState.RUNNING || state == MoveState.SWIMMING) 
                return true;
            return false;
        }

        public bool Idling(){
            if (state == MoveState.IDLE || state == MoveState.SWIMMINGIDLE) 
                return true;
            return false;
        }

        public bool Swiming(){
            if (state == MoveState.SWIMMING || state == MoveState.SWIMMINGIDLE) 
                return true;
            return false;
        }

        protected virtual void UpdateAnimations()
        { 
            // 将动画参数应用于所有动画，角色使用了多个蒙皮网格的装备道具。
            foreach (Animator animator in GetComponentsInChildren<Animator>())
            {
                animator.SetBool("Running", Running());
                
                if (state == MoveState.JUMP)
                    animator.SetTrigger("Jump");

                animator.SetBool("Swimming", Swiming());
                
            }
        }
    }
}