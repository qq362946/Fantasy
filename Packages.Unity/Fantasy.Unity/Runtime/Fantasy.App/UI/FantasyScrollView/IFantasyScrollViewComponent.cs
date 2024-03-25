using System;

namespace Fantasy
{
    public interface IFantasyScrollViewComponent : IDisposable
    {
        void Refresh();
        FTask MoveToBegin(float duration);
        FTask MoveToEnd(float duration);
        FTask MoveToIdx(int idx, float duration);
    }
}