using System.Collections;
using System.Collections.Generic;
using TopDownShooter;
using UnityEngine;

namespace MicroCharacterController
{
    public class MobilePlayerInput : Inputs
    {
        public Joystick MobileJoystick;
        public CharacterMovement MovementCharacterController;
        //public HoldObjects HoldObjectsController;
        private bool _jetPack;
        private bool _parachute;

        private void Awake()
        {
            MovementCharacterController = GetComponent<CharacterMovement>();
            //HoldObjectsController = GetComponent<HoldObjects>();
        }

        public override float GetHorizontal()
        {
            return MobileJoystick.Horizontal;
        }

        public override float GetVertical()
        {
            return MobileJoystick.Vertical;
        }

        public override bool Jump()
        {
            return false;
        }

        public override bool Dash()
        {
            return false;
        }

        public override bool JetPack()
        {
            return _jetPack;
        }

        public override bool Parachute()
        {
            return _parachute;
        }

        public override bool DropCarryItem()
        {
            return false;
        }

        public void DropOrCarryOnButton()
        {
            //HoldObjectsController.DropOrCarryItem();
        }

        public void MakeJump()
        {
            MovementCharacterController.Jump(MovementCharacterController.JumpHeight);
        }

        public void MakeDash()
        {
            //MovementCharacterController.Dash();
        }

        public void ActiveJetPack(bool active)
        {
            _jetPack = active;
        }

        public void ActiveParachute(bool active)
        {
            _parachute = active;
        }

    }
}