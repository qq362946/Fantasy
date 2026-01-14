using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class PlayerInfo : MonoBehaviour
{
    [FormerlySerializedAs("FollowGameObject")] 
    public GameObject followGameObject;
    [FormerlySerializedAs("LookAtGameObject")] 
    public GameObject lookAtGameObject;
}
