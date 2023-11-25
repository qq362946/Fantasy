// 适用于使文本网格面向摄影机
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    Transform cam;

    void Awake()
    {
        // find main camera
        cam = Camera.main.transform;

        // 默认情况下禁用，直到可见
        enabled = false;
    }

    // 完成所有摄影机更新后刷新
    void LateUpdate()
    {
        transform.forward = cam.forward;
    }

    void OnBecameVisible() { enabled = true; }
    void OnBecameInvisible() { enabled = false; }
}
