using UnityEngine;
public class CameraMMO : MonoBehaviour
{
    public Transform target;

    public int mouseButton = 1; // right button by default

    public float distance = 5;
    public float minDistance = 3;
    public float maxDistance = 20;

    public float zoomSpeedMouse = 1;
    public float zoomSpeedTouch = 0.2f;
    public float rotationSpeed = 2;

    public float xMinAngle = -40;
    public float xMaxAngle = 80;

    // 与角色之间的偏移量
    public Vector3 offset = new Vector3(0,1.5f,0);

    // 设置哪些layer的物体不可以遮挡摄像机
    public LayerMask viewBlockingLayers;

    Vector3 rotation;
    bool rotationInitialized;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPos = target.position + offset;

        if (!Utils.IsCursorOverUserInterface())
        {
            // right mouse rotation if we have a mouse
            if (Input.mousePresent)
            {
                if (Input.GetMouseButton(mouseButton))
                {
                    if (!rotationInitialized)
                    {
                        rotation = transform.eulerAngles;
                        rotationInitialized = true;
                    }

                    rotation.y += Input.GetAxis("Mouse X") * rotationSpeed;
                    rotation.x -= Input.GetAxis("Mouse Y") * rotationSpeed;
                    rotation.x = Mathf.Clamp(rotation.x, xMinAngle, xMaxAngle);
                    transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(new Vector3(45, 0, 0));
            }

            // zoom
            float speed = Input.mousePresent ? zoomSpeedMouse : zoomSpeedTouch;
            float step = Utils.GetZoomUniversal() * speed;
            distance = Mathf.Clamp(distance - step, minDistance, maxDistance);
        }

        // target follow
        transform.position = targetPos - (transform.rotation * Vector3.forward * distance);

        // 判断遮挡重新定位cam
        if (Physics.Linecast(targetPos, transform.position, out RaycastHit hit, viewBlockingLayers))
        {
            float d = Vector3.Distance(targetPos, hit.point) - 0.1f;

            transform.position = targetPos - (transform.rotation * Vector3.forward * d);
        }
    }
}
