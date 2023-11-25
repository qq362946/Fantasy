using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class ScrollPageTool : UIBehaviour, IEndDragHandler, IBeginDragHandler
{

    public RectTransform scrollTransformRect; //content
    public RectTransform baseRect; //子控件
    public float spacing = 0;//间距
    public int durationFrame = 10;

    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    int targetIndex = 0;
    Vector2 baseRectSize;

    public enum eAxis
    {
        Horizontal,
        Vertical
    }
    public eAxis mAxis = eAxis.Vertical;

    public float mScale = 1;

    //TODO
    //public enum eDirection
    //{
    //    Normal = 1,
    //    Backwards = -1
    //}
    //public eDirection mDirection = eDirection.Normal;

    protected override void Awake()
    {
        baseRectSize = new Vector2(baseRect.rect.width, baseRect.rect.height);
        SetPositionFromIndex(0, false);
    }

    // 开始拖动
    public void OnBeginDrag(PointerEventData eventData)
    {
        StopAllCoroutines();
    }

    // 结束拖动
    public void OnEndDrag(PointerEventData eventData)
    {
        targetIndex = GetCurrentPositionIndex();
        StartCoroutine(SnapRect());
    }

    private IEnumerator SnapRect()
    {
        float timer = 0f;
        float speed = Mathf.Max(0.01f, (float)1 / durationFrame);

        int axis = (int)mAxis;
        float oldPosition = scrollTransformRect.anchoredPosition[axis];
        float targetPosition = GetPositionFromIndex(targetIndex)[axis];
        Vector2 targetVector = new Vector2();
        while (timer < 1f)
        {
            float pos = Mathf.Lerp(oldPosition, targetPosition, curve.Evaluate(timer));
            timer += speed;
            targetVector[axis] = pos;
            scrollTransformRect.anchoredPosition = targetVector;
            yield return new WaitForEndOfFrame();
        }
        targetVector[axis] = targetPosition;
        scrollTransformRect.anchoredPosition = targetVector;
    }

    // 根据index 设置 position
    void SetPositionFromIndex(int i, bool animate = true)
    {
        StopAllCoroutines();
        int max = scrollTransformRect.gameObject.transform.childCount - 1;
        targetIndex = Mathf.Clamp(i, 0, max);
        if (!animate)
        {
            var pos = GetPositionFromIndex(i);
            scrollTransformRect.anchoredPosition = pos;
        }
        else
        {
            StartCoroutine(SnapRect());
        }
    }

    //根据index 获得 position
    Vector2 GetPositionFromIndex(int i)
    {
        int axis = (int)mAxis;
        Vector2 v = new Vector2(0f, 0f);

        int plus = eAxis.Horizontal == mAxis ? -1 : 1;
        v[axis] = (baseRectSize[axis] + spacing) * i * plus;
        return v;
    }

    //获取当前的 index
    int GetCurrentPositionIndex()
    {
        float item = GetOffset();
        if (eAxis.Horizontal == mAxis && 0 < item) return 0;
        if (eAxis.Vertical == mAxis && 0 > item) return 0;

        //int value = Mathf.RoundToInt(item);

        float abs_item = Mathf.Abs(item);
        int value = 0;
        float deci = abs_item % 1;
        if (targetIndex < abs_item)
        {
            value = Mathf.FloorToInt(abs_item);
            if (deci > 0.2) value++;
        }
        else
        {
            value = Mathf.CeilToInt(abs_item);
            if (deci < 0.6) value--;
        }
        int max = scrollTransformRect.gameObject.transform.childCount - 1;
        value = Mathf.Clamp(value, 0, max);
        return value;
    }

    //获取offset
    float GetOffset()
    {
        int axis = (int)mAxis;
        var anchorPos = scrollTransformRect.anchoredPosition[axis];
        float offset = anchorPos / (baseRectSize[axis] + spacing);
        return offset;
    }

    //脚本重置
    ScrollRect scrollRect;
    protected override void Reset()
    {
        base.Reset();

        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
        scrollRect.inertia = false;

        if (scrollRect.movementType == ScrollRect.MovementType.Elastic)
        {
            Debug.LogError("ScrollRectController does not work with Elastic movement type, chage it to Clamped or Unrestricted");
        }
    }

}

