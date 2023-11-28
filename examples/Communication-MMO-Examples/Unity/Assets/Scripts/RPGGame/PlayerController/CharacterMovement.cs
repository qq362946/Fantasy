using System;
using System.Collections;
using UnityEngine;
namespace PlatformCharacterController
{
    public class CharacterMovement : Movement
    {
        [Header("Player Controller Settings")] [Tooltip("Speed for the player.")]
        public float RunningSpeed = 5f;

        [Tooltip("Slope angle limit to slide.")]
        public float SlopeLimit = 45;

        [Tooltip("Slide friction.")] [Range(0.1f, 0.9f)]
        public float SlideFriction = 0.3f;

        [Tooltip("Gravity force.")] [Range(0, -100)]
        public float Gravity = -30f;

        [Tooltip("Max speed for the player when fall.")] [Range(0, 100)]
        public float MaxDownYVelocity = 15;

        [Tooltip("Can the user control the player?")]
        public bool CanControl = true;

        [Header("Jump Settings")] [Tooltip("This allow the character to jump.")]
        public bool CanJump = true;

        [Tooltip("Jump max elevation for the character.")]
        public float JumpHeight = 2f;

        [Tooltip("This allow the character to jump in air after another jump.")]
        public bool CanDoubleJump = true;

        [Header("Dash Settings")] [Tooltip("The player have dash?.")]
        public bool CanDash = true;

        [Tooltip("Cooldown for the dash.")] public float DashCooldown = 3;

        [Tooltip("Force for the dash, a greater value more distance for the dash.")]
        public float DashForce = 5f;

        [Header("JetPack")] [Tooltip("Player have jetpack?")]
        public bool Jetpack = true;

        [Tooltip("The fuel maxima capacity for the jetpack.")]
        public float JetPackMaxFuelCapacity = 90;

        [Tooltip("The current fuel for the jetpack, if 0 the jet pack off.")]
        public float JetPackFuel;

        [Tooltip("The force for the jetpack, this impulse the player up.")]
        public float JetPackForce;

        [Tooltip("Jet pack consume this quantity by second active.")]
        public float FuelConsumeSpeed;

        [Header("SlowFall")] [Tooltip("This allow the player a slow fall, you can use an item like a parachute.")]
        public bool HaveSlowFall;

        [Tooltip("Speed vertical for the slow fall.")] [Range(0, 5)]
        public float SlowFallSpeed = 1.5f;

        [Tooltip("Slow fall forward speed.")] [Range(0, 1)]
        public float SlowFallForwardSpeed = 0.1f;

        [Header("Push settings:")]
        [Tooltip(
            "True to only move the object in the opposite direction to the pushed face. False to move the object based on where it is pushed.")]
        public bool PushInFixedDirections;

        [Tooltip("Force of pushing objects")] public float PushPower = 2.0f;

        [Tooltip("This is the drag force for the character, a standard value are (8, 0, 8). ")]
        public Vector3 DragForce;

        [Tooltip("Player Status: Holds or not an object")]
        public bool HoldingObject;


        [Header("Swimming")] 
        [Tooltip("Player Status: Swimming")]
        public bool Swimming;
        public LayerMask canStandInWaterCheckLayers = Physics.DefaultRaycastLayers; // 可站立在水中的层
        public float underwaterThreshold = 0.7f; 

        [Header("Effects")] 
        [Tooltip("This position is in the character feet and is use to instantiate effects.")]
        public Transform LowZonePosition;

        public GameObject JumpEffect;
        public GameObject DashEffect;
        public GameObject JetPackObject;
        public GameObject SlowFallObject;

        public Inputs PlayerInputs;

        [Header("Platforms")] public Transform CurrentActivePlatform;

        private CharacterController _controller;
        private SwimmingController _swimmingController;

        private Vector3 _moveDirection;
        private Vector3 _activeGlobalPlatformPoint;
        private Vector3 _activeLocalPlatformPoint;
        private Quaternion _activeGlobalPlatformRotation;
        private Quaternion _activeLocalPlatformRotation;
        private Transform _characterTransform;

        //Input.
        private float _horizontal;
        private float _vertical;

        private bool _dash;
        private bool _flyJetPack;
        private bool _slowFall;

        //get direction for the camera
        private Transform _cameraTransform;
        private Vector3 _forward;
        private Vector3 _right;

        //temporal vars
        private float _originalRunningSpeed;
        private float _dashCooldown;
        
        private bool _doubleJump;
        private bool _invertedControl;
        private bool _isCorrectGrounded; // 过于陡峭的地面
        
