using UnityEngine;
using Fantasy;

public class MoveSender: BehaviourNonAlloc
{
    public CharacterController controller;
    // follow Role
    public Transform followRole;
    // Camera
    CameraMMO cameraMMO;

    public RedBall redBall;

    void Awake()
    {
        cameraMMO = Camera.main.GetComponent<CameraMMO>();
        controller = GetComponent<CharacterController>();
        redBall = GetComponent<RedBall>();

        if(followRole == null) {
            var go = new GameObject();
            followRole = go.transform;
        }
    }

    public void ChangeToSend(Vector3 direction){
        // 开始或停止移动时发送一次位置信息 
        ToggleMovement();

        DirectionDetect(direction);

        WaitingDetect(direction);

        CamDetect();

        justSend = false;
        lastPosition = transform.position;
    }

    void DirectionDetect(Vector3 _direction){
        // 通过键盘输入判断是否在一个方向上前进
        // 方向改变重置小球位置
        if(newPre)
        {
            lastDirection = _direction;
            newPre = false;
            
            // 发送位置信息
            if(lastDirection != Vector3.zero) SendPreMove(transform.position+transform.forward*5);
            // Debug.Log("--->发送位置!");
        }
        var lastNor = lastDirection.normalized;
        var curNor = _direction.normalized;
        var disNor =  FixedVector3.SqrMagnitude(lastNor.ToFixedVector3() - curNor.ToFixedVector3());
        if (disNor > 0.3f)
        {
            if(curNor != Vector3.zero){
                ReSetPosition(followRole.position);
                lastPosition = transform.position;
                newPre = true;
                //Debug.Log("--方向改变了!");
            }
        }
    }

    // 开始或停止移动的方法
    void ToggleMovement()
    {
        if(lastPosition == Vector3.zero) return;
        var P1 = lastPosition.ToFixedVector3();
        var P2 = transform.position.ToFixedVector3();
        var dislast = FixedVector3.Distance(P1,P2);

        if (dislast > 0.05f)
        {
            // 触发相应的事件
            if (!isMoving && !isWaiting && !justSend)
            {
                newPre = true;
                //Debug.Log("--->开始移动!");
            }
            isMoving = true;
        }
        else if (dislast < 0.05f)
        {
            if(!isMoving) return;

            if(isWaiting){
                SendPreMove(followRole.position);
                ReSetPosition(followRole.position);
                isMoving = false;
                //Debug.Log("--->中止移动!");
            }  
            else
            {
                SendPreMove(followRole.position);
                ReSetPosition(followRole.position);
                isMoving = false;
                //Debug.Log("--->停止移动!");
            }  
        } 
    }

    void WaitingDetect(Vector3 _direction){
        // 小球距离角色大于5，停止移动等待角色
        var P1 = transform.position.ToFixedVector3();
        var P2 = followRole.position.ToFixedVector3();
        var disRole = FixedVector3.Distance(P1,P2);
        if(disRole > 5 && !isWaiting){
            redBall.CanControl = false;
            SendPreMove(transform.position);
            isWaiting = true;
            // Debug.Log("--waiting!");
        }
        if(disRole < 0.25 && isWaiting){
            redBall.CanControl = true;
            if(isWaiting)
            {
                SendPreMove(transform.position+_direction*5);
                isWaiting = false;
            }
            //Debug.Log("--快到达预测点了!");
        }
    }

    void CamDetect(){
        if(lastRot == Quaternion.identity) return;
        // 还要考虑通过相机角度判断是否在一个方向上前进
        // 相机角度改变重置小球位置
        if(Quaternion.Angle(lastRot, cameraMMO.transform.rotation) > 1f){
            if(lastRot != Quaternion.identity){
                ReSetPosition(followRole.position);
                newPre = true;
                // Debug.Log("--相机角度改变了!");
            }
        }
        lastRot = cameraMMO.transform.rotation; 
    }

    public void ReSetPosition(Vector3 pos)
    {
        controller.enabled = false;
        transform.position = pos;
        controller.enabled = true;
    }

    Vector3 lastDirection; // 上一次的方向
    bool newPre = false; // 是否新的预测点
    bool isWaiting = false; // 是否等待角色
    Quaternion lastRot; // 上一次的相机角度

    // 标志变量，表示角色是否正在移动
    private bool isMoving = false;
    private Vector3 lastPosition;
    private bool justSend = false;
    
    public virtual void SendPreMove(Vector3 prePos)
    {
        justSend = true;
    }
    
    public virtual void SendStopMove()
    {
        
    }
}