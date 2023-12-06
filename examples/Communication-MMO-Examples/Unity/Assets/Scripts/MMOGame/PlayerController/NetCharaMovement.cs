using UnityEngine;
using MicroCharacterController;
using Fantasy;
using BestGame;

[RequireComponent(typeof(AudioSource))]
public class NetCharaMovement : Movement
{
    public float RunningSpeed = 5f;
    public float rotateSpeed = 30f;
    public float lerpSpeed = 0.5f;
    public float minThreshold = 0.01f;
    public float Gravity = -30f;

    public float underwaterThreshold = 0.7f; 
    public LayerMask canStandInWaterCheckLayers = Physics.DefaultRaycastLayers; // 可站立在水中的层

    public long MoveEndTime;
    public Vector3 TargetPos;
    public Quaternion TargetRotation;

    public override void MoveTarget(MoveInfo moveInfo)
    {
        MoveEndTime = moveInfo.MoveEndTime;
        TargetPos = moveInfo.Position.ToVector3();
        TargetRotation = moveInfo.Rotation.ToQuaternion();
        Log.Info($"==> MoveTarget: {TargetPos}");
    }

    private void MoveUpdate()
    {
        if(TargetPos == Vector3.zero) return;

        if ((transform.position - TargetPos).magnitude < RunningSpeed * Time.deltaTime)
        {
            transform.position = TargetPos;
            return;
        }
        
        Vector3 dir = (TargetPos - transform.position).normalized;
        transform.position += dir * RunningSpeed * Time.deltaTime;
        transform.forward = dir;
    }

    void Update()
    {
        CheckGroundStatus();

        MoveUpdate();

        // 角色动画更新
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        SetGroundedState();
    }
}