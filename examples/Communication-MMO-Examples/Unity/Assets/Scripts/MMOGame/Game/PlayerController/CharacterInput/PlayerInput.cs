using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroCharacterController
{
    public class PlayerInput : Inputs
    {
        public override float GetHorizontal()
        {
            return Input.GetAxis("Horizontal");
        }

        public override float GetVertical()
        {
            return Input.GetAxis("Vertical");
        }

        public override bool Jump()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        public override bool Dash()
        {
            return Input.GetKeyDown(KeyCode.F);
        }

        public override bool JetPack()
        {
            return Input.GetKey(KeyCode.X);
        }

        public override bool Parachute()
        {
            return Input.GetKeyDown(KeyCode.T);
        }

        public override bool DropCarryItem()
        {
            return Input.GetKeyDown(KeyCode.K);
        }
    }
}