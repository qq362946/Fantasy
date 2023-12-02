using UnityEngine;

public partial class PreBall : BehaviourNonAlloc
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
    protected Vector3 _direction; // 前进方向
    protected Vector3 _velocity; // 垂直移动
    public float Gravity = -30f;
    public float RunningSpeed = 20f;

    public float SlopeLimit = 45;
    public float SlideFriction = 0.3f;
    private bool _isCorrectGrounded; // 过于陡峭的地面

    //Ground.
    protected Vector3 _hitNormal;
    protected bool _isGrounded;

    // follow Role
    public Transform followRole;

    // Camera
    CameraMMO cameraMMO;

    public bool CanControl = true;

    void Awake(){
        InitSender();
        cameraMMO = Camera.main.GetComponent<CameraMMO>();
        PlayerInputs = GetComponent<Inputs>();

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

        // 开始或停止移动时发送一次位置信息 
        ToggleMovement();

        // 通过键盘输入判断是否在一个方向上前进
        // 方向改变重置小球位置
        if(newPre)
        {
            lastDirection = _direction;
            newPre = false;
            
            // 发送位置信息，如果开始或停止此帧已发送过，则不再发送
            if(!frameSended){
                SendStartMove();
                Debug.Log("--->发送位置!");
            }

            frameSended = false;
        }
        var lastNor = lastDirection.normalized;
        var curNor = _direction.normalized;
        if (Vector3.Distance(lastNor, curNor) > 0.3f)
        {
            if(curNor != Vector3.zero){
                ResetPosition(followRole.position);
                newPre = true;
                // Debug.Log("--方向改变了!");
            }
        }

        // 小球距离角色大于5，停止移动等待角色
        if(Vector3.Distance(transform.position,followRole.position) > 5){
            CanControl = false;
            isWaiting = true;
        }
        if(Vector3.Distance(transform.position,followRole.position) < 0.5){
            CanControl = true;
            if(isWaiting)
            {
                newPre = true;
                isWaiting = false;
            }
            // Debug.Log("--快到达预测点了!");
        }

        // 还要考虑通过相机角度判断是否在一个方向上前进
        // 相机角度改变重置小球位置
        if(Quaternion.Angle(lastRot, cameraMMO.transform.rotation) > 1f){
            if(lastRot != Quaternion.identity){
                ResetPosition(followRole.position);
                newPre = true;
                // Debug.Log("--相机角度改变了!");
            }
        }
        lastRot = cameraMMO.transform.rotation;
        
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

    // 开始或停止移动的方法
    void ToggleMovement()
    {
        if (followRole == null) return;
        var pos = followRole.position;
        if(Vector3.Distance(lastPosition, pos) > 0.02f){
            // 触发相应的事件
            if (!isMoving){
                SendStartMove();
                frameSended = true;
                Debug.Log("--->开始移动!");
            }   
            isMoving = true;
        }
        if(Vector3.Distance(lastPosition, pos) < 0.02f){
            if (isMoving){
                SendStopMove();
                frameSended = true;
                Debug.Log("--->停止移动!");
            }
            isMoving = false;
        }
        lastPosition = followRole.position;
    }

    public void ResetPosition(Vector3 pos)
    {
        controller.enabled = false;
        transform.position = pos;
        controller.enabled = true;
    }

    public float MaxDownYVelocity = 15;
    public Vector3 DragForce;

    Vector3 lastDirection; // 上一次的方向
    bool newPre = true; // 是否新的预测点
    bool isWaiting = false; // 是否等待角色
    Quaternion lastRot; // 上一次的相机角度

    // 标志变量，表示角色是否正在移动
    private bool isMoving = false;
    private Vector3 lastPosition;
    // 标志变量，表示这帧已发送过
    private bool frameSended = false;
}