        private bool _activeFall;
        private Vector3 _hitNormal;
        private Vector3 _move;
        private Vector3 _direction;

        private void Awake()
        {
            PlayerInputs = GetComponent<Inputs>();
            animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            _characterTransform = transform;
            _originalRunningSpeed = RunningSpeed;
            _swimmingController = GetComponent<SwimmingController>();
        }

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _dashCooldown = DashCooldown;
            _gravity = Gravity;
        }

        private void Update()
        {
            CheckGroundStatus();

            //capture input in this region, you can use PlayerInput class or simple replace "_jump = PlayerInputs.Jump()" whit  _jump = Input.GetButtonDown("buttonName") for example.
            if(PlayerInputs != null)
            {
                _horizontal = PlayerInputs.GetHorizontal();
                _vertical = PlayerInputs.GetVertical();
                _jump = PlayerInputs.Jump();
                _dash = PlayerInputs.Dash();
                _flyJetPack = PlayerInputs.JetPack();
                _activeFall = PlayerInputs.Parachute();
            }
            

            //this invert controls 
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

            //if player can control the character
            if (CanControl)
            {
                //jet pack
                if (Jetpack && _flyJetPack && JetPackFuel > 0 && !HoldingObject)
                {
                    //if slow fall is active deactivate.
                    if (_slowFall)
                    {
                        _slowFall = false;
                        SlowFallObject.SetActive(false);
                    }

                    FlyByJetPack();
                }

                //slow fall
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

            //dash cooldown
            if (DashCooldown > 0)
            {
                DashCooldown -= Time.fixedDeltaTime;
            }
            else
            {
                DashCooldown = 0;
            }

            //set running animation
            // SetRunningAnimation((Math.Abs(_horizontal) > 0 || Math.Abs(_vertical) > 0));

            // 刷新移动状态
            state = UpdateMoveState();
            // 刷新动画状态
            UpdateAnimations();

            //platforms
            if (!CurrentActivePlatform || !CurrentActivePlatform.CompareTag("Platform")) return;
            if (CurrentActivePlatform)
            {
                var newGlobalPlatformPoint = CurrentActivePlatform.TransformPoint(_activeLocalPlatformPoint);
                _moveDirection = newGlobalPlatformPoint - _activeGlobalPlatformPoint;
                if (_moveDirection.magnitude > 0.01f)
                {
                    _controller.Move(_moveDirection);
                }

                if (!CurrentActivePlatform) return;

                // Support moving platform rotation
                var newGlobalPlatformRotation = CurrentActivePlatform.rotation * _activeLocalPlatformRotation;
                var rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_activeGlobalPlatformRotation);
                // Prevent rotation of the local up vector
                rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;
                _characterTransform.rotation = rotationDiff * _characterTransform.rotation;
                _characterTransform.eulerAngles = new Vector3(0, _characterTransform.eulerAngles.y, 0);

                UpdateMovingPlatform();
            }
            else
            {
                if (!(_moveDirection.magnitude > 0.01f)) return;
                _moveDirection = Vector3.Lerp(_moveDirection, Vector3.zero, Time.deltaTime);
                _controller.Move(_moveDirection);
            }

        }

