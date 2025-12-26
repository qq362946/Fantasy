using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable PossibleNullReferenceException

public class MoveComponent : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度")]
    public float moveSpeed = 5f;

    [Tooltip("到达目标点的最小距离")]
    public float stopDistance = 0.1f;

    [Tooltip("是否显示目标点标记")]
    public bool showTargetMarker = true;

    [Tooltip("运行时标记预制体（可选，不设置则自动创建简单标记）")]
    public GameObject targetMarkerPrefab;

    [Tooltip("标记缩放大小")]
    public float markerScale = 0.5f;

    [Header("动画设置")]
    [Tooltip("Animator组件（不设置则自动获取）")]
    public Animator animator;

    [Tooltip("动画速度平滑时间")]
    public float animationSmoothTime = 0.1f;

    [Tooltip("角色旋转速度（度/秒）- 越小越平滑，不晕")]
    public float rotationSpeed = 360f;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private float currentAnimationSpeed = 0f;
    private float animationVelocity = 0f;
    private GameObject targetMarkerInstance;

    void Start()
    {
        targetPosition = transform.position;

        // 自动获取Animator组件
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 创建或初始化目标标记
        if (showTargetMarker)
        {
            CreateTargetMarker();
        }
    }

    void OnDestroy()
    {
        // 清理标记对象
        if (targetMarkerInstance != null)
        {
            Destroy(targetMarkerInstance);
        }
    }

    void Update()
    {
        HandleMouseInput();
        MoveToTarget();
        UpdateAnimation();
    }

    // 处理鼠标输入
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                SetTargetPosition(hit.point);
            }
        }
    }

    // 设置目标位置
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        targetPosition.y = transform.position.y; // 保持在同一水平面
        isMoving = true;

        // 更新标记位置
        UpdateMarkerPosition();
    }

    // 移动到目标位置
    private void MoveToTarget()
    {
        if (!isMoving) return;

        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        // 检查是否到达目标点
        if (distance <= stopDistance)
        {
            isMoving = false;
            HideMarker();
            return;
        }

        // 移动角色
        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 旋转角色面向移动方向（使用平滑旋转）
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // 使用 RotateTowards 实现恒定速度的平滑旋转
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // 更新动画状态
    private void UpdateAnimation()
    {
        if (animator == null) return;

        // 计算目标动画速度
        float targetSpeed = isMoving ? moveSpeed : 0f;

        // 平滑过渡动画速度
        currentAnimationSpeed = Mathf.SmoothDamp(
            currentAnimationSpeed,
            targetSpeed,
            ref animationVelocity,
            animationSmoothTime
        );

        // 设置 Animator 参数
        // Speed: 0=Idle, 2=Walk, 6=Run
        animator.SetFloat("Speed", currentAnimationSpeed);
        animator.SetFloat("MotionSpeed", 1f);
    }

    // 创建目标标记
    private void CreateTargetMarker()
    {
        if (targetMarkerPrefab != null)
        {
            // 使用用户提供的预制体
            targetMarkerInstance = Instantiate(targetMarkerPrefab);
        }
        else
        {
            // 自动创建简单的圆环标记
            targetMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetMarkerInstance.name = "TargetMarker";

            // 移除碰撞体，避免干扰物理
            Destroy(targetMarkerInstance.GetComponent<Collider>());

            // 设置材质颜色
            Renderer renderer = targetMarkerInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0f, 1f, 0f, 0.5f); // 半透明绿色
            }

            // 设置为扁平的圆盘形状
            targetMarkerInstance.transform.localScale = new Vector3(markerScale, 0.05f, markerScale);
        }

        // 初始隐藏
        targetMarkerInstance.SetActive(false);
    }

    // 更新标记位置
    private void UpdateMarkerPosition()
    {
        if (targetMarkerInstance != null && showTargetMarker)
        {
            targetMarkerInstance.transform.position = targetPosition + Vector3.up * 0.1f; // 稍微抬高避免Z-fighting
            targetMarkerInstance.SetActive(true);
        }
    }

    // 隐藏标记
    private void HideMarker()
    {
        if (targetMarkerInstance != null)
        {
            targetMarkerInstance.SetActive(false);
        }
    }

    // 可选：在编辑器中也显示辅助线
    private void OnDrawGizmos()
    {
        if (showTargetMarker && isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
