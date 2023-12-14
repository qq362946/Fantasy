using System;
using System.Collections;
using UnityEngine;
namespace MicroCharacterController
{
    public partial class CharacterMovement
    {
        [Header("Swimming")] 
        [Tooltip("Player Status: Swimming")]
        public bool Swimming;
        public LayerMask canStandInWaterCheckLayers = Physics.DefaultRaycastLayers; // 可站立在水中的层
        public float underwaterThreshold = 0.7f; 
    }
}