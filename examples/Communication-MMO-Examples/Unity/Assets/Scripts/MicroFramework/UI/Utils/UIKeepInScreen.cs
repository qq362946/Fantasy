// 此组件可挂到可移动窗口，使它们仅
// 在屏幕边界内显示或移动。
using UnityEngine;

public class UIKeepInScreen : BehaviourNonAlloc
{
    [Header("Components")]
    public RectTransform rectTransform;

    void Update()
    {
        Rect rect = rectTransform.rect;

        // to 世界坐标
        Vector2 minworld = transform.TransformPoint(rect.min);
        Vector2 maxworld = transform.TransformPoint(rect.max);
        Vector2 sizeworld = maxworld - minworld;

        // 保持最小位置在屏幕边界
        // 限制位置在 (0,0) - maxworld
        maxworld = new Vector2(Screen.width, Screen.height) - sizeworld;
        float x = Mathf.Clamp(minworld.x, 0, maxworld.x);
        float y = Mathf.Clamp(minworld.y, 0, maxworld.y);

        // 设置tip的坐标位置
        Vector2 offset = (Vector2)transform.position - minworld;
        transform.position = new Vector2(x, y) + offset;
    }
}