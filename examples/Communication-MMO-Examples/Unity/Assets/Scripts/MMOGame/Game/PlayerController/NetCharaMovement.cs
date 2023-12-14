using UnityEngine;
using MicroCharacterController;
using Fantasy;

[RequireComponent(typeof(AudioSource))]
public class NetCharaMovement : Movement
{
    public float RunningSpeed = 5f;
    public float rotationSpeed = 30f;
    private float minAngleThreshold = 1.0f; // 角度阈值，小于这个值直接改变角度

    public CharacterController controller => base._controller as CharacterController;


    public long MoveStartTime;
    public long MoveEndTime;
    public Vector3 TargetPosition;
    public Quaternion TargetRotation;
    public MoveState MoveState = MoveState.IDLE;

    public override void MoveTarget(MoveInfo moveInfo)
    {
        MoveStartTime = moveInfo.MoveStartTime;
        MoveEndTime = moveInfo.MoveEndTime;
        TargetPosition = moveInfo.Position.ToVector3();
        TargetRotation = moveInfo.Rotation.ToQuaternion();
        MoveState = moveInfo.MoveState.ToMoveState();
        // Log.Info($"==> MoveState: {MoveState}");
    }

    void Update()
    {
        CheckGroundStatus();

        MoveUpdate();

        RotateUpdate();

        // 移动状态
        state = MoveState;

        // 角色动画更新
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        SetGroundedState();
    }

    private void MoveUpdate()
    {
        if(TargetPosition == Vector3.zero) return;

        if ((transform.position - TargetPosition).magnitude < RunningSpeed * Time.deltaTime)
        {
            transform.position = TargetPosition;
            return;
        }
        
        // Lerp位置插值
        var selfPosition = transform.position;
        var targetPosition = TargetPosition;

        // 使用 Vector3.Lerp 进行插值计算
        var newPosition = Vector3.Slerp(selfPosition, targetPosition, RunningSpeed * Time.deltaTime);

        // 应用新的位置
        transform.position = newPosition;
    }

    private void RotateUpdate()
    {
        var selfRotation = transform.rotation;
        var targetRotation = TargetRotation;

        // 计算角度差
        float angleDifference = Quaternion.Angle(selfRotation, targetRotation);

        if (angleDifference > minAngleThreshold)
        {
            // 使用 Quaternion.Slerp 进行插值计算，并确保输入是归一化的
            var newRotation = Quaternion.Slerp(selfRotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 应用新的旋转
            transform.rotation = newRotation;
        }
        else
        {
            // 直接设置为目标旋转，因为角度差太小
            transform.rotation = targetRotation;
        }
    }

    private void CheckGroundStatus()
    {
#if UNITY_EDITOR
        // 在场景视图中可视化地面检测射线的辅助工具
        Debug.DrawLine(
            transform.position + (Vector3.up * 0.1f),
            transform.position + Vector3.down * (controller.height / 2 + 0.2f),
            Color.red
        );
#endif
        // 值得注意的是，在示例角色中，变换的位置位于中心
        var grounded = Physics.Raycast(transform.position, Vector3.down, controller.height / 2 + 0.2f);
        _isGrounded = grounded || controller.isGrounded;
    }
}