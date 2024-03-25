using System.Collections.Generic;
using UnityEngine;

namespace Fantasy
{
    public class FantasyScrollViewComponent<TItem, TData> : Entity, IFantasyScrollViewComponent where TItem : SubUI, IScrollItem<TData>, new()
    {
        public IFantasyScrollView scroll;
        public readonly List<TItem> items = new();
        public List<TData> data;

        public void Initialize(IFantasyScrollView scrollView)
        {
            scroll = scrollView;
            scrollView.OnScrollItemShow = OnScrollItemShow;
            scrollView.OnScrollItemCreate = OnScrollItemCreate;
            scrollView.OnScrollItemHide = OnScrollItemHide;
        }

        public void SetData(List<TData> dataList)
        {
            data = dataList;
            scroll.SetItemNum(data.Count);
            scroll.Refresh();
        }

        public void Refresh() => scroll.Refresh();

        private void OnScrollItemHide(int idx, int itemIdx)
        {
            if (idx < data.Count)
                items[itemIdx].OnHide(idx);
        }

        private void OnScrollItemCreate(GameObject gameObject, int itemIdx)
        {
            var ui = Create<TItem>(Scene);
            ui.GameObject = gameObject;
            ui.Initialize();
            ui.OnCreate(scroll);
            EntitiesSystem.Instance.Awake(ui);
            EntitiesSystem.Instance.StartUpdate(ui);
            items.Add(ui);
        }

        private void OnScrollItemShow(int idx, int itemIdx)
        {
            if (idx < data.Count)
                items[itemIdx].OnShowOrRefresh(idx, data[idx]);
        }

        public FTask MoveToBegin(float duration)
        {
            var tcs = FTask.Create();
            scroll.MoveToBegin(duration, () => tcs.SetResult());
            return tcs;
        }

        public FTask MoveToEnd(float duration)
        {
            var tcs = FTask.Create();
            scroll.MoveToEnd(duration, () => tcs.SetResult());
            return tcs;
        }

        public FTask MoveToIdx(int idx, float duration)
        {
            var tcs = FTask.Create();
            scroll.MoveToIdx(idx, duration, () => tcs.SetResult());
            return tcs;
        }

        public override void Dispose()
        {
            items.Clear();
            base.Dispose();
        }
    }
}