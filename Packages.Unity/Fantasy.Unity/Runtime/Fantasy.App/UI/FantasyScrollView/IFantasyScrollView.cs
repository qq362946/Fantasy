using System;
using UnityEngine;

namespace Fantasy
{
    public delegate void OnScrollItemShow(int idx, int itemIdx);

    public delegate void OnScrollItemHide(int idx, int itemIdx);

    public delegate void OnScrollItemCreate(GameObject gameObject, int itemIdx);

    public interface IFantasyScrollView
    {
        void MoveToBegin(float duration, Action onCompleted = null);
        void MoveToEnd(float duration, Action onCompleted = null);
        void MoveToIdx(int idx, float duration, Action onCompleted = null);

        void SetItemNum(int itemNum);
        void Refresh();

        OnScrollItemShow OnScrollItemShow { get; set; }
        OnScrollItemCreate OnScrollItemCreate { get; set; }
        OnScrollItemHide OnScrollItemHide { get; set; }
    }
}