        private void FixedUpdate()
        {
            if (CanControl)
            {
                //this activate or deactivate jet pack Object and effect.
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

            //get the input direction for the camera position.
            _forward = _cameraTransform.TransformDirection(Vector3.forward);
            _forward.y = 0f;
            _forward = _forward.normalized;
            _right = new Vector3(_forward.z, 0.0f, -_forward.x);

            _move = (_horizontal * _right + _vertical * _forward);
            _direction = (_horizontal * _right + _vertical * _forward);


            //if no is correct grounded then slide.
            if (!_isCorrectGrounded && _isGrounded)
            {
                _move.x += (1f - _hitNormal.y) * _hitNormal.x * (1f - SlideFriction);
                _move.z += (1f - _hitNormal.y) * _hitNormal.z * (1f - SlideFriction);
            }

            _move.Normalize();
            //move the player if no is active the slow fall(this avoid change the speed for the fall)
            if (!_slowFall && _controller.enabled)
            {
                _controller.Move(Time.deltaTime * RunningSpeed * _move);
            }

            //Check if is correct grounded.
            _isCorrectGrounded = (Vector3.Angle(Vector3.up, _hitNormal) <= SlopeLimit);

            //set the forward direction
            if (_direction != Vector3.zero)
            {
                transform.forward = _direction;
            }

            //gravity force
            if (_velocity.y >= -MaxDownYVelocity)
            {
                _velocity.y += Gravity * Time.deltaTime;
            }
            
            //stop gravity if player are on water
            if (Swimming)
            {
                _velocity.y = 0;
            }

            _velocity.x /= 1 + DragForce.x * Time.deltaTime;
            _velocity.y /= 1 + DragForce.y * Time.deltaTime;
            _velocity.z /= 1 + DragForce.z * Time.deltaTime;
            if (_controller.enabled)
            {
                _controller.Move(_velocity * Time.deltaTime);
            }

            SetGroundedState();
        }

        public override void SetPosition(Vector3 pos)
        {
            _controller.enabled = false;
            transform.position = pos;
            _controller.enabled = true;
        }

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

        // 更新动画控制器状态
        protected override void UpdateAnimations()
        {
            animator.SetBool("Running", Running());
            
            if (state == MoveState.JUMP)
                animator.SetTrigger("Jump");

            animator.SetBool("Swimming", Swiming()); 
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

            _controller.Move(transform.forward * SlowFallForwardSpeed);
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

        public override bool IsMoving()
        {
            return (Math.Abs(_horizontal) > 0 || Math.Abs(_vertical) > 0); 
        }

        public override void Navigate(Vector3 destination, float stoppingDistance)
        {
            // character controller movement doesn't allow navigation (yet)
        }

        public void LookAtTarget(Transform target)
        {
            transform.LookAt(target, Vector3.up);
            transform.rotation = Quaternion.Euler(0, _characterTransform.eulerAngles.y, 0);
        }

        //change the speed for the player
        public override void SetSpeed(float speed)
        {
            RunningSpeed = speed;
        }

        //change the speed for the player for a time period
        public void ChangeSpeedInTime(float speedPlus, float time)
        {
            StartCoroutine(ModifySpeedByTime(speedPlus, time));
        }

        //invert player control(like a confuse skill)
        public void InvertPlayerControls(float invertTime)
        {
            //check if not are already inverted
            if (!_invertedControl)
            {
                StartCoroutine(InvertControls(invertTime));
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

        public void ActivateDeactivateDash(bool canDash)
        {
            CanDash = canDash;
        }

        public void ActivateDeactivateSlowFall(bool canSlowFall)
        {
            HaveSlowFall = canSlowFall;
        }

        public void ActivateDeactivateJetpack(bool haveJetPack)
        {
            Jetpack = haveJetPack;
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

        private void CheckGroundStatus()
        {
#if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view

            Debug.DrawLine(
                transform.position + (Vector3.up * 0.1f),
                transform.position + Vector3.down * (_controller.height / 2 + 0.2f),
                Color.red
            );

#endif
            // it is also good to note that the transform position in the sample character is at the center
            var grounded = Physics.Raycast(transform.position, Vector3.down, _controller.height / 2 + 0.2f);

            _isGrounded = grounded || _controller.isGrounded;
        }

        #region MoveState
        protected bool EventDied()
        {
            //return health.current == 0;
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
            // Collider waterCollider = _swimmingController.waterCollider;
            // if (_swimmingController!=null && waterCollider != null)

            if (_swimmingController == null) return false;
            if (_swimmingController.waterCollider == null) return false;
            Collider waterCollider = _swimmingController.waterCollider;
           
            // 从水底到玩家位置的光线投射
            Vector3 origin = new Vector3(transform.position.x,
                                        waterCollider.bounds.max.y,
                                        transform.position.z);
            float distance = _controller.height * underwaterThreshold;
            Debug.DrawLine(origin, origin + Vector3.down * distance, Color.cyan);

            // 如果光线投射没有击中任何东西就在水下
            return !Utils.RaycastWithout(origin, Vector3.down, out RaycastHit hit, distance, gameObject, canStandInWaterCheckLayers);
            
        }
        #endregion

        #region Animator
        private void SetDashAnimation()
        {
            animator.SetTrigger("Dash");
        }

        private void SetGroundedState()
        {
            //avoid set the grounded var in animator multiple time
            if (animator.GetBool("Grounded") != _isGrounded)
            {
                animator.SetBool("Grounded", _isGrounded);
            }
        }

        #endregion

        #region Coroutine

        //Use this to deactivate te player control for a period of time.
        public IEnumerator DeactivatePlayerControlByTime(float time)
        {
            _controller.enabled = false;
            CanControl = false;
            yield return new WaitForSeconds(time);
            CanControl = true;
            _controller.enabled = true;
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

        //modify speed by time coroutine.
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