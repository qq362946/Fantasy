using System;
using System.Collections.Generic;
using UnityEngine;
using PlatformCharacterController;
[RequireComponent(typeof(AudioSource))]
public class NetRoleMovement : Movement
{
    private CharacterController _controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckGroundStatus();

        // 角色动画更新
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        SetGroundedState();
    }

    public override void SetPosition(Vector3 pos)
    {
        _controller.enabled = false;
        transform.position = pos;
        _controller.enabled = true;
    }

    protected override void UpdateAnimations()
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

    private void SetGroundedState()
    {
        //avoid set the grounded var in animator multiple time
        if (animator.GetBool("Grounded") != _isGrounded)
        {
            animator.SetBool("Grounded", _isGrounded);
        }
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

    // public void RpcSyncUnitState(UnitState UnitState)
    // {
    //     state = UnitState.MoveState;
    //     position = UnitState.Position;
    //     rotation = UnitState.Rotation;
    //     MoveSpeed = UnitState.MoveSpeed;
    // }
}