//#define PROFILE

namespace SRF.UI.Layout
{
    using System;
    using Internal;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public interface IVirtualView
    {
        void SetDataContext(object data);
    }

    /// <summary>
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.VirtualVerticalLayoutGroup)]
    public class VirtualVerticalLayoutGroup : LayoutGroup, IPointerClickHandler
    {
        private readonly SRList<object> _itemList = new SRList<object>();
        private readonly SRList<int> _visibleItemList = new SRList<int>();

        private bool _isDirty = false;
        private SRList<Row> _rowCache = new SRList<Row>();
        private ScrollRect _scrollRect;
        private int _selectedIndex;
        private object _selectedItem;

        [SerializeField] private SelectedItemChangedEvent _selectedItemChanged;

        private int _visibleItemCount;
        private SRList<Row> _visibleRows = new SRList<Row>();
        public StyleSheet AltRowStyleSheet;
        public bool EnableSelection = true;
        public RectTransform ItemPrefab;

        /// <summary>
        /// Rows to show above and below the visible rect to reduce pop-in
        /// </summary>
        public int RowPadding = 2;

        public StyleSheet RowStyleSheet;
        public StyleSheet SelectedRowStyleSheet;

        /// <summary>
        /// Spacing to add between rows
        /// </summary>
        public float Spacing;

        /// <summary>
        /// If true, the scroll view will stick to the last element when fully scrolled to the bottom and an item is added
        /// </summary>
        public bool StickToBottom = true;

        public SelectedItemChangedEvent SelectedItemChanged
        {
            get { return _selectedItemChanged; }
            set { _selectedItemChanged = value; }
        }

        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value || !EnableSelection)
                {
                    return;
                }

                var newSelectedIndex = value == null ? -1 : _itemList.IndexOf(value);

                // Ensure that the new selected item is present in the item list
                if (value != null && newSelectedIndex < 0)
                {
                    throw new InvalidOperationException("Cannot select item not present in layout");
                }

                // Invalidate old selected item row
                if (_selectedItem != null)
                {
                    InvalidateItem(_selectedIndex);
                }

                _selectedItem = value;
                _selectedIndex = newSelectedIndex;

                // Invalidate the newly selected item
                if (_selectedItem != null)
                {
                    InvalidateItem(_selectedIndex);
                }

                SetDirty();

                if (_selectedItemChanged != null)
                {
                    _selectedItemChanged.Invoke(_selectedItem);
                }
            }
        }

        public override float minHeight
        {
            get { return _itemList.Count*ItemHeight + padding.top + padding.bottom + Spacing*_itemList.Count; }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!EnableSelection)
            {
                return;
            }

            var hitObject = eventData.pointerPressRaycast.gameObject;

            if (hitObject == null)
            {
                return;
            }

            var hitPos = hitObject.transform.position;
            var localPos = rectTransform.InverseTransformPoint(hitPos);
            var row = Mathf.FloorToInt(Mathf.Abs(localPos.y)/ItemHeight);

            if (row >= 0 && row < _itemList.Count)
            {
                SelectedItem = _itemList[row];
            }
            else
            {
                SelectedItem = null;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            ScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            var view = ItemPrefab.GetComponent(typeof (IVirtualView));

            if (view == null)
            {
                Debug.LogWarning(
                    "[VirtualVerticalLayoutGroup] ItemPrefab does not have a component inheriting from IVirtualView, so no data binding can occur");
            }
        }

        private void OnScrollRectValueChanged(Vector2 d)
        {
            if (d.y < 0 || d.y > 1)
            {
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(d.y);
            }

            //CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void Start()
        {
            base.Start();
            ScrollUpdate();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected void Update()
        {
            if (!AlignBottom && !AlignTop)
            {
                Debug.LogWarning("[VirtualVerticalLayoutGroup] Only Lower or Upper alignment is supported.", this);
                childAlignment = TextAnchor.UpperLeft;
            }

            if (SelectedItem != null && !_itemList.Contains(SelectedItem))
            {
                SelectedItem = null;
            }

            if (_isDirty)
            {
                _isDirty = false;
                ScrollUpdate();
            }
        }

        /// <summary>
        /// Invalidate a single row (before removing, or changing selection status)
        /// </summary>
        /// <param name="itemIndex"></param>
        protected void InvalidateItem(int itemIndex)
        {
            if (!_visibleItemList.Contains(itemIndex))
            {
                return;
            }

            _visibleItemList.Remove(itemIndex);

            for (var i = 0; i < _visibleRows.Count; i++)
            {
                if (_visibleRows[i].Index == itemIndex)
                {
                    RecycleRow(_visibleRows[i]);
                    _visibleRows.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// After removing or inserting a row, ensure that the cached indexes (used for layout) match up
        /// with the item index in the list
        /// </summary>
        protected void RefreshIndexCache()
        {
            for (var i = 0; i < _visibleRows.Count; i++)
            {
                _visibleRows[i].Index = _itemList.IndexOf(_visibleRows[i].Data);
            }
        }

        protected void ScrollUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            //Debug.Log("[SRConsole] ScrollUpdate {0}".Fmt(Time.frameCount));

            var pos = rectTransform.anchoredPosition;
            var startY = pos.y;

            var viewHeight = ((RectTransform) ScrollRect.transform).rect.height;

            // Determine the range of rows that should be visible
            var rowRangeLower = Mathf.FloorToInt(startY/(ItemHeight + Spacing));
            var rowRangeHigher = Mathf.CeilToInt((startY + viewHeight)/(ItemHeight + Spacing));

            // Apply padding to reduce pop-in
            rowRangeLower -= RowPadding;
            rowRangeHigher += RowPadding;

            rowRangeLower = Mathf.Max(0, rowRangeLower);
            rowRangeHigher = Mathf.Min(_itemList.Count, rowRangeHigher);

            var isDirty = false;

#if PROFILE
			Profiler.BeginSample("Visible Rows Cull");
#endif

            for (var i = 0; i < _visibleRows.Count; i++)
            {
                var row = _visibleRows[i];

                // Move on if row is still visible
                if (row.Index >= rowRangeLower && row.Index <= rowRangeHigher)
                {
                    continue;
                }

                _visibleItemList.Remove(row.Index);
                _visibleRows.Remove(row);
                RecycleRow(row);
                isDirty = true;
            }

#if PROFILE
			Profiler.EndSample();
			Profiler.BeginSample("Item Visible Check");
#endif

            for (var i = rowRangeLower; i < rowRangeHigher; ++i)
            {
                if (i >= _itemList.Count)
                {
                    break;
                }

                // Move on if row is already visible
                if (_visibleItemList.Contains(i))
                {
                    continue;
                }

                var row = GetRow(i);
                _visibleRows.Add(row);
                _visibleItemList.Add(i);
                isDirty = true;
            }

#if PROFILE
			Profiler.EndSample();
#endif

            // If something visible has explicitly been changed, or the visible row count has changed
            if (isDirty || _visibleItemCount != _visibleRows.Count)
            {
                //Debug.Log("[SRConsole] IsDirty {0}".Fmt(Time.frameCount));
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }

            _visibleItemCount = _visibleRows.Count;
        }

        public override void CalculateLayoutInputVertical()
        {
            SetLayoutInputForAxis(minHeight, minHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            var width = rectTransform.rect.width - padding.left - padding.right;

            // Position visible rows at 0 x
            for (var i = 0; i < _visibleRows.Count; i++)
            {
                var item = _visibleRows[i];

                SetChildAlongAxis(item.Rect, 0, padding.left, width);
            }

            // Hide non-active rows to one side. More efficient than enabling/disabling them
            for (var i = 0; i < _rowCache.Count; i++)
            {
                var item = _rowCache[i];

                SetChildAlongAxis(item.Rect, 0, -width - padding.left, width);
            }
        }

        public override void SetLayoutVertical()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            //Debug.Log("[SRConsole] SetLayoutVertical {0}".Fmt(Time.frameCount));

            // Position visible rows by the index of the item they represent
            for (var i = 0; i < _visibleRows.Count; i++)
            {
                var item = _visibleRows[i];

                SetChildAlongAxis(item.Rect, 1, item.Index*ItemHeight + padding.top + Spacing*item.Index, ItemHeight);
            }
        }

        private new void SetDirty()
        {
            base.SetDirty();

            if (!IsActive())
            {
                return;
            }

            _isDirty = true;
            //CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        [Serializable]
        public class SelectedItemChangedEvent : UnityEvent<object> {}

        [Serializable]
        private class Row
        {
            public object Data;
            public int Index;
            public RectTransform Rect;
            public StyleRoot Root;
            public IVirtualView View;
        }

        #region Public Data Methods

        public void AddItem(object item)
        {
            _itemList.Add(item);
            SetDirty();

            if (StickToBottom && Mathf.Approximately(ScrollRect.verticalNormalizedPosition, 0f))
            {
                ScrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        public void RemoveItem(object item)
        {
            if (SelectedItem == item)
            {
                SelectedItem = null;
            }

            var index = _itemList.IndexOf(item);

            InvalidateItem(index);
            _itemList.Remove(item);

            RefreshIndexCache();

            SetDirty();
        }

        public void ClearItems()
        {
            for (var i = _visibleRows.Count - 1; i >= 0; i--)
            {
                InvalidateItem(_visibleRows[i].Index);
            }

            _itemList.Clear();
            SetDirty();
        }

        #endregion

        #region Internal Properties

        private ScrollRect ScrollRect
        {
            get
            {
                if (_scrollRect == null)
                {
                    _scrollRect = GetComponentInParent<ScrollRect>();
                }

                return _scrollRect;
            }
        }

        private bool AlignBottom
        {
            get
            {
                return childAlignment == TextAnchor.LowerRight || childAlignment == TextAnchor.LowerCenter ||
                       childAlignment == TextAnchor.LowerLeft;
            }
        }

        private bool AlignTop
        {
            get
            {
                return childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter ||
                       childAlignment == TextAnchor.UpperRight;
            }
        }

        private float _itemHeight = -1;

        private float ItemHeight
        {
            get
            {
                if (_itemHeight <= 0)
                {
                    var layoutElement = ItemPrefab.GetComponent(typeof (ILayoutElement)) as ILayoutElement;

                    if (layoutElement != null)
                    {
                        _itemHeight = layoutElement.preferredHeight;
                    }
                    else
                    {
                        _itemHeight = ItemPrefab.rect.height;
                    }

                    if (_itemHeight.ApproxZero())
                    {
                        Debug.LogWarning(
                            "[VirtualVerticalLayoutGroup] ItemPrefab must have a preferred size greater than 0");
                        _itemHeight = 10;
                    }
                }

                return _itemHeight;
            }
        }

        #endregion

        #region Row Pooling and Provisioning

        private Row GetRow(int forIndex)
        {
            // If there are no rows available in the cache, create one from scratch
            if (_rowCache.Count == 0)
            {
                var newRow = CreateRow();
                PopulateRow(forIndex, newRow);
                return newRow;
            }

            var data = _itemList[forIndex];

            Row row = null;
            Row altRow = null;

            // Determine if the row we're looking for is an alt row
            var target = forIndex%2;

            // Try and find a row which previously had this data, so we can reuse it
            for (var i = 0; i < _rowCache.Count; i++)
            {
                row = _rowCache[i];

                // If this row previously represented this data, just use that one.
                if (row.Data == data)
                {
                    _rowCache.RemoveAt(i);
                    PopulateRow(forIndex, row);
                    break;
                }

                // Cache a row which is was the same alt state as the row we're looking for, in case
                // we don't find an exact match.
                if (row.Index%2 == target)
                {
                    altRow = row;
                }

                // Didn't match, reset to null
                row = null;
            }

            // If an exact match wasn't found, but a row with the same alt-status was found, use that one.
            if (row == null && altRow != null)
            {
                _rowCache.Remove(altRow);
                row = altRow;
                PopulateRow(forIndex, row);
            }
            else if (row == null)
            {
                // No match found, use the last added item in the cache
                row = _rowCache.PopLast();
                PopulateRow(forIndex, row);
            }

            return row;
        }

        private void RecycleRow(Row row)
        {
            _rowCache.Add(row);
        }

        private void PopulateRow(int index, Row row)
        {
            row.Index = index;

            // Set data context on row
            row.Data = _itemList[index];
            row.View.SetDataContext(_itemList[index]);

            // If we're using stylesheets
            if (RowStyleSheet != null || AltRowStyleSheet != null || SelectedRowStyleSheet != null)
            {
                // If there is a selected row stylesheet, and this is the selected row, use that one
                if (SelectedRowStyleSheet != null && SelectedItem == row.Data)
                {
                    row.Root.StyleSheet = SelectedRowStyleSheet;
                }
                else
                {
                    // Otherwise just use the stylesheet suitable for the row alt-status
                    row.Root.StyleSheet = index%2 == 0 ? RowStyleSheet : AltRowStyleSheet;
                }
            }
        }

        private Row CreateRow()
        {
            var item = new Row();

            var row = SRInstantiate.Instantiate(ItemPrefab);
            item.Rect = row;
            item.View = row.GetComponent(typeof (IVirtualView)) as IVirtualView;

            if (RowStyleSheet != null || AltRowStyleSheet != null || SelectedRowStyleSheet != null)
            {
                item.Root = row.gameObject.GetComponentOrAdd<StyleRoot>();
                item.Root.StyleSheet = RowStyleSheet;
            }

            row.SetParent(rectTransform, false);

            return item;
        }

        #endregion
    }
}
