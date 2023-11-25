// 在Start（）中设置刚体的速度。
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DefaultVelocity : MonoBehaviour
{
    public Rigidbody rigidBody;
    public Vector3 velocity;

    void Start()
    {
        rigidBody.velocity = velocity;
    }
}
