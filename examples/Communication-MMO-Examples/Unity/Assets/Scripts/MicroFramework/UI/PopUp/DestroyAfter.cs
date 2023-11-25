// 在一段时间后销毁游戏对象。
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public float time = 1;

    void Start()
    {
        Destroy(gameObject, time);
    }
}
