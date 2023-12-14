using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroCharacterController
{
    public class SwimmingController : MonoBehaviour
    {
        public CharacterMovement MovementCharacterController;
        public Collider waterCollider;

        private void SetSwimmingState(bool swimming)
        {
            MovementCharacterController.Swimming = swimming;
            MovementCharacterController.animator.SetTrigger("Swim");
            MovementCharacterController.animator.SetBool("Swimming", swimming);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Water")
                waterCollider = other;
            if (!other.CompareTag("Water")) return;
            SetSwimmingState(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "Water")
                waterCollider = null;
            if (!other.CompareTag("Water")) return;
            SetSwimmingState(false);
        }

    }
}