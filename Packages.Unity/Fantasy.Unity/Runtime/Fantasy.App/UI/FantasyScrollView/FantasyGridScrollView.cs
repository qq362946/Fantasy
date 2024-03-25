using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Fantasy
{
    [DisallowMultipleComponent]
    public class FantasyGridScrollView : MonoBehaviour, IFantasyScrollView
    {
        public ScrollRect scrollRect;
        public FantasyRef itemTemplate;
        public GridLayoutGroup gridLayoutGroup;
        public ContentSizeFitter contentSizeFitter;

        private RectTransform _contentRect;
        private RectTransform _viewportRect;
        private RectTransform _scrollViewRect;

        private readonly Queue<int> _pools = new();
        private readonly List<GameObject> _items = new();
        private readonly Dictionary<int, int> _showingItems = new();
        private readonly HashSet<int> _toShow = new();
        private readonly List<int> _toRemove = new();

        private int _itemNum; // 当前item元素的总数
        private int _hCount; // 水平item的个数
        private int _vCount; // 竖直item的个数


        private void Awake()
        {
            if (contentSizeFitter)
                contentSizeFitter.enabled = false;
            gridLayoutGroup.enabled = false;
            itemTemplate.gameObject.SetActive(false);
            _scrollViewRect = scrollRect.transform as RectTransform;
            _viewportRect = scrollRect.viewport;
            _contentRect = scrollRect.content;
            _contentRect.anchorMax = Vector2.up;
            _contentRect.anchorMin = Vector2.up;
            scrollRect.onValueChanged.AddListener(_ => { RefreshViewport(); });

#if UNITY_EDITOR
            _previousContentWidth = _contentRect.rect.width;
            // 这里是为了让LayoutGroup在运行中发生更改时，能够实时反应到表现上
            if (!gridLayoutGroup)
                return;
            _layoutGroupInstanceId = gridLayoutGroup.GetInstanceID();
            _contentSizeFitterInstanceId = contentSizeFitter.GetInstanceID();
            FantasyEditorEventHelper.AddListener(_layoutGroupInstanceId, RefreshWhenPropertyChanged);
            FantasyEditorEventHelper.AddListener(_contentSizeFitterInstanceId, RefreshWhenPropertyChanged);
#endif
        }

#if UNITY_EDITOR
        private int _layoutGroupInstanceId;
        private int _contentSizeFitterInstanceId;

        private void OnDestroy()
        {
            FantasyEditorEventHelper.RemoveAllListener(_layoutGroupInstanceId);
            FantasyEditorEventHelper.RemoveAllListener(_contentSizeFitterInstanceId);
        }

        private void RefreshWhenPropertyChanged()
        {
            SetItemNum(_itemNum);
            RefreshViewport();
        }

        private float _previousContentWidth;
        private void Update()
        {
            if (Math.Abs(_previousContentWidth - _contentRect.rect.width) > 0.01f)
            {
                _previousContentWidth = _contentRect.rect.width;
                RefreshWhenPropertyChanged();
            }
        }
#endif

        private void RefreshViewport(bool refreshAll = false)
        {
            var contentRectAnchoredPosition = _contentRect.anchoredPosition;
            var upperLeftPos = new Vector2(-contentRectAnchoredPosition.x, contentRectAnchoredPosition.y);
            var viewportSize = GetViewportSize();
            var lowerRightPos = upperLeftPos + new Vector2(viewportSize.x, viewportSize.y);


            var begin = GetIdxAfterPosition(upperLeftPos);
            var end = GetIdxBeforePosition(lowerRightPos);

            if (begin.x == -1 || begin.y == -1 || end.x == -1 || end.y == -1)
            {
                // 没有item在范围内
                foreach (var (idx, itemIdx) in _showingItems)
                {
                    Return(itemIdx);
                    OnScrollItemHide?.Invoke(idx, itemIdx);
                }

                _showingItems.Clear();
                return;
            }

            _toRemove.Clear();
            foreach (var (idx, itemIdx) in _showingItems)
            {
                if (_toShow.Contains(idx))
                    continue;
                Return(itemIdx);
                OnScrollItemHide?.Invoke(idx, itemIdx);
                _toRemove.Add(idx);
            }

            foreach (var idx in _toRemove)
            {
                _showingItems.Remove(idx);
            }

            _toRemove.Clear();

            _toShow.Clear();
            for (int i = begin.x; i <= end.x; i++)
            {
                for (int j = begin.y; j <= end.y; j++)
                {
                    var idx = GetItemIdx(j, i);
                    if (idx < 0 || idx >= _itemNum)
                        continue;
                    _toShow.Add(idx);
                    if (_showingItems.TryGetValue(idx, out var itemIdx)) // 已经在显示中
                    {
                        PlaceItem(itemIdx, idx);
                        if (refreshAll)
                            OnScrollItemShow?.Invoke(idx, itemIdx);
                    }
                    else
                    {
                        itemIdx = Rent();
                        PlaceItem(itemIdx, idx);
                        OnScrollItemShow?.Invoke(idx, itemIdx);
                        _showingItems.Add(idx, itemIdx);
                    }
                }
            }

            _toShow.Clear();
        }

        private void PlaceItem(int itemIdx, int idx)
        {
            var (vIdx, hIdx, xPos, yPos) = GetItemPosition(idx);
            if (vIdx < 0 || hIdx < 0)
                return;
            var item = _items[itemIdx];
            item.name = $"v{vIdx}_h{hIdx}";
            var itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = Vector2.up;
            itemRect.anchorMax = Vector2.up;
            itemRect.pivot = Vector2.up;
            itemRect.sizeDelta = gridLayoutGroup.cellSize;
            itemRect.anchoredPosition = new Vector2(xPos, -yPos);
        }

        #region 移动

        public void MoveToBegin(float duration, Action onCompleted = null)
        {
            MoveToIdx(0, duration, onCompleted);
        }

        public void MoveToEnd(float duration, Action onCompleted = null)
        {
            MoveToIdx(_itemNum - 1, duration, onCompleted);
        }

        public void MoveToIdx(int idx, float duration, Action onCompleted = null)
        {
            if (_itemNum <= 0 || idx < 0 || idx + 1 > _itemNum)
            {
                Log.Warning($"不可移动到指定索引 {idx}");
                return;
            }

            var (_, _, xBegin, yBegin) = GetItemPosition(idx);
            var cellSize = gridLayoutGroup.cellSize;
            var (xEnd, yEnd) = (xBegin + cellSize.x, yBegin + cellSize.y);

            var contentRectAnchoredPosition = _contentRect.anchoredPosition;
            var viewportSize = GetViewportSize();
            var upperLeftPos = new Vector2(-contentRectAnchoredPosition.x, contentRectAnchoredPosition.y);
            var lowerRightPos = upperLeftPos + new Vector2(viewportSize.x, viewportSize.y);

            if (xBegin >= upperLeftPos.x && yBegin >= upperLeftPos.y && xEnd <= lowerRightPos.x && yEnd <= lowerRightPos.y) // 在区域内
                onCompleted?.Invoke();
            else if (xBegin < upperLeftPos.x && yBegin < upperLeftPos.y) // 左上
                MoveToContentPos(new Vector2(-xBegin, yBegin), duration, onCompleted);
            else if (xBegin < upperLeftPos.x && yEnd > lowerRightPos.y) // 左下
                MoveToContentPos(new Vector2(-xBegin, yEnd - viewportSize.y), duration, onCompleted);
            else if (xEnd > lowerRightPos.x && yBegin < upperLeftPos.y) // 右上
                MoveToContentPos(new Vector2(-(xEnd - viewportSize.x), yBegin), duration, onCompleted);
            else if (xEnd > lowerRightPos.x && yEnd > lowerRightPos.y) // 右下
                MoveToContentPos(new Vector2(-(xEnd - viewportSize.x), yEnd - viewportSize.y), duration, onCompleted);
            else if (xBegin < upperLeftPos.x) // 左
                MoveToContentPosX(-xBegin, duration, onCompleted);
            else if (xEnd > lowerRightPos.x) // 右
                MoveToContentPosX(-(xEnd - viewportSize.x), duration, onCompleted);
            else if (yBegin < upperLeftPos.y) // 上
                MoveToContentPosY(yBegin, duration, onCompleted);
            else if (yEnd > lowerRightPos.y) // 下
                MoveToContentPosY(yEnd - viewportSize.y, duration, onCompleted);
            else // 包围
                MoveToContentPos(new Vector2(-xBegin, yBegin), duration, onCompleted);
        }

        private void MoveToContentPos(Vector3 pos, float duration, Action onCompleted = null)
        {
            var elasticity = scrollRect.elasticity;
            scrollRect.elasticity = 0;
            var movementType = scrollRect.movementType;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _contentRect.DOLocalMove(pos, duration).OnComplete(() =>
            {
                scrollRect.StopMovement();
                scrollRect.movementType = movementType;
                scrollRect.elasticity = elasticity;
                onCompleted?.Invoke();
            });
        }

        private void MoveToContentPosX(float pos, float duration, Action onCompleted = null)
        {
            var elasticity = scrollRect.elasticity;
            scrollRect.elasticity = 0;
            var movementType = scrollRect.movementType;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _contentRect.DOLocalMoveX(pos, duration).OnComplete(() =>
            {
                scrollRect.StopMovement();
                scrollRect.movementType = movementType;
                scrollRect.elasticity = elasticity;
                onCompleted?.Invoke();
            });
        }

        private void MoveToContentPosY(float pos, float duration, Action onCompleted = null)
        {
            var elasticity = scrollRect.elasticity;
            scrollRect.elasticity = 0;
            var movementType = scrollRect.movementType;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _contentRect.DOLocalMoveY(pos, duration).OnComplete(() =>
            {
                scrollRect.StopMovement();
                scrollRect.movementType = movementType;
                scrollRect.elasticity = elasticity;
                onCompleted?.Invoke();
            });
        }

        #endregion

        private Vector2 GetViewportSize()
        {
            var viewportRect = _viewportRect.rect;
            var viewportWidth = viewportRect.width;
            var viewportHeight = viewportRect.height;
            if (viewportWidth == 0)
            {
                viewportWidth = _scrollViewRect.rect.width;
                var scrollBar = scrollRect.verticalScrollbar;
                if (!scrollBar)
                    viewportWidth -= ((RectTransform)scrollBar.transform).rect.width;
            }

            if (viewportHeight == 0)
            {
                viewportHeight = _scrollViewRect.rect.height;
                var scrollBar = scrollRect.horizontalScrollbar;
                if (!scrollBar)
                    viewportHeight -= ((RectTransform)scrollBar.transform).rect.height;
            }

            return new Vector2(viewportWidth, viewportHeight);
        }

        public void SetItemNum(int itemNum)
        {
            if (itemNum <= 0)
            {
                _itemNum = 0;
                _vCount = 0;
                _hCount = 0;
                if (contentSizeFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                {
                    _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
                }

                if (contentSizeFitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                {
                    _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
                }

                return;
            }

            _itemNum = itemNum;
            var horizontalFit = contentSizeFitter.horizontalFit;
            var verticalFit = contentSizeFitter.verticalFit;
            var padding = gridLayoutGroup.padding;
            var spacing = gridLayoutGroup.spacing;
            var cellSize = gridLayoutGroup.cellSize;
            var constraint = gridLayoutGroup.constraint;

            int v_count;
            int h_count;
            switch (constraint)
            {
                case GridLayoutGroup.Constraint.Flexible:
                {
                    switch (horizontalFit)
                    {
                        case (ContentSizeFitter.FitMode.Unconstrained):
                        {
                            var width = _contentRect.rect.width - padding.horizontal;
                            if (width < cellSize.x)
                                h_count = 1;
                            else
                                h_count = Mathf.FloorToInt((width - cellSize.x) / (cellSize.x + spacing.x)) + 1;

                            v_count = itemNum / h_count + (itemNum % h_count > 0 ? 1 : 0);
                            if (verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                                _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.vertical + cellSize.y * v_count + spacing.y * (v_count - 1));
                            break;
                        }
                        case (ContentSizeFitter.FitMode.MinSize):
                        {
                            h_count = 1;
                            v_count = itemNum;
                            _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, padding.horizontal + cellSize.x);
                            if (verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                                _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.vertical + cellSize.y * v_count + spacing.y * (v_count - 1));
                            break;
                        }
                        case (ContentSizeFitter.FitMode.PreferredSize):
                        {
                            h_count = Mathf.CeilToInt(Mathf.Sqrt(itemNum));
                            v_count = itemNum / h_count + (itemNum % h_count > 0 ? 1 : 0);
                            _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, padding.horizontal + cellSize.x * h_count + spacing.x * (h_count - 1));
                            if (verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                                _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.vertical + cellSize.y * v_count + spacing.y * (v_count - 1));
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
                case GridLayoutGroup.Constraint.FixedColumnCount:
                {
                    h_count = gridLayoutGroup.constraintCount;
                    v_count = itemNum / h_count + (itemNum % h_count > 0 ? 1 : 0);
                    if (horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, padding.horizontal + cellSize.x * h_count + spacing.x * (h_count - 1));
                    if (verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.vertical + cellSize.y * v_count + spacing.y * (v_count - 1));
                    break;
                }
                case GridLayoutGroup.Constraint.FixedRowCount:
                {
                    v_count = gridLayoutGroup.constraintCount;
                    h_count = itemNum / v_count + (itemNum % v_count > 0 ? 1 : 0);
                    if (horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, padding.horizontal + cellSize.x * h_count + spacing.x * (h_count - 1));
                    if (verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.vertical + cellSize.y * v_count + spacing.y * (v_count - 1));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _vCount = v_count;
            _hCount = h_count;
        }

        public void Refresh()
        {
            RefreshViewport(true);
        }

        public OnScrollItemShow OnScrollItemShow { get; set; }
        public OnScrollItemCreate OnScrollItemCreate { get; set; }
        public OnScrollItemHide OnScrollItemHide { get; set; }


        #region Pool

        private int Rent()
        {
            GameObject item;
            if (!_pools.TryDequeue(out var itemIdx))
            {
                item = Instantiate(itemTemplate.gameObject, _contentRect);
                _items.Add(item);
                itemIdx = _items.Count - 1;
                OnScrollItemCreate?.Invoke(item, itemIdx);
            }
            else
            {
                item = _items[itemIdx];
            }

            item.SetActive(true);
            return itemIdx;
        }

        private void Return(int itemIdx)
        {
            var item = _items[itemIdx];
            item.name = "poolItem";
            item.SetActive(false);
            _pools.Enqueue(itemIdx);
        }

        #endregion

        /// <summary>
        /// 获取某个Idx的位置信息
        /// </summary>
        /// <param name="idx">Item数据索引</param>
        private (int vIdx, int hIdx, float xPos, float yPos) GetItemPosition(int idx)
        {
            int vIdx;
            int hIdx;
            if (idx < 0 || idx >= _itemNum)
            {
                return (-1, -1, 0, 0);
            }

            var startCorner = gridLayoutGroup.startCorner;
            var startAxis = gridLayoutGroup.startAxis;
            var constrain = gridLayoutGroup.constraint;
            var constrainCount = gridLayoutGroup.constraintCount;

            var h_count = _hCount;
            var v_count = _vCount;


            switch (startCorner)
            {
                case GridLayoutGroup.Corner.UpperLeft when startAxis == GridLayoutGroup.Axis.Horizontal:
                    (vIdx, hIdx) = (idx / h_count, idx % h_count);
                    break;
                case GridLayoutGroup.Corner.UpperLeft when startAxis == GridLayoutGroup.Axis.Vertical:
                    (vIdx, hIdx) = (idx % v_count, idx / v_count);
                    break;
                case GridLayoutGroup.Corner.UpperRight when startAxis == GridLayoutGroup.Axis.Horizontal:
                    (vIdx, hIdx) = (idx / h_count, h_count - 1 - idx % h_count);
                    break;
                case GridLayoutGroup.Corner.UpperRight when startAxis == GridLayoutGroup.Axis.Vertical:
                    (vIdx, hIdx) = (idx % v_count, h_count - 1 - idx / v_count);
                    break;
                case GridLayoutGroup.Corner.LowerLeft when startAxis == GridLayoutGroup.Axis.Horizontal:
                    (vIdx, hIdx) = (v_count - 1 - idx / h_count, idx % h_count);
                    break;
                case GridLayoutGroup.Corner.LowerLeft when startAxis == GridLayoutGroup.Axis.Vertical:
                    (vIdx, hIdx) = (v_count - 1 - idx % v_count, idx / v_count);
                    break;
                case GridLayoutGroup.Corner.LowerRight when startAxis == GridLayoutGroup.Axis.Horizontal:
                    (vIdx, hIdx) = (v_count - 1 - idx / h_count, h_count - 1 - idx % h_count);
                    break;
                case GridLayoutGroup.Corner.LowerRight when startAxis == GridLayoutGroup.Axis.Vertical:
                    (vIdx, hIdx) = (v_count - 1 - idx % v_count, h_count - 1 - idx / v_count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (constrain == GridLayoutGroup.Constraint.FixedColumnCount && startAxis == GridLayoutGroup.Axis.Vertical && _itemNum > constrainCount)
            {
                var remain = _itemNum - constrainCount;
                var value = remain / (v_count - 1) + (remain % (v_count - 1) == 0 ? 0 : 1);
                var domain = value + remain;
                // 最后的这几个item需要补位
                if (idx >= domain)
                {
                    switch (startCorner)
                    {
                        case GridLayoutGroup.Corner.UpperLeft:
                            vIdx = 0;
                            hIdx = value + idx - domain;
                            break;
                        case GridLayoutGroup.Corner.UpperRight:
                            vIdx = 0;
                            hIdx = h_count - 1 - (value + idx - domain);
                            break;
                        case GridLayoutGroup.Corner.LowerLeft:
                            vIdx = _vCount - 1;
                            hIdx = value + idx - domain;
                            break;
                        case GridLayoutGroup.Corner.LowerRight:
                            vIdx = _vCount - 1;
                            hIdx = h_count - 1 - (value + idx - domain);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if (constrain == GridLayoutGroup.Constraint.FixedRowCount && startAxis == GridLayoutGroup.Axis.Horizontal && _itemNum > constrainCount)
            {
                var remain = _itemNum - constrainCount;
                var value = remain / (h_count - 1) + (remain % (h_count - 1) == 0 ? 0 : 1);
                var domain = value + remain;
                // 最后的这几个item需要补位
                if (idx >= domain)
                {
                    switch (startCorner)
                    {
                        case GridLayoutGroup.Corner.UpperLeft:
                            hIdx = 0;
                            vIdx = value + idx - domain;
                            break;
                        case GridLayoutGroup.Corner.LowerLeft:
                            hIdx = 0;
                            vIdx = v_count - 1 - (value + idx - domain);
                            break;
                        case GridLayoutGroup.Corner.UpperRight:
                            hIdx = h_count - 1;
                            vIdx = value + idx - domain;
                            break;
                        case GridLayoutGroup.Corner.LowerRight:
                            hIdx = h_count - 1;
                            vIdx = v_count - 1 - (value + idx - domain);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var pos = GetItemPosition(hIdx, vIdx);

            // 此处返回的坐标值的坐标系Y轴正方向朝下
            return (vIdx, hIdx, pos.x, pos.y);
        }

        private Vector2 GetItemPosition(int hIdx, int vIdx)
        {
            if (hIdx < 0 || vIdx < 0 || _hCount == 0 || _vCount == 0 || hIdx >= _hCount || vIdx >= _vCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            var padding = gridLayoutGroup.padding;
            var spacing = gridLayoutGroup.spacing;
            var cellSize = gridLayoutGroup.cellSize;
            var childAlignment = gridLayoutGroup.childAlignment;
            var constraint = gridLayoutGroup.constraint;

            var h_count = _hCount;
            var v_count = _vCount;
            float xPos;
            float yPos;
            switch (constraint)
            {
                case GridLayoutGroup.Constraint.Flexible:
                {
                    xPos = padding.left + hIdx * (cellSize.x + spacing.x);
                    yPos = padding.top + vIdx * (cellSize.y + spacing.y);
                    break;
                }
                case GridLayoutGroup.Constraint.FixedColumnCount or GridLayoutGroup.Constraint.FixedRowCount:
                {
                    var contentRect = _contentRect.rect;

                    switch (childAlignment)
                    {
                        case TextAnchor.UpperLeft:
                            (xPos, yPos) = (LeftPosX(), UpperPosY());
                            break;
                        case TextAnchor.UpperCenter:
                            (xPos, yPos) = (CenterPosX(), UpperPosY());
                            break;
                        case TextAnchor.UpperRight:
                            (xPos, yPos) = (RightPosX(), UpperPosY());
                            break;
                        case TextAnchor.MiddleLeft:
                            (xPos, yPos) = (LeftPosX(), MiddlePosY());
                            break;
                        case TextAnchor.MiddleCenter:
                            (xPos, yPos) = (CenterPosX(), MiddlePosY());
                            break;
                        case TextAnchor.MiddleRight:
                            (xPos, yPos) = (RightPosX(), MiddlePosY());
                            break;
                        case TextAnchor.LowerLeft:
                            (xPos, yPos) = (LeftPosX(), LowerPosY());
                            break;
                        case TextAnchor.LowerCenter:
                            (xPos, yPos) = (CenterPosX(), LowerPosY());
                            break;
                        case TextAnchor.LowerRight:
                            (xPos, yPos) = (RightPosX(), LowerPosY());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;

                    float LeftPosX() => padding.left + hIdx * (cellSize.x + spacing.x);
                    float CenterPosX() => contentRect.width / 2 - (cellSize.x * h_count + spacing.x * (h_count - 1)) / 2 + hIdx * (cellSize.x + spacing.x);
                    float RightPosX() => contentRect.width - padding.right - (cellSize.x * h_count + spacing.x * (h_count - 1)) + hIdx * (cellSize.x + spacing.x);
                    float UpperPosY() => padding.top + vIdx * (cellSize.y + spacing.y);
                    float MiddlePosY() => contentRect.height / 2 - (cellSize.y * v_count + spacing.y * (v_count - 1)) / 2 + vIdx * (cellSize.y + spacing.y);
                    float LowerPosY() => contentRect.height - padding.bottom - (cellSize.y * v_count + spacing.y * (v_count - 1)) + vIdx * (cellSize.y + spacing.y);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new Vector2(xPos, yPos);
        }

        private int GetItemIdx(int vIdx, int hIdx)
        {
            if (vIdx < 0 || hIdx < 0 || vIdx >= _vCount || hIdx >= _hCount)
                return -1;

            var startAxis = gridLayoutGroup.startAxis;
            var startCorner = gridLayoutGroup.startCorner;
            var idx = startCorner switch
            {
                GridLayoutGroup.Corner.UpperLeft when startAxis == GridLayoutGroup.Axis.Horizontal => _hCount * vIdx + hIdx,
                GridLayoutGroup.Corner.UpperLeft when startAxis == GridLayoutGroup.Axis.Vertical => _vCount * hIdx + vIdx,
                GridLayoutGroup.Corner.UpperRight when startAxis == GridLayoutGroup.Axis.Horizontal => _hCount * vIdx + (_hCount - 1 - hIdx),
                GridLayoutGroup.Corner.UpperRight when startAxis == GridLayoutGroup.Axis.Vertical => _vCount * (_hCount - 1 - hIdx) + vIdx,
                GridLayoutGroup.Corner.LowerLeft when startAxis == GridLayoutGroup.Axis.Horizontal => _hCount * (_vCount - 1 - vIdx) + hIdx,
                GridLayoutGroup.Corner.LowerLeft when startAxis == GridLayoutGroup.Axis.Vertical => _vCount * hIdx + (_vCount - 1 - vIdx),
                GridLayoutGroup.Corner.LowerRight when startAxis == GridLayoutGroup.Axis.Horizontal => _hCount * (_vCount - 1 - vIdx) + (_hCount - 1 - hIdx),
                GridLayoutGroup.Corner.LowerRight when startAxis == GridLayoutGroup.Axis.Vertical => _vCount * (_hCount - 1 - hIdx) + (_vCount - 1 - vIdx),
                _ => throw new ArgumentOutOfRangeException()
            };

            return idx;
        }

        private Vector2Int GetIdxAfterPosition(Vector2 position)
        {
            if (_itemNum == 0)
                return new Vector2Int(-1, -1);

            int x;
            int y;
            var xPos = position.x;
            var yPos = position.y;

            var cellSize = gridLayoutGroup.cellSize;
            var spacing = gridLayoutGroup.spacing;

            var upperLeftPos = GetItemPosition(0, 0);
            var lowerRightPos = GetItemPosition(_hCount - 1, _vCount - 1);
            lowerRightPos += cellSize;
            if (xPos <= upperLeftPos.x)
            {
                x = 0;
            }
            else if (xPos >= lowerRightPos.x)
            {
                x = -1;
            }
            else
            {
                var value = lowerRightPos.x - xPos;
                x = _hCount - 1 - Mathf.FloorToInt(value / (cellSize.x + spacing.x));
            }

            if (yPos <= upperLeftPos.y)
                y = 0;
            else if (yPos >= lowerRightPos.y)
                y = -1;
            else
            {
                var value = lowerRightPos.y - yPos;
                y = _vCount - 1 - Mathf.FloorToInt(value / (cellSize.y + spacing.y));
            }

            return new Vector2Int(x, y);
        }

        private Vector2Int GetIdxBeforePosition(Vector2 position)
        {
            if (_itemNum == 0)
                return new Vector2Int(-1, -1);

            int x;
            int y;
            var xPos = position.x;
            var yPos = position.y;

            var cellSize = gridLayoutGroup.cellSize;
            var spacing = gridLayoutGroup.spacing;

            var upperLeftPos = GetItemPosition(0, 0);
            var lowerRightPos = GetItemPosition(_hCount - 1, _vCount - 1);
            lowerRightPos += cellSize;
            if (xPos <= upperLeftPos.x)
                x = -1;
            else if (xPos >= lowerRightPos.x)
                x = _hCount - 1;
            else
            {
                var value = xPos - upperLeftPos.x;
                x = Mathf.FloorToInt(value / (cellSize.x + spacing.x));
            }

            if (yPos <= upperLeftPos.y)
                y = -1;
            else if (yPos >= lowerRightPos.y)
                y = _vCount - 1;
            else
            {
                var value = yPos - upperLeftPos.y;
                y = Mathf.FloorToInt(value / (cellSize.y + spacing.y));
            }

            return new Vector2Int(x, y);
        }
    }
}