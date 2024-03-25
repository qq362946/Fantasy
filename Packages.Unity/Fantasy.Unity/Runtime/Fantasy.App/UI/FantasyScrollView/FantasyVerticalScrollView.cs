using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Fantasy
{
    /// <summary>
    /// 循环复用滑动列表（竖直滑动）
    /// </summary>
    [DisallowMultipleComponent]
    public class FantasyVerticalScrollView : MonoBehaviour, IFantasyScrollView
    {
        public ScrollRect scrollRect;
        public FantasyRef itemTemplate;
        public VerticalLayoutGroup verticalLayoutGroup;
        public ContentSizeFitter contentSizeFitter;

        private RectTransform _contentRect;
        private RectTransform _itemTemplateRect;
        private RectTransform _viewportRect;
        private RectTransform _scrollViewRect;

        private readonly float[] _slidingPosition = new float[2];
        private readonly int[] _slidingIdx = { -1, -1 };
        private readonly int[] _previousIdx = { -1, -1 };
        private readonly LinkedList<int> _showingItems = new();
        private readonly Queue<int> _pools = new();
        private readonly List<GameObject> _items = new();

        public OnScrollItemShow OnScrollItemShow { get; set; }
        public OnScrollItemCreate OnScrollItemCreate { get; set; }
        public OnScrollItemHide OnScrollItemHide { get; set; }

        private int _itemNum;

        private void Awake()
        {
            if (contentSizeFitter)
                contentSizeFitter.enabled = false;
            verticalLayoutGroup.enabled = false;

            itemTemplate.gameObject.SetActive(false);
            _scrollViewRect = scrollRect.transform as RectTransform;
            _contentRect = scrollRect.content;
            _viewportRect = scrollRect.viewport;
            _itemTemplateRect = itemTemplate.transform as RectTransform;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
            scrollRect.onValueChanged.AddListener(_ => RefreshViewport());
#if UNITY_EDITOR
            // 这里是为了让LayoutGroup在运行中发生更改时，能够实时反应到表现上
            if (!verticalLayoutGroup)
                return;
            _layoutGroupInstanceId = verticalLayoutGroup.GetInstanceID(); // 保证Destroy时能够正确找到对应的instanceId
            FantasyEditorEventHelper.AddListener(_layoutGroupInstanceId, () =>
            {
                SetItemNum(_itemNum);
                Refresh();
            });
#endif
        }

#if UNITY_EDITOR
        private int _layoutGroupInstanceId;
        private void OnDestroy()
        {
            FantasyEditorEventHelper.RemoveAllListener(_layoutGroupInstanceId);
        }
#endif
        private void RefreshViewport(bool refreshAll = false)
        {
            // 计算当前content中显示的区域范围
            float viewportHeight = _viewportRect.rect.height;
            if (viewportHeight == 0)
                viewportHeight = _scrollViewRect.rect.height;
            float contentHeight = _contentRect.rect.height;
            _slidingPosition[0] = Mathf.Max(0, _contentRect.anchoredPosition.y);
            _slidingPosition[1] = Mathf.Min(contentHeight, _contentRect.anchoredPosition.y + viewportHeight);

            // 计算需要显示的数据索引
            var padding = verticalLayoutGroup.padding;
            var spacing = verticalLayoutGroup.spacing;
            var childAlignment = verticalLayoutGroup.childAlignment;
            var templateHeight = _itemTemplateRect.rect.height;

            // 记录上一次展示的item
            _previousIdx[0] = _slidingIdx[0];
            _previousIdx[1] = _slidingIdx[1];
            if (_slidingPosition[1] <= _slidingPosition[0])
            {
                // 没有需要显示的数据
                ClearShow();
                _slidingIdx[0] = -1;
                _slidingIdx[1] = -1;
                return;
            }

            // 计算viewport上边界往下会会显示的最小item的索引
            if (_slidingPosition[0] <= padding.top)
            {
                _slidingIdx[0] = 0;
            }
            else
            {
                var idx = Mathf.FloorToInt((_slidingPosition[0] - padding.top) / (templateHeight + spacing));
                var bottomPosition = padding.top + (idx + 1) * (templateHeight + spacing) - spacing;
                if (bottomPosition > _slidingPosition[0])
                    _slidingIdx[0] = idx;
                else
                    _slidingIdx[0] = idx + 1;
            }

            if (_slidingIdx[0] + 1 > _itemNum)
            {
                // 没有需要显示的数据
                ClearShow();
                _slidingIdx[0] = -1;
                _slidingIdx[1] = -1;
                return;
            }

            // 计算viewport下边界往下会会显示的最大item的索引
            if (_slidingPosition[1] >= contentHeight - padding.bottom)
            {
                _slidingIdx[1] = _itemNum - 1;
            }
            else
            {
                // _slidingPosition[1] - padding.top 一定 大于 0
                var idx = Mathf.FloorToInt((_slidingPosition[1] - padding.top) / (templateHeight + spacing));
                _slidingIdx[1] = idx;
            }

            if (_slidingIdx[0] > _slidingIdx[1])
            {
                // 没有需要显示的数据
                ClearShow();
                _slidingIdx[0] = -1;
                _slidingIdx[1] = -1;
                return;
            }

            if (_showingItems.Count == 0) // 没有已展示的item
            {
                for (int i = _slidingIdx[0]; i <= _slidingIdx[1]; i++)
                {
                    var itemIdx = Rent();
                    PlaceItem(itemIdx, i, childAlignment, padding);
                    _showingItems.AddLast(itemIdx);
                    OnScrollItemShow?.Invoke(i, itemIdx);
                }
            }
            else
            {
                if (_slidingIdx[0] > _previousIdx[1] || _slidingIdx[1] < _previousIdx[0]) // 之前展示的元素和将展示的元素没有交集
                {
                    ClearShow();
                    for (int i = _slidingIdx[0]; i <= _slidingIdx[1]; i++)
                    {
                        var itemIdx = Rent();
                        PlaceItem(itemIdx, i, childAlignment, padding);
                        _showingItems.AddLast(itemIdx);
                        OnScrollItemShow?.Invoke(i, itemIdx);
                    }
                }
                else // 之前展示的元素和将展示的元素有交集
                {
                    // 交集的部分处理
                    {
                        var node = _showingItems.First;
                        for (int i = _previousIdx[0]; i <= _previousIdx[1]; i++)
                        {
                            var itemIdx = node.Value;
                            if (i >= _slidingIdx[0] && i <= _slidingIdx[1])
                            {
                                // 这里刷新元素位置和大小，是因为保证item元素大小的正常显示
                                PlaceItem(itemIdx, i, childAlignment, padding);
                                if (refreshAll)
                                    OnScrollItemShow?.Invoke(i, itemIdx);
                            }

                            node = node.Next;
                            if (node == null)
                                break;
                        }
                    }

                    // 需要移除的前半部分
                    if (_previousIdx[0] < _slidingIdx[0])
                        for (int i = _previousIdx[0]; i < _slidingIdx[0]; i++)
                        {
                            var itemIdx = _showingItems.First.Value;
                            Return(itemIdx);
                            _showingItems.RemoveFirst();
                            OnScrollItemHide?.Invoke(i, itemIdx);
                        }

                    // 需要移除的后半部分
                    if (_previousIdx[1] > _slidingIdx[1])
                        for (int i = _previousIdx[1]; i > _slidingIdx[1]; i--)
                        {
                            var itemIdx = _showingItems.Last.Value;
                            Return(itemIdx);
                            _showingItems.RemoveLast();
                            OnScrollItemHide?.Invoke(i, itemIdx);
                        }

                    // 需要添加的前半部分
                    if (_previousIdx[0] > _slidingIdx[0])
                        for (int i = _previousIdx[0] - 1; i >= _slidingIdx[0]; i--)
                        {
                            var itemIdx = Rent();
                            PlaceItem(itemIdx, i, childAlignment, padding);
                            _showingItems.AddFirst(itemIdx);
                            OnScrollItemShow?.Invoke(i, itemIdx);
                        }

                    // 需要添加的后半部分
                    if (_previousIdx[1] < _slidingIdx[1])
                        for (int i = _previousIdx[1] + 1; i <= _slidingIdx[1]; i++)
                        {
                            var itemIdx = Rent();
                            PlaceItem(itemIdx, i, childAlignment, padding);
                            _showingItems.AddLast(itemIdx);
                            OnScrollItemShow?.Invoke(i, itemIdx);
                        }
                }
            }
        }

        /// <summary>
        /// 放置Item
        /// </summary>
        private void PlaceItem(int itemIdx, int idx, TextAnchor childAlignment, RectOffset padding)
        {
            var item = _items[itemIdx];
            item.name = idx.ToString();
            var itemRect = (RectTransform)item.transform;
            var contentWidth = _contentRect.rect.width;
            var childControlWidth = verticalLayoutGroup.childControlWidth;
            if (contentWidth == 0)
            {
                if (scrollRect.verticalScrollbar)
                    contentWidth = _scrollViewRect.rect.width - ((RectTransform)scrollRect.verticalScrollbar.transform).rect.width;
                else
                    contentWidth = _scrollViewRect.rect.width;
            }

            switch (childAlignment)
            {
                case TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft:
                    // 左上角
                    itemRect.anchorMin = new Vector2(0, 1);
                    itemRect.anchorMax = new Vector2(0, 1);
                    itemRect.pivot = new Vector2(0, 1);
                    itemRect.anchoredPosition = new Vector2(padding.left, -GetItemLocation(idx));
                    break;
                case TextAnchor.UpperCenter or TextAnchor.LowerCenter or TextAnchor.MiddleCenter:
                    // 上中
                    itemRect.anchorMin = new Vector2(0.5f, 1);
                    itemRect.anchorMax = new Vector2(0.5f, 1);
                    itemRect.pivot = new Vector2(0.5f, 1);
                    itemRect.anchoredPosition = new Vector2(childControlWidth ? 0 : padding.left - padding.right, -GetItemLocation(idx));
                    break;
                case TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight:
                    // 右上角
                    itemRect.anchorMin = new Vector2(1, 1);
                    itemRect.anchorMax = new Vector2(1, 1);
                    itemRect.pivot = new Vector2(1, 1);
                    itemRect.anchoredPosition = new Vector2(-padding.right, -GetItemLocation(idx));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (childControlWidth)
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth - padding.horizontal);
            else
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _itemTemplateRect.rect.width);
        }

        private float GetItemLocation(int idx)
        {
            if (idx < 0)
                throw new Exception("idx can't less than 0");
            var padding = verticalLayoutGroup.padding;
            var spacing = verticalLayoutGroup.spacing;
            return padding.top + idx * (_itemTemplateRect.rect.height + spacing);
        }

        private void ClearShow()
        {
            if (_showingItems.Count == 0)
                return;
            for (int i = _previousIdx[0]; i <= _previousIdx[1]; i++)
            {
                var itemIdx = _showingItems.First.Value;
                Return(itemIdx);
                _showingItems.RemoveFirst();
                OnScrollItemHide?.Invoke(i, itemIdx);
            }
        }

        public void SetItemNum(int itemNum)
        {
            if (itemNum <= 0)
            {
                _itemNum = 0;
                _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
                return;
            }

            _itemNum = itemNum;
            var padding = verticalLayoutGroup.padding;
            var spacing = verticalLayoutGroup.spacing;
            // 计算并设置好content的大小
            _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding.top + padding.bottom + _itemTemplateRect.sizeDelta.y * itemNum + spacing * Mathf.Max(0, itemNum - 1));
        }


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


        #region 移动

        public void MoveToBegin(float duration, Action onCompleted = null) => MoveToContentPos(0, duration, onCompleted);

        public void MoveToEnd(float duration, Action onCompleted = null)
        {
            float viewportHeight = _viewportRect.rect.height;
            if (viewportHeight == 0)
                viewportHeight = _scrollViewRect.rect.height;
            MoveToContentPos(_contentRect.rect.height - viewportHeight, duration, onCompleted);
        }

        public void MoveToIdx(int idx, float duration, Action onCompleted = null)
        {
            if (_itemNum <= 0 || idx < 0 || idx + 1 > _itemNum)
            {
                Log.Warning($"不可移动到指定索引 {idx}");
                return;
            }

            if (idx > _slidingIdx[0] && idx < _slidingIdx[1])
            {
                scrollRect.StopMovement();
                onCompleted?.Invoke();
                return;
            }

            var targetLocation = GetItemLocation(idx);
            var targetBottomLocation = targetLocation + _itemTemplateRect.rect.height;
            if (idx == _slidingIdx[0])
            {
                if (targetLocation > _slidingPosition[0] && targetBottomLocation < _slidingPosition[1])
                {
                    scrollRect.StopMovement();
                    onCompleted?.Invoke();
                }
                else
                {
                    MoveToContentPos(targetLocation + 0.01f, duration, onCompleted);
                }

                return;
            }

            if (idx < _slidingIdx[0])
            {
                MoveToContentPos(targetLocation + 0.01f, duration, onCompleted);
                return;
            }

            float viewportHeight = _viewportRect.rect.height;
            if (viewportHeight == 0)
                viewportHeight = _scrollViewRect.rect.height;
            if (idx == _slidingIdx[1])
            {
                if (targetLocation > _slidingPosition[0] && targetBottomLocation < _slidingPosition[1])
                {
                    scrollRect.StopMovement();
                    onCompleted?.Invoke();
                }
                else
                {
                    MoveToContentPos(targetBottomLocation - viewportHeight - 0.01f, duration, onCompleted);
                }

                return;
            }

            if (idx > _slidingIdx[0])
            {
                MoveToContentPos(targetBottomLocation - viewportHeight - 0.01f, duration, onCompleted);
            }
        }

        public void MoveToContentPos(float pos, float duration, Action onCompleted = null)
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

        /// <summary>
        /// 刷新数据
        /// </summary>
        public void Refresh()
        {
            RefreshViewport(true);
        }
    }
}