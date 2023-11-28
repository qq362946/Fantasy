using System;
using System.Collections;
using UnityEngine;
namespace PlatformCharacterController
{
    public enum MoveState: ushort
        { IDLE = 0, RUNNING = 1, AIRBORNE = 2, SWIMMING = 3, JUMP = 4 , SWIMMINGIDLE = 5 , MOUNTED = 6, MOUNTED_AIRBORNE = 7, MOUNTED_SWIMMING = 8, DEAD = 9, }
        
    public class Movement : BehaviourNonAlloc
    {
        [HideInInspector]public MoveState state;
        [HideInInspector]public Animator animator;

        protected float MoveSpeed;
        protected Vector3 position;
        protected Quaternion rotation;
        
        protected float _gravity;
        protected bool _isGrounded;
        protected Vector3 _velocity;
        protected bool _jump;


        protected bool isGroundedWithinTolerance =>
            _isGrounded ;
        
        public virtual void SetSpeed(float speed){}

        public virtual bool IsMoving(){return  false;}

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
        }

        protected virtual MoveState UpdateMoveState()
        {
            return MoveState.IDLE;   
        }
        
        public virtual void SetPosition(Vector3 pos)
        {
        }
    }
}