using UnityEngine;

public class RedBall : BehaviourNonAlloc
{
    public Inputs PlayerInputs;
    public GameObject sphere;
    private float _horizontal;
    private float _vertical;
    public CharacterController controller;
    protected Transform _cameraTransform;
    protected Transform _characterTransform;

    protected Vector3 _forward;
    protected Vector3 _right;
    protected Vector3 _move;  // 水平移动
    public Vector3 _direction; // 前进方向
    protected Vector3 _velocity; // 垂直移动
    public float Gravity = -30f;
    public float RunningSpeed = 20f;

    public float SlopeLimit = 45;
    public float SlideFriction = 0.3f;
    private bool _isCorrectGrounded; // 过于陡峭的地面

    //Ground.
    protected Vector3 _hitNormal;
    protected bool _isGrounded;


    public bool CanControl = true;

    public MoveSender moveSender;

    void Awake()
    {
        PlayerInputs = GetComponent<Inputs>();
        moveSender = GetComponent<MoveSender>();

        _characterTransform = transform;

        // sphere.GetComponent<Renderer>().enabled = false;
        
    }

    private void Start()
    {
        _cameraTransform = Camera.main.transform;
        controller.enabled = true;
    }

    public void Move(Vector3 motion)
    {
        controller.Move(motion);
    }

    private void Update()
    {
        if (PlayerInputs != null)
        {
            _horizontal = PlayerInputs.GetHorizontal();
            _vertical = PlayerInputs.GetVertical();
        }
    }

    private void FixedUpdate()
    {
        // 获取相机位置的输入方向。
        _forward = _cameraTransform.TransformDirection(Vector3.forward);
        _forward.y = 0f;
        _forward = _forward.normalized;
        _right = new Vector3(_forward.z, 0.0f, -_forward.x);

        // 计算水平移动与前进方向
        _move = (_horizontal * _right + _vertical * _forward);
        _direction = (_horizontal * _right + _vertical * _forward);

        moveSender.ChangeToSend(_direction);
        
        // 如果控制器不可控，则不执行后续操作。
        if(!CanControl) return;

        // 如果正确着地，则执行滑动。
        if (!_isCorrectGrounded && _isGrounded)
        {
            _move.x += (1f - _hitNormal.y) * _hitNormal.x * (1f - SlideFriction);
            _move.z += (1f - _hitNormal.y) * _hitNormal.z * (1f - SlideFriction);
        }

        _move.Normalize();
        // 如果未激活慢落（这样可以避免改变下落速度），并且控制器已启用，则移动玩家。
        // ==> _move 水平方向的移动
        if (controller.enabled)
            Move(Time.deltaTime * RunningSpeed * _move);

        // 检查是否正确着地。
        _isCorrectGrounded = (Vector3.Angle(Vector3.up, _hitNormal) <= SlopeLimit);

        // 设置前进方向
        if (_direction != Vector3.zero)
            transform.forward = _direction;
        
        // 重力力量
        if (_velocity.y >= -MaxDownYVelocity)
            _velocity.y += Gravity * Time.deltaTime;
        
        // 应用阻尼效果，减缓速度
        _velocity.x /= 1 + DragForce.x * Time.deltaTime;
        _velocity.y /= 1 + DragForce.y * Time.deltaTime;
        _velocity.z /= 1 + DragForce.z * Time.deltaTime;

        // ==> _velocity 垂直方向的移动
        if (controller.enabled)
            Move(_velocity * Time.deltaTime);
    }

    public bool NoInput()
    {
        return _horizontal == 0 && _vertical == 0;
    }

    public float MaxDownYVelocity = 15;
    public Vector3 DragForce;

    
}